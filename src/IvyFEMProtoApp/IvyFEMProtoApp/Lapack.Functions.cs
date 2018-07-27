﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Lapack
{
    class Functions
    {
        ////////////////////////////////////////////////////////////////
        // BLAS
        ////////////////////////////////////////////////////////////////
        public static double ddot(double[] X, double[] Y)
        {
            if (X.Length != Y.Length)
            {
                throw new ArgumentException("Mismatched size: v1(" + X.Length + "), v2(" + Y.Length + ")");
            }

            int n = X.Length;
            int incX = 1;
            int incY = 1;

            double ret = 0;
            unsafe
            {
                ret = IvyFEM.Lapack.ImportedFunctions.ddot_(&n, X, &incX, Y, &incY);
            }
            return ret;
        }

        public static int dgemmAB(out double[] C, out int cRow, out int cCol,
            double[]  A, int aRow, int aCol,
            double[]  B, int bRow, int bCol)
        {
            if (aCol != bRow)
            {
                throw new ArgumentException("Mismatched size: aCol != bRow(" + aCol + " != " + bRow + ")");
            }

            cRow = aRow;
            cCol = bCol;

            C = new double[cRow * cCol];

            byte transa = Trans.Nop;
            byte transb = Trans.Nop;
            int m = aRow;
            int n = bCol;
            int k = aCol;

            double alpha = 1.0;

            int lda = aRow;
            int ldb = bRow;

            double beta = 0.0;
            int ldc = aRow;

            unsafe
            {
                IvyFEM.Lapack.ImportedFunctions.dgemm_(
                    &transa, &transb,
                    &m, &n, &k,
                    &alpha, A, &lda,
                    B, &ldb,
                    &beta, C, &ldc);
            }
            return 0;
        }

        public static double dnrm2(double[] X)
        {
            int n = X.Length;
            int incX = 1;

            double ret = 0;
            unsafe
            {
                ret = IvyFEM.Lapack.ImportedFunctions.dnrm2_(&n, X, &incX);
            }
            return ret;
        }

        public static void dscal(double[] X, double a)
        {
            int n = X.Length;
            int incX = 1;

            unsafe
            {
                IvyFEM.Lapack.ImportedFunctions.dscal_(&n, &a, X, &incX);
            }
        }

        public static Complex zdotc(Complex[] X, Complex[] Y)
        {
            if (X.Length != Y.Length)
            {
                throw new ArgumentException("Mismatched size: v1(" + X.Length + "), v2(" + Y.Length + ")");
            }

            int n = X.Length;
            int incX = 1;
            int incY = 1;

            Complex ret = (Complex)0;
            unsafe
            {
                fixed (Complex* XP = &X[0])
                fixed (Complex* YP = &Y[0])
                {
                    ret = IvyFEM.Lapack.ImportedFunctions.zdotc_(&n, XP, &incX, YP, &incY);
                }
            }
            return ret;
        }

        public static Complex zdotu(Complex[] X, Complex[] Y)
        {
            if (X.Length != Y.Length)
            {
                throw new ArgumentException("Mismatched size: v1(" + X.Length + "), v2(" + Y.Length + ")");
            }

            int n = X.Length;
            int incX = 1;
            int incY = 1;

            Complex ret = (Complex)0;
            unsafe
            {
                fixed (Complex* XP = &X[0])
                fixed (Complex* YP = &Y[0])
                {
                    ret = IvyFEM.Lapack.ImportedFunctions.zdotu_(&n, XP, &incX, YP, &incY);
                }
            }
            return ret;
        }

        public static int zgemmAB(out Complex[] C, out int cRow, out int cCol,
            Complex[] A, int aRow, int aCol,
            Complex[] B, int bRow, int bCol)
        {
            if (aCol != bRow)
            {
                throw new ArgumentException("Mismatched size: aCol != bRow(" + aCol + " != " + bRow + ")");
            }

            cRow = aRow;
            cCol = bCol;

            C = new Complex[cRow * cCol];

            byte transa = Trans.Nop;
            byte transb = Trans.Nop;
            int m = aRow;
            int n = bCol;
            int k = aCol;

            Complex alpha = (Complex)1.0;

            int lda = aRow;
            int ldb = bRow;

            Complex beta = (Complex)0.0;
            int ldc = aRow;

            unsafe
            {
                fixed (Complex* AP = &A[0])
                fixed (Complex* BP = &B[0])
                fixed (Complex* CP = &C[0])
                {
                    IvyFEM.Lapack.ImportedFunctions.zgemm_(
                        &transa, &transb,
                        &m, &n, &k,
                        &alpha, AP, &lda,
                        BP, &ldb,
                        &beta, CP, &ldc);
                }
            }
            return 0;
        }

        public static void zscal(Complex[] X, Complex a)
        {
            int n = X.Length;
            int incX = 1;

            unsafe
            {
                fixed (Complex* XP = &X[0])
                {
                    IvyFEM.Lapack.ImportedFunctions.zscal_(&n, &a, XP, &incX);
                }
            }
        }

        ////////////////////////////////////////////////////////////////
        // LAPACK
        ////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////
        // LAPACKE
        ////////////////////////////////////////////////////////////////
        public static int dgeev(double[] A, int xRow, int xCol,
            out Complex[] eVals, out Complex[][] eVecs)
        {
            byte jobvl = Job.DontCompute;
            byte jobvr = Job.Compute;
            int n = xCol;
            int lda = n;

            double[] wr = new double[n];
            double[] wi = new double[n];

            int ldvl = 1;
            double[] vl = null;

            int ldvr = n;
            double[] vr = new double[ldvr * n];

            int ret = IvyFEM.Lapack.ImportedFunctions.LAPACKE_dgeev(
                MatrixLayout.ColMajor,
                jobvl, jobvr,
                n, A, lda,
                wr, wi,
                vl, ldvl,
                vr, ldvr);

            if (ret != 0)
            {
                throw new InvalidOperationException("Error occurred: ret = " + ret);
                return ret;
            }

            // 固有値を格納
            eVals = new Complex[n];
            for (int i = 0; i < n; i++)
            {
                eVals[i].Real = wr[i];
                eVals[i].Imaginary = wi[i];
            }

            // 固有ベクトルを格納
            eVecs = new Complex[n][];
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(wi[i]) < Constants.PrecisionLowerLimit)
                {
                    // 実数の固有ベクトル
                    eVecs[i] = new Complex[ldvr];
                    for (int j = 0; j < ldvr; j++)
                    {
                        eVecs[i][j].Real = vr[i * ldvr + j];
                        eVecs[i][j].Imaginary = 0.0;
                    }
                }
                else
                {
                    // 複素数（複素共役）の固有ベクトル
                    var vec1 = new Complex[ldvr];
                    var vec2 = new Complex[ldvr];

                    for (int j = 0; j < ldvr; j++)
                    {
                        vec1[j].Real = vr[i * ldvr + j];
                        vec2[j].Real = vr[i * ldvr + j];
                        vec1[j].Imaginary = vr[(i + 1) * ldvr + j];
                        vec2[j].Imaginary = -vr[(i + 1) * ldvr + j];
                    }
                    eVecs[i] = vec1;
                    eVecs[i + 1] = vec2;

                    // 2列参照するので
                    i++;
                }
            }

            return ret;
        }

        public static int dgesv(out double[] X, out int xRow, out int xCol,
            double[] A, int aRow, int aCol,
            double[] B, int bRow, int bCol)
        {
            X = null;
            xRow = 0;
            xCol = 0;

            int n = aRow;
            int nrhs = bCol;
            int lda = n;
            int[] ipiv = new int[n];
            int ldb = bRow;

            // LAPACKE
            int ret = IvyFEM.Lapack.ImportedFunctions.LAPACKE_dgesv(
                MatrixLayout.ColMajor, n, nrhs, A, lda, ipiv, B, ldb);
            /*
            // LAPACK
            int ret = 0;
            unsafe
            {
                IvyFEM.Lapack.ImportedFunctions.dgesv_(
                    &n, &nrhs,
                    A, &lda, ipiv,
                    B, &ldb,
                    &ret);
            }
            */
            if (ret != 0)
            {
                X = null;
                xRow = 0;
                xCol = 0;
                throw new InvalidOperationException("Error occurred: ret = " + ret);
                return ret;
            }

            X = B;
            xRow = bRow;
            xCol = bCol;
            return ret;
        }

        public static int dggev(double[] A, int aRow, int aCol,
            double[] B, int bRow, int bCol,
            out Complex[] eVals, out Complex[][] eVecs)
        {
            byte jobvl = Job.DontCompute;
            byte jobvr = Job.Compute;
            int n = aCol;
            int lda = n;
            int ldb = n;

            // 計算された固有値の実部の分子部分が入る．
            double[] alphar = new double[n];
            // 計算された固有値の虚部の分子部分が入る．
            // 複素共役対の場合は，alphai[j]=(正値)，alphai[j+1]=(負値) の順に入る．
            double[] alphai = new double[n];
            // 計算された固有値の分母部分が入る
            //  (alphar[j] + alphai[j] * i) / beta[j] (i:虚数単位)が一般化固有値となる
            double[] beta = new double[n];

            int ldvl = 1;
            double[] vl = null;

            int ldvr = n;
            double[] vr = new double[ldvr * n];

            int ret = IvyFEM.Lapack.ImportedFunctions.LAPACKE_dggev(
                MatrixLayout.ColMajor, jobvl, jobvr,
                n, A, lda, B, ldb,
                alphar, alphai, beta, vl, ldvl, vr, ldvr);
            if (ret != 0)
            {
                throw new InvalidOperationException("Error occurred: ret = " + ret);
                return ret;
            }

            eVals = new Complex[n];
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(beta[i]) < Constants.PrecisionLowerLimit)
                {
                    eVals[i].Real = ((Math.Abs(alphar[i]) < Constants.PrecisionLowerLimit) ?
                        (double.NaN)
                        : ((Math.Abs(alphar[i]) > 0) ? (double.PositiveInfinity) : (double.NegativeInfinity)));
                    eVals[i].Imaginary = ((Math.Abs(alphai[i]) < Constants.PrecisionLowerLimit) ?
                        (double.NaN)
                        : ((Math.Abs(alphai[i]) > 0) ? (double.PositiveInfinity) : (double.NegativeInfinity)));
                }
                else
                {
                    eVals[i].Real = ((Math.Abs(alphar[i]) < Constants.PrecisionLowerLimit) ?
                        (0.0) : (alphar[i] / beta[i]));
                    eVals[i].Imaginary = ((Math.Abs(alphai[i]) < Constants.PrecisionLowerLimit) ?
                        (0.0) : (alphai[i] / beta[i]));
                }
            }

            eVecs = new Complex[n][];
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(alphai[i]) < Constants.PrecisionLowerLimit)
                {
                    // 実数の固有ベクトル
                    eVecs[i] = new Complex[ldvr];

                    for (int j = 0; j < ldvr; ++j)
                    {
                        eVecs[i][j].Real = vr[i * ldvr + j];
                        eVecs[i][j].Imaginary = 0.0;
                    }
                }
                else
                {
                    // 複素数（複素共役）の固有ベクトル
                    var vec1 = new Complex[ldvr];
                    var vec2 = new Complex[ldvr];

                    for (int j = 0; j < ldvr; j++)
                    {
                        vec1[j].Real = vr[i * ldvr + j];
                        vec2[j].Real = vr[i * ldvr + j];
                        vec1[j].Imaginary = vr[(i + 1) * ldvr + j];
                        vec2[j].Imaginary = -vr[(i + 1) * ldvr + j];
                    }
                    eVecs[i] = vec1;
                    eVecs[i + 1] = vec2;

                    i++;
                }
            }

            return ret;
        }

        public static int zgesv(out Complex[] X, out int xRow, out int xCol,
                         Complex[] A, int aRow, int aCol,
                         Complex[] B, int bRow, int bCol)
        {
            int n = aRow;
            int nrhs = bCol;
            int lda = n;
            int[] ipiv = new int[n];
            int ldb = bRow;

            int ret = 0;
            unsafe
            {
                fixed (Complex* AP = &A[0])
                fixed (Complex* BP = &B[0])
                {
                    ret = IvyFEM.Lapack.ImportedFunctions.LAPACKE_zgesv(
                        MatrixLayout.ColMajor,
                        n, nrhs, AP, lda, ipiv, BP, ldb);
                }
            }

            if (ret != 0)
            {
                X = null;
                xRow = 0;
                xCol = 0;
                throw new InvalidOperationException("Error occurred: ret = " + ret);
                return ret;
            }

            X = B;
            xRow = bRow;
            xCol = bCol;

            return ret;
        }

        public static void zlacgv(Complex[] X)
        {
            int n = X.Length;
            int incX = 1;

            unsafe
            {
                fixed (Complex* XP = &X[0])
                {
                    // LAPACK
                    //IvyFEM.Lapack.ImportedFunctions.zlacgv_(&n, XP, &incX);

                    // LAPACKE
                    IvyFEM.Lapack.ImportedFunctions.LAPACKE_zlacgv(n, XP, incX);
                }
            }
        }
    }
}