using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    class Point2D
    {
        public Vector2 Point { get; set; } = new Vector2();
        public int Elem { get; set; } = 0;
        public uint Dir { get; set; } = 0;

        public Point2D()
        {

        }

        public Point2D(double x, double y, int elem, uint dir)
        {
            Point = new Vector2((float)x, (float)y);
            Elem = elem;
            Dir = dir;
        }

        public Point2D(Point2D src)
        {
            Point = src.Point;
            Elem = src.Elem;
            Dir = src.Dir;
        }

        public string Dump()
        {
            string ret = "";
            string CRLF = System.Environment.NewLine;

            ret += "Point2D" + CRLF;
            ret += "Point = (" + Point.X + ", " + Point.Y + ")" + CRLF;
            ret += "Elem = " + Elem + CRLF;
            ret += "Dir = " + Dir + CRLF;
            return ret;
        }
    }

}
