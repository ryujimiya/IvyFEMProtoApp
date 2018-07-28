using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices; // DllImport

namespace IvyFEM.Lapack
{
    /// <summary>
    /// liblapacke.dll
    /// プロジェクトターゲットはAnyCPU でなくx64を指定する
    /// （AnyCPUを指定すると呼び出し時System.BadImageFormatExceptionが発生してしまう）
    /// </summary>
    class ImportedFunctions
    {
        ////////////////////////////////////////////////////////////////
        // BLAS
        ////////////////////////////////////////////////////////////////
        [DllImport("libblas.dll")]
        public static extern unsafe double ddot_(int* n, double[] dx, int* incx, double[] dy, int* incy);

        [DllImport("libblas.dll")]
        public static extern unsafe void dgemm_(
            byte* transa, byte* transb,
            int* m, int* n, int* k,
            double* alpha, double[] a, int* lda,
            double[] b, int* ldb,
            double* beta, double[] c, int* ldc);

        [DllImport("libblas.dll")]
        public static extern unsafe double dnrm2_(int* n, double[] x, int* incx);

        [DllImport("libblas.dll")]
        public static extern unsafe void dscal_(int* n, double* da, double[] dx, int* incx);

        // X^H * Y
        [DllImport("libblas.dll")]
        public static extern unsafe System.Numerics.Complex zdotc_(int* n, System.Numerics.Complex* zx, int* incx,
            System.Numerics.Complex* zy, int* incy);

        // X^T * Y
        [DllImport("libblas.dll")]
        public static extern unsafe System.Numerics.Complex zdotu_(int* n, System.Numerics.Complex* zx, int* incx,
            System.Numerics.Complex* zy, int* incy);

        [DllImport("libblas.dll")]
        public static extern unsafe void zgemm_(
            byte* transa, byte* transb,
            int* m, int* n, int* k,
            System.Numerics.Complex* alpha, System.Numerics.Complex* a, int* lda,
            System.Numerics.Complex* b, int* ldb,
            System.Numerics.Complex* beta, System.Numerics.Complex* c, int* ldc);

        [DllImport("libblas.dll")]
        public static extern unsafe void zscal_(int* n, System.Numerics.Complex* za,
            System.Numerics.Complex* zx, int* incx);


        ////////////////////////////////////////////////////////////////
        // LAPACK
        ////////////////////////////////////////////////////////////////
        [DllImport("liblapack.dll")]
        public static extern unsafe void dgesv_(
            int* n, int* nrhs,
            double[] a, int* lda, int[] ipiv,
            double[] b, int* ldb,
            int* info);

        [DllImport("liblapack.dll")]
        public static extern unsafe void zlacgv_(int* n, System.Numerics.Complex* x, int* incx);

        ////////////////////////////////////////////////////////////////
        // LAPACKE
        ////////////////////////////////////////////////////////////////
        [DllImport("liblapacke.dll")]
        public static extern int LAPACKE_dgeev(
            int matrix_layout,
            byte jobvl, byte jobvr,
            int n, double[] a, int lda,
            double[] wr, double[] wi,
            double[] vl, int ldvl,
            double[] vr, int ldvr);


        [DllImport("liblapacke.dll")]
        public static extern int LAPACKE_dgesv(
            int matrix_layout,
            int n, int nrhs,
            double[] a, int lda, int[] ipiv,
            double[] b, int ldb);

        [DllImport("liblapacke.dll")]
        public static extern int LAPACKE_dggev(
            int matrix_layout,
            byte jobvl, byte jobvr,
            int n, double[] a, int lda, 
            double[] b, int ldb,
            double[] alphar, double[] alphai, double[] beta,
            double[] vl, int ldvl,
            double[] vr, int ldvr);

        [DllImport("liblapacke.dll")]
        public static extern unsafe int LAPACKE_zgesv(
            int matrix_layout,
            int n, int nrhs,
            System.Numerics.Complex* a, int lda, int[] ipiv,
            System.Numerics.Complex* b, int ldb);

        [DllImport("liblapacke.dll")]
        public static extern unsafe int LAPACKE_zlacgv(int n, System.Numerics.Complex* x, int incx);

    }
}
