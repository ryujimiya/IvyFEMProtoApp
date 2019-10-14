﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using IvyFEM;

namespace IvyFEMProtoApp
{
    public class CalcDraw2D
    {
        /// <summary>
        /// Cadパネル
        /// </summary>
        private OpenTK.GLControl GLControl = null;
        /// <summary>
        /// カメラ
        /// </summary>
        private Camera2D Camera = null;

        /// <summary>
        /// 描画オブジェクトアレイインスタンス
        /// </summary>
        public FieldDrawerArray DrawerArray { get; private set; } = new FieldDrawerArray();


        public CalcDraw2D(OpenTK.GLControl GLControl, Camera2D camera)
        {
            this.GLControl = GLControl;
            Camera = camera;
        }

        public void PanelPaint()
        {
            GL.ClearColor(Color4.White);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(1.1f, 4.0f);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            OpenGLUtils.SetModelViewTransform(Camera);

            DrawerArray.Draw();
            GLControl.SwapBuffers();
        }

        public void PanelResize()
        {
            int width = GLControl.Size.Width;
            int height = GLControl.Size.Height;
            Camera.WindowAspect = ((double)width / height);
            GL.Viewport(0, 0, width, height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            OpenGLUtils.SetProjectionTransform(Camera);
        }
    }
}
