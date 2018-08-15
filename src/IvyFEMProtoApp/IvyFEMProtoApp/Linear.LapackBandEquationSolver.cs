using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Linear
{
    class LapackBandEquationSolver : IEquationSolver
    {
        public bool DoubleSolve(out double[] X, DoubleSparseMatrix A, double[] B)
        {
            bool success = false;
            IvyFEM.Lapack.DoubleBandMatrix bandA = (IvyFEM.Lapack.DoubleBandMatrix)A;
            A = null;
            int xRow;
            int xCol;
            int ret = IvyFEM.Lapack.Functions.dgbsv(out X, out xRow, out xCol,
                bandA.Buffer, bandA.RowLength, bandA.ColumnLength,
                bandA.SubdiaLength, bandA.SuperdiaLength,
                B, B.Length, 1);
            System.Diagnostics.Debug.Assert(ret == 0);
            success = (ret == 0);
            return success;
        }

        public bool ComplexSolve(out Complex[] X, ComplexSparseMatrix A, Complex[] B)
        {
            bool success = false;
            IvyFEM.Lapack.ComplexBandMatrix bandA = (IvyFEM.Lapack.ComplexBandMatrix)A;
            A = null;
            int xRow;
            int xCol;
            int ret = IvyFEM.Lapack.Functions.zgbsv(out X, out xRow, out xCol,
                bandA.Buffer, bandA.RowLength, bandA.ColumnLength,
                bandA.SubdiaLength, bandA.SuperdiaLength,
                B, B.Length, 1);
            System.Diagnostics.Debug.Assert(ret == 0);
            success = (ret == 0);
            return success;
        }

    }
}
