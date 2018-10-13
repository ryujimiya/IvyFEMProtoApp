using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class ConstraintDrawer
    {
        private Constraint Constraint = null;

        public ConstraintDrawer(Constraint constraint)
        {
            Constraint = constraint;
        }

        public void Draw()
        {
            if (Constraint is LineConstraint)
            {
                DrawLineConstraint();
            }
        }

        private void DrawLineConstraint()
        {
            LineConstraint lineConstraint = Constraint as LineConstraint;
            var pt = lineConstraint.Point;
            var normal = lineConstraint.Normal;
            var horizontal = new OpenTK.Vector2d(-normal.Y, normal.X);
            var pt1 = pt - horizontal * 100.0;
            var pt2 = pt + horizontal * 100.0;
            GL.Color3(1.0, 0.0, 0.0);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex2(pt1.X, pt1.Y);
            GL.Vertex2(pt2.X, pt2.Y);
            GL.End();
        }

    }
}
