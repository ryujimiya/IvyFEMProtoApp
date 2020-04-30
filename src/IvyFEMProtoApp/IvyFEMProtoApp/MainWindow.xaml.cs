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
        private Problem Problem = null;

        /// <summary>
        /// カメラ
        /// </summary>
        internal Camera2D Camera { get; private set; } = new Camera2D();

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
            Problem = new Problem();
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
                else/* if (Modifiers.HasFlag(System.Windows.Forms.Keys.Shift))*/
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
            Problem.Init(this);
        }
        
        private void SetProblemTitle(MenuItem menuItem)
        {
            string menuItemTitle = menuItem.Header.ToString();
            this.Title = menuItemTitle + " - " + TitleBase;
        }

        private void Cad2DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeBluePrint(this);
        }

        private void CoarseMesh2DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCoarseMesh(this);
        }

        private void Mesh2DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMesh(this);
        }

        private void Mesh2DHollowLoopBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMeshHollowLoop(this);
        }

        private void DrawStringBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DrawStringTest(Camera, GLControl);
        }

        private void LapackBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.InterseMatrixExample();
            Problem.LinearEquationExample();
            Problem.EigenValueExample();
        }

        private void LisBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.LisExample();
        }

        private void FFTBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FFTExample(this);
        }

        private void ExampleFEMBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ExampleFEMProblem(this);
        }

        private void CadEditBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            var cadEditWindow = new CadEditWindow();
            cadEditWindow.Owner = this;
            cadEditWindow.ShowDialog();
        }

        private void ElasticLinear1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticLinearStVenantProblem1(this, false, isSaintVenant);
            Problem.ElasticLinearStVenantProblem1(this, true, isSaintVenant);
        }

        private void ElasticLinearTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticTDProblem1(this, isSaintVenant);
        }

        private void ElasticLinear2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticLinearStVenantProblem2(this, isSaintVenant);
        }

        private void ElasticLinearTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticTDProblem2(this, isSaintVenant);
        }

        private void ElasticLinearEigenBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticLinearStVenantEigenProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelastic1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticLinearStVenantProblem1(this, false, isSaintVenant);
        }

        private void SaintVenantHyperelasticTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticTDProblem1(this, isSaintVenant);
        }

        private void SaintVenantHyperelastic2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticLinearStVenantProblem2(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticTDProblem2(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticEigenBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticLinearStVenantEigenProblem(this, isSaintVenant);
        }

        private void MooneyRivlinHyperelasticBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticProblem(this, isMooney);
        }

        private void MooneyRivlinHyperelasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.HyperelasticProblem(this, isMooney);
        }

        private void OgdenHyperelasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.HyperelasticTDProblem(this, isMooney);
        }

        private void ElasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticMultipointConstraintTDProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticMultipointConstraintTDProblem(this, isSaintVenant);
        }

        private void MooneyRivlinHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticMultipointConstraintTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.HyperelasticMultipointConstraintTDProblem(this, isMooney);
        }

        private void ElasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticContactTDProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticContactTDProblem(this, isSaintVenant);
        }

        private void MooneyRivlinHyperelasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticContactTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticContactTDProblem(this, isMooney);
        }

        private void ElasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticCircleContactTDProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticCircleContactTDProblem(this, isSaintVenant);
        }

        private void MooneyRivlinHyperelasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticCircleContactTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticCircleContactTDProblem(this, isMooney);
        }

        private void ElasticTwoBodyContactBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticTwoBodyContactProblem(this, isSaintVenant);
        }

        private void ElasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticTwoBodyContactTDProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticTwoBodyContactBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticTwoBodyContactProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticTwoBodyContactTDProblem(this, isSaintVenant);
        }

        private void MooneyRivlinHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticTwoBodyContactTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticTwoBodyContactTDProblem(this, isMooney);
        }

        private void Truss1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TrussProblem1(this);
        }

        private void TrussTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TrussTDProblem1(this);
        }

        private void Truss2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TrussProblem2(this);
        }

        private void TrussTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TrussTDProblem2(this);
        }

        private void Beam1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamProblem1(this, isTimoshenko);
        }

        private void BeamTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamTDProblem1(this, isTimoshenko);
        }

        private void BeamEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamEigenProblem1(this, isTimoshenko);
        }

        private void Beam2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamProblem2(this, isTimoshenko);
        }

        private void BeamTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamTDProblem2(this, isTimoshenko);
        }

        private void Beam3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamProblem3(this, isTimoshenko);
        }

        private void BeamTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamTDProblem3(this, isTimoshenko);
        }

        private void Frame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameProblem0(this, isTimoshenko);
        }

        private void FrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameTDProblem0(this, isTimoshenko);
        }

        private void FrameEigen0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameEigenProblem0(this, isTimoshenko);
        }

        private void Frame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameProblem1(this, isTimoshenko);
        }

        private void FrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameTDProblem1(this, isTimoshenko);
        }

        private void Frame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameProblem2(this, isTimoshenko);
        }

        private void FrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameTDProblem2(this, isTimoshenko);
        }

        private void Frame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameProblem3(this, isTimoshenko);
        }

        private void FrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameTDProblem3(this, isTimoshenko);
        }

        private void TimoshenkoBeam1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamProblem1(this, isTimoshenko);
        }

        private void TimoshenkoBeamTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamTDProblem1(this, isTimoshenko);
        }

        private void TimoshenkoBeamEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamEigenProblem1(this, isTimoshenko);
        }

        private void TimoshenkoBeam2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamProblem2(this, isTimoshenko);
        }

        private void TimoshenkoBeamTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamTDProblem2(this, isTimoshenko);
        }

        private void TimoshenkoBeam3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamProblem3(this, isTimoshenko);
        }

        private void TimoshenkoBeamTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamTDProblem3(this, isTimoshenko);
        }

        private void TimoshenkoFrame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameProblem0(this, isTimoshenko);
        }

        private void TimoshenkoFrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameTDProblem0(this, isTimoshenko);
        }

        private void TimoshenkoFrameEigen0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameEigenProblem0(this, isTimoshenko);
        }

        private void TimoshenkoFrame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameProblem1(this, isTimoshenko);
        }

        private void TimoshenkoFrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameTDProblem1(this, isTimoshenko);
        }

        private void TimoshenkoFrame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameProblem2(this, isTimoshenko);
        }

        private void TimoshenkoFrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameTDProblem2(this, isTimoshenko);
        }

        private void TimoshenkoFrame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameProblem3(this, isTimoshenko);
        }

        private void TimoshenkoFrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameTDProblem3(this, isTimoshenko);
        }

        private void CorotationalFrame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameProblem0(this, isTimoshenko);
        }

        private void CorotationalFrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameTDProblem0(this, isTimoshenko);
        }

        private void CorotationalFrame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameProblem1(this, isTimoshenko);
        }

        private void CorotationalFrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameTDProblem1(this, isTimoshenko);
        }

        private void CorotationalFrame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameProblem2(this, isTimoshenko);
        }

        private void CorotationalFrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameTDProblem2(this, isTimoshenko);
        }

        private void CorotationalFrame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameProblem3(this, isTimoshenko);
        }

        private void CorotationalFrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameTDProblem3(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameProblem0(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameTDProblem0(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameProblem1(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameTDProblem1(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameProblem2(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameTDProblem2(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameProblem3(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameTDProblem3(this, isTimoshenko);
        }

        private void FieldConsistentTLFrame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameProblem0(this);
        }

        private void FieldConsistentTLFrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameTDProblem0(this);
        }

        private void FieldConsistentTLFrame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameProblem1(this);
        }

        private void FieldConsistentTLFrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameTDProblem1(this);
        }

        private void FieldConsistentTLFrame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameProblem2(this);
        }

        private void FieldConsistentTLFrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameTDProblem2(this);
        }

        private void FieldConsistentTLFrame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameProblem3(this);
        }

        private void FieldConsistentTLFrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLTDFrameProblem3(this);
        }

        private void TimoshenkoTLFrame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameProblem0(this);
        }

        private void TimoshenkoTLFrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameTDProblem0(this);
        }

        private void TimoshenkoTLFrame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameProblem1(this);
        }

        private void TimoshenkoTLFrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameTDProblem1(this);
        }

        private void TimoshenkoTLFrame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameProblem2(this);
        }

        private void TimoshenkoTLFrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameTDProblem2(this);
        }

        private void TimoshenkoTLFrame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameProblem3(this);
        }

        private void TimoshenkoTLFrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameTDProblem3(this);
        }

        private void PoissonBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PoissonProblem(this);
        }

        private void DiffusionBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DiffusionProblem(this);
        }

        private void DiffusionTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DiffusionTDProblem(this);
        }

        private void AdvectionDiffusionBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.AdvectionDiffusionProblem(this);
        }

        private void AdvectionDiffusionTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.AdvectionDiffusionTDProblem(this);
        }

        private void HelmholtzBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.HelmholtzProblem(this);
        }

        private void HPlaneWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideProblem1_0(this);
            Problem.HWaveguideProblem1(this);
        }

        private void HPlaneWaveguide2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideProblem2_0(this);
            Problem.HWaveguideProblem2(this);
        }

        private void EPlaneWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.EWaveguideProblem1_0(this);
            Problem.EWaveguideProblem1(this);
        }

        private void HPlaneWaveguideHigherOrderABC1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCProblem1_0(this);
            Problem.HWaveguideHigherOrderABCProblem1(this);
        }

        private void HPlaneWaveguideHigherOrderABC2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCProblem2_0(this);
            Problem.HWaveguideHigherOrderABCProblem2(this);
        }

        private void HPlaneWaveguideHigherOrderABC3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCProblem3_0(this);
            Problem.HWaveguideHigherOrderABCProblem3(this);
        }

        private void HPlaneWaveguideHigherOrderABCTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCTDProblem1_0(this);
            Problem.HWaveguideHigherOrderABCTDProblem1(this);
        }

        private void HPlaneWaveguideHigherOrderABCTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCTDProblem2_0(this);
            Problem.HWaveguideHigherOrderABCTDProblem2(this);
        }

        private void HPlaneWaveguideHigherOrderABCTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCTDProblem3_0(this);
            Problem.HWaveguideHigherOrderABCTDProblem3(this);
        }

        private void HPlaneWaveguideFirstOrderABCTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideFirstOrderABCTDProblem3_0(this);
            Problem.HWaveguideFirstOrderABCTDProblem3(this);
        }

        private void HPlaneWaveguidePML1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLProblem1_0(this);
            Problem.HWaveguidePMLProblem1(this);
        }

        private void HPlaneWaveguidePML2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLProblem2_0(this);
            Problem.HWaveguidePMLProblem2(this);
        }

        private void HPlaneWaveguidePML3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLProblem3_0(this);
            Problem.HWaveguidePMLProblem3(this);
        }

        private void HPlaneWaveguidePMLTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLTDProblem1_0(this);
            Problem.HWaveguidePMLTDProblem1(this);
        }

        private void HPlaneWaveguidePMLTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLTDProblem2_0(this);
            Problem.HWaveguidePMLTDProblem2(this);
        }

        private void HPlaneWaveguidePMLTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLTDProblem3_0(this);
            Problem.HWaveguidePMLTDProblem3(this);
        }

        private void Waveguide2DEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 1;
            Problem.Waveguide2DEigenProblem1(this, false, feOrder);
            WPFUtils.DoEvents(20 * 1000);
            Problem.Waveguide2DEigenProblem1(this, true, feOrder);
        }

        private void Waveguide2DEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 1;
            Problem.Waveguide2DEigenProblem2(this, feOrder);
        }

        private void Waveguide2DEigen3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 1;
            Problem.Waveguide2DEigenProblem3(this, feOrder);
        }

        private void Waveguide2DEigen2ndOrder1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 2;
            Problem.Waveguide2DEigenProblem1(this, false, feOrder);
            WPFUtils.DoEvents(20 * 1000);
            Problem.Waveguide2DEigenProblem1(this, true, feOrder);
        }

        private void Waveguide2DEigen2ndOrder2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 2;
            Problem.Waveguide2DEigenProblem2(this, feOrder);
        }

        private void Waveguide2DEigen2ndOrder3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 2;
            Problem.Waveguide2DEigenProblem3(this, feOrder);
        }

        private void Waveguide2DEigenOpen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 1;
            Problem.Waveguide2DEigenOpenProblem2(this, feOrder);
        }

        private void Waveguide2DEigenOpen3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 1;
            Problem.Waveguide2DEigenOpenProblem3(this, feOrder);
        }

        private void Waveguide2DEigenOpen2ndOrder2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 2;
            Problem.Waveguide2DEigenOpenProblem2(this, feOrder);
        }

        private void Waveguide2DEigenOpen2ndOrder3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 2;
            Problem.Waveguide2DEigenOpenProblem3(this, feOrder);
        }

        private void SquareLatticePCWaveguideEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PCWaveguideEigenSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguideEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PCWaveguideEigenTriangleLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguideEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PCWaveguideEigenTriangleLatticeProblem2(this);
        }

        private void SquareLatticePBG1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PBGSquareLatticeProblem1(this);
        }

        private void TriangleLatticePBG1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PBGTriangleLatticeProblem1(this);
        }

        private void TriangleLatticePBG2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PBGTriangleLatticeProblem2(this);
        }

        private void SquareLatticePCWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguideSquareLatticeProblem1_0(this);
            Problem.PCWaveguideSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguideTriangleLatticeProblem1_0(this);
            Problem.PCWaveguideTriangleLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguide2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguideTriangleLatticeProblem2_0(this);
            Problem.PCWaveguideTriangleLatticeProblem2(this);
        }

        private void TriangleLatticePCWaveguide3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PCWaveguideTriangleLatticeProblem3(this);
        }

        private void SquareLatticePCWaveguidePBC1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePBCSquareLatticeProblem1_0(this);
            Problem.PCWaveguidePBCSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguidePBC1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePBCTriangleLatticeProblem1_0(this);
            Problem.PCWaveguidePBCTriangleLatticeProblem1(this);
        }

        private void SquareLatticePCWaveguideModalABCZTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguideModalABCZTDSquareLatticeProblem1_0(this);
            Problem.PCWaveguideModalABCZTDSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguideModalABCZTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguideModalABCZTDTriangleLatticeProblem1_0(this);
            Problem.PCWaveguideModalABCZTDTriangleLatticeProblem1(this);
        }

        private void SquareLatticePCWaveguidePML1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePMLSquareLatticeProblem1_0(this);
            Problem.PCWaveguidePMLSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguidePML1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePMLTriangleLatticeProblem1_0(this);
            Problem.PCWaveguidePMLTriangleLatticeProblem1(this);
        }

        private void SquareLatticePCWaveguidePMLTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePMLTDSquareLatticeProblem1_0(this);
            Problem.PCWaveguidePMLTDSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguidePMLTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePMLTDTriangleLatticeProblem1_0(this);
            Problem.PCWaveguidePMLTDTriangleLatticeProblem1(this);
        }

        // mu = 0.02, 0.002 FluidEquationType.StdGNavierStokes
        // mu = 0.0002 FluidEquationType.SUPGNavierStokes
        // mu = 0.00002 Not converge
        private void StdGFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidProblem1(this, fluidEquationType);
        }

        private void StdGFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidTDProblem1(this, fluidEquationType);
        }

        private void StdGFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidProblem2(this, fluidEquationType);
        }

        private void StdGFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidTDProblem2(this, fluidEquationType);
        }

        private void SUPGFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidProblem1(this, fluidEquationType);
        }

        private void SUPGFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidTDProblem1(this, fluidEquationType);
        }

        private void SUPGFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidProblem2(this, fluidEquationType);
        }

        private void SUPGFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidTDProblem2(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PressurePoissonFluidProblem1(this);

            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidProblem1(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PressurePoissonFluidTDProblem1(this);

            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidTDProblem1(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PressurePoissonFluidProblem2(this);

            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidProblem2(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PressurePoissonFluidTDProblem2(this);

            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidTDProblem2(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid1RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PressurePoissonFluidRKTDProblem1(this); // NG

            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidRKTDProblem1(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid2RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            MessageBox.Show("FIX ME: You won't get results.");
            //Problem.PressurePoissonFluidRKTDProblem2(this); // NG
            Problem.PressurePoissonWithBellFluidRKTDProblem2(this); // NG
        }

        private void SUPGPressurePoissonFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //MessageBox.Show("FIX ME: now, conv ratio = 10^-4");
            MessageBox.Show("FIX ME: now, conv ratio = 10^-3");
            FluidEquationType fluidEquationType = FluidEquationType.SUPGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidProblem1(this, fluidEquationType);
        }

        private void SUPGPressurePoissonFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //これは10^-4のまま
            MessageBox.Show("FIX ME: now, conv ratio = 10^-4");
            FluidEquationType fluidEquationType = FluidEquationType.SUPGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidTDProblem1(this, fluidEquationType);
        }

        private void SUPGPressurePoissonFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //MessageBox.Show("FIX ME: now, conv ratio = 10^-4");
            MessageBox.Show("FIX ME: now, conv ratio = 10^-3");
            FluidEquationType fluidEquationType = FluidEquationType.SUPGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidProblem2(this, fluidEquationType);
        }

        private void SUPGPressurePoissonFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //MessageBox.Show("FIX ME: now, conv ratio = 10^-4");
            MessageBox.Show("FIX ME: now, conv ratio = 10^-3");
            FluidEquationType fluidEquationType = FluidEquationType.SUPGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidTDProblem2(this, fluidEquationType);
        }

        private void SUPGPressurePoissonFluid1RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            MessageBox.Show("FIXME: Not work! diverge!");
            FluidEquationType fluidEquationType = FluidEquationType.SUPGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidRKTDProblem1(this, fluidEquationType);
        }

        private void StdGVorticityFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidProblem1(this, fluidEquationType);
        }

        private void StdGVorticityFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidTDProblem1(this, fluidEquationType);
        }

        private void StdGVorticityFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidProblem2(this, fluidEquationType);
        }

        private void StdGVorticityFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidTDProblem2(this, fluidEquationType);
        }

        private void SUPGVorticityFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidProblem1(this, fluidEquationType);
        }

        private void SUPGVorticityFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidTDProblem1(this, fluidEquationType);
        }

        private void SUPGVorticityFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidProblem2(this, fluidEquationType);
        }

        private void SUPGVorticityFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidTDProblem2(this, fluidEquationType);
        }

        private void StdGVorticityFluid1RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.VorticityFluidRKTDProblem1(this);
        }

        private void StdGVorticityFluid2RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.VorticityFluidRKTDProblem2(this);
        }

        private void Optimize1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.Optimize1Problem(this);
        }
    }
}
