﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    class Vertex2D : IObject
    {
        public Vector2 Point { get; set; }
        public double[] Color { get; } = new double[3];

        public Vertex2D()
        {

        }

        public Vertex2D(Vector2 point)
        {
            Point = new Vector2(point.X, point.Y);
            for (int i = 0; i < 3; i++)
            {
                Color[i] = 0.0;
            }
        }

        public Vertex2D(Vertex2D src)
        {
            Copy(src);
        }

        public void Copy(IObject src)
        {
            Vertex2D srcVertex = src as Vertex2D;
            Point = new Vector2(srcVertex.Point.X, srcVertex.Point.Y);
            for (int i = 0; i < 3; i++)
            {
                Color[i] = srcVertex.Color[i];
            }

        }

        public string Dump()
        {
            string ret = "";
            string CRLF = System.Environment.NewLine;

            ret += "■Vertex2D" + CRLF;
            ret += "Point = (" + Point.X + ", " + Point.Y + ")" + CRLF;
            for (int i = 0; i < 3; i++)
            {
                ret += "Color[" + i + "] = " + Color[i] + CRLF;
            }
            return ret;
        }
    }

}
