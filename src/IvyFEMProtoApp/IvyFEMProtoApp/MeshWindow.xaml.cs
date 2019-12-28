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
using System.Windows.Shapes;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using IvyFEM;

namespace IvyFEMProtoApp
{
    /// <summary>
    /// MeshWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MeshWindow : Window
    {
        private MeshDraw2D MeshDraw = null;

        public double BackgroundWidth { get; private set; } = 0;
        public double BackgroundHeight { get; private set; } = 0;
        public Mesher2D Mesher { get; private set; } = null;

        public MeshWindow()
        {
            InitializeComponent();
        }

        public void Init(Mesher2D mesher, double width, double height)
        {
            Mesher = mesher;
            BackgroundWidth = width;
            BackgroundHeight = height;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MeshDraw = new MeshDraw2D(GLControl, BackgroundWidth, BackgroundHeight);

            GLControl.MakeCurrent();
            MeshDraw.Init(Mesher);
        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
            GLControl.MakeCurrent();
            GLControl.Invalidate();
            GLControl.Update();
        }

        private void GLControl_Load(object sender, EventArgs e)
        {
            GLControl.MakeCurrent();
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(Color4.Black);
        }

        private void GLControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            if (MeshDraw != null)
            {
                GLControl.MakeCurrent();
                MeshDraw.PanelPaint();
            }
        }

        private void GLControl_Resize(object sender, EventArgs e)
        {
            if (MeshDraw != null)
            {
                GLControl.MakeCurrent();
                MeshDraw.PanelResize();
            }
        }

        private void GLControl_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        private void GLControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        private void GLControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        private void GLControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        private void GLControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        private void GLControl_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {

        }

        private void GLControl_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {

        }

        private void GLControl_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {

        }
    }
}
