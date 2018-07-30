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
                double area = triFE.GetArea();

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

                        double[] a = new double[dof * dof]; // 11,21,12,22の順
                        System.Diagnostics.Debug.Assert(a.Length == 4);
                        int index = (int)(col * elemNodeCnt + row);
                        double kroneckerDelta = mu * (sNyNy[index] + sNxNx[index]);
                        a[0] = (lambda + mu) * sNxNx[index] + kroneckerDelta;
                        a[1] = lambda * sNyNx[index] + mu * sNxNy[index];
                        a[2] = lambda * sNxNy[index] + mu * sNyNx[index];
                        a[3] = (lambda + mu) * sNyNy[index] + kroneckerDelta;
                        
                        for (int dofRow = 0; dofRow < dof; dofRow++)
                        {
                            for (int dofCol = 0; dofCol < dof; dofCol++)
                            {
                                A[rowNodeId * dof + dofRow, colNodeId * dof + dofCol] += 
                                    a[dofCol * dof + dofRow];
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
                    B[rowNodeId * dof + 0] += area * rho * gx * 0.33333333333333333;
                    B[rowNodeId * dof + 1] += area * rho * gy * 0.33333333333333333;
                }
            }

            var fixedCoIdFixedCad = World.GetFixedCoordIdFixedCad();

            for (int rowNodeId = 0; rowNodeId < nodeCnt; rowNodeId++)
            {
                int rowCoId = World.Node2Coord(rowNodeId);
                if (!fixedCoIdFixedCad.ContainsKey(rowCoId))
                {
                    continue;
                }
                IList<FieldFixedCad> fixedCads = fixedCoIdFixedCad[rowCoId];
                foreach (var fixedCad in fixedCads)
                {
                    int iDof = fixedCad.DofIndex;
                    double value = fixedCad.Value;
                    for (int colNodeId = 0; colNodeId < nodeCnt; colNodeId++)
                    {
                        for (int dofCol = 0; dofCol < dof; dofCol++)
                        {
                            double a = ((colNodeId == rowNodeId  && dofCol == iDof) ? 1 : 0);
                            A[rowNodeId * dof + iDof, colNodeId * dof + dofCol] = a;
                        }
                    }

                    B[rowNodeId * dof + iDof] = value;
                }
            }

            double[] X;
            int xRow;
            int xCol;
            int ret = IvyFEM.Lapack.Functions.dgesv(out X, out xRow, out xCol,
                A.Buffer, A.RowSize, A.ColumnSize,
                B, B.Length, 1);
            U = X;
        }
    }
}
