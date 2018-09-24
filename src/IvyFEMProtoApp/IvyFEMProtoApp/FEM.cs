using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    abstract class FEM
    {
        public FEWorld World { get; set; } = null;
        public IvyFEM.Linear.IEquationSolver Solver { get; set; } = null;

        public abstract void Solve();

        protected static void SetFixedCadsCondtion(FEWorld world, IvyFEM.Linear.DoubleSparseMatrix A, double[]B,
            int[] nodeCnts, int[] dofs)
        {
            var fixedCoIdFixedCad = world.GetFixedCoordIdFixedCad();

            // A21の右辺移行
            // Note:速度改善のためcolを先にしている
            for (uint colQuantityId = 0; colQuantityId < dofs.Length; colQuantityId++)
            {
                int colNodeCnt = nodeCnts[colQuantityId];
                int colDofCnt = dofs[colQuantityId];
                int colOffset = 0;
                for (int i = 0; i < colQuantityId; i++)
                {
                    colOffset += nodeCnts[i] * dofs[i];
                }
                for (int colNodeId = 0; colNodeId < colNodeCnt; colNodeId++)
                {
                    // fixed節点、自由度
                    int colCoId = world.Node2Coord(colQuantityId, colNodeId);
                    if (!fixedCoIdFixedCad.ContainsKey(colCoId))
                    {
                        continue;
                    }
                    IList<FieldFixedCad> fixedCads = fixedCoIdFixedCad[colCoId];
                    foreach (var fixedCad in fixedCads)
                    {
                        uint iColQuantity = fixedCad.QuantityId;                        
                        uint iColDof = fixedCad.DofIndex;
                        double value = fixedCad.Value;
                        if (iColQuantity != colQuantityId)
                        {
                            continue;
                        }

                        for (uint rowQuantityId = 0; rowQuantityId < dofs.Length; rowQuantityId++)
                        {
                            int rowNodeCnt = nodeCnts[rowQuantityId];
                            int rowDofCnt = dofs[rowQuantityId];
                            int rowOffset = 0;
                            for (int i = 0; i < rowQuantityId; i++)
                            {
                                rowOffset += nodeCnts[i] * dofs[i];
                            }
                            for (int rowNodeId = 0; rowNodeId < rowNodeCnt; rowNodeId++)
                            {
                                int rowCoId = world.Node2Coord(rowQuantityId, rowNodeId);
                                IList<FieldFixedCad> rowfixedCads = new List<FieldFixedCad>();
                                if (fixedCoIdFixedCad.ContainsKey(rowCoId))
                                {
                                    rowfixedCads = fixedCoIdFixedCad[rowCoId];
                                }
                                // fixedでない節点、自由度
                                IList<uint> rowDofs = new List<uint>();
                                for (uint rowDof = 0; rowDof < rowDofCnt; rowDof++)
                                {
                                    rowDofs.Add(rowDof);
                                }
                                foreach (var rowfixedCad in rowfixedCads)
                                {
                                    uint iRowQuantity = rowfixedCad.QuantityId;
                                    if (iRowQuantity == rowQuantityId)
                                    {
                                        rowDofs.Remove(rowfixedCad.DofIndex);
                                    }
                                }
                                if (rowDofs.Count == 0)
                                {
                                    continue;
                                }

                                foreach (int rowDof in rowDofs)
                                {
                                    double a = A[rowOffset + rowNodeId * rowDofCnt + rowDof,
                                        colOffset + colNodeId * colDofCnt + (int)iColDof];
                                    B[rowOffset + rowNodeId * rowDofCnt + rowDof] -= a * value;
                                    A[rowOffset + rowNodeId * rowDofCnt + rowDof,
                                        colOffset + colNodeId * colDofCnt + (int)iColDof] = 0;
                                }
                            }
                        }
                    }
                }
            }

            // A11, A12
            for (uint rowQuantityId = 0; rowQuantityId < dofs.Length; rowQuantityId++)
            {
                int rowNodeCnt = nodeCnts[rowQuantityId];
                int rowDofCnt = dofs[rowQuantityId];
                int rowOffset = 0;
                for (int i = 0; i < rowQuantityId; i++)
                {
                    rowOffset += nodeCnts[i] * dofs[i];
                }
                for (int rowNodeId = 0; rowNodeId < rowNodeCnt; rowNodeId++)
                {
                    int rowCoId = world.Node2Coord(rowQuantityId, rowNodeId);
                    if (!fixedCoIdFixedCad.ContainsKey(rowCoId))
                    {
                        continue;
                    }
                    IList<FieldFixedCad> fixedCads = fixedCoIdFixedCad[rowCoId];
                    foreach (var fixedCad in fixedCads)
                    {
                        uint iRowQuantity = fixedCad.QuantityId;
                        if (iRowQuantity != rowQuantityId)
                        {
                            continue;
                        }
                        uint iRowDof = fixedCad.DofIndex;
                        double value = fixedCad.Value;
                        for (int colQuantityId = 0; colQuantityId < dofs.Length; colQuantityId++)
                        {
                            int colNodeCnt = nodeCnts[colQuantityId];
                            int colDofCnt = dofs[colQuantityId];
                            int colOffset = 0;
                            for (int i = 0; i < colQuantityId; i++)
                            {
                                colOffset += nodeCnts[i] * dofs[i];
                            }
                            for (int colNodeId = 0; colNodeId < colNodeCnt; colNodeId++)
                            {
                                for (int colDof = 0; colDof < colDofCnt; colDof++)
                                {
                                    double a = (colQuantityId == rowQuantityId &&
                                        colNodeId == rowNodeId &&
                                        colDof == iRowDof) ? 1 : 0;
                                    A[rowOffset + rowNodeId * rowDofCnt + (int)iRowDof,
                                        colOffset + colNodeId * colDofCnt + colDof] = a;
                                }
                            }
                            B[rowOffset + rowNodeId * rowDofCnt + iRowDof] = value;
                        }
                    }
                }
            }
        }
    }
}
