using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    abstract class FEM
    {
        public FEWorld World { get; set; } = null;

        public abstract void Solve();
    }
}
