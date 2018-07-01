using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class QuadArray2D
    {
        public uint Id { get; set; } = 0;
        public uint LCadId { get; set; } = 0;
        public int Layer { get; set; } = 0;
        public IList<Quad2D> Quads { get; } = new List<Quad2D>();
    }
}
