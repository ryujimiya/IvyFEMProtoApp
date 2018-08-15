using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    abstract class Elastic2DBaseFEM : FEM
    {
        //Solve
        // Output
        public double[] U { get; protected set; }

        protected abstract void CalcLinearElasticAB(IvyFEM.Linear.DoubleSparseMatrix A, double[] B,
            int nodeCnt, int dof);

        protected abstract void CalcSaintVenantKirchhoffHyperelasticAB(IvyFEM.Linear.DoubleSparseMatrix A, double[] B,
            int nodeCnt, int dof);

        public override void Solve()
        {
            int nodeCnt = (int)World.GetNodeCount();
            int dof = 2;

            if (HasNonLinearElasticMaterial())
            {
                U = new double[nodeCnt * dof];
                double[] prevU = null;
                SetInitialU(nodeCnt, dof);
                double norm = 0;
                const int maxCnt = 50;
                int itr = 0;
                for (; itr < maxCnt; itr++)
                {
                    var A = new IvyFEM.Linear.DoubleSparseMatrix(nodeCnt * dof, nodeCnt * dof);
                    var B = new double[nodeCnt * dof];

                    CalcLinearElasticAB(A, B, nodeCnt, dof);
                    CalcSaintVenantKirchhoffHyperelasticAB(A, B, nodeCnt, dof);

                    SetFixedCadsCondtion(World, A, B, nodeCnt, dof);

                    if (itr > 0)
                    {
                        double[] R = IvyFEM.Lapack.Functions.daxpy(-1.0, prevU, U);
                        norm = IvyFEM.Lapack.Functions.dnrm2(R);
                        System.Diagnostics.Debug.Assert(norm >= 0);
                        if (norm < Constants.PrecisionLowerLimit)
                        {
                            break;
                        }
                    }

                    //---------------------------------------------------
                    double[] X;
                    Solver.DoubleSolve(out X, A, B);
                    prevU = U;
                    U = X;
                    //---------------------------------------------------
                }
                System.Diagnostics.Debug.WriteLine("itr = " + itr + " norm = " + norm);
                //System.Diagnostics.Debug.Assert(itr < maxCnt);
            }
            else
            {
                var A = new IvyFEM.Linear.DoubleSparseMatrix(nodeCnt * dof, nodeCnt * dof);
                var B = new double[nodeCnt * dof];

                CalcLinearElasticAB(A, B, nodeCnt, dof);

                SetFixedCadsCondtion(World, A, B, nodeCnt, dof);


                //-------------------------------
                double[] X;
                Solver.DoubleSolve(out X, A, B);
                U = X;
                //-------------------------------
            }

        }

        protected bool HasNonLinearElasticMaterial()
        {
            bool hasNonlinear = false;
            IList<uint> feIds = World.GetTriangleFEIds();
            foreach (uint feId in feIds)
            {
                TriangleFE triFE = World.GetTriangleFE(feId);
                Material ma = World.GetMaterial(triFE.MaterialId);
                switch (ma.MaterialType)
                {
                    case MaterialType.ELASTIC:
                        break;
                    case MaterialType.SAINTVENANT_KIRCHHOFF_HYPERELASTIC:
                        hasNonlinear = true;
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        throw new NotImplementedException("MaterialType is not supported: " + ma.MaterialType);
                        break;
                }
            }
            return hasNonlinear;
        }

        protected void SetInitialU(int nodeCnt, int dof)
        {
            var fixedCoIdFixedCad = World.GetFixedCoordIdFixedCad();
            foreach (var pair in fixedCoIdFixedCad)
            {
                int fixedCoId = pair.Key;
                int fixedNodeId = World.Coord2Node(fixedCoId);
                if (fixedNodeId == -1)
                {
                    continue;
                }
                var fixedCads = pair.Value;
                foreach (FieldFixedCad fixedCad in fixedCads)
                {
                    int iDof = fixedCad.DofIndex;
                    double value = fixedCad.Value;
                    U[fixedNodeId * dof + iDof] = value;
                }
            }
        }

        public static void SetStressValue(uint displacementValueId, uint stressValueId, uint equivStressValueId, FEWorld world)
        {
            System.Diagnostics.Debug.Assert(world.IsFieldValueId(displacementValueId));
            FieldValue uFV = world.GetFieldValue(displacementValueId);

            FieldValue sigmaFV = null;
            if (stressValueId != 0)
            {
                System.Diagnostics.Debug.Assert(world.IsFieldValueId(stressValueId));
                sigmaFV = world.GetFieldValue(stressValueId);
                System.Diagnostics.Debug.Assert(sigmaFV.Type == FieldValueType.SYMMETRICAL_TENSOR2);
                System.Diagnostics.Debug.Assert(sigmaFV.Dof == 3);
            }
            FieldValue eqSigmaFV = null;
            if (equivStressValueId != 0)
            {
                System.Diagnostics.Debug.Assert(world.IsFieldValueId(equivStressValueId));
                eqSigmaFV = world.GetFieldValue(equivStressValueId);
                System.Diagnostics.Debug.Assert(eqSigmaFV.Type == FieldValueType.SCALAR);
                System.Diagnostics.Debug.Assert(eqSigmaFV.Dof == 1);
            }

            IList<uint> feIds = world.GetTriangleFEIds();
            foreach (uint feId in feIds)
            {
                TriangleFE triFE = world.GetTriangleFE(feId);
                int[] coIds = triFE.CoordIds;
                Material ma = world.GetMaterial(triFE.MaterialId);
                double lambda = 0;
                double mu = 0;
                if (ma.MaterialType == MaterialType.ELASTIC)
                {
                    var ma1 = ma as ElasticMaterial;
                    lambda = ma1.LameLambda;
                    mu = ma1.LameMu;
                }
                else if (ma.MaterialType == MaterialType.SAINTVENANT_KIRCHHOFF_HYPERELASTIC)
                {
                    var ma1 = ma as SaintVenantKirchhoffHyperelasticMaterial;
                    lambda = ma1.LameLambda;
                    mu = ma1.LameMu;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                double[][] Nu = triFE.CalcNu();
                double[] Nx = Nu[0];
                double[] Ny = Nu[1];
                double[] uu = new double[4]; // 00, 10, 01, 11 (dux/dx duy/dx dux/dy duy/duy)
                for (int iNode = 0; iNode < coIds.Length; iNode++)
                {
                    int coId = coIds[iNode];
                    double[] u = uFV.GetValue(coId, FieldDerivationType.VALUE);
                    uu[0] += u[0] * Nx[iNode];
                    uu[1] += u[1] * Nx[iNode];
                    uu[2] += u[0] * Ny[iNode];
                    uu[3] += u[1] * Ny[iNode];
                }

                //ε strain
                double[] eps = new double[4];
                if (ma.MaterialType == MaterialType.ELASTIC)
                {
                    eps[0] = 0.5 * (uu[0] + uu[0]);
                    eps[1] = 0.5 * (uu[1] + uu[2]);
                    eps[2] = 0.5 * (uu[2] + uu[1]);
                    eps[3] = 0.5 * (uu[3] + uu[3]);
                }
                else if (ma.MaterialType == MaterialType.SAINTVENANT_KIRCHHOFF_HYPERELASTIC)
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
                    double[] Sigma = sigmaFV.GetValues(FieldDerivationType.VALUE);
                    uint dof = sigmaFV.Dof;
                    Sigma[(feId - 1) * dof + 0] = sigma[0]; // σxx
                    Sigma[(feId - 1) * dof + 1] = sigma[3]; // σyy
                    Sigma[(feId - 1) * dof + 2] = sigma[2]; // τxy
                }
                if (equivStressValueId != 0)
                {
                    double[] EqSigma = eqSigmaFV.GetValues(FieldDerivationType.VALUE);
                    EqSigma[feId - 1] = misesStress;
                }
            }
        }

    }
}
