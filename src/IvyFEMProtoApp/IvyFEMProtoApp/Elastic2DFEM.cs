using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class Elastic2DFEM : Elastic2DBaseFEM
    {
        public Elastic2DFEM() : base()
        {

        }

        public Elastic2DFEM(FEWorld world)
        {
            World = world;
        }

        protected override void CalcLinearElasticAB(IvyFEM.Linear.DoubleSparseMatrix A, double[] B,
            int nodeCnt, int dof)
        {
            IList<uint> feIds = World.GetTriangleFEIds();
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
                if (ma0.MaterialType != MaterialType.ELASTIC)
                {
                    continue;
                }
                var ma = ma0 as ElasticMaterial;
                double lambda = ma.LameLambda;
                double mu = ma.LameMu;
                double rho = ma.MassDensity;
                double gx = ma.GravityX;
                double gy = ma.GravityY;

                double sN = triFE.CalcSN();
                double[] sNN = triFE.CalcSNN();
                double[][] sNuNvs = triFE.CalcSNuNvs();
                double[] sNxNx = sNuNvs[0];
                double[] sNyNx = sNuNvs[1];
                double[] sNxNy = sNuNvs[2];
                double[] sNyNy = sNuNvs[3];

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
                        k[0] = (lambda + mu) * sNxNx[index] + mu * (sNxNx[index] + sNyNy[index]);
                        k[1] = lambda * sNyNx[index] + mu * sNxNy[index];
                        k[2] = lambda * sNxNy[index] + mu * sNyNx[index];
                        k[3] = (lambda + mu) * sNyNy[index] + mu * (sNxNx[index] + sNyNy[index]);

                        for (int rowDof = 0; rowDof < dof; rowDof++)
                        {
                            for (int colDof = 0; colDof < dof; colDof++)
                            {
                                A[rowNodeId * dof + rowDof, colNodeId * dof + colDof] +=
                                    k[colDof * dof + rowDof];
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
                    B[rowNodeId * dof + 0] += rho * gx * sN;
                    B[rowNodeId * dof + 1] += rho * gy * sN;
                }
            }
        }

        protected override void CalcSaintVenantKirchhoffHyperelasticAB(IvyFEM.Linear.DoubleSparseMatrix A, double[] B,
            int nodeCnt, int dof)
        {
            IList<uint> feIds = World.GetTriangleFEIds();

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
                if (ma0.MaterialType != MaterialType.SAINTVENANT_KIRCHHOFF_HYPERELASTIC)
                {
                    continue;
                }
                var ma = ma0 as SaintVenantKirchhoffHyperelasticMaterial;
                double lambda = ma.LameLambda;
                double mu = ma.LameMu;
                double rho = ma.MassDensity;
                double gx = ma.GravityX;
                double gy = ma.GravityY;

                double[][] Nu = triFE.CalcNu();
                IntegrationPoints iPs = triFE.GetIntegrationPoints(1);
                System.Diagnostics.Debug.Assert(iPs.L.Length == 1);
                double detJ = triFE.GetDetJacobian();
                double weight = iPs.Weight[0];
                double detJWeight = 0.5 * weight * detJ;
                double sN = triFE.CalcSN();

                double[,] uu = new double[dof, dof];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int iNodeId = nodes[iNode];
                    if (iNodeId == -1)
                    {
                        continue;
                    }
                    for (int iDof = 0; iDof < dof; iDof++)
                    {
                        for (int jDof = 0; jDof < dof; jDof++)
                        {
                            uu[iDof, jDof] += U[iNodeId * dof + iDof] * Nu[jDof][iNode];
                        }
                    }
                }

                double[,] e = new double[dof, dof];
                for (int iDof = 0; iDof < dof; iDof++)
                {
                    for (int jDof = 0; jDof < dof; jDof++)
                    {
                        e[iDof, jDof] = 0.5 * (uu[iDof, jDof] + uu[jDof, iDof]);
                        for (int kDof = 0; kDof < dof; kDof++)
                        {
                            e[iDof, jDof] += 0.5 * uu[kDof, iDof] * uu[kDof, jDof];
                        }
                    }
                }

                double[,,,] b = new double[elemNodeCnt, dof, dof, dof];
                {
                    double[,] z = new double[dof, dof];
                    for (int iDof = 0; iDof < dof; iDof++)
                    {
                        for (int jDof = 0; jDof < dof; jDof++)
                        {
                            z[iDof, jDof] = uu[iDof, jDof];
                        }
                        z[iDof, iDof] += 1.0;
                    }
                    for (int kNode = 0; kNode < elemNodeCnt; kNode++)
                    {
                        int kNodeId = nodes[kNode];
                        if (kNodeId == -1)
                        {
                            continue;
                        }
                        for (int jDof = 0; jDof < dof; jDof++)
                        {
                            for (int iDof = 0; iDof < dof; iDof++)
                            {
                                for (int kDof = 0; kDof < dof; kDof++)
                                {
                                    b[kNode, kDof, iDof, jDof] = Nu[jDof][kNode] * z[kDof, iDof];
                                }
                            }
                        }
                    }
                }

                double[,] s = new double[dof, dof];
                {
                    // trace(e)
                    double tr = 0.0;
                    for (int iDof = 0; iDof < dof; iDof++)
                    {
                        for (int jDof = 0; jDof < dof; jDof++)
                        {
                            s[iDof, jDof] = 2.0 * mu * e[iDof, jDof];
                        }
                        tr += e[iDof, iDof];
                    }
                    for (int iDof = 0; iDof < dof; iDof++)
                    {
                        s[iDof, iDof] += lambda * tr;
                    }
                }

                double[,] q = new double[elemNodeCnt, dof];
                for (int kNode = 0; kNode < elemNodeCnt; kNode++)
                {
                    int kNodeId = nodes[kNode];
                    if (kNodeId == -1)
                    {
                        continue;
                    }
                    for (int kDof = 0; kDof < dof; kDof++)
                    {
                        for (int iDof = 0; iDof < dof; iDof++)
                        {
                            for (int jDof = 0; jDof < dof; jDof++)
                            {
                                q[kNode, kDof]
                                    += detJWeight * s[iDof, jDof] * b[kNode, kDof, iDof, jDof];
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
                    for (int col = 0; col < elemNodeCnt; col++)
                    {
                        int colNodeId = nodes[col];
                        if (colNodeId == -1)
                        {
                            continue;
                        }

                        double[,] k = new double[dof, dof];

                        for (int iDof = 0; iDof < dof; iDof++)
                        {
                            for (int jDof = 0; jDof < dof; jDof++)
                            {
                                {
                                    double tmp = 0.0;
                                    for (int gDof = 0; gDof < dof; gDof++)
                                    {
                                        for (int hDof = 0; hDof < dof; hDof++)
                                        {
                                            tmp += b[row, iDof, gDof, hDof] * b[col, jDof, gDof, hDof]
                                                + b[row, iDof, gDof, hDof] * b[col, jDof, hDof, gDof];
                                        }
                                    }
                                    k[iDof, jDof] += detJWeight * mu * tmp;
                                }
                                {
                                    double tmp1 = 0.0;
                                    double tmp2 = 0.0;
                                    for (int gDof = 0; gDof < dof; gDof++)
                                    {
                                        tmp1 += b[row, iDof, gDof, gDof];
                                        tmp2 += b[col, jDof, gDof, gDof];
                                    }
                                    k[iDof, jDof] += detJWeight * lambda * tmp1 * tmp2;
                                }
                            }
                        }

                        {
                            double tmp = 0.0;
                            for (int gDof = 0; gDof < dof; gDof++)
                            {
                                for (int hDof = 0; hDof < dof; hDof++)
                                {
                                    tmp += s[gDof, hDof] * Nu[hDof][row] * Nu[gDof][col];
                                }
                            }
                            for (int iDof = 0; iDof < dof; iDof++)
                            {
                                k[iDof, iDof] += detJWeight * tmp;
                            }
                        }

                        for (int rowDof = 0; rowDof < dof; rowDof++)
                        {
                            for (int colDof = 0; colDof < dof; colDof++)
                            {
                                A[rowNodeId * dof + rowDof, colNodeId * dof + colDof] +=
                                    k[rowDof, colDof];
                            }
                        }
                    }
                }

                double[,] f = new double[elemNodeCnt, dof];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int iNodeId = nodes[iNode];
                    if (iNodeId == -1)
                    {
                        continue;
                    }
                    f[iNode, 0] += rho * gx * sN;
                    f[iNode, 1] += rho * gy * sN;
                }

                for (int row = 0; row < elemNodeCnt; row++)
                {
                    int rowNodeId = nodes[row];
                    if (rowNodeId == -1)
                    {
                        continue;
                    }
                    B[rowNodeId * dof + 0] += f[row, 0] - q[row, 0];
                    B[rowNodeId * dof + 1] += f[row, 1] - q[row, 1];
                }
            }

            double[] AU = A * U;
            double[] tmpB = IvyFEM.Lapack.Functions.daxpy(1.0, B, AU);
            tmpB.CopyTo(B, 0); // Note: B = tmpBとできない
        }

    }
}
