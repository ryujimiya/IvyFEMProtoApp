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
        private MeshDraw MeshDraw = null;

        public double BackgroundWidth { get; private set; } = 0;
        public double BackgroundHeight { get; private set; } = 0;
        public Mesher2D Mesher2D { get; private set; } = null;

        public MeshWindow()
        {
            InitializeComponent();
        }

        public void Set(Mesher2D mesher2D, double width, double height)
        {
            Mesher2D = mesher2D;
            BackgroundWidth = width;
            BackgroundHeight = height;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MeshDraw = new MeshDraw(glControl, BackgroundWidth, BackgroundHeight);

            glControl.MakeCurrent();
            MeshDraw.Init(Mesher2D);
        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
            glControl.MakeCurrent();
            glControl.Invalidate();
            glControl.Update();
        }

        private void glControl_Load(object sender, EventArgs e)
        {
            glControl.MakeCurrent();
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(Color4.Black);
        }

        private void glControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            if (MeshDraw != null)
            {
                glControl.MakeCurrent();
                MeshDraw.PanelPaint();
            }
        }

        private void glControl_Resize(object sender, EventArgs e)
        {
            if (MeshDraw != null)
            {
                glControl.MakeCurrent();
                MeshDraw.PanelResize();
            }
        }

        private void glControl_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        private void glControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        private void glControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        private void glControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        private void glControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        private void glControl_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {

        }

        private void glControl_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {

        }

        private void glControl_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {

        }
    }
}
