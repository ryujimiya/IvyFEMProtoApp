using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Linear
{
    class LisEquationSolver : IEquationSolver
    {
        public bool DoubleSolve(out double[] X, DoubleSparseMatrix A, double[] B)
        {
            bool success = false;
            using (IvyFEM.Lis.LisMatrix lisA = (IvyFEM.Lis.LisMatrix)A)
            using (IvyFEM.Lis.LisVector lisB = (IvyFEM.Lis.LisVector)B)
            using (IvyFEM.Lis.LisVector lisX = new IvyFEM.Lis.LisVector())
            using (IvyFEM.Lis.LisSolver lisSolver = new IvyFEM.Lis.LisSolver())
            {
                int ret;
                int n = B.Length;
                lisX.SetSize(0, n);
                A = null;
                ret = lisSolver.SetOption("-print mem");
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = lisSolver.SetOptionC();
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = lisSolver.Solve(lisA, lisB, lisX);
                System.Diagnostics.Debug.Assert(ret == 0);
                success = (ret == 0);
                int iter;
                ret = lisSolver.GetIter(out iter);
                System.Diagnostics.Debug.Assert(ret == 0);
                System.Diagnostics.Debug.WriteLine("Lis Solve iter = " + iter);
                System.Numerics.Complex[] complexX = new System.Numerics.Complex[n];
                ret = lisX.GetValues(0, n, complexX);
                System.Diagnostics.Debug.Assert(ret == 0);
                X = new double[n];
                for (int i = 0; i < n; i++)
                {
                    X[i] = complexX[i].Real;
                }
            }
            return success;
        }

        public bool ComplexSolve(out Complex[] X, ComplexSparseMatrix A, Complex[] B)
        {
            bool success = false;
            using (IvyFEM.Lis.LisMatrix lisA = (IvyFEM.Lis.LisMatrix)A)
            using (IvyFEM.Lis.LisVector lisB = (IvyFEM.Lis.LisVector)B)
            using (IvyFEM.Lis.LisVector lisX = new IvyFEM.Lis.LisVector())
            using (IvyFEM.Lis.LisSolver lisSolver = new IvyFEM.Lis.LisSolver())
            {
                int ret;
                int n = B.Length;
                lisX.SetSize(0, n);
                A = null;
                ret = lisSolver.SetOption("-print mem");
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = lisSolver.SetOptionC();
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = lisSolver.Solve(lisA, lisB, lisX);
                System.Diagnostics.Debug.Assert(ret == 0);
                success = (ret == 0);
                int iter;
                ret = lisSolver.GetIter(out iter);
                System.Diagnostics.Debug.Assert(ret == 0);
                System.Diagnostics.Debug.WriteLine("Lis Solve iter = " + iter);
                X = new System.Numerics.Complex[n];
                ret = lisX.GetValues(0, n, X);
                System.Diagnostics.Debug.Assert(ret == 0);
            }
            return success;
        }
    }
}
