using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Linear
{
    class Functions
    {
        /////////////////////////////////////////////////////////////////////////////////
        // double

        // LU : Lii = 1 Lij (i=1...n -1 j=0...i-1) Uij (i=0...n-1 j=i...n-1)
        // Note: M = L * U
        public static DoubleSparseMatrix DoubleCalcILU(DoubleSparseMatrix A, int fillinLevel)
        {
            System.Diagnostics.Debug.Assert(A.RowLength == A.ColumnLength);
            int n = A.RowLength;

            DoubleSparseMatrix LU = new DoubleSparseMatrix(A);
            int[,] level = new int[n, n];
            for (int row = 0; row < n; row++)
            {
                for (int col = 0; col < n; col++)
                {
                    level[row, col] = (Math.Abs(A[row, col]) >= IvyFEM.Constants.PrecisionLowerLimit) ?
                        0 : (fillinLevel + 1);
                }
            }

            for (int i = 1; i < n; i++)
            {
                for (int k = 0; k <= (i - 1); k++)
                {
                    //if (!LU.RowColIndexValues[i].ContainsKey(k))
                    //{
                    //    continue;
                    //}
                    if (level[i, k] > fillinLevel)
                    {
                        continue;
                    }
                    LU[i, k] /= LU[k, k];
                    double LUik = LU[i, k];
                    foreach (var pair in LU.RowColIndexValues[k])
                    {
                        int j = pair.Key;
                        double LUkj = pair.Value;
                        if (j >= k + 1 && j < n)
                        {
                            //
                        }
                        else
                        {
                            continue;
                        }

                        level[i, j] = Math.Min(level[i, j], level[i, k] + level[k, j] + 1);
                        if (level[i, j] <= fillinLevel)
                        {
                            LU[i, j] -= LUik * LUkj;
                        }
                    }
                }
            }
            return LU;
        }

        public static void DoubleSolveLU(out double[] X, DoubleSparseMatrix LU, double[] B)
        {
            System.Diagnostics.Debug.Assert(LU.RowLength == LU.ColumnLength);
            int n = LU.RowLength;

            X = new double[n];
            B.CopyTo(X, 0);

            // Ly = b 
            for (int row = 1; row < n; row++)
            {
                foreach (var pair in LU.RowColIndexValues[row])
                {
                    int col = pair.Key;
                    double value = pair.Value;
                    if (col >= 0 && col < row)
                    {
                        X[row] -= value * X[col]; // LU[row, col]
                    }
                }
            }

            // Ux = y
            for (int row = n - 2; row >= 0; row--)
            {
                foreach (var pair in LU.RowColIndexValues[row])
                {
                    int col = pair.Key;
                    double value = pair.Value;
                    if (col >= row + 1 && col < n)
                    {
                        X[row] -= value * X[col]; // LU[row, col]

                    }
                }
                X[row] /= LU[row, row];
            }
        }

        public static bool DoubleSolvePreconditionedCG(
            out double[] X, DoubleSparseMatrix A, double[] B, DoubleSparseMatrix LU)
        {
            System.Diagnostics.Debug.Assert(A.RowLength == A.ColumnLength);
            int n = A.RowLength;
            System.Diagnostics.Debug.Assert(B.Length == n);
            System.Diagnostics.Debug.Assert(LU.RowLength == LU.ColumnLength);
            System.Diagnostics.Debug.Assert(LU.RowLength == n);
            double convRatio = IvyFEM.Linear.Constants.ConvRatioTolerance;
            double tolerance = convRatio;
            const int maxCnt = 1000;
            double[] r = new double[n];
            double[] x = new double[n];
            double[] z = new double[n];
            double[] p = new double[n];
            int iter = 0;

            B.CopyTo(r, 0);
            double sqInvNorm0;
            {
                double sqNorm0 = IvyFEM.Lapack.Functions.ddot(r, r);
                if (sqNorm0 < IvyFEM.Constants.PrecisionLowerLimit)
                {
                    convRatio = 0;
                    X = x;
                    System.Diagnostics.Debug.WriteLine("iter = " + iter + " norm: " + convRatio);
                    return true;
                }
                sqInvNorm0 = 1.0 / sqNorm0;
            }

            // 前処理あり
            IvyFEM.Linear.Functions.DoubleSolveLU(out z, LU, r);
            // 前処理なし
            //z = r;

            z.CopyTo(p, 0);
            double rz = IvyFEM.Lapack.Functions.ddot(r, z);

            for (iter = 0; iter < maxCnt; iter++)
            {
                double[] Ap = A * p;
                double alpha;
                {
                    double pAp = IvyFEM.Lapack.Functions.ddot(p, Ap);
                    alpha = rz / pAp;
                }
                r = IvyFEM.Lapack.Functions.daxpy(-alpha, Ap, r);
                x = IvyFEM.Lapack.Functions.daxpy(alpha, p, x);

                {
                    double sqNorm = IvyFEM.Lapack.Functions.ddot(r, r);
                    if (sqNorm * sqInvNorm0 < tolerance * tolerance)
                    {
                        convRatio = Math.Sqrt(sqNorm * sqInvNorm0);
                        X = x;
                        System.Diagnostics.Debug.WriteLine("iter = " + iter + " norm: " + convRatio);
                        return true;
                    }
                }

                // 前処理あり
                IvyFEM.Linear.Functions.DoubleSolveLU(out z, LU, r);
                // 前処理なし
                //z = r;

                double rzPrev = rz;
                rz = IvyFEM.Lapack.Functions.ddot(r, z);
                double beta = rz / rzPrev;

                p = IvyFEM.Lapack.Functions.daxpy(beta, p, z);
            }

            {
                double sqNormRes = IvyFEM.Lapack.Functions.ddot(r, r);
                convRatio = Math.Sqrt(sqNormRes * sqInvNorm0);
                System.Diagnostics.Debug.WriteLine("iter = " + iter + " norm: " + convRatio);
                X = x;
            }
            System.Diagnostics.Debug.WriteLine("Not converged");
            return false;
        }

        public static bool DoubleSolveCG(out double[] X, DoubleSparseMatrix A, double[] B, int fillinLevel)
        {
            int t;
            t = System.Environment.TickCount;
            DoubleSparseMatrix LU = IvyFEM.Linear.Functions.DoubleCalcILU(A, fillinLevel);
            System.Diagnostics.Debug.WriteLine("DoubleSolveCG 1: t= " + (System.Environment.TickCount - t));
            t = System.Environment.TickCount;
            bool success = IvyFEM.Linear.Functions.DoubleSolvePreconditionedCG(out X, A, B, LU);
            System.Diagnostics.Debug.WriteLine("DoubleSolveCG 2: t= " + (System.Environment.TickCount - t));
            return success;
        }

        /////////////////////////////////////////////////////////////////////////////////
        // complex

        // LU : Lii = 1 Lij (i=1...n -1 j=0...i-1) Uij (i=0...n-1 j=i...n-1)
        // Note: M = L * U
        public static ComplexSparseMatrix ComplexCalcILU(ComplexSparseMatrix A, int fillinLevel)
        {
            System.Diagnostics.Debug.Assert(A.RowLength == A.ColumnLength);
            int n = A.RowLength;

            ComplexSparseMatrix LU = new ComplexSparseMatrix(A);
            int[,] level = new int[n, n];
            for (int row = 0; row < n; row++)
            {
                for (int col = 0; col < n; col++)
                {
                    level[row, col] = (A[row, col].Magnitude >= IvyFEM.Constants.PrecisionLowerLimit) ?
                        0 : (fillinLevel + 1);
                }
            }

            for (int i = 1; i < n; i++)
            {
                for (int k = 0; k <= (i - 1); k++)
                {
                    //if (!LU.RowColIndexValues[i].ContainsKey(k))
                    //{
                    //    continue;
                    //}
                    if (level[i, k] > fillinLevel)
                    {
                        continue;
                    }
                    LU[i, k] /= LU[k, k];
                    System.Numerics.Complex LUik = LU[i, k];
                    foreach (var pair in LU.RowColIndexValues[k])
                    {
                        int j = pair.Key;
                        System.Numerics.Complex LUkj = pair.Value;
                        if (j >= k + 1 && j < n)
                        {
                            //
                        }
                        else
                        {
                            continue;
                        }

                        level[i, j] = Math.Min(level[i, j], level[i, k] + level[k, j] + 1);
                        if (level[i, j] <= fillinLevel)
                        {
                            LU[i, j] -= LUik * LUkj;
                        }
                    }
                }
            }
            return LU;
        }

        public static void ComplexSolveLU(
            out System.Numerics.Complex[] X, ComplexSparseMatrix LU, System.Numerics.Complex[] B)
        {
            System.Diagnostics.Debug.Assert(LU.RowLength == LU.ColumnLength);
            int n = LU.RowLength;

            X = new System.Numerics.Complex[n];
            B.CopyTo(X, 0);

            // Ly = b 
            for (int row = 1; row < n; row++)
            {
                foreach (var pair in LU.RowColIndexValues[row])
                {
                    int col = pair.Key;
                    System.Numerics.Complex value = pair.Value;
                    if (col >= 0 && col < row)
                    {
                        X[row] -= value * X[col]; // LU[row, col]
                    }
                }
            }

            // Ux = y
            for (int row = n - 2; row >= 0; row--)
            {
                foreach (var pair in LU.RowColIndexValues[row])
                {
                    int col = pair.Key;
                    System.Numerics.Complex value = pair.Value;
                    if (col >= row + 1 && col < n)
                    {
                        X[row] -= value * X[col]; // LU[row, col]

                    }
                }
                X[row] /= LU[row, row];
            }
        }

        public static bool ComplexSolvePreconditionedCOCG(
            out System.Numerics.Complex[] X,
            ComplexSparseMatrix A, System.Numerics.Complex[] B, ComplexSparseMatrix LU)
        {
            System.Diagnostics.Debug.Assert(A.RowLength == A.ColumnLength);
            int n = A.RowLength;
            System.Diagnostics.Debug.Assert(B.Length == n);
            System.Diagnostics.Debug.Assert(LU.RowLength == LU.ColumnLength);
            System.Diagnostics.Debug.Assert(LU.RowLength == n);
            double convRatio = IvyFEM.Constants.PrecisionLowerLimit;
            double convRatioTol = convRatio;
            const int maxCnt = 1000;
            System.Numerics.Complex[] r = new System.Numerics.Complex[n];
            System.Numerics.Complex[] x = new System.Numerics.Complex[n];
            System.Numerics.Complex[] z = new System.Numerics.Complex[n];
            System.Numerics.Complex[] p = new System.Numerics.Complex[n];
            int iter = 0;

            B.CopyTo(r, 0);
            double sqInvNorm0;
            {
                double sqNorm0 = IvyFEM.Lapack.Functions.zdotc(r, r).Real;
                if (sqNorm0 < IvyFEM.Constants.PrecisionLowerLimit)
                {
                    convRatio = 0;
                    X = x;
                    System.Diagnostics.Debug.WriteLine("iter = " + iter + " norm: " + convRatio);
                    return true;
                }
                sqInvNorm0 = 1.0 / sqNorm0;
            }

            // 前処理あり
            IvyFEM.Linear.Functions.ComplexSolveLU(out z, LU, r);
            // 前処理なし
            //z = r;

            z.CopyTo(p, 0);
            System.Numerics.Complex rz = IvyFEM.Lapack.Functions.zdotu(r, z);

            for (iter = 0; iter < maxCnt; iter++)
            {
                System.Numerics.Complex[] Ap = A * p;
                System.Numerics.Complex alpha;
                {
                    System.Numerics.Complex pAp = IvyFEM.Lapack.Functions.zdotu(p, Ap);
                    alpha = rz / pAp;
                }
                r = IvyFEM.Lapack.Functions.zaxpy(-alpha, Ap, r);
                x = IvyFEM.Lapack.Functions.zaxpy(alpha, p, x);

                {
                    double sqNorm = IvyFEM.Lapack.Functions.zdotc(r, r).Real;
                    if (sqNorm * sqInvNorm0 < convRatioTol * convRatioTol)
                    {
                        convRatio = Math.Sqrt(sqNorm * sqInvNorm0);
                        X = x;
                        System.Diagnostics.Debug.WriteLine("iter = " + iter + " norm: " + convRatio);
                        return true;
                    }
                }

                // 前処理あり
                IvyFEM.Linear.Functions.ComplexSolveLU(out z, LU, r);
                // 前処理なし
                //z = r;

                System.Numerics.Complex rzPrev = rz;
                rz = IvyFEM.Lapack.Functions.zdotu(r, z);
                System.Numerics.Complex beta = rz / rzPrev;

                p = IvyFEM.Lapack.Functions.zaxpy(beta, p, z);
            }

            {
                double sqNormRes = IvyFEM.Lapack.Functions.zdotc(r, r).Real;
                convRatio = Math.Sqrt(sqNormRes * sqInvNorm0);
                System.Diagnostics.Debug.WriteLine("iter = " + iter + " norm: " + convRatio);
                X = x;
            }
            System.Diagnostics.Debug.WriteLine("Not converged");
            return false;
        }

        public static bool ComplexSolveCOCG(
            out System.Numerics.Complex[] X, ComplexSparseMatrix A, System.Numerics.Complex[] B, int fillinLevel)
        {
            int t;
            t = System.Environment.TickCount;
            ComplexSparseMatrix LU = IvyFEM.Linear.Functions.ComplexCalcILU(A, fillinLevel);
            System.Diagnostics.Debug.WriteLine("ComplexSolveCOCG 1: t= " + (System.Environment.TickCount - t));
            t = System.Environment.TickCount;
            bool success = IvyFEM.Linear.Functions.ComplexSolvePreconditionedCOCG(out X, A, B, LU);
            System.Diagnostics.Debug.WriteLine("ComplexSolveCOCG 2: t= " + (System.Environment.TickCount - t));
            return success;
        }

    }
}
