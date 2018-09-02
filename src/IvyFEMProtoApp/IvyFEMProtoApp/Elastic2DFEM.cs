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
                Material ma0 = World.GetMaterial(triFE.MaterialId);
                if (ma0.MaterialType != MaterialType.Elastic)
                {
                    continue;
                }
                int[] coIds = triFE.CoordIds;
                uint elemNodeCnt = triFE.NodeCount;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = coIds[iNode];
                    int nodeId = World.Coord2Node(coId);
                    nodes[iNode] = nodeId;
                }

                var ma = ma0 as ElasticMaterial;
                double lambda = ma.LameLambda;
                double mu = ma.LameMu;
                double rho = ma.MassDensity;
                double gx = ma.GravityX;
                double gy = ma.GravityY;

                double sN = triFE.CalcSN();
                double[,] sNN = triFE.CalcSNN();
                double[,][,] sNuNv = triFE.CalcSNuNv();
                double[,] sNxNx = sNuNv[0, 0];
                double[,] sNyNx = sNuNv[1, 0];
                double[,] sNxNy = sNuNv[0, 1];
                double[,] sNyNy = sNuNv[1, 1];

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
                        double[,] m = new double[dof, dof];
                        k[0, 0] = (lambda + mu) * sNxNx[row, col] + mu * (sNxNx[row, col] + sNyNy[row, col]);
                        k[1, 0] = lambda * sNyNx[row, col] + mu * sNxNy[row, col];
                        k[0, 1] = lambda * sNxNy[row, col] + mu * sNyNx[row, col];
                        k[1, 1] = (lambda + mu) * sNyNy[row, col] + mu * (sNxNx[row, col] + sNyNy[row, col]);

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

        protected override void CalcSaintVenantHyperelasticAB(IvyFEM.Linear.DoubleSparseMatrix A, double[] B,
            int nodeCnt, int dof)
        {
            IList<uint> feIds = World.GetTriangleFEIds();

            foreach (uint feId in feIds)
            {
                TriangleFE triFE = World.GetTriangleFE(feId);
                Material ma0 = World.GetMaterial(triFE.MaterialId);
                if (ma0.MaterialType != MaterialType.SaintVenantHyperelastic)
                {
                    continue;
                }
                int[] coIds = triFE.CoordIds;
                uint elemNodeCnt = triFE.NodeCount;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = coIds[iNode];
                    int nodeId = World.Coord2Node(coId);
                    nodes[iNode] = nodeId;
                }

                var ma = ma0 as SaintVenantHyperelasticMaterial;
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
                double detJWeight = (1.0 / 2.0) * weight * detJ;
                double sN = triFE.CalcSN();

                double[,] uu = new double[dof, dof];
                for (int iDof = 0; iDof < dof; iDof++)
                {
                    for (int jDof = 0; jDof < dof; jDof++)
                    {
                        for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                        {
                            int iNodeId = nodes[iNode];
                            if (iNodeId == -1)
                            {
                                continue;
                            }
                            uu[iDof, jDof] += U[iNodeId * dof + iDof] * Nu[jDof][iNode];
                        }
                    }
                }

                double[,] e = new double[dof, dof];
                for (int iDof = 0; iDof < dof; iDof++)
                {
                    for (int jDof = 0; jDof < dof; jDof++)
                    {
                        e[iDof, jDof] = (1.0 / 2.0) * (uu[iDof, jDof] + uu[jDof, iDof]);
                        for (int kDof = 0; kDof < dof; kDof++)
                        {
                            e[iDof, jDof] += (1.0 / 2.0) * uu[kDof, iDof] * uu[kDof, jDof];
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
                    double tmp = 0.0;
                    for (int iDof = 0; iDof < dof; iDof++)
                    {
                        for (int jDof = 0; jDof < dof; jDof++)
                        {
                            s[iDof, jDof] = 2.0 * mu * e[iDof, jDof];
                        }
                        tmp += e[iDof, iDof];
                    }
                    for (int iDof = 0; iDof < dof; iDof++)
                    {
                        s[iDof, iDof] += lambda * tmp;
                    }
                }

                double[,] q = new double[elemNodeCnt, dof];
                for (int kNode = 0; kNode < elemNodeCnt; kNode++)
                {
                    for (int kDof = 0; kDof < dof; kDof++)
                    {
                        for (int iDof = 0; iDof < dof; iDof++)
                        {
                            for (int jDof = 0; jDof < dof; jDof++)
                            {
                                q[kNode, kDof] +=
                                    detJWeight * s[iDof, jDof] * b[kNode, kDof, iDof, jDof];
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

                        for (int rowDof = 0; rowDof < dof; rowDof++)
                        {
                            for (int colDof = 0; colDof < dof; colDof++)
                            {
                                {
                                    double tmp = 0.0;
                                    for (int gDof = 0; gDof < dof; gDof++)
                                    {
                                        for (int hDof = 0; hDof < dof; hDof++)
                                        {
                                            tmp += 
                                                b[row, rowDof, gDof, hDof] * b[col, colDof, gDof, hDof] +
                                                b[row, rowDof, gDof, hDof] * b[col, colDof, hDof, gDof];
                                        }
                                    }
                                    k[rowDof, colDof] += detJWeight * mu * tmp;
                                }
                                {
                                    double tmp1 = 0.0;
                                    double tmp2 = 0.0;
                                    for (int gDof = 0; gDof < dof; gDof++)
                                    {
                                        tmp1 += b[row, rowDof, gDof, gDof];
                                        tmp2 += b[col, colDof, gDof, gDof];
                                    }
                                    k[rowDof, colDof] += detJWeight * lambda * tmp1 * tmp2;
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
                            for (int rowDof = 0; rowDof < dof; rowDof++)
                            {
                                k[rowDof, rowDof] += detJWeight * tmp;
                            }
                        }

                        for (int rowDof = 0; rowDof < dof; rowDof++)
                        {
                            for (int colDof = 0; colDof < dof; colDof++)
                            {
                                A[rowNodeId * dof + rowDof, colNodeId * dof + colDof] +=
                                    k[rowDof, colDof];
                                B[rowNodeId * dof + rowDof] +=
                                    k[rowDof, colDof] * U[colNodeId * dof + colDof];
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
        }

    }
}
