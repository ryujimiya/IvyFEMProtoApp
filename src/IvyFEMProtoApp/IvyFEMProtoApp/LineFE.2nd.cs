using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    partial class LineFE
    {
        public double[,] Calc2ndSNN()
        {
            double l = GetLineLength();
            double[,] sNN = new double[3, 3]
            {
                { 4.0, -1.0, 2.0 },
                { -1.0, 4.0, 2.0 },
                { 2.0, 2.0, 16.0 }
            };
            for (int i = 0; i < sNN.GetLength(0); i++)
            {
                for (int j = 0; j < sNN.GetLength(1); j++)
                {
                    sNN[i, j] *= l / 30.0;
                }
            }
            return sNN;
        }

        public double[,] Calc2ndSNxNx()
        {
            double l = GetLineLength();
            double[,] sNxNx = new double[3, 3] 
            {
                { 7.0, 1.0, -8.0 },
                { 1.0, 7.0, -8.0 },
                { -8.0, -8.0, 16.0 }
            };
            for (int i = 0; i < sNxNx.GetLength(0); i++)
            {
                for (int j = 0; j < sNxNx.GetLength(1); j++)
                {
                    sNxNx[i, j] *= 1.0 / (3.0 * l);
                }
            }
            return sNxNx;
        }
    }
}
