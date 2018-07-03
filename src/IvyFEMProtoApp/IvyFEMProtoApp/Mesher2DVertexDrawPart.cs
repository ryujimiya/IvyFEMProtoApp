using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class Mesher2DVertexDrawPart
    {
        public uint VId { get; set; } = 0;
        public uint CadId { get; set; } = 0;
        public uint MshId { get; set; } = 0;
        public bool IsSelected { get; set; } = false;
        public double Height { get; set; } = 0;
    }

}
