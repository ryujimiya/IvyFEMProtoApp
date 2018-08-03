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

        public abstract void Solve();

        protected static void SetFixedCadsCondtion(FEWorld world, IvyFEM.Lapack.DoubleMatrix A, double[]B, int nodeCnt, int dof)
        {
            var fixedCoIdFixedCad = world.GetFixedCoordIdFixedCad();

            for (int rowNodeId = 0; rowNodeId < nodeCnt; rowNodeId++)
            {
                int rowCoId = world.Node2Coord(rowNodeId);
                IList<FieldFixedCad> rowfixedCads = new List<FieldFixedCad>();
                if (fixedCoIdFixedCad.ContainsKey(rowCoId))
                {
                    rowfixedCads = fixedCoIdFixedCad[rowCoId];
                }
                for (int colNodeId = 0; colNodeId < nodeCnt; colNodeId++)
                {
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
                        for (int dofRow = 0; dofRow < dof; dofRow++)
                        {
                            var hits = (
                                from rowfixedCad in rowfixedCads
                                where rowfixedCad.DofIndex == dofRow
                                select rowfixedCad).ToList();
                            if (hits.Count > 0)
                            {
                                continue;
                            }
                            double a = A[rowNodeId * dof + dofRow, colNodeId * dof + iDof];
                            B[rowNodeId * dof + dofRow] -= a * value;
                            A[rowNodeId * dof + dofRow, colNodeId * dof + iDof] = 0;
                        }
                    }
                }
            }

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
                        for (int dofCol = 0; dofCol < dof; dofCol++)
                        {
                            double a = ((colNodeId == rowNodeId && dofCol == iDof) ? 1 : 0);
                            A[rowNodeId * dof + iDof, colNodeId * dof + dofCol] = a;
                        }
                    }

                    B[rowNodeId * dof + iDof] = value;
                }
            }
        }
    }
}
