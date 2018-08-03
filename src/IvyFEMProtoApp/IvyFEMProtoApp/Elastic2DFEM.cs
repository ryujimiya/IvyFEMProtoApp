using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class Elastic2DFEM : FEM
    {
        //Solve
        // Output
        public double[] U { get; private set; }

        public Elastic2DFEM() : base()
        {

        }

        public Elastic2DFEM(FEWorld world)
        {
            World = world;
        }

        public override void Solve()
        {
            int nodeCnt = (int)World.GetNodeCount();
            IList<uint> feIds = World.GetTriangleFEIds();

            int dof = 2;
            var A = new Lapack.DoubleMatrix(nodeCnt * dof, nodeCnt * dof);
            var B = new double[nodeCnt * dof];

            foreach (uint feId in feIds)
            {
                TriangleFE triFE = World.GetTriangleFE(feId);
                int[] coIds = triFE.CoordIds;
                uint elemNodeCnt = triFE.NodeCount;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = coIds[iNode];
                    int nodeId = World.Coord2Node(coId);
                    nodes[iNode] = nodeId;
                }

                Material ma0 = World.GetMaterial(triFE.MaterialId);
                System.Diagnostics.Debug.Assert(ma0.MaterialType == MaterialType.ELASTIC);
                var ma = ma0 as ElasticMaterial;
                double lambda = ma.LameLambda;
                double mu = ma.LameMu;
                double rho = ma.MassDensity;
                double gx = ma.GravityX;
                double gy = ma.GravityY;

                double[] sNN = triFE.CalcSNN();
                double[][] sNuNvs = triFE.CalcSNuNvs();
                double[] sNxNx = sNuNvs[0];
                double[] sNyNx = sNuNvs[1];
                double[] sNxNy = sNuNvs[2];
                double[] sNyNy = sNuNvs[3];
                double lumpedSNN = triFE.CalcLumpedSNN();

                for (int row = 0; row < elemNodeCnt; row++)
                {
                    int rowNodeId = nodes[row];
                    if (rowNodeId == -1)
                    {
                        continue;
                    }
                    for (int col = 0; col < elemNodeCnt; col++)
                    {
                        int colNodeId = nodes[col];
                        if (colNodeId == -1)
                        {
                            continue;
                        }

                        double[] k = new double[dof * dof]; // 11,21,12,22の順
                        System.Diagnostics.Debug.Assert(k.Length == 4);
                        int index = (int)(col * elemNodeCnt + row);
                        double kroneckerDelta = mu * (sNyNy[index] + sNxNx[index]);
                        k[0] = (lambda + mu) * sNxNx[index] + kroneckerDelta;
                        k[1] = lambda * sNyNx[index] + mu * sNxNy[index];
                        k[2] = lambda * sNxNy[index] + mu * sNyNx[index];
                        k[3] = (lambda + mu) * sNyNy[index] + kroneckerDelta;
                        
                        for (int dofRow = 0; dofRow < dof; dofRow++)
                        {
                            for (int dofCol = 0; dofCol < dof; dofCol++)
                            {
                                A[rowNodeId * dof + dofRow, colNodeId * dof + dofCol] += 
                                    k[dofCol * dof + dofRow];
                            }
                        }
                    }
                }

                for (int row = 0; row < elemNodeCnt; row++)
                {
                    int rowNodeId = nodes[row];
                    if (rowNodeId == -1)
                    {
                        continue;
                    }
                    B[rowNodeId * dof + 0] += rho * gx * lumpedSNN;
                    B[rowNodeId * dof + 1] += rho * gy * lumpedSNN;
                }
            }

            SetFixedCadsCondtion(World, A, B, nodeCnt, dof);

            double[] X;
            int xRow;
            int xCol;
            int ret = IvyFEM.Lapack.Functions.dgesv(out X, out xRow, out xCol,
                A.Buffer, A.RowSize, A.ColumnSize,
                B, B.Length, 1);
            U = X;
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
                System.Diagnostics.Debug.Assert(sigmaFV.Dof == 6);
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
                Material ma0 = world.GetMaterial(triFE.MaterialId);
                System.Diagnostics.Debug.Assert(ma0.MaterialType == MaterialType.ELASTIC);
                var ma = ma0 as ElasticMaterial;
                double lambda = ma.LameLambda;
                double mu = ma.LameMu;

                double[] a;
                double[] b;
                double[] c;
                triFE.CalcTransMatrix(out a, out b, out c);
                double[] dNdx = b;
                double[] dNdy = c;

                double[] dudx = new double[4]; // 00, 10, 01, 11
                for (int iNode = 0; iNode < 3; iNode++)
                {
                    int coId = coIds[iNode];
                    double[] u = uFV.GetValue(coId, FieldDerivationType.VALUE);
                    dudx[0] += u[0] * dNdx[iNode];
                    dudx[1] += u[1] * dNdx[iNode];
                    dudx[2] += u[0] * dNdy[iNode];
                    dudx[3] += u[1] * dNdy[iNode]; 
                }

                //ε strain
                double[] eps = new double[4];
                eps[0] = 0.5 * (dudx[0] + dudx[0]);
                eps[1] = 0.5 * (dudx[1] + dudx[2]);
                eps[2] = 0.5 * (dudx[2] + dudx[1]);
                eps[3] = 0.5 * (dudx[3] + dudx[3]);

                // σ stress
                double[] sigma = new double[4];
				sigma[0] = mu* eps[0] + lambda * (eps[0] + eps[3]);
				sigma[1] = mu* eps[1];
                sigma[2] = mu * eps[2];
                sigma[3] = mu* eps[3] + lambda * (eps[0] + eps[3]);

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
