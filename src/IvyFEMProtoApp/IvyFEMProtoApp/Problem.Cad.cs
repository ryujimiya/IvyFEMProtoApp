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
        public void MakeBluePrint(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, -1.0));
                pts.Add(new OpenTK.Vector2d(2.0, -1.0));
                pts.Add(new OpenTK.Vector2d(2.0, 1.0));
                var res = cad2D.AddPolygon(pts);
                //System.Diagnostics.Debug.WriteLine(res.Dump());
                //System.Diagnostics.Debug.WriteLine(cad2D.Dump());
                //AlertWindow.ShowText(res.Dump());
                //AlertWindow.ShowText(cad2D.Dump());
                var resCircle = cad2D.AddCircle(new OpenTK.Vector2d(1.5, 0.5), 0.25, res.AddLId);
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
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, -1.0));
                pts.Add(new OpenTK.Vector2d(2.0, -1.0));
                pts.Add(new OpenTK.Vector2d(2.0, 1.0));
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
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, -1.0));
                pts.Add(new OpenTK.Vector2d(2.0, -1.0));
                pts.Add(new OpenTK.Vector2d(2.0, 1.0));
                var res = cad2D.AddPolygon(pts);
            }

            double eLen = 0.05;
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

        public void MakeMeshHollowLoop(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            uint hollowLoopId = 0;
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                var res = cad2D.AddPolygon(pts);
                var resCircle = cad2D.AddCircle(new OpenTK.Vector2d(0.5, 0.5), 0.25, res.AddLId);
                hollowLoopId = resCircle.AddLId;
            }

            double eLen = 0.05;
            //Mesher2D mesher2D = new Mesher2D(cad2D, eLen);
            Mesher2D mesher2D = new Mesher2D();
            mesher2D.SetMeshingModeElemLength(eLen);
            IList<uint> lIds = cad2D.GetElemIds(CadElementType.Loop);
            foreach (uint lId in lIds)
            {
                if (lId == hollowLoopId)
                {
                    // メッシュ切りしない
                    continue;
                }
                mesher2D.AddCutMeshLCadId(lId);
            }
            mesher2D.Meshing(cad2D);

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

        public void DrawStringTest(Camera Camera, OpenTK.GLControl glControl)
        {
            // ウィンドウの幅、高さの取得
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int winW = viewport[2];
            int winH = viewport[3];
            int fontSize = 18;
            fontSize = (int)(fontSize * winW / (double)400);

            GL.ClearColor(Color4.White);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(1.1f, 4.0f);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            Camera.Fit(new BoundingBox3D(0, winW, 0, winH, 0, 0));
            OpenGLUtils.SetModelViewTransform(Camera);

            double asp = (winW + 1.0) / (winH + 1.0);
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(-asp, asp, -1.0, 1.0, -1.0, 1.0);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();

            string text = "DrawStringテスト";
            int n = 20;
            for (int i = 0; i < n; i++)
            {
                double xx = -asp + asp * 2.0 * i / (double)n;
                double yy = 1.0 - 2.0  * (i + 1) / (double)n;
                OpenTK.Vector2d drawpp = new OpenTK.Vector2d(xx, yy);
                GL.Translate(drawpp.X, drawpp.Y, 1.0);
                double ratio = 1.0 - i / (double)n;
                GL.Color3(1.0 * ratio, 0.5 * ratio, 0.5 * ratio);
                OpenGLUtils.DrawString(text, fontSize);
                GL.Translate(-drawpp.X, -drawpp.Y, -1.0);
            }

            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();

            glControl.SwapBuffers();
        }
    }
}
