using System;
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

        public double GetLineLength()
        {
            double[] co1 = World.GetVertexCoord(VertexCoordIds[0]);
            double[] co2 = World.GetVertexCoord(VertexCoordIds[1]);
            OpenTK.Vector2d v1 = new OpenTK.Vector2d(co1[0], co1[1]);
            OpenTK.Vector2d v2 = new OpenTK.Vector2d(co2[0], co2[1]);
            double l = (v2 - v1).Length;
            return l;
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
