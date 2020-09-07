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
//using OpenTK; // System.Numericsと衝突するので注意
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
        /// タイトルベース
        /// </summary>
        private string TitleBase = "";

        /// <summary>
        /// 問題
        /// </summary>
        private Problem Problem = new Problem();

        private int Dimension => Problem.Dimension;

        /// <summary>
        /// カメラ
        /// </summary>
        private Camera2D Camera2D = new Camera2D();
        private Camera3D Camera3D = new Camera3D();
        internal Camera Camera => Dimension == 2 ? (Camera)Camera2D : (Camera)Camera3D;

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
        /// 場を描画する?
        /// </summary>
        internal bool IsFieldDraw { get; set; } = false;

        /// <summary>
        /// 描画アレイ
        /// </summary>
        internal DrawerArray DrawerArray { get; private set; } = new DrawerArray();
        internal FieldDrawerArray FieldDrawerArray { get; private set; } = new FieldDrawerArray();

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
            TitleBase = this.Title;
        }

        /// <summary>
        /// ウィンドウがアクティブになった
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Activated(object sender, EventArgs e)
        {
            GLControl.MakeCurrent();
        }

        /// <summary>
        /// ウィンドウが閉じられようとしている
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // リソースの開放が遅いので強制終了
            System.Environment.Exit(0);
        }

        /// <summary>
        /// GLControlの起動時に実行される。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLControl_Load(object sender, EventArgs e)
        {
            GL.Enable(EnableCap.DepthTest);
        }

        /// <summary>
        /// GLControlのサイズ変更時に実行される。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLControl_Resize(object sender, EventArgs e)
        {
            GLControl_ResizeProc();
        }

        internal void GLControl_ResizeProc()
        {
            int width = GLControl.Size.Width;
            int height = GLControl.Size.Height;
            Camera.WindowAspect = ((double)width / height);
            GL.Viewport(0, 0, width, height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            OpenGLUtils.SetProjectionTransform(Camera);
        }

        /// <summary>
        /// GLControl キーが押下された
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLControl_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            Modifiers = e.Modifiers;
        }

        /// <summary>
        /// GLControl キーが離された
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLControl_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            Modifiers = new System.Windows.Forms.Keys();
        }

        /// <summary>
        /// GLControl クリックされた
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLControl_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (IsFieldDraw)
            {
                return;
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Left &&
                !Modifiers.HasFlag(System.Windows.Forms.Keys.Control) &&
                !Modifiers.HasFlag(System.Windows.Forms.Keys.Shift))
            {
                int sizeBuffer = 2048;
                int[] pickSelectBuffer = new int[sizeBuffer];

                OpenGLUtils.PickPre(
                    sizeBuffer, pickSelectBuffer,
                    (uint)e.X, (uint)e.Y, 5, 5, Camera);
                DrawerArray.DrawSelection();

                IList<SelectedObject> selectedObjs = OpenGLUtils.PickPost(pickSelectBuffer,
                    (uint)e.X, (uint)e.Y, Camera);

                DrawerArray.ClearSelected();
                if (selectedObjs.Count > 0)
                {
                    DrawerArray.AddSelected(selectedObjs[0].Name);
                    System.Diagnostics.Debug.WriteLine("selectedObjs[0].Name[1] = " + selectedObjs[0].Name[1]);
                }
                GLControl.Invalidate();
            }
        }

        /// <summary>
        /// GLControl マウスボタンが押された
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int winW = viewport[2];
            int winH = viewport[3];
            MovBeginX = (2.0 * e.X - winW) / winW;
            MovBeginY = (winH - 2.0 * e.Y) / winH;
        }

        /// <summary>
        /// GLControl マウスが移動した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.None)
            {
                // ドラッグ中でない
                return;
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                int[] viewport = new int[4];
                GL.GetInteger(GetPName.Viewport, viewport);

                int winW = viewport[2];
                int winH = viewport[3];
                double movEndX = (2.0 * e.X - winW) / winW;
                double movEndY = (winH - 2.0 * e.Y) / winH;
                if (Modifiers.HasFlag(System.Windows.Forms.Keys.Control))
                {
                    Camera.MouseRotation(MovBeginX, MovBeginY, movEndX, movEndY);
                }
                else
                {
                    Camera.MousePan(MovBeginX, MovBeginY, movEndX, movEndY);
                }
                MovBeginX = movEndX;
                MovBeginY = movEndY;
                GLControl.Invalidate();
            }
        }

        /// <summary>
        /// GLControl マウスホイール
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            double delta = e.Delta;
            double scale = Camera.Scale;

            scale *= Math.Pow(1.1, delta / 120.0);
            Camera.Scale = scale;

            GLControl_ResizeProc();
            GLControl.Invalidate();
        }

        /// <summary>
        /// GLControlの描画時に実行される。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            GL.ClearColor(Color4.White);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(1.1f, 4.0f);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            OpenGLUtils.SetModelViewTransform(Camera);

            if (IsFieldDraw)
            {
                FieldDrawerArray.Draw();
            }
            else
            {
                DrawerArray.Draw();
            }

            GLControl.SwapBuffers();
        }

        private void InitProblem(MenuItem menuItem)
        {
            SetProblemTitle(menuItem);

            Camera2D = new Camera2D();
            Camera3D = new Camera3D();

            Problem.Init(this);
        }
        
        private void SetProblemTitle(MenuItem menuItem)
        {
            string menuItemTitle = menuItem.Header.ToString();
            this.Title = menuItemTitle + " - " + TitleBase;
        }

        ////////////////////////////////////////////////////////////////
        private void ResetZoomBtn_Click(object sender, RoutedEventArgs e)
        {
            Camera.Scale = 1.0;
            GLControl_ResizeProc();
            GLControl.Invalidate();
        }

        public void DoZoom(int count, bool zoomIn)
        {
            double scale = Camera.Scale;

            double delta = zoomIn ?
                30.0 * count : -30.0 * count;
            scale *= Math.Pow(1.1, delta / 120.0);
            Camera.Scale = scale;

            GLControl_ResizeProc();
            GLControl.Invalidate();
            GLControl.Update();
        }

        private void ZoomInBtn_Click(object sender, RoutedEventArgs e)
        {
            int count = 1;
            var window = new InputWindow();
            window.Owner = this;
            window.TextBox1.Text = string.Format("{0}", count);
            window.ShowDialog();
            count = int.Parse(window.TextBox1.Text);

            DoZoom(count, true);
        }


        private void ZoomOutBtn_Click(object sender, RoutedEventArgs e)
        {
            int count = 1;
            var window = new InputWindow();
            window.Owner = this;
            window.TextBox1.Text = string.Format("{0}", count);
            window.ShowDialog();
            count = int.Parse(window.TextBox1.Text);

            DoZoom(count, false);
        }
    }
}
