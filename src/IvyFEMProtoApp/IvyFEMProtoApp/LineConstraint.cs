using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class LineConstraint : Constraint
    {
        public OpenTK.Vector2d Point { get; set; } = new OpenTK.Vector2d();
        public OpenTK.Vector2d Normal { get; set; } = new OpenTK.Vector2d();

        public LineConstraint()
        {

        }

        public LineConstraint(OpenTK.Vector2d point, OpenTK.Vector2d normal)
        {
            Point = new OpenTK.Vector2d(point.X, point.Y);
            Normal = new OpenTK.Vector2d(normal.X, normal.Y);
        }

        public double GetValue(double[] x)
        {
            System.Diagnostics.Debug.Assert(x.Length == 2);
            OpenTK.Vector2d xVec = new OpenTK.Vector2d(x[0], x[1]);
            OpenTK.Vector2d lineVec = xVec - Point;
            double value = OpenTK.Vector2d.Dot(lineVec, Normal);
            return value;
        }

        public double GetDerivation(int iDof, double[] x)
        {
            System.Diagnostics.Debug.Assert(x.Length == 2);
            if (iDof >= 2)
            {
                throw new InvalidOperationException();
            }
            double dValue = Normal[iDof];
            return dValue;
        }

        public double Get2ndDerivation(int iDof, int jDof, double[] x)
        {
            System.Diagnostics.Debug.Assert(x.Length == 2);
            if (iDof >= 2 || jDof >= 2)
            {
                throw new InvalidOperationException();
            }
            double d2Value = 0;
            return d2Value;
        }
    }
}
