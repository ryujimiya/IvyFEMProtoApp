using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using IvyFEM;

namespace IvyFEMProtoApp
{
    public class CalcDraw
    {
        /// <summary>
        /// Cadパネル
        /// </summary>
        private OpenTK.GLControl glControl = null;
        /// <summary>
        /// カメラ
        /// </summary>
        private Camera2D Camera = null;

        /// <summary>
        /// 描画オブジェクトアレイインスタンス
        /// </summary>
        public FieldDrawerArray DrawerArray { get; private set; } = new FieldDrawerArray();


        public CalcDraw(OpenTK.GLControl glControl, Camera2D camera)
        {
            this.glControl = glControl;
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
            glControl.SwapBuffers();
        }

        public void PanelResize()
        {
            int width = glControl.Size.Width;
            int height = glControl.Size.Height;
            Camera.WindowAspect = ((double)width / height);
            GL.Viewport(0, 0, width, height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            OpenGLUtils.SetProjectionTransform(Camera);
        }
    }
}
