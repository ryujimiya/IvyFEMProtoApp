using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    interface IFieldDrawer : IDrawer 
    {
        bool Update(FEWorld world);
    }
}
