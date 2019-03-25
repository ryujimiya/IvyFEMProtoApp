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
        internal CadDesign CadDesign = null;

        /// <summary>
        /// 場を描画する?
        /// </summary>
        internal bool IsFieldDraw { get; set; } = false;

        /// <summary>
        /// 計算サンプル
        /// </summary>
        private ProblemCadEdit Problem = new ProblemCadEdit();
        /// <summary>
        /// 計算結果表示
        /// </summary>
        internal CalcDraw CalcDraw = null;

        public CadEditWindow()
        {
            InitializeComponent();
            CadDesign = new CadDesign(glControl, 100, 100);
            CadDesign.Change += CadDesign_Change;
            CadDesign.ShowProperty += CadDesign_ShowProperty;
            CadDesign.CadMode = CadDesignBase.CadModeType.None;
            CalcDraw = new CalcDraw(glControl, CadDesign.Camera);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesign.CadMode);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            glControl.MakeCurrent();
        }

        private void glControl_Load(object sender, EventArgs e)
        {
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(Color4.Black);
        }

        private void glControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
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

        private void glControl_Resize(object sender, EventArgs e)
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

        private void glControl_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            CadDesign.CadPanelMouseClick(e);
        }

        private void glControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            CadDesign.CadPanelMouseDown(e);
        }

        private void glControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            {
                System.Drawing.Point pt = e.Location;
                OpenTK.Vector2d coord = CadDesign.ScreenPointToCoord(pt);
                CoordStatusLabel.Content = string.Format("({0:F6}, {1:F6})", coord.X, coord.Y);
            }
            CadDesign.CadPanelMouseMove(e);
        }

        private void glControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            CadDesign.CadPanelMouseUp(e);
        }

        private void glControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            CadDesign.CadPanelMouseWheel(e);
        }

        private void glControl_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            CadDesign.CadPanelKeyDown(e);
        }

        private void glControl_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            CadDesign.CadPanelKeyPress(e);
        }

        private void glControl_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            CadDesign.CadPanelKeyUp(e);
        }

        private void CadDesign_Change(object sender, CadDesign.ChangeEventArgs e)
        {
            CadDesignBase.CadModeType prevCadMode = e.PrevCadMode;

        }

        private void CadDesign_ShowProperty(object sender, CadDesign.ShowPropertyEventArgs e)
        {
            IvyFEM.CadElementType cadElemType = e.CadElemType;
            uint cadId = e.CadId;
            MessageBox.Show("プロパティ: " + cadElemType + " Id = " + cadId);
        }

        private void ModeBtn_Click(CadDesign.CadModeType cadMode)
        {
            Button[] modeBtns = { noneBtn, polygonBtn, moveBtn, portBtn, eraseBtn };
            CadDesign.CadModeType[] cadModes = {
                CadDesignBase.CadModeType.None,
                CadDesignBase.CadModeType.Polygon,
                CadDesignBase.CadModeType.Move,
                CadDesignBase.CadModeType.Port,
                CadDesignBase.CadModeType.Erase
            };

            CadDesign.CadMode = cadMode;
            for (int i = 0; i < cadModes.Length; i++)
            {
                CadDesignBase.CadModeType mode = cadModes[i];
                Button btn = modeBtns[i];
                if (mode == cadMode)
                {
                    btn.Background = Brushes.Pink;
                }
                else
                {
                    btn.Background = null;
                }
            }
        }

        private void noneBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesignBase.CadModeType.None);
            IsFieldDraw = false;
            glControl.Invalidate();
        }

        private void polygonBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesignBase.CadModeType.Polygon);
            IsFieldDraw = false;
            glControl.Invalidate();
            glControl.Update();
        }

        private void moveBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesignBase.CadModeType.Move);
            IsFieldDraw = false;
            glControl.Invalidate();
            glControl.Update();
        }

        private void portBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesignBase.CadModeType.Port);
            IsFieldDraw = false;
            glControl.Invalidate();
            glControl.Update();
        }

        private void eraseBtn_Click(object sender, RoutedEventArgs e)
        {
            ModeBtn_Click(CadDesignBase.CadModeType.Erase);
            IsFieldDraw = false;
            glControl.Invalidate();
            glControl.Update();
        }

        private void calcSampleBtn_Click(object sender, RoutedEventArgs e)
        {
            Problem.ElasticProblem(CadDesign.Cad2D, CadDesign.Camera, this);
        }

        private void openBtn_Click(object sender, RoutedEventArgs e)
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

        private void saveBtn_Click(object sender, RoutedEventArgs e)
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

        private void meshBtn_Click(object sender, RoutedEventArgs e)
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
