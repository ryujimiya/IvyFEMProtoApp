using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void MakeBluePrint(MainWindow mainWindow)
        {
            double WaveguideWidth = 1.0;
            double InputWGLength = 1.0 * WaveguideWidth;
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, WaveguideWidth));  // 頂点1
                pts.Add(new OpenTK.Vector2d(0.0, 0.0)); // 頂点2
                pts.Add(new OpenTK.Vector2d(InputWGLength, 0.0)); // 頂点3
                pts.Add(new OpenTK.Vector2d(InputWGLength, (-InputWGLength))); // 頂点4
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), (-InputWGLength))); // 頂点5
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), WaveguideWidth)); // 頂点6
                var res = cad2D.AddPolygon(pts);
                //System.Diagnostics.Debug.WriteLine(res.Dump());
                //System.Diagnostics.Debug.WriteLine(cad2D.Dump());
                //AlertWindow.ShowText(res.Dump());
                //AlertWindow.ShowText(cad2D.Dump());
                var resCircle = cad2D.AddCircle(new OpenTK.Vector2d(
                    InputWGLength + 0.5 * WaveguideWidth, 0.5 * WaveguideWidth),
                    0.25 * WaveguideWidth,
                    res.AddLId);
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
        }

        public void MakeCoarseMesh(MainWindow mainWindow)
        {
            double WaveguideWidth = 1.0;
            double InputWGLength = 1.0 * WaveguideWidth;
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, WaveguideWidth));  // 頂点1
                pts.Add(new OpenTK.Vector2d(0.0, 0.0)); // 頂点2
                pts.Add(new OpenTK.Vector2d(InputWGLength, 0.0)); // 頂点3
                pts.Add(new OpenTK.Vector2d(InputWGLength, (-InputWGLength))); // 頂点4
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), (-InputWGLength))); // 頂点5
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), WaveguideWidth)); // 頂点6
                var res = cad2D.AddPolygon(pts);
            }

            Mesher2D mesher2D = new Mesher2D(cad2D);

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
        }

        public void MakeMesh(MainWindow mainWindow)
        {
            double WaveguideWidth = 1.0;
            double InputWGLength = 1.0 * WaveguideWidth;
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, WaveguideWidth));  // 頂点1
                pts.Add(new OpenTK.Vector2d(0.0, 0.0)); // 頂点2
                pts.Add(new OpenTK.Vector2d(InputWGLength, 0.0)); // 頂点3
                pts.Add(new OpenTK.Vector2d(InputWGLength, (-InputWGLength))); // 頂点4
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), (-InputWGLength))); // 頂点5
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), WaveguideWidth)); // 頂点6
                var res = cad2D.AddPolygon(pts);
            }

            double eLen = WaveguideWidth * 0.05;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
        }
    }
}
