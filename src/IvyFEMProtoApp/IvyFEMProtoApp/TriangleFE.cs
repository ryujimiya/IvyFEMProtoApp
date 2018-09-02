using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class TriangleFE : FE
    {
        public TriangleFE() : base()
        {
            Type = ElementType.Tri;
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
            OpenTK.Vector2d v1 = new OpenTK.Vector2d(co1[0], co1[1]);
            OpenTK.Vector2d v2 = new OpenTK.Vector2d(co2[0], co2[1]);
            OpenTK.Vector2d v3 = new OpenTK.Vector2d(co3[0], co3[1]);
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
            OpenTK.Vector2d v1 = new OpenTK.Vector2d(co1[0], co1[1]);
            OpenTK.Vector2d v2 = new OpenTK.Vector2d(co2[0], co2[1]);
            OpenTK.Vector2d v3 = new OpenTK.Vector2d(co3[0], co3[1]);
            double area = CadUtils.TriArea(v1, v2, v3);
            OpenTK.Vector2d[] v = { v1, v2, v3 };
            for (int k = 0; k < 3; k++)
            {
                int l = (k + 1) % 3;
                int m = (k + 2) % 3;
                a[k] = (1.0 / (2.0 * area)) * (v[l].X * v[m].Y - v[m].X * v[l].Y);
                b[k] = (1.0 / (2.0 * area)) * (v[l].Y - v[m].Y);
                c[k] = (1.0 / (2.0 * area)) * (v[m].X - v[l].X);
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
        public double[,] CalcSNN()
        {
            double A = GetArea();
            double[,] sNN = new double[3, 3]
            {
                {
                    A / 6.0,
                    A / 12.0,
                    A / 12.0,
                },
                {
                    A / 12.0,
                    A / 6.0,
                    A / 12.0,
                },
                {
                    A / 12.0,
                    A / 12.0,
                    A / 6.0
                }
            };
            return sNN;
        }

        /// <summary>
        /// S{Nu}{Nv}Tdx, u,v = x, y
        /// </summary>
        /// <returns></returns>
        public double[,][,] CalcSNuNv()
        {
            double A = GetArea();
            double[] a;
            double[] b;
            double[] c;
            CalcTransMatrix(out a, out b, out c);

            double[,][,] sNuNv = new double[2, 2][,];

            // sNxNx
            sNuNv[0, 0] = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[0, 0][i, j] = A * b[i] * b[j];
                }
            }

            // sNyNx
            sNuNv[1, 0] = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[1, 0][i, j] = A * c[i] * b[j];
                }
            }

            // sNxNy
            sNuNv[0, 1] = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[0, 1][i, j] = A * b[i] * c[j];
                }
            }

            // sNyNy
            sNuNv[1, 1] = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[1, 1][i, j] = A * c[i] * c[j];
                }
            }
            return sNuNv;
        }
    }
}
