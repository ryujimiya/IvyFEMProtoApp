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
        public void MakeCad3D7(MainWindow mainWindow)
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

            uint tmpId;
            IList<uint> innerVIds = new List<uint>();
            tmpId = cad.AddVertex(CadElementType.Edge, 12, new OpenTK.Vector3d(0.0, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);

            tmpId = cad.AddVertex(CadElementType.Loop, 6, new OpenTK.Vector3d(0.5, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);
            tmpId = cad.AddVertex(CadElementType.Loop, 6, new OpenTK.Vector3d(0.5, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);

            tmpId = cad.AddVertex(CadElementType.Edge, 10, new OpenTK.Vector3d(1.0, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);

            for (int i = 0; i < innerVIds.Count - 1; i++)
            {
                uint sVId = innerVIds[i];
                uint eVid = innerVIds[i + 1];
                var cvRes1 = cad.ConnectVertexLine(sVId, eVid);
                System.Diagnostics.Debug.Assert(cvRes1.AddEId != 0);
                if (i == innerVIds.Count - 2)
                {
                    System.Diagnostics.Debug.Assert(cvRes1.AddLId != 0);
                }
            }

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

        public void MakeCoarseMesh3D7(MainWindow mainWindow)
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

            uint tmpId;
            IList<uint> innerVIds = new List<uint>();
            tmpId = cad.AddVertex(CadElementType.Edge, 12, new OpenTK.Vector3d(0.0, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);

            tmpId = cad.AddVertex(CadElementType.Loop, 6, new OpenTK.Vector3d(0.5, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);
            tmpId = cad.AddVertex(CadElementType.Loop, 6, new OpenTK.Vector3d(0.5, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);

            tmpId = cad.AddVertex(CadElementType.Edge, 10, new OpenTK.Vector3d(1.0, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);

            for (int i = 0; i < innerVIds.Count - 1; i++)
            {
                uint sVId = innerVIds[i];
                uint eVid = innerVIds[i + 1];
                var cvRes1 = cad.ConnectVertexLine(sVId, eVid);
                System.Diagnostics.Debug.Assert(cvRes1.AddEId != 0);
                if (i == innerVIds.Count - 2)
                {
                    System.Diagnostics.Debug.Assert(cvRes1.AddLId != 0);
                }
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

        public void MakeMesh3D7(MainWindow mainWindow)
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

            uint tmpId;
            IList<uint> innerVIds = new List<uint>();
            tmpId = cad.AddVertex(CadElementType.Edge, 12, new OpenTK.Vector3d(0.0, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);

            tmpId = cad.AddVertex(CadElementType.Loop, 6, new OpenTK.Vector3d(0.5, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);
            tmpId = cad.AddVertex(CadElementType.Loop, 6, new OpenTK.Vector3d(0.5, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);

            tmpId = cad.AddVertex(CadElementType.Edge, 10, new OpenTK.Vector3d(1.0, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            innerVIds.Add(tmpId);

            for (int i = 0; i < innerVIds.Count - 1; i++)
            {
                uint sVId = innerVIds[i];
                uint eVid = innerVIds[i + 1];
                var cvRes1 = cad.ConnectVertexLine(sVId, eVid);
                System.Diagnostics.Debug.Assert(cvRes1.AddEId != 0);
                if (i == innerVIds.Count - 2)
                {
                    System.Diagnostics.Debug.Assert(cvRes1.AddLId != 0);
                }
            }

            {
                IList<uint> lIds1 = new List<uint> {
                    1, 2, 3, 4, 5, 6, 7
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
