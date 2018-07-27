using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Lapack
{
    class MatrixLayout
    {
        public const int RowMajor = 101;
        public const int ColMajor = 102;
    }

    class Job
    {
        public static byte DontCompute = Convert.ToByte('N');
        public static byte Compute = Convert.ToByte('V');
    }

    class Trans
    {
        public static byte Nop = Convert.ToByte('N');
        public static byte Transpose = Convert.ToByte('T');
        public static byte ComplexTranspose = Convert.ToByte('C');
    }
}
