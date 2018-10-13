using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    interface Constraint
    {
        double GetValue(double[] x);
        double GetDerivation(int iDof, double[] x);
        double Get2ndDerivation(int iDof, int jDof, double[] x);
    }
}
