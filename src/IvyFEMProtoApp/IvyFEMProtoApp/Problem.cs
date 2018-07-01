using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using IvyFEM;

namespace IvyFEMProtoApp
{
    class Problem
    {
        private double WaveguideWidth = 0;
        private double InputWGLength = 0;
        public CadObject2DDrawer Drawer { get; private set; } = null;

        public Problem()
        {
            WaveguideWidth = 1.0;
            InputWGLength = 1.0 * WaveguideWidth;
        }

        public void MakeBluePrint()
        {
            {
                CadObject2D cad2D = new CadObject2D();
                // 図面作成
                IList<Vector2> pts = new List<Vector2>();
                pts.Add(new Vector2(0.0f, (float)WaveguideWidth));  // 頂点1
                pts.Add(new Vector2(0.0f, 0.0f)); // 頂点2
                pts.Add(new Vector2((float)InputWGLength, 0.0f)); // 頂点3
                pts.Add(new Vector2((float)InputWGLength, (float)(-InputWGLength))); // 頂点4
                pts.Add(new Vector2((float)(InputWGLength + WaveguideWidth), (float)(-InputWGLength))); // 頂点5
                pts.Add(new Vector2((float)(InputWGLength + WaveguideWidth), (float)WaveguideWidth)); // 頂点6
                var res = cad2D.AddPolygon(pts);
                System.Diagnostics.Debug.WriteLine(res.Dump());
                System.Diagnostics.Debug.WriteLine(cad2D.Dump());
                //AlertWindow.ShowText(res.Dump());
                //AlertWindow.ShowText(cad2D.Dump());
                Drawer = new CadObject2DDrawer(cad2D);
            }
        }
    }
}
