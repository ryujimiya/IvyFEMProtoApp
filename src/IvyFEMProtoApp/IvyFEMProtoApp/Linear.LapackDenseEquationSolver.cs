using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Linear
{
    class LapackDenseEquationSolver : IEquationSolver
    {
        public bool DoubleSolve(out double[] X, DoubleSparseMatrix A, double[] B)
        {
            bool success = false;
            IvyFEM.Lapack.DoubleMatrix denseA = (IvyFEM.Lapack.DoubleMatrix)A;
            int xRow;
            int xCol;
            int ret = IvyFEM.Lapack.Functions.dgesv(out X, out xRow, out xCol,
                denseA.Buffer, denseA.RowLength, denseA.ColumnLength,
                B, B.Length, 1);
            System.Diagnostics.Debug.Assert(ret == 0);
            success = (ret == 0);
            return success;
        }

        public bool ComplexSolve(out Complex[] X, ComplexSparseMatrix A, Complex[] B)
        {
            bool success = false;
            IvyFEM.Lapack.ComplexMatrix denseA = (IvyFEM.Lapack.ComplexMatrix)A;
            int xRow;
            int xCol;
            int ret = IvyFEM.Lapack.Functions.zgesv(out X, out xRow, out xCol,
                denseA.Buffer, denseA.RowLength, denseA.ColumnLength,
                B, B.Length, 1);
            System.Diagnostics.Debug.Assert(ret == 0);
            success = (ret == 0);

            return success;
        }
    }
}
