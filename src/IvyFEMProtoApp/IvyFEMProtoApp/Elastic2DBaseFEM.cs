using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    delegate void CalcElementAB(
        uint feId, IvyFEM.Linear.DoubleSparseMatrix A, double[] B);

    abstract class Elastic2DBaseFEM : FEM
    {
        protected uint[] QuantityIds { get; set; } = null;
        protected int[] NodeCounts { get; set; } = null;
        protected int[] Dofs { get; set; } = null;
        protected int NodeCount
        {
            get
            {
                if (Dofs == null)
                {
                    return 0;
                }
                if (NodeCounts == null)
                {
                    return 0;
                }
                int cnt = 0;
                for (int iVar = 0; iVar < Dofs.Length; iVar++)
                {
                    cnt += NodeCounts[iVar] * Dofs[iVar];
                }
                return cnt;
            }
        }
        public double ConvRatioToleranceForNewtonRaphson { get; set; }
            = 1.0e+2 * IvyFEM.Linear.Constants.ConvRatioTolerance; // 収束しないので収束条件を緩めている

        // Calc Matrix
        protected IList<CalcElementAB> CalcElementABs { get; set; } = new List<CalcElementAB>();

        //Solve
        // Output
        public double[] U { get; protected set; }

        protected void CalcAB(IvyFEM.Linear.DoubleSparseMatrix A, double[] B)
        {
            uint quantityId = 0; // Note: 複数変数のときでも要素Idは同じはずなので0指定
            IList<uint> feIds = World.GetTriangleFEIds(quantityId);
            foreach (uint feId in feIds)
            {
                foreach (var calcElementAB in CalcElementABs)
                {
                    calcElementAB(feId, A, B);
                }
            }
        }

        public override void Solve()
        {
            int nodeCnt = NodeCount;

            if (HasNonLinearElasticMaterial())
            {
                // Newton Raphson
                double sqNorm = 0;
                double sqInvNorm0 = 0;
                double convRatio = ConvRatioToleranceForNewtonRaphson;
                double tolerance = convRatio;
                const int maxIter = IvyFEM.Linear.Constants.MaxIter;
                int iter = 0;
                U = new double[nodeCnt];
                for (iter = 0; iter < maxIter; iter++)
                {
                    int t;
                    var A = new IvyFEM.Linear.DoubleSparseMatrix(nodeCnt, nodeCnt);
                    var B = new double[nodeCnt];

                    t = System.Environment.TickCount;
                    CalcAB(A, B);
                    System.Diagnostics.Debug.WriteLine("CalcAB: t = " + (System.Environment.TickCount - t));

                    t = System.Environment.TickCount;
                    SetFixedCadsCondtion(World, A, B, NodeCounts, Dofs);
                    System.Diagnostics.Debug.WriteLine("Condition: t = " + (System.Environment.TickCount - t));

                    t = System.Environment.TickCount;
                    double[] AU = A * U;
                    double[] R = IvyFEM.Lapack.Functions.daxpy(-1.0, B, AU);
                    sqNorm = IvyFEM.Lapack.Functions.ddot(R, R);
                    System.Diagnostics.Debug.WriteLine("Calc Norm: t = " + (System.Environment.TickCount - t));
                    if (iter == 0)
                    {
                        if (sqNorm < IvyFEM.Constants.PrecisionLowerLimit)
                        {
                            convRatio = 0;
                            break;
                        }
                        sqInvNorm0 = 1.0 / sqNorm;
                    }
                    else
                    {
                        if (sqNorm * sqInvNorm0 < tolerance * tolerance)
                        {
                            convRatio = Math.Sqrt(sqNorm * sqInvNorm0);
                            break;
                        }
                    }

                    t = System.Environment.TickCount;
                    //---------------------------------------------------
                    double[] X;
                    Solver.DoubleSolve(out X, A, B);
                    U = X;
                    //---------------------------------------------------
                    System.Diagnostics.Debug.WriteLine("Solve: t = " + (System.Environment.TickCount - t));
                }
                System.Diagnostics.Debug.WriteLine("Newton Raphson iter = " + iter + " norm = " + convRatio);
                System.Diagnostics.Debug.Assert(iter < maxIter);
            }
            else
            {
                int t;
                var A = new IvyFEM.Linear.DoubleSparseMatrix(nodeCnt, nodeCnt);
                var B = new double[nodeCnt];

                t = System.Environment.TickCount;
                CalcAB(A, B);
                System.Diagnostics.Debug.WriteLine("CalcAB: t = " + (System.Environment.TickCount - t));

                t = System.Environment.TickCount;
                SetFixedCadsCondtion(World, A, B, NodeCounts, Dofs);
                System.Diagnostics.Debug.WriteLine("Condtion: t = " + (System.Environment.TickCount - t));

                t = System.Environment.TickCount;
                //-------------------------------
                double[] X;
                Solver.DoubleSolve(out X, A, B);
                U = X;
                //-------------------------------
                System.Diagnostics.Debug.WriteLine("Solve: t = " + (System.Environment.TickCount - t));
            }

        }

        protected bool HasNonLinearElasticMaterial()
        {
            bool hasNonlinear = false;
            uint quantityId = 0; // Note: 複数変数のときでも要素Idは同じはずなので0指定
            IList<uint> feIds = World.GetTriangleFEIds(quantityId);
            foreach (uint feId in feIds)
            {
                TriangleFE triFE = World.GetTriangleFE(quantityId, feId);
                Material ma = World.GetMaterial(triFE.MaterialId);
                if (ma is LinearElasticMaterial)
                {
                    // linear
                }
                else
                {
                    hasNonlinear = true;
                    break;
                }
            }
            return hasNonlinear;
        }

        public static void SetStressValue(
            uint displacementValueId, uint stressValueId, uint equivStressValueId, FEWorld world)
        {
            System.Diagnostics.Debug.Assert(world.IsFieldValueId(displacementValueId));
            FieldValue uFV = world.GetFieldValue(displacementValueId);
            uint uQuantityId = uFV.QuantityId;

            FieldValue sigmaFV = null;
            if (stressValueId != 0)
            {
                System.Diagnostics.Debug.Assert(world.IsFieldValueId(stressValueId));
                sigmaFV = world.GetFieldValue(stressValueId);
                System.Diagnostics.Debug.Assert(sigmaFV.Type == FieldValueType.SymmetricTensor2);
                System.Diagnostics.Debug.Assert(sigmaFV.Dof == 3);
            }
            FieldValue eqSigmaFV = null;
            if (equivStressValueId != 0)
            {
                System.Diagnostics.Debug.Assert(world.IsFieldValueId(equivStressValueId));
                eqSigmaFV = world.GetFieldValue(equivStressValueId);
                System.Diagnostics.Debug.Assert(eqSigmaFV.Type == FieldValueType.Scalar);
                System.Diagnostics.Debug.Assert(eqSigmaFV.Dof == 1);
            }

            IList<uint> feIds = world.GetTriangleFEIds(uQuantityId);
            foreach (uint feId in feIds)
            {
                TriangleFE triFE = world.GetTriangleFE(uQuantityId, feId);
                int[] coIds = triFE.NodeCoordIds;
                Material ma = world.GetMaterial(triFE.MaterialId);
                double lambda = 0;
                double mu = 0;
                if (ma is LinearElasticMaterial)
                {
                    var ma1 = ma as LinearElasticMaterial;
                    lambda = ma1.LameLambda;
                    mu = ma1.LameMu;
                }
                else if (ma is SaintVenantHyperelasticMaterial)
                {
                    var ma1 = ma as SaintVenantHyperelasticMaterial;
                    lambda = ma1.LameLambda;
                    mu = ma1.LameMu;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                    throw new NotImplementedException();
                }

                var ip = triFE.GetIntegrationPoints(TriangleIntegrationPointCount.Point1);
                System.Diagnostics.Debug.Assert(ip.PointCount == 1);
                double[] L = ip.Ls[0];
                double[][] Nu = triFE.CalcNu(L);
                double[] Nx = Nu[0];
                double[] Ny = Nu[1];
                double[] uu = new double[4]; // 00, 10, 01, 11 (dux/dx duy/dx dux/dy duy/duy)
                for (int iNode = 0; iNode < coIds.Length; iNode++)
                {
                    int coId = coIds[iNode];
                    double[] u = uFV.GetValue(coId, FieldDerivationType.Value);
                    uu[0] += u[0] * Nx[iNode];
                    uu[1] += u[1] * Nx[iNode];
                    uu[2] += u[0] * Ny[iNode];
                    uu[3] += u[1] * Ny[iNode];
                }

                //ε strain
                double[] eps = new double[4];
                if (ma is LinearElasticMaterial)
                {
                    eps[0] = 0.5 * (uu[0] + uu[0]);
                    eps[1] = 0.5 * (uu[1] + uu[2]);
                    eps[2] = 0.5 * (uu[2] + uu[1]);
                    eps[3] = 0.5 * (uu[3] + uu[3]);
                }
                else if (ma is SaintVenantHyperelasticMaterial)
                {
                    eps[0] = 0.5 * (uu[0] + uu[0] + uu[0] * uu[0] + uu[1] * uu[1]);
                    eps[1] = 0.5 * (uu[1] + uu[2] + uu[2] * uu[0] + uu[1] * uu[3]);
                    eps[2] = 0.5 * (uu[2] + uu[1] + uu[0] * uu[2] + uu[3] * uu[1]);
                    eps[3] = 0.5 * (uu[3] + uu[3] + uu[2] * uu[2] + uu[3] * uu[3]);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                // σ stress
                double[] sigma = new double[4];
                sigma[0] = mu * eps[0] + lambda * (eps[0] + eps[3]);
                sigma[1] = mu * eps[1];
                sigma[2] = mu * eps[2];
                sigma[3] = mu * eps[3] + lambda * (eps[0] + eps[3]);

                double misesStress = Math.Sqrt(
                    0.5 * (sigma[0] - sigma[3]) * (sigma[0] - sigma[3]) +
                    0.5 * sigma[3] * sigma[3] +
                    0.5 * sigma[0] * sigma[0] +
                    3 * sigma[2] * sigma[2]);

                if (stressValueId != 0)
                {
                    double[] Sigma = sigmaFV.GetValues(FieldDerivationType.Value);
                    uint dof = sigmaFV.Dof;
                    Sigma[(feId - 1) * dof + 0] = sigma[0]; // σxx
                    Sigma[(feId - 1) * dof + 1] = sigma[3]; // σyy
                    Sigma[(feId - 1) * dof + 2] = sigma[2]; // τxy
                }
                if (equivStressValueId != 0)
                {
                    double[] EqSigma = eqSigmaFV.GetValues(FieldDerivationType.Value);
                    EqSigma[feId - 1] = misesStress;
                }
            }
        }

    }
}
