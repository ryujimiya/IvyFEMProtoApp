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
        public TriangleFE() : base()
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

        public IntegrationPoints GetIntegrationPoints(int integrationPointCount)
        {
            foreach (var ip in IntegrationPoints.TriangleIntegrationPoints)
            {
                if (ip.PointCount == integrationPointCount)
                {
                    return ip;
                }
            }
            System.Diagnostics.Debug.Assert(false);
            return null;
        }

        public double GetDetJacobian()
        {
            double A = GetArea();
            return 2.0 * A;
        }

        /// <summary>
        /// N
        /// </summary>
        /// <param name="L"></param>
        /// <returns></returns>
        public double [] CalcN(double[] L)
        {
            double[] N = new double[3];

            // N = L
            L.CopyTo(N, 0);

            return N;
        }

        /// <summary>
        /// dN/du
        /// </summary>
        /// <returns></returns>
        public double[][] CalcNu()
        {
            double[][] Nu = new double[2][];
            double[] a;
            double[] b;
            double[] c;
            CalcTransMatrix(out a, out b, out c);

            // dN/dx
            Nu[0] = b;

            // dN/dy
            Nu[1] = c;
            return Nu;
        }


        /// <summary>
        /// SNdx
        /// </summary>
        /// <returns></returns>
        public double CalcSN()
        {
            double A = GetArea();

            return A / 3.0;
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
        /// S{Nu}{Nv}Tdx, u,v = x, y
        /// </summary>
        /// <returns></returns>
        public double[][] CalcSNuNvs()
        {
            double A = GetArea();
            double[] a;
            double[] b;
            double[] c;
            CalcTransMatrix(out a, out b, out c);

            /// Note: Columnから先に格納: col * NodeCount + row
            double[][] sNuNv = new double[4][];

            int index;
            // sNxNx
            index = 0;
            sNuNv[index] = new double[9];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[index][i * 3 + j] = A * b[i] * b[j];
                }
            }

            // sNyNx
            index = 1;
            sNuNv[index] = new double[9];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[index][i * 3 + j] = A * b[i] * c[j];
                }
            }

            // sNyNx
            index = 2;
            sNuNv[index] = new double[9];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[index][i * 3 + j] = A * c[i] * b[j];
                }
            }

            // sNyNy
            index = 3;
            sNuNv[index] = new double[9];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[index][i * 3 + j] = A * c[i] * c[j];
                }
            }
            return sNuNv;
        }
    }
}
