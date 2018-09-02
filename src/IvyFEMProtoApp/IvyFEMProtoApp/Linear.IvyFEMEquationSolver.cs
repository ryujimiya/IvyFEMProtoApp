using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Linear
{
    class IvyFEMEquationSolver : IEquationSolver
    {
        public IvyFEMEquationSolverMethod Method { get; set; } = IvyFEMEquationSolverMethod.Default;
        public int ILUFillinLevel { get; set; } = 1;

        public bool DoubleSolve(out double[] X, DoubleSparseMatrix A, double[] B)
        {
            bool success = false;
            X = null;

            switch (Method)
            {
                case IvyFEMEquationSolverMethod.Default:
                case IvyFEMEquationSolverMethod.CG:
                    success = DoubleSolveCG(out X, A, B);
                    break;

                case IvyFEMEquationSolverMethod.COCG:
                    // 複素数のみ
                    throw new NotImplementedException();
                    //break;

                default:
                    throw new NotImplementedException();
                    //break;
            }

            return success;
        }

        public bool ComplexSolve(
            out System.Numerics.Complex[] X, ComplexSparseMatrix A, System.Numerics.Complex[] B)
        {
            bool success = false;
            X = null;

            switch (Method)
            {
                case IvyFEMEquationSolverMethod.CG:
                    // エルミート行列の場合はCG
                    // 必要なケースがでてくれば実装する
                    throw new NotImplementedException();
                    //break;

                case IvyFEMEquationSolverMethod.Default:
                case IvyFEMEquationSolverMethod.COCG:
                    // 複素対称行列の場合はCOCG
                    success = ComplexSolveCOCG(out X, A, B);
                    break;

                default:
                    throw new NotImplementedException();
                    //break;
            }

            return success;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // double
        private bool DoubleSolveCG(out double[] X, DoubleSparseMatrix A, double[] B)
        {
            int t;
            t = System.Environment.TickCount;
            System.Diagnostics.Debug.Assert(A.IsSymmetric());
            System.Diagnostics.Debug.WriteLine("  IsSymmetric t = " + (System.Environment.TickCount - t));

            //t = System.Environment.TickCount;
            //bool success = IvyFEM.Linear.Functions.DoubleSolveCG(out X, A, B, ILUFillinLevel);
            //System.Diagnostics.Debug.Assert(success);
            //System.Diagnostics.Debug.WriteLine("  DoubleSolveCG t = " + (System.Environment.TickCount - t));

            // Nativeを使う
            bool success = false;
            {
                System.Diagnostics.Debug.Assert(A.RowLength == A.ColumnLength);
                int n = A.RowLength;
                int[] APtrs;
                int[] AIndexs;
                double[] AValues;
                t = System.Environment.TickCount;
                A.GetCSR(out APtrs, out AIndexs, out AValues);
                System.Diagnostics.Debug.WriteLine("  GetCSR t = " + (System.Environment.TickCount - t));
                t = System.Environment.TickCount;
                success = IvyFEM.Native.Functions.DoubleSolveCG(
                    out X, n, APtrs, AIndexs, AValues, B, ILUFillinLevel);
                System.Diagnostics.Debug.Assert(success);
                System.Diagnostics.Debug.WriteLine("  DoubleSolveCG t = " + (System.Environment.TickCount - t));
            }

            return success;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // complex
        private bool ComplexSolveCOCG(
            out System.Numerics.Complex[] X, ComplexSparseMatrix A, System.Numerics.Complex[] B)
        {
            int t;
            t = System.Environment.TickCount;
            System.Diagnostics.Debug.Assert(A.IsSymmetric());
            System.Diagnostics.Debug.WriteLine("  IsSymmetric t = " + (System.Environment.TickCount - t));

            //t = System.Environment.TickCount;
            //bool success = IvyFEM.Linear.Functions.ComplexSolveCOCG(out X, A, B, ILUFillinLevel);
            //System.Diagnostics.Debug.Assert(success);
            //System.Diagnostics.Debug.WriteLine("  ComplexSolveCOCG t = " + (System.Environment.TickCount - t));

            // Nativeを使う
            bool success = false;
            {
                System.Diagnostics.Debug.Assert(A.RowLength == A.ColumnLength);
                int n = A.RowLength;
                int[] APtrs;
                int[] AIndexs;
                System.Numerics.Complex[] AValues;
                t = System.Environment.TickCount;
                A.GetCSR(out APtrs, out AIndexs, out AValues);
                System.Diagnostics.Debug.WriteLine("  GetCSR t = " + (System.Environment.TickCount - t));
                t = System.Environment.TickCount;
                success = IvyFEM.Native.Functions.ComplexSolveCOCG(
                    out X, n, APtrs, AIndexs, AValues, B, ILUFillinLevel);
                System.Diagnostics.Debug.Assert(success);
                System.Diagnostics.Debug.WriteLine("  ComplexSolveCOCG t = " + (System.Environment.TickCount - t));
            }

            return success;
        }
    }
}
