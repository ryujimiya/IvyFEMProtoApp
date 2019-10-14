using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using IvyFEM;
using System.Drawing;

namespace IvyFEMProtoApp
{
    public class MeshDraw2D
    {
        /// <summary>
        /// Cadパネル
        /// </summary>
        private OpenTK.GLControl GLControl = null;
        /// <summary>
        /// カメラ
        /// </summary>
        public Camera2D Camera { get; private set; } = new Camera2D();
        /// <summary>
        /// 描画オブジェクトアレイインスタンス
        /// </summary>
        public DrawerArray DrawerArray { get; private set; } = new DrawerArray();
        /// <summary>
        /// 図面背景
        /// </summary>
        public BackgroundDrawer BackgroundDrawer { get; private set; } = null;


        public MeshDraw2D(OpenTK.GLControl GLControl, double width, double height)
        {
            this.GLControl = GLControl;

            BackgroundDrawer = new BackgroundDrawer(width, height);

            // 領域を決定する
            SetupRegionSize();
        }

        public void Init(Mesher2D mesher2D)
        {
            var drawerArray = this.DrawerArray;
            {
                drawerArray.Clear();

                //drawerArray.Add(BackgroundDrawer);
                IDrawer mesh2DDrawer = new Mesher2DDrawer(mesher2D);
                drawerArray.Add(mesh2DDrawer);

                SetupRegionSize();
                PanelResize();
                GLControl.Invalidate();
                GLControl.Update();
            }
        }

        /// <summary>
        /// バックグラウンドサイズを設定する
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetBackgroundSize(double width, double height)
        {
            BackgroundDrawer = new BackgroundDrawer(width, height);

            // 領域を決定する
            SetupRegionSize();
            PanelResize();

            GLControl.Invalidate();
            GLControl.Update();
        }

        /// <summary>
        /// 領域を決定する
        /// </summary>
        public void SetupRegionSize(double offsetX = 0, double offsetY = 0, double scale = 1.4)
        {
            // 描画オブジェクトのバウンディングボックスを使ってカメラの変換行列を初期化する
            Camera.Fit(DrawerArray.GetBoundingBox(Camera.RotMatrix33()));
            // カメラのスケール調整
            // DrawerArrayのInitTransを実行すると、物体のバウンディングボックス + マージン分(×1.5)がとられる。
            // マージンを表示上をなくすためスケールを拡大して調整する
            Camera.Scale = scale;
            // カメラをパンニングさせ位置を調整
            Camera.MousePan(0.0, 0.0, offsetX, offsetY);
        }

        /// <summary>
        /// SimpleOpenGlControlのシーンの描画
        /// </summary>
        private void renderScene()
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

        /// <summary>
        /// SimpleOpenGlControlのリサイズ処理
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        private void resizeScene(int w, int h)
        {
            Camera.WindowAspect = (double)w / h;
            GL.Viewport(0, 0, w, h);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            OpenGLUtils.SetProjectionTransform(Camera);
        }

        public void PanelPaint()
        {
            renderScene();
        }

        public void PanelResize()
        {
            int width = GLControl.Size.Width;
            int height = GLControl.Size.Height;
            resizeScene(width, height);
        }
    }
}
