using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using IvyFEM;
using System.Drawing;
using System.Drawing.Imaging;

namespace IvyFEMProtoApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private Problem Problem = null;
        private Camera Camera = new Camera();
        private DrawerArray DrawerArray = new DrawerArray();

        private BitmapData TextureBitmapData;
        private int Texture;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ウィンドウがロードされた
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Problem = new Problem();
        }

        /// <summary>
        /// glControlの起動時に実行される。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Load(object sender, EventArgs e)
        {
            GL.ClearColor(Color4.White);
            GL.Enable(EnableCap.DepthTest);
            // test
            //GL.Enable(EnableCap.Lighting);
            //GL.Enable(EnableCap.Texture2D);

            // test
            // テクスチャー画像ファイル
            Bitmap file = new Bitmap("texture.png");

            //png画像の反転を直す
            file.RotateFlip(RotateFlipType.RotateNoneFlipY);

            //データ読み込み
            TextureBitmapData = file.LockBits(new System.Drawing.Rectangle(0, 0, file.Width, file.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            //Textureの許可
            GL.Enable(EnableCap.Texture2D);

            //テクスチャ用バッファの生成
            Texture = GL.GenTexture();

            //テクスチャ用バッファのひもづけ
            GL.BindTexture(TextureTarget.Texture2D, Texture);

            //テクスチャの設定
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);

            //テクスチャ用バッファに色情報を流し込む
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.Rgba,
                TextureBitmapData.Width, TextureBitmapData.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte,
                TextureBitmapData.Scan0);
        }

        /// <summary>
        /// glControlのサイズ変更時に実行される。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Resize(object sender, EventArgs e)
        {
            glControl_ResizeProc();
        }

        private void glControl_ResizeProc()
        {
            int width = glControl.Size.Width;
            int height = glControl.Size.Height;
            Camera.WindowAspect = ((double)width / height);
            GL.Viewport(0, 0, width, height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            DrawerGLUtils.SetProjectionTransform(Camera);

        }

        /// <summary>
        /// glControlの描画時に実行される。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            /*
            GL.Color4(Color4.Black);
            GL.Begin(BeginMode.Polygon);
            GL.Vertex3(0.5, 0.5, 0.0);
            GL.Vertex3(-0.5, 0.5, 0.0);
            GL.Vertex3(-0.5, -0.5, 0.0);
            GL.Vertex3(0.5, -0.5, 0.0);
            GL.End();
            */
            DrawerArray.Draw();

            glControl.SwapBuffers();
        }

        private void cad2DBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.MakeBluePrint();
            var drawer = Problem.Drawer;
            DrawerArray.Clear();
            DrawerArray.Add(drawer);
            //Camera.IsPers = true; // test
            DrawerArray.InitTransform(Camera);
            glControl_ResizeProc();
            glControl.Invalidate();
        }
    }
}
