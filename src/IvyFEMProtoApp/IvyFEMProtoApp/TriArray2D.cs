using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class TriArray2D
    {
        public uint Id { get; set; } = 0;
        public uint LCadId { get; set; } = 0;
        public int Layer { get; set; } = 0;
        public IList<Tri2D> Tris { get; set; } = new List<Tri2D>();
    }

}
