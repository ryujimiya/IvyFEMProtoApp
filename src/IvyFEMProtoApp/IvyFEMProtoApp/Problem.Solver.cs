using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void InterseMatrixExample()
        {
            //       1  1 -1
            // A =  -2  0  1
            //       0  2  1
            double[] a = new double[9] { 1, -2, 0, 1, 0, 2, -1, 1, 1 };
            var A = new IvyFEM.Lapack.DoubleMatrix(a, 3, 3, false);
            var X = IvyFEM.Lapack.DoubleMatrix.Inverse(A);
            string CRLF = System.Environment.NewLine;
            string ret =
                "      1  1 -1" + CRLF +
                "A =  -2  0  1" + CRLF +
                "      0  2  1" + CRLF +
                CRLF +
                "            1  -2  -3  1" + CRLF +
                "X = A^-1 = ---  2   1  1" + CRLF +
                "            4  -4  -2  2" + CRLF;
            ret +=  "calculated X" + CRLF +
                X.GetType() + CRLF +
                "RowLength = " + X.RowLength + CRLF +
                "ColumnLength = " + X.ColumnLength + CRLF;
            for (int row = 0; row < X.RowLength; row++)
            {
                for (int col = 0; col < X.ColumnLength; col++)
                {
                    ret += "[" + row + ", " + col + "] = " + X[row, col] + CRLF;
                }
            }

            var C = A * X;
            ret += "C = A * X" + CRLF +
                " will be identity matrix" + CRLF;
            ret += "calculated C" + CRLF + 
                C.GetType() + CRLF  +
                "RowLength = " + C.RowLength + CRLF +
                "ColumnLength = " + C.ColumnLength + CRLF;
            for (int row = 0; row < C.RowLength; row++)
            {
                for (int col = 0; col < C.ColumnLength; col++)
                {
                    ret += "[" + row + ", " + col + "] = " + C[row, col] + CRLF;
                }
            }
            System.Diagnostics.Debug.WriteLine(ret);
            AlertWindow.ShowDialog(ret);
        }

        public void LinearEquationExample()
        {
            //      3 -2  1      7
            // A =  1  2 -2  b = 2
            //      4  3 -2      7
            double[] a = new double[9]
            {
                3, 1, 4,
                -2, 2, 3,
                1, -2, -2
            };
            double[] b = new double[3] { 7, 2, 7 };
            var A = new IvyFEM.Lapack.DoubleMatrix(a, 3, 3, false);
            double[] x;
            int xRow;
            int xCol;
            IvyFEM.Lapack.Functions.dgesv(out x, out xRow, out xCol,
                A.Buffer, A.RowLength, A.ColumnLength, b, b.Length, 1);

            string ret;
            string CRLF = System.Environment.NewLine;

            ret = "      3 -2  1      7" + CRLF +
                  " A =  1  2 -2  b = 2" + CRLF +
                  "      4  3 -2      7" + CRLF +
                  "Answer : 2, -1, -1" + CRLF;
            ret += CRLF;
            ret += CRLF;
            for (int i = 0; i < x.Length; i++)
            {
                ret += "x[" + i + "] = " + x[i] + CRLF;
            }
            System.Diagnostics.Debug.WriteLine(ret);
            AlertWindow.ShowDialog(ret);
        }

        public void EigenValueExample()
        {
            //      3  4 -5  3
            //      0  1  8  0
            // X =  0  0  2 -1
            //      0  0  0  1
            double[] x = new double[16]
            {
                3, 0, 0, 0,
                4, 1, 0, 0,
                -5, 8, 2, 0,
                3, 0, -1, 1
            };
            var X = new IvyFEM.Lapack.DoubleMatrix(x, 4, 4, false);
            System.Numerics.Complex[] eVals;
            System.Numerics.Complex[][] eVecs;
            IvyFEM.Lapack.Functions.dgeev(X.Buffer, X.RowLength, X.ColumnLength,
                out eVals, out eVecs);

            string ret;
            string CRLF = System.Environment.NewLine;

            ret = "      3  4 -5  3" + CRLF +
                  "      0  1  8  0" + CRLF +
                  " X =  0  0  2 -1" + CRLF +
                  "      0  0  0  1" + CRLF;
            ret += "Eigen Value : 3, 2, 1, 1" + CRLF;
            ret += CRLF;
            ret += CRLF;
            for (int i = 0; i < eVals.Length; i++)
            {
                ret += "eVal[" + i + "] = " + eVals[i] + CRLF;
            }
            System.Diagnostics.Debug.WriteLine(ret);
            AlertWindow.ShowDialog(ret);
        }


        public void LisExample()
        {
            int ret;
            int comm = IvyFEM.Lis.Constants.LisCommWorld;
            int n = 12;
            int gn = 0;
            int @is = 0;
            int ie = 0;

            using (IvyFEM.Lis.LisInitializer LisInitializer = new IvyFEM.Lis.LisInitializer())
            using (var A = new IvyFEM.Lis.LisMatrix(comm))
            using (var b = new IvyFEM.Lis.LisVector(comm))
            using (var u = new IvyFEM.Lis.LisVector(comm))
            using (var x = new IvyFEM.Lis.LisVector(comm))
            using (var solver = new IvyFEM.Lis.LisSolver())
            {
                ret = A.SetSize(0, n);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = A.GetSize(out n, out gn);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = A.GetRange(out @is, out ie);
                System.Diagnostics.Debug.Assert(ret == 0);

                for (int i = @is; i < ie; i++)
                {
                    if (i > 0)
                    {
                        ret = A.SetValue(IvyFEM.Lis.SetValueFlag.LisInsValue, i, i - 1, -1.0);
                        System.Diagnostics.Debug.Assert(ret == 0);
                    }
                    if (i < gn - 1)
                    {
                        ret = A.SetValue(IvyFEM.Lis.SetValueFlag.LisInsValue, i, i + 1, -1.0);
                        System.Diagnostics.Debug.Assert(ret == 0);
                    }
                    ret = A.SetValue(IvyFEM.Lis.SetValueFlag.LisInsValue, i, i, 2.0);
                    System.Diagnostics.Debug.Assert(ret == 0);
                }
                ret = A.SetType(IvyFEM.Lis.MatrixType.LisMatrixCSR);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = A.Assemble();
                System.Diagnostics.Debug.Assert(ret == 0);

                ret = u.SetSize(0, n);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = b.SetSize(0, n);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = x.SetSize(0, n);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = u.SetAll(1.0);
                System.Diagnostics.Debug.Assert(ret == 0);

                ret = IvyFEM.Lis.LisMatrix.Matvec(A, u, b);
                System.Diagnostics.Debug.Assert(ret == 0);

                ret = solver.SetOption("-print mem");
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = solver.SetOptionC();
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = solver.Solve(A, b, x);
                System.Diagnostics.Debug.Assert(ret == 0);
                int iter;
                ret = solver.GetIter(out iter);

                string str = "";
                string CRLF = System.Environment.NewLine;
                str += "number of iterations = " + iter + CRLF;
                {
                    System.Numerics.Complex[] values = new System.Numerics.Complex[n];
                    ret = x.GetValues(0, n, values);
                    System.Diagnostics.Debug.Assert(ret == 0);
                    for (int i = 0; i < n; i++)
                    {
                        str += i + "  " + values[i] + CRLF;
                    }
                }
                System.Diagnostics.Debug.WriteLine(str);
                AlertWindow.ShowDialog(str);
            }

            using (IvyFEM.Lis.LisInitializer LisInitializer = new IvyFEM.Lis.LisInitializer())
            using (var v = new IvyFEM.Lis.LisVector(comm))
            {
                int n1 = 5;
                ret = v.SetSize(0, n1);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = v.SetAll(new System.Numerics.Complex(1.0, 1.0));
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = v.Conjugate();
                System.Diagnostics.Debug.Assert(ret == 0);
                System.Numerics.Complex[] values = new System.Numerics.Complex[n1];
                ret = v.GetValues(0, n1, values);
                System.Diagnostics.Debug.Assert(ret == 0);
                string str = "";
                string CRLF = System.Environment.NewLine;
                str += "Conjugate of (1, 1)" + CRLF;
                for (int i = 0; i < n1; i++)
                {
                    str += i + "  " + values[i] + CRLF;
                }
                System.Diagnostics.Debug.WriteLine(str);
                AlertWindow.ShowDialog(str);
            }
        }
    }
}
