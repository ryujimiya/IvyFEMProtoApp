using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class VertexDrawPart
    {
        public uint VId { get; set; } = 0;
        public uint CadId { get; set; } = 0;
        public uint MshId { get; set; } = 0;

        public bool IsSelected { get; set; } = false;
        public bool IsShow { get; set; } = false;
        public float[] Color { get;} = new float[3];

        public double Height { get; set; } = 0;

        public VertexDrawPart()
        {

        }

    }
}
