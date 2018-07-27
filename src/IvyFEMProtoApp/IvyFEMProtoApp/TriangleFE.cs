using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    class TriangleFE : FE
    {
        public TriangleFE()
        {
            Type = ElementType.TRI;
            NodeCount = 3;
        }

        public TriangleFE(TriangleFE src)
        {
            Copy(src); 
        }

        public override void Copy(IObject src)
        {
            base.Copy(src);
        }

        public double GetArea()
        {
            double[] co1 = World.GetCoord(CoordIds[0]);
            double[] co2 = World.GetCoord(CoordIds[1]);
            double[] co3 = World.GetCoord(CoordIds[2]);
            Vector2 v1 = new Vector2((float)co1[0], (float)co1[1]);
            Vector2 v2 = new Vector2((float)co2[0], (float)co2[1]);
            Vector2 v3 = new Vector2((float)co3[0], (float)co3[1]);
            double area = CadUtils.TriArea(v1, v2, v3);
            return area;
        }

        public void CalcTransMatrix(out double[] a, out double[] b, out double[] c)
        {
            a = new double[3];
            b = new double[3];
            c = new double[3];
            double[] co1 = World.GetCoord(CoordIds[0]);
            double[] co2 = World.GetCoord(CoordIds[1]);
            double[] co3 = World.GetCoord(CoordIds[2]);
            Vector2 v1 = new Vector2((float)co1[0], (float)co1[1]);
            Vector2 v2 = new Vector2((float)co2[0], (float)co2[1]);
            Vector2 v3 = new Vector2((float)co3[0], (float)co3[1]);
            double area = CadUtils.TriArea(v1, v2, v3);
            Vector2[] v = { v1, v2, v3 };
            for (int k = 0; k < 3; k++)
            {
                int l = (k + 1) % 3;
                int m = (k + 2) % 3;
                a[k] = (0.5 / area) * (v[l].X * v[m].Y - v[m].X * v[k].Y);
                b[k] = (0.5 / area) * (v[l].Y - v[m].Y);
                c[k] = (0.5 / area) * (v[m].X - v[l].X);
            }            
        }

        /// <summary>
        /// S{N}{N}Tdx
        /// </summary>
        /// <returns></returns>
        public double[] CalcSNN()
        {
            double A = GetArea();
            /// Note: Columnから先に格納: col * NodeCount + row
            double[] sNN = new double[9]
            {
                A / 6.0,
                A / 12.0,
                A / 12.0,
                A / 12.0,
                A / 6.0,
                A / 12.0,
                A / 12.0,
                A / 12.0,
                A / 6.0
            };
            return sNN;
        }

        /// <summary>
        /// S{Nx}{Nx}Tdx, S{Ny}{Ny}Tdy
        /// </summary>
        /// <returns></returns>
        public double[][] CalcSNxNxs()
        {
            double A = GetArea();
            double[] a;
            double[] b;
            double[] c;
            CalcTransMatrix(out a, out b, out c);

            /// Note: Columnから先に格納: col * NodeCount + row
            double[][] sNxNx = new double[2][];

            int index = 0;
            sNxNx[index] = new double[9];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNxNx[index][i * 3 + j] = A * b[i] * b[j];
                }
            }
            index++;

            sNxNx[index] = new double[9];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNxNx[index][i * 3 + j] = A * c[i] * c[j];
                }
            }
            return sNxNx;
        }


    }
}
