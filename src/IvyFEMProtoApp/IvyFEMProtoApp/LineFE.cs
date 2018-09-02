using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class LineFE : FE
    {
        public LineFE() : base()
        {
            Type = ElementType.Line;
            NodeCount = 2;
        }

        public LineFE(TriangleFE src)
        {
            Copy(src);
        }

        public override void Copy(IObject src)
        {
            base.Copy(src);
        }

        public double GetLineLength()
        {
            double[] co1 = World.GetCoord(CoordIds[0]);
            double[] co2 = World.GetCoord(CoordIds[1]);
            OpenTK.Vector2d v1 = new OpenTK.Vector2d(co1[0], co1[1]);
            OpenTK.Vector2d v2 = new OpenTK.Vector2d(co2[0], co2[1]);
            double l = (v2 - v1).Length;
            return l;
        }

        /// <summary>
        /// S{N}{N}Tdx
        /// </summary>
        /// <returns></returns>
        public double[] CalcSNN()
        {
            double l = GetLineLength();
            /// Note: Columnから先に格納: col * NodeCount + row
            double[] sNN = new double[4]
            {
                l / 3.0,
                l / 6.0,
                l / 6.0,
                l / 3.0
            };
            return sNN;
        }

        /// <summary>
        /// S{Nx}{Nx}Tdx
        /// </summary>
        /// <returns></returns>
        public double[] CalcSNxNx()
        {
            double l = GetLineLength();
            /// Note: Columnから先に格納: col * NodeCount + row
            double[] sNxNx = new double[4]
            {
                1.0 / l,
                -1.0 / l,
                -1.0 / l,
                1.0 / l
            };
            return sNxNx;
        }

    }
}
