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
            Problem = new Problem();
        }

        /// <summary>
        /// ウィンドウがアクティブになった
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Activated(object sender, EventArgs e)
        {
            glControl.MakeCurrent();
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

        internal void glControl_ResizeProc()
        {
            int width = glControl.Size.Width;
            int height = glControl.Size.Height;
            Camera.WindowAspect = ((double)width / height);
            GL.Viewport(0, 0, width, height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            OpenGLUtils.SetProjectionTransform(Camera);
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
                glControl.Invalidate();
            }
        }

        /// <summary>
        /// glControl マウスボタンが押された
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int winW = viewport[2];
            int winH = viewport[3];
            MovBeginX = (2.0 * e.X - winW) / winW;
            MovBeginY = (winH - 2.0 * e.Y) / winH;
        }

        /// <summary>
        /// glControl マウスが移動した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
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
                glControl.Invalidate();
            }
        }

        /// <summary>
        /// glControl マウスホイール
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            double delta = e.Delta;
            double scale = Camera.Scale;

            scale *= Math.Pow(1.1, delta / 120.0);
            Camera.Scale = scale;

            glControl_ResizeProc();
            glControl.Invalidate();
        }

        /// <summary>
        /// glControlの描画時に実行される。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
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

            glControl.SwapBuffers();
        }

        private void cad2DBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.MakeBluePrint(this);
        }

        private void coarseMesh2DBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.MakeCoarseMesh(this);
        }

        private void mesh2DBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.MakeMesh(this);
        }

        private void mesh2DHollowLoopBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.MakeMeshHollowLoop(this);
        }

        private void drawStringBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.DrawStringTest(Camera, glControl);
        }

        private void lapackBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.InterseMatrixExample();
            Problem.LinearEquationExample();
            Problem.EigenValueExample();
        }

        private void lisBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.LisExample();
        }

        private void elasticBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = false;
            Problem.ElasticProblem(this, false, isSaintVenant);
            Problem.ElasticProblem(this, true, isSaintVenant);
        }

        private void elasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = false;
            Problem.ElasticTDProblem(this, isSaintVenant);
        }

        private void saintVenantHyperelasticBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = true;
            Problem.ElasticProblem(this, false, isSaintVenant);
        }

        private void saintVenantHyperelasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = true;
            Problem.ElasticTDProblem(this, isSaintVenant);
        }

        private void mooneyRivlinHyperelasticBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isMooney = true;
            Problem.HyperelasticProblem(this, isMooney);
        }

        private void mooneyRivlinHyperelasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isMooney = true;
            Problem.HyperelasticTDProblem(this, isMooney);
        }

        private void ogdenHyperelasticBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isMooney = false; // Ogden
            Problem.HyperelasticProblem(this, isMooney);
        }

        private void ogdenHyperelasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isMooney = false; // Ogden
            Problem.HyperelasticTDProblem(this, isMooney);
        }

        private void elasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = false;
            Problem.ElasticMultipointConstraintTDProblem(this, isSaintVenant);
        }

        private void saintVenantHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = true;
            Problem.ElasticMultipointConstraintTDProblem(this, isSaintVenant);
        }

        private void mooneyRivlinHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isMooney = true;
            Problem.HyperelasticMultipointConstraintTDProblem(this, isMooney);
        }

        private void ogdenHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isMooney = false; // Ogden
            Problem.HyperelasticMultipointConstraintTDProblem(this, isMooney);
        }

        private void elasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = false;
            Problem.ElasticContactTDProblem(this, isSaintVenant);
        }

        private void saintVenantHyperelasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = true;
            Problem.ElasticContactTDProblem(this, isSaintVenant);
        }

        private void mooneyRivlinHyperelasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isMooney = true;
            Problem.HyperelasticContactTDProblem(this, isMooney);
        }

        private void ogdenHyperelasticContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            // Ogden
            bool isMooney = false;
            Problem.HyperelasticContactTDProblem(this, isMooney);
        }

        private void elasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = false;
            Problem.ElasticCircleContactTDProblem(this, isSaintVenant);
        }

        private void saintVenantHyperelasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = true;
            Problem.ElasticCircleContactTDProblem(this, isSaintVenant);
        }

        private void mooneyRivlinHyperelasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isMooney = true;
            Problem.HyperelasticCircleContactTDProblem(this, isMooney);
        }

        private void ogdenHyperelasticCircleContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            // Ogden
            bool isMooney = false;
            Problem.HyperelasticCircleContactTDProblem(this, isMooney);
        }

        private void elasticTwoBodyContactBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = false;
            Problem.ElasticTwoBodyContactProblem(this, isSaintVenant);
        }

        private void elasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = false;
            Problem.ElasticTwoBodyContactTDProblem(this, isSaintVenant);
        }

        private void saintVenantHyperelasticTwoBodyContactBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = true;
            Problem.ElasticTwoBodyContactProblem(this, isSaintVenant);
        }

        private void saintVenantHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSaintVenant = true;
            Problem.ElasticTwoBodyContactTDProblem(this, isSaintVenant);
        }

        private void mooneyRivlinHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isMooney = true;
            Problem.HyperelasticTwoBodyContactTDProblem(this, isMooney);
        }

        private void ogdenHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            // Ogden
            bool isMooney = false;
            Problem.HyperelasticTwoBodyContactTDProblem(this, isMooney);
        }

        private void poissonBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.PoissonProblem(this);
        }

        private void diffusionBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.DiffusionProblem(this);
        }

        private void diffusionTDBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.DiffusionTDProblem(this);
        }

        private void advectionDiffusionBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.AdvectionDiffusionProblem(this);
        }

        private void advectionDiffusionTDBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.AdvectionDiffusionTDProblem(this);
        }

        private void helmholtzBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.HelmholtzProblem(this);
        }

        // mu = 0.02, 0.002 FluidEquationType.StdGNavierStokes
        // mu = 0.0002 FluidEquationType.SUPGNavierStokes
        // mu = 0.00002 Not converge
        private void stdgFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidProblem1(this, fluidEquationType);
        }

        private void stdgFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidTDProblem1(this, fluidEquationType);
        }

        private void stdgFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidProblem2(this, fluidEquationType);
        }

        private void stdgFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidTDProblem2(this, fluidEquationType);
        }

        private void supgFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidProblem1(this, fluidEquationType);
        }

        private void supgFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidTDProblem1(this, fluidEquationType);
        }

        private void supgFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidProblem2(this, fluidEquationType);
        }

        private void supgFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidTDProblem2(this, fluidEquationType);
        }

        private void stdgVorticityFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidProblem1(this, fluidEquationType);
        }

        private void stdgVorticityFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidTDProblem1(this, fluidEquationType);
        }

        private void stdgVorticityFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidProblem2(this, fluidEquationType);
        }

        private void stdgVorticityFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidTDProblem2(this, fluidEquationType);
        }

        private void supgVorticityFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidProblem1(this, fluidEquationType);
        }

        private void supgVorticityFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidTDProblem1(this, fluidEquationType);
        }

        private void supgVorticityFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidProblem2(this, fluidEquationType);
        }

        private void supgVorticityFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidTDProblem2(this, fluidEquationType);
        }

        private void waveguideBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.WaveguideProblem(this);
        }

        private void sampleFEMBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.SampleFEMProblem(this);
        }

        private void cadEditBtn_Click(object sender, RoutedEventArgs e)
        {
            var cadEditWindow = new CadEditWindow();
            cadEditWindow.Owner = this;
            cadEditWindow.ShowDialog();
        }
    }
}
