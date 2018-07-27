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
        public static extern unsafe Complex zdotc_(int* n, Complex* zx, int* incx, Complex* zy, int* incy);

        // X^T * Y
        [DllImport("libblas.dll")]
        public static extern unsafe Complex zdotu_(int* n, Complex* zx, int* incx, Complex* zy, int* incy);

        [DllImport("libblas.dll")]
        public static extern unsafe void zgemm_(
            byte* transa, byte* transb,
            int* m, int* n, int* k,
            Complex* alpha, Complex* a, int* lda,
            Complex* b, int* ldb,
            Complex* beta, Complex* c, int* ldc);

        [DllImport("libblas.dll")]
        public static extern unsafe void zscal_(int* n, Complex* za, Complex* zx, int* incx);


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
        public static extern unsafe void zlacgv_(int* n, Complex* x, int* incx);

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
            Complex* a, int lda, int[] ipiv,
            Complex* b, int ldb);

        [DllImport("liblapacke.dll")]
        public static extern unsafe int LAPACKE_zlacgv(int n, Complex* x, int incx);

    }
}
