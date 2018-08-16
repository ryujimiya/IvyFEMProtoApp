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

        protected static void SetFixedCadsCondtion(FEWorld world, IvyFEM.Linear.DoubleSparseMatrix A, double[]B, int nodeCnt, int dof)
        {
            var fixedCoIdFixedCad = world.GetFixedCoordIdFixedCad();

            // A21の右辺移行
            for (int rowNodeId = 0; rowNodeId < nodeCnt; rowNodeId++)
            {
                int rowCoId = world.Node2Coord(rowNodeId);
                IList<FieldFixedCad> rowfixedCads = new List<FieldFixedCad>();
                if (fixedCoIdFixedCad.ContainsKey(rowCoId))
                {
                    rowfixedCads = fixedCoIdFixedCad[rowCoId];
                }
                // fixedでない節点、自由度
                IList<int> rowDofs = new List<int>();
                for (int rowDof = 0; rowDof < dof; rowDof++)
                {
                    rowDofs.Add(rowDof);
                }
                foreach (var rowfixedCad in rowfixedCads)
                {
                    rowDofs.Remove(rowfixedCad.DofIndex);
                }
                foreach (int rowDof in rowDofs)
                {
                    for (int colNodeId = 0; colNodeId < nodeCnt; colNodeId++)
                    {
                        // fixed節点、自由度
                        int colCoId = world.Node2Coord(colNodeId);
                        if (!fixedCoIdFixedCad.ContainsKey(colCoId))
                        {
                            continue;
                        }
                        IList<FieldFixedCad> fixedCads = fixedCoIdFixedCad[colCoId];
                        foreach (var fixedCad in fixedCads)
                        {
                            System.Diagnostics.Debug.Assert(fixedCad.ValueType == FieldValueType.VECTOR2);
                            int iDof = fixedCad.DofIndex;
                            double value = fixedCad.Value;
                            double a = A[rowNodeId * dof + rowDof, colNodeId * dof + iDof];
                            B[rowNodeId * dof + rowDof] -= a * value;
                            A[rowNodeId * dof + rowDof, colNodeId * dof + iDof] = 0;
                        }
                    }
                }
            }

            // A11, A12
            for (int rowNodeId = 0; rowNodeId < nodeCnt; rowNodeId++)
            {
                int rowCoId = world.Node2Coord(rowNodeId);
                if (!fixedCoIdFixedCad.ContainsKey(rowCoId))
                {
                    continue;
                }
                IList<FieldFixedCad> fixedCads = fixedCoIdFixedCad[rowCoId];
                foreach (var fixedCad in fixedCads)
                {
                    System.Diagnostics.Debug.Assert(fixedCad.ValueType == FieldValueType.VECTOR2);
                    int iDof = fixedCad.DofIndex;
                    double value = fixedCad.Value;
                    for (int colNodeId = 0; colNodeId < nodeCnt; colNodeId++)
                    {
                        for (int colDof = 0; colDof < dof; colDof++)
                        {
                            double a = ((colNodeId == rowNodeId && colDof == iDof) ? 1 : 0);
                            A[rowNodeId * dof + iDof, colNodeId * dof + colDof] = a;
                        }
                    }

                    B[rowNodeId * dof + iDof] = value;
                }
            }
        }
    }
}
