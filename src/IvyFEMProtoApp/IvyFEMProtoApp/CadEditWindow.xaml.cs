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

namespace IvyFEMProtoApp
{
    /// <summary>
    /// CadEditWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CadEditWindow : Window
    {
        /// <summary>
        /// Cadデザイン
        /// </summary>
        internal CadDesign2D CadDesign = null;

        /// <summary>
        /// 場を描画する?
        /// </summary>
        internal bool IsFieldDraw { get; set; } = false;

        /// <summary>
        /// サンプル問題
        /// </summary>
        private Problem Problem = new Problem();
        /// <summary>
        /// 計算結果表示
        /// </summary>
        internal CalcDraw2D CalcDraw = null;

        public CadEditWindow()
        {
            InitializeComponent();
            CadDesign = new CadDesign2D(GLControl, 100, 100);
            CadDesign.Change += CadDesign_Change;
            CadDesign.ShowProperty += CadDesign_ShowProperty;
            CadDesign.CadMode = CadDesign2DBase.CadModeType.None;
            CalcDraw = new CalcDraw2D(GLControl, CadDesign.Camera);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesign.CadMode);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            GLControl.MakeCurrent();
        }

        private void GLControl_Load(object sender, EventArgs e)
        {
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(Color4.Black);
        }

        private void GLControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            try
            {
                if (IsFieldDraw)
                {
                    CalcDraw.PanelPaint();
                }
                else
                {
                    CadDesign.CadPanelPaint();
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
            }
        }

        private void GLControl_Resize(object sender, EventArgs e)
        {
            if (IsFieldDraw)
            {
                CalcDraw.PanelResize();
            }
            else
            {
                CadDesign.CadPanelResize();
            }
        }

        private void GLControl_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            CadDesign.CadPanelMouseClick(e);
        }

        private void GLControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            CadDesign.CadPanelMouseDown(e);
        }

        private void GLControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            {
                System.Drawing.Point pt = e.Location;
                OpenTK.Vector2d coord = CadDesign.ScreenPointToCoord(pt);
                CoordStatusLabel.Content = string.Format("({0:F6}, {1:F6})", coord.X, coord.Y);
            }
            CadDesign.CadPanelMouseMove(e);
        }

        private void GLControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            CadDesign.CadPanelMouseUp(e);
        }

        private void GLControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            CadDesign.CadPanelMouseWheel(e);
        }

        private void GLControl_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            CadDesign.CadPanelKeyDown(e);
        }

        private void GLControl_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            CadDesign.CadPanelKeyPress(e);
        }

        private void GLControl_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            CadDesign.CadPanelKeyUp(e);
        }

        private void CadDesign_Change(object sender, CadDesign2D.ChangeEventArgs e)
        {
            CadDesign2DBase.CadModeType prevCadMode = e.PrevCadMode;

        }

        private void CadDesign_ShowProperty(object sender, CadDesign2D.ShowPropertyEventArgs e)
        {
            IvyFEM.CadElementType cadElemType = e.CadElemType;
            uint cadId = e.CadId;
            MessageBox.Show("プロパティ: " + cadElemType + " Id = " + cadId);
        }

        private void ModeBtn_Click(CadDesign2D.CadModeType cadMode)
        {
            Button[] modeBtns = {
                NoneBtn,
                PolygonBtn,
                MoveBtn,
                ArcBtn,
                PortBtn,
                EraseBtn
            };
            CadDesign2D.CadModeType[] cadModes = {
                CadDesign2DBase.CadModeType.None,
                CadDesign2DBase.CadModeType.Polygon,
                CadDesign2DBase.CadModeType.Move,
                CadDesign2DBase.CadModeType.Arc,
                CadDesign2DBase.CadModeType.Port,
                CadDesign2DBase.CadModeType.Erase
            };

            CadDesign.CadMode = cadMode;
            for (int i = 0; i < cadModes.Length; i++)
            {
                CadDesign2DBase.CadModeType mode = cadModes[i];
                Button btn = modeBtns[i];
                if (mode == cadMode)
                {
                    btn.Background = Brushes.LightBlue;
                }
                else
                {
                    btn.Background = null;
                }
            }
        }

        private void NoneBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesign2DBase.CadModeType.None);
            IsFieldDraw = false;
            GLControl.Invalidate();
        }

        private void PolygonBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesign2DBase.CadModeType.Polygon);
            IsFieldDraw = false;
            GLControl.Invalidate();
            GLControl.Update();
        }

        private void MoveBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesign2DBase.CadModeType.Move);
            IsFieldDraw = false;
            GLControl.Invalidate();
            GLControl.Update();
        }

        private void ArcBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesign2DBase.CadModeType.Arc);
            IsFieldDraw = false;
            GLControl.Invalidate();
            GLControl.Update();
        }

        private void PortBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesign2DBase.CadModeType.Port);
            IsFieldDraw = false;
            GLControl.Invalidate();
            GLControl.Update();
        }

        private void EraseBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesign2DBase.CadModeType.Erase);
            IsFieldDraw = false;
            GLControl.Invalidate();
            GLControl.Update();
        }

        private void CalcExampleBtn_Click(object sender, RoutedEventArgs e)
        {
            var portEdges = CadDesign.PortEdges;
            if (portEdges.Count < 2)
            {
                MessageBox.Show("ポートを2つ選択してください");
                return;
            }
            uint zeroEId = portEdges[0].EdgeId;
            uint moveEId = portEdges[1].EdgeId;
            Problem.CalcSampleProblem(
                CadDesign.Cad2D, CadDesign.Camera, zeroEId, moveEId, this);
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "ファイルを開く";
            dialog.Filter = "CADオブジェクトファイル(*.cadobj2)|*.cadobj2";
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            CadDesign.Init();
            string cadObjFileName = dialog.FileName;
            Problem.CadObjLoadFromFile(cadObjFileName, CadDesign.Cad2D, this);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Title = "ファイルを保存";
            dialog.Filter = "CADオブジェクトファイル(*.cadobj2)|*.cadobj2";
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            string cadObjFileName = dialog.FileName;
            Problem.CadObjSaveToFile(cadObjFileName, CadDesign.Cad2D);
            CadDesign.IsDirty = false;
            MessageBox.Show("保存しました", "");
        }

        private void MeshBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Title = "ファイルを保存";
            dialog.Filter = "メッシュオブジェクトファイル(*.mshobj2)|*.mshobj2";
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            string meshObjFileName = dialog.FileName;
            Problem.MeshObjFileTest(meshObjFileName, CadDesign.Cad2D);
        }
    }
}
