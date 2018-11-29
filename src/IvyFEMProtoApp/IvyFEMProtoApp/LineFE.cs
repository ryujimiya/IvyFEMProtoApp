﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    partial class LineFE : FE
    {
        public LineFE() : base()
        {
            Type = ElementType.Line;
            Order = 1;
            VertexCount = 2;
            NodeCount = GetNodeCount();
        }

        public LineFE(int order) : base()
        {
            Type = ElementType.Line;
            Order = order;
            VertexCount = 2;
            NodeCount = GetNodeCount();
        }

        public LineFE(TriangleFE src)
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
                nodeCnt = 2;
            }
            else if (Order == 2)
            {
                nodeCnt = 3;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return nodeCnt;
        }

        private double[] AddDisplacement(int iNode, double[] co)
        {
            int dim = co.Length;
            double[] curCo = new double[dim];
            co.CopyTo(curCo, 0);
            if (Displacements != null)
            {
                double[] u = Displacements[iNode];
                for (int iDim = 0; iDim < dim; iDim++)
                {
                    curCo[iDim] += u[iDim];
                }
            }
            return curCo;
        }

        public double GetLineLength()
        {
            double[] co1 = World.GetVertexCoord(VertexCoordIds[0]);
            double[] co2 = World.GetVertexCoord(VertexCoordIds[1]);
            co1 = AddDisplacement(0, co1);
            co2 = AddDisplacement(1, co2);
            OpenTK.Vector2d v1 = new OpenTK.Vector2d(co1[0], co1[1]);
            OpenTK.Vector2d v2 = new OpenTK.Vector2d(co2[0], co2[1]);
            double l = (v2 - v1).Length;
            return l;
        }

        public double[] GetNormal()
        {
            double[] co1 = World.GetVertexCoord(VertexCoordIds[0]);
            double[] co2 = World.GetVertexCoord(VertexCoordIds[1]);
            co1 = AddDisplacement(0, co1);
            co2 = AddDisplacement(1, co2);
            OpenTK.Vector2d v1 = new OpenTK.Vector2d(co1[0], co1[1]);
            OpenTK.Vector2d v2 = new OpenTK.Vector2d(co2[0], co2[1]);
            var t = v2 - v1;
            t = CadUtils.Normalize(t);
            // n = t x e3
            double[] normal = { t.Y, -t.X};
            return normal;
        }

        public void CalcTransMatrix(out double[] a, out double[] b)
        {
            a = new double[2];
            b = new double[2];
            double[] co1 = World.GetVertexCoord(VertexCoordIds[0]);
            double[] co2 = World.GetVertexCoord(VertexCoordIds[1]);
            co1 = AddDisplacement(0, co1);
            co2 = AddDisplacement(1, co2);
            OpenTK.Vector2d v1 = new OpenTK.Vector2d(co1[0], co1[1]);
            OpenTK.Vector2d v2 = new OpenTK.Vector2d(co2[0], co2[1]);
            var t = v2 - v1;
            t = CadUtils.Normalize(t);
            double l = GetLineLength();
            {
                a[0] = (1.0 / l) * OpenTK.Vector2d.Dot(v2, t);
                a[1] = (1.0 / l) * OpenTK.Vector2d.Dot(-v1, t);
            }
            {
                b[0] = (1.0 / l) * (-1.0);
                b[1] = (1.0 / l) * 1.0;
            }
        }

        // ξ([-1, 1])からL1,L2に変換
        public static double[] GetLFromXi(double xi)
        {
            return new double[2] { (1.0 - xi) / 2.0, (1.0 + xi) / 2.0 };
        }

        public static IntegrationPoints GetIntegrationPoints(LineIntegrationPointCount integrationPointCount)
        {
            foreach (var ip in IntegrationPoints.LineIntegrationPoints)
            {
                if (ip.PointCount == (int)integrationPointCount)
                {
                    return ip;
                }
            }
            System.Diagnostics.Debug.Assert(false);
            return null;
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
        /// S{Nx}{Nx}Tdx
        /// </summary>
        /// <returns></returns>
        public double[,] CalcSNxNx()
        {
            double[,] ret = null;
            if (Order == 1)
            {
                ret = Calc1stSNxNx();
            }
            else if (Order == 2)
            {
                ret = Calc2ndSNxNx();
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return ret;
        }

    }
}
