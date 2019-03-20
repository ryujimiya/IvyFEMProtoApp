using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace IvyFEMProtoApp
{
    public class BackgroundDrawer : IDrawer
    {
        protected double Width  = 0;
        protected double Height = 0;
        protected double Z = 0;

        public RotMode SutableRotMode => RotMode.RotMode2D;

        public bool IsAntiAliasing { get => false; set => throw new NotImplementedException(); }

        public BackgroundDrawer(double w, double h)
        {
            Width = w;
            Height = h;
            Z = 0;
        }

        public void AddSelected(int[] selectFlag)
        {

        }

        public void ClearSelected()
        {

        }

        public virtual void Draw()
        {

        }

        public void DrawSelection(uint idraw)
        {

        }

        public BoundingBox3D GetBoundingBox(Matrix3d rot)
        {
            BoundingBox3D bb = new BoundingBox3D(0, Width, 0, Height, Z, Z);
            return bb;
        }
    }
}
