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
        public void MakeCad3D1(MainWindow mainWindow)
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

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                cad.AddCube(pts);
            }
            cad.AddRectLoop(1, new OpenTK.Vector2d(0.25, 0.25), new OpenTK.Vector2d(0.75, 0.75));
            cad.LiftLoop(1, cad.GetLoop(1).Normal * (-0.1));

            cad.SetLoopColor(1, new double[3] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(7, new double[3] { 0.0, 0.0, 0.0 }); // LiftLoop
            cad.SetLoopColor(8, new double[3] { 1.0, 0.0, 1.0 }); // 側面
            cad.SetLoopColor(9, new double[3] { 1.0, 0.0, 1.0 }); // 側面
            cad.SetLoopColor(10, new double[3] { 1.0, 0.0, 1.0 }); // 側面
            cad.SetLoopColor(11, new double[3] { 1.0, 0.0, 1.0 }); // 側面
            cad.SetEdgeColor(21, new double[3] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(22, new double[3] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(23, new double[3] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(24, new double[3] { 0.0, 0.0, 1.0 });
            cad.SetVertexColor(13, new double[3] { 1.0, 1.0, 0.0 });
            cad.SetVertexColor(14, new double[3] { 1.0, 1.0, 0.0 });
            cad.SetVertexColor(15, new double[3] { 1.0, 1.0, 0.0 });
            cad.SetVertexColor(16, new double[3] { 1.0, 1.0, 0.0 });

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

        public void MakeCoarseMesh3D1(MainWindow mainWindow)
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

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                cad.AddCube(pts);
            }
            cad.AddRectLoop(1, new OpenTK.Vector2d(0.25, 0.25), new OpenTK.Vector2d(0.75, 0.75));
            cad.LiftLoop(1, cad.GetLoop(1).Normal * (-0.1));

            System.Diagnostics.Debug.Assert(cad.IsElementId(CadElementType.Loop, 11)); // 11までのはず
            System.Diagnostics.Debug.Assert(!cad.IsElementId(CadElementType.Loop, 12)); // 11までのはず

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

        public void MakeMesh3D1(MainWindow mainWindow)
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

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                cad.AddCube(pts);
            }
            cad.AddRectLoop(1, new OpenTK.Vector2d(0.25, 0.25), new OpenTK.Vector2d(0.75, 0.75));
            cad.LiftLoop(1, cad.GetLoop(1).Normal * (-0.1));

            System.Diagnostics.Debug.Assert(cad.IsElementId(CadElementType.Loop, 11)); // 11までのはず
            System.Diagnostics.Debug.Assert(!cad.IsElementId(CadElementType.Loop, 12)); // 11までのはず
            {
                IList<uint> lIds1 = new List<uint> {
                    1, 2, 3, 4, 5, 6,
                    7, 8, 9, 10, 11
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
