﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    partial class TriangleFE : FE
    {
        public TriangleFE() : base()
        {
            Type = ElementType.Tri;
            Order = 1;
            VertexCount = 3;
            NodeCount = GetNodeCount();
        }

        public TriangleFE(int order) : base()
        {
            Type = ElementType.Tri;
            Order = order;
            VertexCount = 3;
            NodeCount = GetNodeCount();
        }

        public TriangleFE(TriangleFE src)
        {
            Copy(src); 
        }

        public override void Copy(IObject src)
        {
            base.Copy(src);
        }

        protected uint GetNodeCount()
        {
            uint nodeCnt = 0;
            if (Order == 1)
            {
                nodeCnt = 3;
            }
            else if (Order == 2)
            {
                nodeCnt = 6;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return nodeCnt;
        }

        public double GetArea()
        {
            double[] co1 = World.GetVertexCoord(VertexCoordIds[0]);
            double[] co2 = World.GetVertexCoord(VertexCoordIds[1]);
            double[] co3 = World.GetVertexCoord(VertexCoordIds[2]);
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
            double[] co1 = World.GetVertexCoord(VertexCoordIds[0]);
            double[] co2 = World.GetVertexCoord(VertexCoordIds[1]);
            double[] co3 = World.GetVertexCoord(VertexCoordIds[2]);
            OpenTK.Vector2d v1 = new OpenTK.Vector2d(co1[0], co1[1]);
            OpenTK.Vector2d v2 = new OpenTK.Vector2d(co2[0], co2[1]);
            OpenTK.Vector2d v3 = new OpenTK.Vector2d(co3[0], co3[1]);
            double A = CadUtils.TriArea(v1, v2, v3);
            OpenTK.Vector2d[] v = { v1, v2, v3 };
            for (int k = 0; k < 3; k++)
            {
                int l = (k + 1) % 3;
                int m = (k + 2) % 3;
                a[k] = (1.0 / (2.0 * A)) * (v[l].X * v[m].Y - v[m].X * v[l].Y);
                b[k] = (1.0 / (2.0 * A)) * (v[l].Y - v[m].Y);
                c[k] = (1.0 / (2.0 * A)) * (v[m].X - v[l].X);
            }            
        }

        public static IntegrationPoints GetIntegrationPoints(TriangleIntegrationPointCount integrationPointCount)
        {
            foreach (var ip in IntegrationPoints.TriangleIntegrationPoints)
            {
                if (ip.PointCount == (int)integrationPointCount)
                {
                    return ip;
                }
            }
            System.Diagnostics.Debug.Assert(false);
            return null;
        }

        public double GetDetJacobian(double[] L)
        {
            double A = GetArea();
            return 2.0 * A;
        }

        /// <summary>
        /// N
        /// </summary>
        /// <param name="L"></param>
        /// <returns></returns>
        public double[] CalcN(double[] L)
        {
            double[] ret = null;
            if (Order == 1)
            {
                ret = Calc1stN(L);
            }
            else if (Order == 2)
            {
                ret = Calc2ndN(L);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return ret;
        }

        /// <summary>
        /// dN/du
        /// </summary>
        /// <returns></returns>
        public double[][] CalcNu(double[] L)
        {
            double[][] ret = null;
            if (Order == 1)
            {
                ret = Calc1stNu(L);
            }
            else if (Order == 2)
            {
                ret = Calc2ndNu(L);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return ret;
        }

        /// <summary>
        /// SNdx
        /// </summary>
        /// <returns></returns>
        public double[] CalcSN()
        {
            double[] ret = null;
            if (Order == 1)
            {
                ret = Calc1stSN();
            }
            else if (Order == 2)
            {
                ret = Calc2ndSN();
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return ret;
        }

        /// <summary>
        /// S{N}{N}Tdx
        /// </summary>
        /// <returns></returns>
        public double[,] CalcSNN()
        {
            double[,] ret = null;
            if (Order == 1)
            {
                ret = Calc1stSNN();
            }
            else if (Order == 2)
            {
                ret = Calc2ndSNN();
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return ret;
        }

        /// <summary>
        /// S{Nu}{Nv}Tdx, u,v = x, y
        /// </summary>
        /// <returns></returns>
        public double[,][,] CalcSNuNv()
        {
            double[,][,] ret = null;
            if (Order == 1)
            {
                ret = Calc1stSNuNv();
            }
            else if (Order == 2)
            {
                ret = Calc2ndSNuNv();
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return ret;
        }
    }
}
