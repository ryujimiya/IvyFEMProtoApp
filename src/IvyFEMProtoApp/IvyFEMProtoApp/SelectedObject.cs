using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    class SelectedObject
    {
        public uint NameDepth { get; set; } = 0;
        public int[] Name { get; } = new int[4];
        public Vector3 PickedPos { get; set; } = new Vector3();
    }
}
