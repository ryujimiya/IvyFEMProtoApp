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
        /// <summary>
        /// 問題
        /// </summary>
        private Problem Problem = null;

        /// <summary>
        /// カメラ
        /// </summary>
        private Camera Camera = new Camera();

        /// <summary>
        /// マウス移動量X方向
        /// </summary>
        private double MovBeginX = 0;

        /// <summary>
        /// マウス移動量Y方向
        /// </summary>
        private double MovBeginY = 0;

        /// <summary>
        /// キー入力修飾子
        /// </summary>
        private System.Windows.Forms.Keys Modifiers = new System.Windows.Forms.Keys();

        /// <summary>
        /// 描画アレイ
        /// </summary>
        private DrawerArray DrawerArray = new DrawerArray();

        //private BitmapData TextureBitmapData;
        //private int Texture;

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
            GL.Enable(EnableCap.DepthTest);
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
        /// glControl キーが押下された
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            Modifiers = e.Modifiers;
        }

        /// <summary>
        /// glControl キーが離された
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            Modifiers = new System.Windows.Forms.Keys();
        }

        /// <summary>
        /// glControl クリックされた
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int winW = viewport[2];
            int winH = viewport[3];
            MovBeginX = (2.0 * e.X - winW) / winW;
            MovBeginY = (winH - 2.0 * e.Y) / winH;
            if (e.Button == System.Windows.Forms.MouseButtons.Left &&
                !Modifiers.HasFlag(System.Windows.Forms.Keys.Control) &&
                !Modifiers.HasFlag(System.Windows.Forms.Keys.Shift))
            {
                int sizeBuffer = 2048;
                int[] pickSelectBuffer = new int[sizeBuffer];

                DrawerGLUtils.PickPre(
                    sizeBuffer, pickSelectBuffer,
                    (uint)e.X, (uint)e.Y, 5, 5, Camera);
                DrawerArray.DrawSelection();

                IList<SelectedObject> selectedObjs = DrawerGLUtils.PickPost(pickSelectBuffer,
                    (uint)e.X, (uint)e.Y, Camera);

                DrawerArray.ClearSelected();
                if (selectedObjs.Count > 0)
                {
                    DrawerArray.AddSelected(selectedObjs[0].Name);
                    System.Diagnostics.Debug.WriteLine("selectedObjs[0].Name[1] = " + selectedObjs[0].Name[1]);
                }
                glControl.Invalidate();
            }
        }

        /// <summary>
        /// glControlの描画時に実行される。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            GL.ClearColor(Color4.White);
            //GL.ClearColor(0.2f, 0.7f, 0.7f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(1.1f, 4.0f);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            DrawerGLUtils.SetModelViewTransform(Camera);

            DrawerArray.Draw();

            glControl.SwapBuffers();
        }

        private void cad2DBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.MakeBluePrint();
            var drawer = Problem.Drawer;
            DrawerArray.Clear();
            DrawerArray.Add(drawer);
            DrawerArray.InitTransform(Camera);
            glControl_ResizeProc();
            glControl.Invalidate();
        }

        private void coarseMesh2DBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.MakeCoarseMesh();
            var drawer = Problem.Drawer;
            DrawerArray.Clear();
            DrawerArray.Add(drawer);
            DrawerArray.InitTransform(Camera);
            glControl_ResizeProc();
            glControl.Invalidate();
        }

        private void mesh2DBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.MakeMesh();
            var drawer = Problem.Drawer;
            DrawerArray.Clear();
            DrawerArray.Add(drawer);
            DrawerArray.InitTransform(Camera);
            glControl_ResizeProc();
            glControl.Invalidate();
        }
    }
}
