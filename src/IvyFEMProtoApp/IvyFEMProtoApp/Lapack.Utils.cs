using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Lapack
{
    class Utils
    {
        public static System.Numerics.Complex[] Conjugate(System.Numerics.Complex[] A)
        {
            return IvyFEM.Lapack.Functions.zlacgv(A);
        }


    }
}
