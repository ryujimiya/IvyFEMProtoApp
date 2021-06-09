﻿using System;
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
        public void MakeCad3D8(MainWindow mainWindow)
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

                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 2.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 2.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 2.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 2.0));

                uint layerCnt = 2;
                bool isMakeRadialLoop = false;//内部の面を作成しない
                cad.AddCubeWithMultiLayers(pts, layerCnt, isMakeRadialLoop);
            }

            uint tmpId;
            tmpId = cad.AddVertex(CadElementType.Edge, 12, new OpenTK.Vector3d(0.0, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId1 = tmpId;

            tmpId = cad.AddVertex(CadElementType.Edge, 10, new OpenTK.Vector3d(1.0, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId4 = tmpId;

            IList<uint> radialEIds = new List<uint> { 9, 10, 22, 11, 12, 21 };
            tmpId = cad.MakeRadialLoop(radialEIds);
            System.Diagnostics.Debug.Assert(tmpId != 0);

            tmpId = cad.AddVertex(CadElementType.Loop, 11, new OpenTK.Vector3d(0.5, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId2 = tmpId;
            tmpId = cad.AddVertex(CadElementType.Loop, 11, new OpenTK.Vector3d(0.5, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId3 = tmpId;

            IList<uint> innerVIds = new List<uint> { innerVId1, innerVId2, innerVId3, innerVId4 };
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

        public void MakeCoarseMesh3D8(MainWindow mainWindow)
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

                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 2.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 2.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 2.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 2.0));

                uint layerCnt = 2;
                bool isMakeRadialLoop = false;//内部の面を作成しない
                cad.AddCubeWithMultiLayers(pts, layerCnt, isMakeRadialLoop);
            }

            uint tmpId;
            tmpId = cad.AddVertex(CadElementType.Edge, 12, new OpenTK.Vector3d(0.0, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId1 = tmpId;

            tmpId = cad.AddVertex(CadElementType.Edge, 10, new OpenTK.Vector3d(1.0, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId4 = tmpId;

            IList<uint> radialEIds = new List<uint> { 9, 10, 22, 11, 12, 21 };
            tmpId = cad.MakeRadialLoop(radialEIds);
            System.Diagnostics.Debug.Assert(tmpId != 0);

            tmpId = cad.AddVertex(CadElementType.Loop, 11, new OpenTK.Vector3d(0.5, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId2 = tmpId;
            tmpId = cad.AddVertex(CadElementType.Loop, 11, new OpenTK.Vector3d(0.5, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId3 = tmpId;

            IList<uint> innerVIds = new List<uint> { innerVId1, innerVId2, innerVId3, innerVId4 };
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

        public void MakeMesh3D8(MainWindow mainWindow)
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

                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 2.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 2.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 2.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 2.0));

                uint layerCnt = 2;
                bool isMakeRadialLoop = false;//内部の面を作成しない
                cad.AddCubeWithMultiLayers(pts, layerCnt, isMakeRadialLoop);
            }

            uint tmpId;
            tmpId = cad.AddVertex(CadElementType.Edge, 12, new OpenTK.Vector3d(0.0, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId1 = tmpId;

            tmpId = cad.AddVertex(CadElementType.Edge, 10, new OpenTK.Vector3d(1.0, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId4 = tmpId;

            IList<uint> radialEIds = new List<uint> { 9, 10, 22, 11, 12, 21 };
            tmpId = cad.MakeRadialLoop(radialEIds);
            System.Diagnostics.Debug.Assert(tmpId != 0);

            tmpId = cad.AddVertex(CadElementType.Loop, 11, new OpenTK.Vector3d(0.5, 0.6, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId2 = tmpId;
            tmpId = cad.AddVertex(CadElementType.Loop, 11, new OpenTK.Vector3d(0.5, 0.8, 1.0)).AddVId;
            System.Diagnostics.Debug.Assert(tmpId != 0);
            uint innerVId3 = tmpId;

            IList<uint> innerVIds = new List<uint> { innerVId1, innerVId2, innerVId3, innerVId4 };
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
                    1, 2, 3, 4, 5, 11, 12 
                };
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(lIds1, holes1, insideVIds1);

                IList<uint> lIds2 = new List<uint> {
                    11, 12, 6, 7, 8, 9, 10
                };
                IList<OpenTK.Vector3d> holes2 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds2 = new List<uint>();
                uint sId2 = cad.AddSolid(lIds2, holes2, insideVIds2);
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
