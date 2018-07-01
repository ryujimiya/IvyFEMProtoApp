using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class Tri2D
    {
        public uint[] V { get; } = new uint[3];
        public int[] G2 { get; } = new int[3];
        public uint[] S2 { get; } = new uint[3];
        public uint[] R2 { get; } = new uint[3];

        public Tri2D()
        {

        }

        public Tri2D(Tri2D src)
        {
            for  (int i = 0; i < 3; i++)
            {
                V[i] = src.V[i];
            }
            for (int i = 0; i < 3; i++)
            {
                G2[i] = src.G2[i];
            }
            for (int i = 0; i < 3; i++)
            {
                S2[i] = src.S2[i];
            }
            for (int i = 0; i < 3; i++)
            {
                R2[i] = src.R2[i];
            }
        }

        public string Dump()
        {
            string ret = "";
            string CRLF = System.Environment.NewLine;

            ret += "Tri2D" + CRLF;
            for (int i = 0; i < 3; i++)
            {
                ret += "V[" + i + "] = " + V[i] + CRLF;
            }
            for (int i = 0; i < 3; i++)
            {
                ret += "G2[" + i + "] = " + G2[i] + CRLF;
            }
            for (int i = 0; i < 3; i++)
            {
                ret += "S2[" + i + "] = " + S2[i] + CRLF;
            }
            for (int i = 0; i < 3; i++)
            {
                ret += "R2[" + i + "] = " + R2[i] + CRLF;
            }
            return ret;
        }
    }
}
