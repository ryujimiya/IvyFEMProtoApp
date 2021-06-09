using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using IvyFEM;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Drawing;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void MakeCad3D6(MainWindow mainWindow)
        {
            /////////////////////
            Dimension = 3; // 3次元
            var camera3D = mainWindow.Camera as Camera3D;
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 70.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            /////////////////////

            double a = 1.0;
            double b = 0.5;
            double d = 0.2;
            double x1 = d;
            double x2 = d + a;
            double y1 = a;
            double y2 = -d;

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(x1, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(x1, y2, 0.0));
                pts.Add(new OpenTK.Vector3d(x2, y2, 0.0));
                pts.Add(new OpenTK.Vector3d(x2, y1, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, y1, 0.0));

                pts.Add(new OpenTK.Vector3d(0.0, 0.0, b));
                pts.Add(new OpenTK.Vector3d(x1, 0.0, b));
                pts.Add(new OpenTK.Vector3d(x1, y2, b));
                pts.Add(new OpenTK.Vector3d(x2, y2, b));
                pts.Add(new OpenTK.Vector3d(x2, y1, b));
                pts.Add(new OpenTK.Vector3d(0.0, y1, b));
                cad.AddCube(pts);
            }

            cad.SetLoopColor(1, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(2, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(3, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(5, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(6, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(8, new double[] { 0.0, 0.0, 0.0 });

            cad.SetLoopColor(7, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(6, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(7, new double[] { 0.0, 0.0, 1.0 });
            cad.SetLoopColor(4, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(3, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(9, new double[] { 0.0, 0.0, 1.0 });

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad3DDrawer(cad);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
        }

        public void MakeCoarseMesh3D6(MainWindow mainWindow)
        {
            /////////////////////
            Dimension = 3; // 3次元
            var camera3D = mainWindow.Camera as Camera3D;
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 70.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            /////////////////////

            double a = 1.0;
            double b = 0.5;
            double d = 0.2;
            double x1 = d;
            double x2 = d + a;
            double y1 = a;
            double y2 = -d;

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(x1, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(x1, y2, 0.0));
                pts.Add(new OpenTK.Vector3d(x2, y2, 0.0));
                pts.Add(new OpenTK.Vector3d(x2, y1, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, y1, 0.0));

                pts.Add(new OpenTK.Vector3d(0.0, 0.0, b));
                pts.Add(new OpenTK.Vector3d(x1, 0.0, b));
                pts.Add(new OpenTK.Vector3d(x1, y2, b));
                pts.Add(new OpenTK.Vector3d(x2, y2, b));
                pts.Add(new OpenTK.Vector3d(x2, y1, b));
                pts.Add(new OpenTK.Vector3d(0.0, y1, b));
                cad.AddCube(pts);
            }

            Mesher3D mesher = new Mesher3D(cad);

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Mesher3DDrawer(mesher);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
        }

        public void MakeMesh3D6(MainWindow mainWindow)
        {
            /////////////////////
            Dimension = 3; // 3次元
            var camera3D = mainWindow.Camera as Camera3D;
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 70.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            /////////////////////

            double a = 1.0;
            double b = 0.5;
            double d = 0.2;
            double x1 = d;
            double x2 = d + a;
            double y1 = a;
            double y2 = -d;

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(x1, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(x1, y2, 0.0));
                pts.Add(new OpenTK.Vector3d(x2, y2, 0.0));
                pts.Add(new OpenTK.Vector3d(x2, y1, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, y1, 0.0));

                pts.Add(new OpenTK.Vector3d(0.0, 0.0, b));
                pts.Add(new OpenTK.Vector3d(x1, 0.0, b));
                pts.Add(new OpenTK.Vector3d(x1, y2, b));
                pts.Add(new OpenTK.Vector3d(x2, y2, b));
                pts.Add(new OpenTK.Vector3d(x2, y1, b));
                pts.Add(new OpenTK.Vector3d(0.0, y1, b));
                cad.AddCube(pts);
            }

            {
                IList<uint> lIds1 = new List<uint> {
                    1, 2, 3, 4, 5, 6, 7, 8
                };
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(lIds1, holes1, insideVIds1);
            }

            //double eLen = 0.05;
            double eLen = 0.10;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Mesher3DDrawer(mesher);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
        }
    }
}
