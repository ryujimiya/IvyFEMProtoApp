using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class Loop2D
    {
        public double[] Color { get; } = new double[3];
        public uint Layer { get; set; } = 0;

        public Loop2D()
        {
            /*
            Color[0] = 0.8;
            Color[1] = 0.8;
            Color[2] = 0.8;
            */
            Color[0] = 0.2;
            Color[1] = 0.2;
            Color[2] = 0.2;
            Layer = 0;
        }

        public Loop2D(Loop2D src)
        {
            Color[0] = src.Color[0];
            Color[1] = src.Color[1];
            Color[2] = src.Color[2];
            Layer = src.Layer;
        }

        public string Dump()
        {
            string ret = "";
            string CRLF = System.Environment.NewLine;

            ret += "■Loop2D" + CRLF;
            for (int i = 0; i < 3; i++)
            {
                ret += "Color[" + i + "] = " + Color[i] + CRLF;
            }
            ret += "LayerId = " + Layer + CRLF;
            return ret;
        }

    }
}
