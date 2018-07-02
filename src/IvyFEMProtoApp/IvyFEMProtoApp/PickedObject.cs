using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class PickedObject
    {
        public uint NameDepth { get; set; }
        public int[] Name { get; set; } = new int[4];
        public double MinDepth { get; set; }
        public double MaxDepth { get; set; }
    }

}
