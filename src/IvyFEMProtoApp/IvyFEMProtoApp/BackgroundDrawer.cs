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
    class BackgroundDrawer : IDrawer
    {
        private double Width = 0;
        private double Height = 0;
        private double Z = 0;

        public RotMode SutableRotMode => RotMode.RotModeNotSet;

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

        public void Draw()
        {

        }

        public void DrawSelection(uint idraw)
        {

        }

        public BoundingBox3D GetBoundingBox(Matrix3d rot)
        {
            double hw = Width * 0.5;
            double hh = Height * 0.5;
            BoundingBox3D bb = new BoundingBox3D(-hw, hw, -hh, hh, Z, Z);
            return bb;
        }
    }
}
