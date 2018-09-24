using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Linear
{
    class Utils
    {
        public static bool DoubleMatrixIsSymmetric(double[,] A)
        {
            bool isSymmetric = true;
            System.Diagnostics.Debug.Assert(A.GetLength(0) == A.GetLength(1));
            int n = A.GetLength(0);
            for (int row = 0; row < n; row++)
            {
                for (int col = 0; col < row; col++)
                {
                    double diff = A[row, col] - A[col, row];
                    if (Math.Abs(diff) >= IvyFEM.Constants.PrecisionLowerLimit)
                    {
                        isSymmetric = false;
                        break;
                    }
                }
                if (!isSymmetric)
                {
                    break;
                }
            }
            return isSymmetric;
        }
    }
}
