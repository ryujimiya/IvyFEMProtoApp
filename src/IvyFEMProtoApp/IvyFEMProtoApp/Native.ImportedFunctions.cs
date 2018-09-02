using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices; // DllImport

namespace IvyFEM.Native
{
    class ImportedFunctions
    {
        ///////////////////////////////////////////////////////////////////
        // DoubleLinear

        [DllImport("IvyFEM.Native.dll")]
        public static extern double DoubleDot(int n, double[] X, double[] Y);

        [DllImport("IvyFEM.Native.dll")]
        public static extern void DoubleMV(double[] Z,
            double alpha, int n, int AIndexsLength, int[] APtrs, int[] AIndexs, double[] AValues, double[] X,
            double beta, double[] Y);

        [DllImport("IvyFEM.Native.dll")]
        public static extern void DoubleAxpy(double[] Z, double alpha, int n, double[] X, double[] Y);

        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe void DoubleDeleteCSR(int* APtrs, int* AIndexs, double* AValues);

        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe void DoubleCalcILU(
            int* LUIndexsLengthP, int** LUPtrsP, int** LUIndexsP, double** LUValuesP,
            int n, int AIndexsLength, int[] APtrs, int[] AIndexs, double[] AValues,
            int fillinLevel);

        [DllImport("IvyFEM.Native.dll")]
        public static extern void DoubleSolveLU(
            double[] X, int n, int LUIndexsLength, int[] LUPtrs, int[] LUIndexs, double[] LUValues, double[] B);

        [DllImport("IvyFEM.Native.dll")]
        public static extern bool DoubleSolvePreconditionedCG(double[] X,
            int n, int AIndexsLength, int[] APtrs, int[] AIndexs, double[] AValues, double[] B,
            int LUIndexsLength, int[] LUPtrs, int[] LUIndexs, double[] LUValues);

        [DllImport("IvyFEM.Native.dll")]
        public static extern bool DoubleSolveCG(double[] X,
            int n, int AIndexsLength, int[] APtrs, int[] AIndexs, double[] AValues, double[] B, int fillinLevel);

        ///////////////////////////////////////////////////////////////////
        // ComplexLinear

        // X^H * Y
        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe void ComplexDotc(
            System.Numerics.Complex* ret, int n, System.Numerics.Complex* X, System.Numerics.Complex* Y);

        // X^T * Y
        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe void ComplexDotu(
            System.Numerics.Complex* ret, int n, System.Numerics.Complex* X, System.Numerics.Complex* Y);

        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe void ComplexMV(System.Numerics.Complex* Z,
            System.Numerics.Complex alpha, int n, 
            int AIndexsLength, int[] APtrs, int[] AIndexs, System.Numerics.Complex* AValues,
            System.Numerics.Complex* X,
            System.Numerics.Complex beta, System.Numerics.Complex* Y);

        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe void ComplexAxpy(
            System.Numerics.Complex* Z, 
            System.Numerics.Complex alpha, int n, System.Numerics.Complex* X, System.Numerics.Complex* Y);

        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe void ComplexDeleteCSR(
            int* APtrs, int* AIndexs, System.Numerics.Complex* AValues);

        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe void ComplexCalcILU(
            int* LUIndexsLengthP, int** LUPtrsP, int** LUIndexsP, System.Numerics.Complex** LUValuesP,
            int n, int AIndexsLength, int[] APtrs, int[] AIndexs, System.Numerics.Complex* AValues,
            int fillinLevel);

        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe void ComplexSolveLU(
            System.Numerics.Complex* X,
            int n, int LUIndexsLength, int[] LUPtrs, int[] LUIndexs, System.Numerics.Complex* LUValues,
            System.Numerics.Complex* B);

        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe bool ComplexSolvePreconditionedCOCG(
            System.Numerics.Complex* X,
            int n, int AIndexsLength, int[] APtrs, int[] AIndexs, System.Numerics.Complex* AValues,
            System.Numerics.Complex* B,
            int LUIndexsLength, int[] LUPtrs, int[] LUIndexs, System.Numerics.Complex* LUValues);

        [DllImport("IvyFEM.Native.dll")]
        public static extern unsafe bool ComplexSolveCOCG(System.Numerics.Complex* X,
            int n, int AIndexsLength, int[] APtrs, int[] AIndexs, System.Numerics.Complex* AValues,
            System.Numerics.Complex* B, int fillinLevel);

    }
}
