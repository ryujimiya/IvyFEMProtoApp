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
        internal ConstraintDrawerArray ConstraintDrawerArray { get; private set; } = new ConstraintDrawerArray();

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

            ConstraintDrawerArray.Draw();
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

        private void SetProblemTitle(MenuItem menuItem)
        {
            string menuItemTitle = menuItem.Header.ToString();
            this.Title = menuItemTitle + " - " + TitleBase;
        }

        private void Cad2DBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.MakeBluePrint(this);
        }

        private void CoarseMesh2DBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.MakeCoarseMesh(this);
        }

        private void Mesh2DBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.MakeMesh(this);
        }

        private void Mesh2DHollowLoopBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.MakeMeshHollowLoop(this);
        }

        private void DrawStringBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.DrawStringTest(Camera, GLControl);
        }

        private void LapackBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.InterseMatrixExample();
            Problem.LinearEquationExample();
            Problem.EigenValueExample();
        }

        private void LisBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.LisExample();
        }

        private void FFTBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.FFTExample(this);
        }

        private void ExampleFEMBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.ExampleFEMProblem(this);
        }

        private void CadEditBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            var cadEditWindow = new CadEditWindow();
            cadEditWindow.Owner = this;
            cadEditWindow.ShowDialog();
        }

        private void ElasticBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticProblem(this, false, isSaintVenant);
            Problem.ElasticProblem(this, true, isSaintVenant);
        }

        private void ElasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticTDProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticProblem(this, false, isSaintVenant);
        }

        private void SaintVenantHyperelasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticTDProblem(this, isSaintVenant);
        }

        private void MooneyRivlinHyperelasticBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticProblem(this, isMooney);
        }

        private void MooneyRivlinHyperelasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.HyperelasticProblem(this, isMooney);
        }

        private void OgdenHyperelasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.HyperelasticTDProblem(this, isMooney);
        }

        private void ElasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticMultipointConstraintTDProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticMultipointConstraintTDProblem(this, isSaintVenant);
        }

        private void MooneyRivlinHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticMultipointConstraintTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.HyperelasticMultipointConstraintTDProblem(this, isMooney);
        }

        private void ElasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticContactTDProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticContactTDProblem(this, isSaintVenant);
        }

        private void MooneyRivlinHyperelasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticContactTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticContactTDProblem(this, isMooney);
        }

        private void ElasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticCircleContactTDProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticCircleContactTDProblem(this, isSaintVenant);
        }

        private void MooneyRivlinHyperelasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticCircleContactTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticCircleContactTDProblem(this, isMooney);
        }

        private void ElasticTwoBodyContactBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticTwoBodyContactProblem(this, isSaintVenant);
        }

        private void ElasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = false;
            Problem.ElasticTwoBodyContactTDProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticTwoBodyContactBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticTwoBodyContactProblem(this, isSaintVenant);
        }

        private void SaintVenantHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isSaintVenant = true;
            Problem.ElasticTwoBodyContactTDProblem(this, isSaintVenant);
        }

        private void MooneyRivlinHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticTwoBodyContactTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticTwoBodyContactTDProblem(this, isMooney);
        }

        private void PoissonBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.PoissonProblem(this);
        }

        private void DiffusionBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.DiffusionProblem(this);
        }

        private void DiffusionTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.DiffusionTDProblem(this);
        }

        private void AdvectionDiffusionBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.AdvectionDiffusionProblem(this);
        }

        private void AdvectionDiffusionTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.AdvectionDiffusionTDProblem(this);
        }

        private void HelmholtzBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.HelmholtzProblem(this);
        }

        private void HPlaneWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.HWaveguideProblem1_0(this);
            Problem.HWaveguideProblem1(this);
        }

        private void HPlaneWaveguide2Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.HWaveguideProblem2_0(this);
            Problem.HWaveguideProblem2(this);
        }

        private void EPlaneWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.EWaveguideProblem1_0(this);
            Problem.EWaveguideProblem1(this);
        }

        private void HPlaneWaveguideHigherOrderABC1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            /*
            //Problem.HWaveguideHigherOrderABCProblema1_0(this);
            Problem.HWaveguideHigherOrderABCProblema1(this);
            */

            //Problem.HWaveguideHigherOrderABCProblemb1_0(this);
            Problem.HWaveguideHigherOrderABCProblemb1(this);
        }

        private void HPlaneWaveguideHigherOrderABC2Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            /*
            //Problem.HWaveguideHigherOrderABCProblema2_0(this);
            Problem.HWaveguideHigherOrderABCProblema2(this);
            */

            //Problem.HWaveguideHigherOrderABCProblemb2_0(this);
            Problem.HWaveguideHigherOrderABCProblemb2(this);
        }

        private void HPlaneWaveguideHigherOrderABC3Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            /*
            //Problem.HWaveguideHigherOrderABCProblema3_0(this);
            Problem.HWaveguideHigherOrderABCProblema3(this);
            */

            //Problem.HWaveguideHigherOrderABCProblemb3_0(this);
            Problem.HWaveguideHigherOrderABCProblemb3(this);
        }

        private void HPlaneWaveguideHigherOrderABCTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            /*
            //Problem.HWaveguideHigherOrderABCTDProblema1_0(this);
            Problem.HWaveguideHigherOrderABCTDProblema1(this);
            */

            //Problem.HWaveguideHigherOrderABCTDProblemb1_0(this);
            Problem.HWaveguideHigherOrderABCTDProblemb1(this);
        }

        private void HPlaneWaveguideHigherOrderABCTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCTDProblemb2_0(this);
            Problem.HWaveguideHigherOrderABCTDProblemb2(this);
        }

        private void HPlaneWaveguideHigherOrderABCTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCTDProblemb3_0(this);
            Problem.HWaveguideHigherOrderABCTDProblemb3(this);
        }

        private void HPlaneWaveguideFirstOrderABCTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.HWaveguideFirstOrderABCTDProblemb3_0(this);
            Problem.HWaveguideFirstOrderABCTDProblemb3(this);
        }

        private void HPlaneWaveguidePML1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            /*
            //Problem.HWaveguidePMLProblema1_0(this);
            Problem.HWaveguidePMLProblema1(this);
            */

            //Problem.HWaveguidePMLProblemb1_0(this);
            Problem.HWaveguidePMLProblemb1(this);
        }

        private void HPlaneWaveguidePML2Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            /*
            //Problem.HWaveguidePMLProblema2_0(this);
            Problem.HWaveguidePMLProblema2(this);
            */

            //Problem.HWaveguidePMLProblemb2_0(this);
            Problem.HWaveguidePMLProblemb2(this);
        }

        private void HPlaneWaveguidePML3Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            /*
            //Problem.HWaveguidePMLProblema3_0(this);
            Problem.HWaveguidePMLProblema3(this);
            */

            //Problem.HWaveguidePMLProblemb3_0(this);
            Problem.HWaveguidePMLProblemb3(this);
        }

        private void HPlaneWaveguidePMLTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.HWaveguidePMLTDProblemb1_0(this);
            Problem.HWaveguidePMLTDProblemb1(this);
        }

        private void HPlaneWaveguidePMLTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.HWaveguidePMLTDProblemb2_0(this);
            Problem.HWaveguidePMLTDProblemb2(this);
        }

        private void HPlaneWaveguidePMLTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.HWaveguidePMLTDProblemb3_0(this);
            Problem.HWaveguidePMLTDProblemb3(this);
        }

        private void SquareLatticePCWaveguideBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.PCWaveguideSquareLatticeProblem1_0(this);
            Problem.PCWaveguideSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguideBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.PCWaveguideTriangleLatticeProblem1_0(this);
            Problem.PCWaveguideTriangleLatticeProblem1(this);
        }

        // mu = 0.02, 0.002 FluidEquationType.StdGNavierStokes
        // mu = 0.0002 FluidEquationType.SUPGNavierStokes
        // mu = 0.00002 Not converge
        private void StdGFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidProblem1(this, fluidEquationType);
        }

        private void StdGFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidTDProblem1(this, fluidEquationType);
        }

        private void StdGFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidProblem2(this, fluidEquationType);
        }

        private void StdGFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidTDProblem2(this, fluidEquationType);
        }

        private void SUPGFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidProblem1(this, fluidEquationType);
        }

        private void SUPGFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidTDProblem1(this, fluidEquationType);
        }

        private void SUPGFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidProblem2(this, fluidEquationType);
        }

        private void SUPGFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidTDProblem2(this, fluidEquationType);
        }

        private void StdGVorticityFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidProblem1(this, fluidEquationType);
        }

        private void StdGVorticityFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidTDProblem1(this, fluidEquationType);
        }

        private void StdGVorticityFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidProblem2(this, fluidEquationType);
        }

        private void StdGVorticityFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidTDProblem2(this, fluidEquationType);
        }

        private void SUPGVorticityFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidProblem1(this, fluidEquationType);
        }

        private void SUPGVorticityFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidTDProblem1(this, fluidEquationType);
        }

        private void SUPGVorticityFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidProblem2(this, fluidEquationType);
        }

        private void SUPGVorticityFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidTDProblem2(this, fluidEquationType);
        }

        private void StdGVorticityFluid1RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.VorticityFluidRKTDProblem1(this);
        }

        private void StdGVorticityFluid2RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.VorticityFluidRKTDProblem2(this);
        }

        private void StdGPressurePoissonFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.PressurePoissonFluidProblem1(this);
            Problem.PressurePoissonWithBellFluidProblem1(this);
        }

        private void StdGPressurePoissonFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.PressurePoissonFluidTDProblem1(this);
            Problem.PressurePoissonWithBellFluidTDProblem1(this);
        }

        private void StdGPressurePoissonFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.PressurePoissonFluidProblem2(this);
            Problem.PressurePoissonWithBellFluidProblem2(this);
        }

        private void StdGPressurePoissonFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.PressurePoissonFluidTDProblem2(this);
            Problem.PressurePoissonWithBellFluidTDProblem2(this);
        }

        private void StdGPressurePoissonFluid1RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.PressurePoissonFluidRKTDProblem1(this);
            MessageBox.Show("Not implemented yet!");
        }

        private void StdGPressurePoissonFluid2RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            //Problem.PressurePoissonFluidRKTDProblem2(this);
            MessageBox.Show("Not implemented yet!");
        }

        private void Optimize1Btn_Click(object sender, RoutedEventArgs e)
        {
            SetProblemTitle(e.Source as MenuItem);

            Problem.Optimize1Problem(this);
        }
    }
}
