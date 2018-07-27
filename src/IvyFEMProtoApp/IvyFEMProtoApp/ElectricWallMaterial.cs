using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class ElectricWallMaterial : Material
    {
        public ElectricWallMaterial()
        {
            MaterialType = MaterialType.ELECTRIC_WALL;
        }
    }
}
