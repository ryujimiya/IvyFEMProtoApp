using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using IvyFEM;

namespace IvyFEMProtoApp
{
    public class CadDesign2D : CadDesign2DBase
    {
        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 変更通知イベント引数
        /// </summary>
        public class ChangeEventArgs : EventArgs
        {
            public CadModeType PrevCadMode { get; private set; } = CadModeType.None;

            public ChangeEventArgs(CadModeType prevCadMode) : base()
            {
                PrevCadMode = prevCadMode;
            }
        }
        /// <summary>
        /// 変更通知デリゲート
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ChangeDelegate(object sender, ChangeEventArgs e);

        /// <summary>
        /// プロパティ変更イベント引数
        /// </summary>
        public class ShowPropertyEventArgs : EventArgs
        {
            public CadElementType CadElemType { get; private set; } = CadElementType.NotSet;
            public uint CadId { get; private set; }

            public ShowPropertyEventArgs(CadElementType cadElemType, uint cadId) : base()
            {
                CadElemType = cadElemType;
                CadId = cadId;
            }
        }
        /// <summary>
        /// プロパティ変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ShowPropertyDelegate(object sender, ShowPropertyEventArgs e);

        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// ループの背景色
        /// </summary>
        private static Color LoopBackColor = Color.FromArgb(
            (int)(255 * 0.9), (int)(255 * 0.4), (int)(255 * 0.4), (int)(255 * 0.4));
        /// <summary>
        /// 編集中対象の描画色
        /// </summary>
        private static readonly Color EditingColor = Color.Yellow;
        /// <summary>
        /// 図形作成中に生成した辺の描画色
        /// </summary>
        private static Color TmpEdgeColor = Color.Black;
        /// <summary>
        /// ポート境界の色
        /// </summary>
        private static readonly Color PortColor = Color.LightBlue;
        /// <summary>
        ///  線の幅
        /// </summary>
        private const int LineWidth = 5;

        ////////////////////////////////////////////////////////////////////////
        // フィールド
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 変更通知イベント
        /// </summary>
        public event ChangeDelegate Change = null;
        /// <summary>
        /// プロパティ表示イベント
        /// </summary>
        public event ShowPropertyDelegate ShowProperty = null;

        /// <summary>
        /// Cadパネル
        /// </summary>
        private OpenTK.GLControl GLControl = null;
        /// <summary>
        /// 背景の横
        /// </summary>
        private double BackgroundWidth = 100;
        /// <summary>
        /// 背景の縦
        /// </summary>
        private double BackgroundHeight = 0;
        /// <summary>
        /// 最小距離
        /// </summary>
        private double MinDistance = 1.25;
        /// <summary>
        /// カメラ
        /// </summary>
        public Camera2D Camera { get; private set; } = new Camera2D();
        /// <summary>
        /// 描画オブジェクトアレイインスタンス
        /// </summary>
        private DrawerArray DrawerArray = new DrawerArray();
        /// <summary>
        /// マウス移動位置
        /// </summary>
        private Point MouseMovePt = new Point();
        /// <summary>
        /// キー修飾子 
        /// </summary>
        private System.Windows.Forms.Keys KeyModifiers = System.Windows.Forms.Keys.None;

        /// <summary>
        /// マウス選択開始ポイント
        /// </summary>
        private Point StartPt;
        /// <summary>
        /// マウス選択終了ポイント
        /// </summary>
        private Point EndPt;
        /// <summary>
        /// ドラッグ中?
        /// </summary>
        private bool DragFlg = false;
        /// <summary>
        /// 移動対称CAD要素タイプ
        /// </summary>
        private CadElementType MovElemType = CadElementType.NotSet;
        /// <summary>
        /// 移動対称要素ID
        /// </summary>
        private uint MovObjId = 0;

        /// <summary>
        /// 円弧をドラッグ中？
        /// </summary>
        private bool DragArcFlg = false;

        /// <summary>
        /// Cadモード
        /// </summary>
        public CadModeType CadMode
        {
            get { return _CadMode; }
            set
            {
                CadModeType prevMode = _CadMode;
                if (prevMode != value)
                {
                    _CadMode = value;
                }
            }
        }
        /// <summary>
        /// 図形を編集中？
        /// </summary>
        public bool IsEditing
        {
            get
            {
                return (EditPts.Count > 0);
            }
        }

        /// <summary>
        /// ポート番号付与用シーケンス
        /// </summary>
        private int PortNumberingSeq = 1;

        /// <summary>
        /// 図面背景
        /// </summary>
        private BackgroundDrawer BackgroundDrawer = null;

        /// <summary>
        /// Cad図面が変更された?
        /// </summary>
        public bool IsDirty { get; set; } = false;

        /// <summary>
        /// エラーメッセージ表示
        /// </summary>
        /// <param name="text"></param>
        /// <param name="caption"></param>
        private static void MessageBoxShowError(string text, string caption)
        {
            System.Windows.MessageBox.Show(text, caption,
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CadDesign2D(OpenTK.GLControl GLControl, double width, double height)
            : base()
        {
            this.GLControl = GLControl;

            BackgroundWidth = width;
            BackgroundHeight = height;
            MinDistance = width / 100.0 * 2.0;

            // 色の設定
            TmpEdgeColor = this.GLControl.ForeColor;

            // 図面背景
            BackgroundDrawer = new BackgroundDrawer(BackgroundWidth, BackgroundHeight);

            // 領域を決定する
            SetupRegionSize();

            // 初期化処理
            Init();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public new void Init()
        {
            base.Init();

            RefreshDrawerAry();

            IsDirty = false;
        }

        /// <summary>
        /// 描画オブジェクトのリストを更新する
        /// Cadオブジェクトを更新したときに実行する
        /// </summary>
        public void RefreshDrawerAry()
        {
            DrawerArray.Clear();
            // 背景を追加する
            DrawerArray.Add(BackgroundDrawer);

            // Cad図面
            Cad2DDrawer drawer = new Cad2DDrawer(Cad);
            uint lineWidth = (uint)(LineWidth * GLControl.Width / (double)400);
            drawer.LineWidth = lineWidth;
            DrawerArray.Add(drawer);
        }

        /// <summary>
        /// 領域を決定する
        /// </summary>
        public void SetupRegionSize(double offsetX = 0, double offsetY = 0, double scale = 1.4)
        {
            // 描画オブジェクトを更新する
            RefreshDrawerAry();
            // 描画オブジェクトのバウンディングボックスを使ってカメラの変換行列を初期化する
            Camera.Fit(DrawerArray.GetBoundingBox(Camera.RotMatrix33()));
            // カメラのスケール調整
            // DrawerArrayのInitTransを実行すると、物体のバウンディングボックス + マージン分(×1.5)がとられる。
            // マージンを表示上なくすためスケールを拡大して調整する
            Camera.Scale = scale;
            // カメラをパンニングさせ位置を調整
            Camera.MousePan(0.0, 0.0, offsetX, offsetY);
        }

        /// <summary>
        /// SimpleOpenGlControlのシーンの描画
        /// </summary>
        private void RenderScene()
        {
            GL.ClearColor(Color4.White);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(1.1f, 4.0f);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            OpenGLUtils.SetModelViewTransform(Camera);

            //System.Diagnostics.Debug.WriteLine("renderScene:Drawers count = {0}", DrawerAry.Drawers.Count);
            try
            {
                DrawerArray.Draw();
                DrawPortNumberText();
                DrawEditPtsTemporayLine();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBoxShowError(exception.Message, "");
            }

            GLControl.SwapBuffers();
        }

        /// <summary>
        /// SimpleOpenGlControlのリサイズ処理
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        private void ResizeScene(int w, int h)
        {
            Camera.WindowAspect = (double)w / h;
            RefreshDrawerAry();

            GL.Viewport(0, 0, w, h);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            OpenGLUtils.SetProjectionTransform(Camera);
        }

        /// <summary>
        /// CadPanelのリサイズイベントハンドラ
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelResize()
        {
            //System.Diagnostics.Debug.WriteLine("CadPanelResize");
            int scrollPosX = GLControl.AutoScrollPosition.X;
            int scrollPosY = GLControl.AutoScrollPosition.Y;
            int w = GLControl.Width;
            int h = GLControl.Height;
            ResizeScene(w, h);
        }

        /// <summary>
        /// Cadパネル描画イベント処理
        /// </summary>
        /// <param name="g"></param>
        public void CadPanelPaint()
        {
            RenderScene();
        }

        /// <summary>
        /// マウスクリックイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseClick(System.Windows.Forms.MouseEventArgs e)
        {

        }

        /// <summary>
        /// マウスダウンイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            bool executed = false;

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                executed = MouseLeftButtonDown(e);
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                executed = MouseRightButtonDown(e);
            }

            if (executed)
            {
                // 描画オブジェクトアレイを更新する
                RefreshDrawerAry();
                // Cadパネルの再描画
                GLControl.Invalidate();
                GLControl.Update();
            }
        }

        /// <summary>
        /// マウス左ボタンが押された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool MouseLeftButtonDown(System.Windows.Forms.MouseEventArgs e)
        {
            bool executed = false;
            Point pt = e.Location;

            DragFlg = true;
            StartPt = e.Location;
            EndPt = StartPt;

            if (CadMode == CadModeType.Move)
            {
                CadElementType partElemType;
                uint partId;
                bool hit = HitTest(pt, out partElemType, out partId);
                if (hit)
                {
                    MovElemType = partElemType;
                    MovObjId = partId;
                }
                executed = true; // 常に実行される
            }
            else if (CadMode == CadModeType.Arc)
            {
                executed = BeginDragArc(pt);
            }

            return executed;
        }

        /// <summary>
        /// マウス左ボタンが押された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool MouseRightButtonDown(System.Windows.Forms.MouseEventArgs e)
        {
            bool executed = false;
            Point pt = e.Location;

            return executed;
        }

        /// <summary>
        /// マウス移動イベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseMove(System.Windows.Forms.MouseEventArgs e)
        {
            bool executed = false;
            Point pt = e.Location;
            Point prevpt = MouseMovePt;
            MouseMovePt = pt;
            EndPt = e.Location;

            bool hitTestFlg = false;
            if (CadMode == CadModeType.None)
            {
                if (DragFlg)
                {
                    /*
                    if (KeyModifiers.HasFlag(System.Windows.Forms.Keys.Control))
                    {
                        // 回転
                        executed = doMouseRotation(prevpt, pt);
                    }
                    else
                    */
                    {
                        // パン
                        executed = DoMousePan(prevpt, pt);
                    }
                }
                else
                {
                    hitTestFlg = true;
                }
            }
            else if (CadMode == CadModeType.Move)
            {
                if (DragFlg)
                {
                    // Cadオブジェクトの移動
                    executed = DoMoveObject(true, ref StartPt, EndPt);
                }
                else
                {
                    hitTestFlg = true;
                }

            }
            else if (CadMode == CadModeType.Arc)
            {
                if (DragFlg)
                {
                    executed = DoDragArc(pt);
                }
                else
                {
                    hitTestFlg = true;
                }
            }
            else if (CadMode != CadModeType.None)
            {
                hitTestFlg = true;
            }

            if(hitTestFlg)
            {
                CadElementType partElemType;
                uint partId;
                bool hit = HitTest(pt, out partElemType, out partId);
                executed = true; // 常に実行される
            }

            if (executed)
            {
                if (!hitTestFlg)
                {
                    // 描画オブジェクトアレイを更新する
                    RefreshDrawerAry();
                }
                // Cadパネルの再描画
                GLControl.Invalidate();
                GLControl.Update();
            }
        }

        /// <summary>
        /// マウスアップイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseUp(System.Windows.Forms.MouseEventArgs e)
        {
            bool executed = false;

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                executed = MouseLeftButtonUp(e);
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                executed = MouseRightButtonUp(e);
            }

            if (executed)
            {
                if (CadMode != CadModeType.None)
                {
                    if (Change != null)
                    {
                        ChangeEventArgs eventArgs = new ChangeEventArgs(CadMode);
                        Change(this, eventArgs);
                    }
                }

                // 描画オブジェクトアレイを更新する
                RefreshDrawerAry();
                // Cadパネルの再描画
                GLControl.Invalidate();
                GLControl.Update();
            }
        }

        /// <summary>
        /// マウス左ボタンが離された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool MouseLeftButtonUp(System.Windows.Forms.MouseEventArgs e)
        {
            bool executed = false;
            Point pt = e.Location;
            EndPt = e.Location;
            DragFlg = false;

            // プロパティの表示
            executed = DoShowProperty(pt);
            if (!executed)
            {
                // Cadオブジェクトの移動
                executed = DoMoveObject(false, ref StartPt, EndPt);
            }
            if (!executed)
            {
                // 円弧⇔直線切り替え
                executed = DoToggleArcLine(pt);
            }
            if (!executed)
            {
                // 円弧のドラッグ終了
                executed = EndDragArc(pt);
            }
            if (!executed)
            {
                // 領域作成
                executed = DoMakeDisconArea(pt);
            }
            if (!executed)
            {
                // 領域削除
                executed = DoEraseDisconArea(pt);
            }
            if (!executed)
            {
                // ポート追加
                executed = DoSelectPort(pt);
            }
            if (!executed)
            {
                // ポート削除
                executed = DoErasePort(pt);
            }
            if (!executed)
            {
                // 辺削除
                executed = DoEraseCadEdge(pt);
            }

            return executed;
        }

        /// <summary>
        /// マウス右ボタンが離された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool MouseRightButtonUp(System.Windows.Forms.MouseEventArgs e)
        {
            bool executed = false;

            return executed;
        }

        /// <summary>
        /// マウスホイール
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool CadPanelMouseWheel(System.Windows.Forms.MouseEventArgs e)
        {
            bool executed = false;

            double delta = e.Delta;
            executed = DoZoom(delta);

            if (executed)
            {
                // 描画オブジェクトアレイを更新する
                RefreshDrawerAry();
                // Cadパネルの再描画
                GLControl.Invalidate();
                GLControl.Update();
            }
            return executed;
        }

        /// <summary>
        /// キーダウンイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelKeyDown(System.Windows.Forms.KeyEventArgs e)
        {
            KeyModifiers = e.Modifiers;
        }

        /// <summary>
        /// キーを押したままのとき発生するイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelKeyPress(System.Windows.Forms.KeyPressEventArgs e)
        {
        }

        /// <summary>
        /// キーアップイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelKeyUp(System.Windows.Forms.KeyEventArgs e)
        {
            KeyModifiers = e.Modifiers;

            if (!KeyModifiers.HasFlag(System.Windows.Forms.Keys.Control))  // Controlキーが離された
            {

            }
        }

        /// <summary>
        /// マウスで指定したウインドウ座標をOpenGL座標に変換
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public OpenTK.Vector2d ScreenPointToCoord(Point pt)
        {
            OpenTK.Vector2d coord;
            {
                double ox;
                double oy;
                OpenGLUtils.ScreenPointToCoord(pt, out ox, out oy);
                coord = new OpenTK.Vector2d(ox, oy);
            }
            return coord;
        }

        /// <summary>
        /// ポート番号テキスト表示
        /// </summary>
        /// <param name="g"></param>
        private void DrawPortNumberText()
        {
            // 描画範囲は方眼紙のバウンディングボックス内
            OpenTK.Matrix3d rot = Camera.RotMatrix33();
            BoundingBox3D bb = BackgroundDrawer.GetBoundingBox(rot);

            // ウィンドウの幅、高さの取得
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int winW = viewport[2];
            int winH = viewport[3];
            int fontSize = 28;
            //fontSize = (int)(fontSize * winW / (double)400);

            // 番号を描画する位置（スクリーン座標）を取得する
            // 注意: マトリクス変換する前に取得する必要がある
            IList<Point> drawPts = new List<Point>();
            foreach (PortEdge edge in PortEdges)
            {
                uint eId = edge.EdgeId;
                OpenTK.Vector2d pp1 = Cad.GetEdge(eId).GetVertexCoord(true);
                OpenTK.Vector2d pp2 = Cad.GetEdge(eId).GetVertexCoord(false);
                double xx;
                double yy;
                xx = (pp1.X + pp2.X) / 2.0;
                yy = (pp1.Y + pp2.Y) / 2.0;
                Point pt = OpenGLUtils.CoordToScreenPoint(xx, yy);
                drawPts.Add(pt);
            }

            // マトリクス変換
            double asp = (winW + 1.0) / (winH + 1.0);
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(-asp, asp, -1.0, 1.0, -1.0, 1.0);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();

            // TODO: 1.05は方眼紙のY方向のマージンがあるために暫定
            //       計算で求めるようにしなければならない
            double scale = 2.0 / ((bb.MaxY - bb.MinY) * 1.05);
            for (int iPort = 0; iPort < PortEdges.Count; iPort++)
            {
                PortEdge edge = PortEdges[iPort];
                uint eId = edge.EdgeId;
                Point pt = drawPts[iPort];
                double drawX = asp - (pt.X / (double)winW) * (2.0 * asp);
                double drawY = (pt.Y / (double)winH) * 2.0 - 1.0;
                OpenTK.Vector2d drawpp = new OpenTK.Vector2d(drawX, drawY);

                string text = string.Format("{0}", edge.No);
                {
                    GL.Translate(drawpp.X, drawpp.Y, 1.0);
                    GL.Color3(Color.DarkBlue);
                    OpenGLUtils.DrawString(text, fontSize);
                    GL.Translate(-drawpp.X, -drawpp.Y, -1.0);

                    GL.Translate((drawpp.X + 0.1 * scale), (drawpp.Y - 0.1 * scale), 1.0);
                    GL.Color3(Color.Black);
                    OpenGLUtils.DrawString(text, fontSize);
                    GL.Translate(-(drawpp.X + 0.1 * scale), -(drawpp.Y - 0.1 * scale), -1.0);
                }
            }
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
        }

        /// <summary>
        /// 図形作成途中の一時的な線分を描画
        /// </summary>
        private void DrawEditPtsTemporayLine()
        {
            Point pt = MouseMovePt;
            double x;
            double y;
            OpenGLUtils.ScreenPointToCoord(pt, out x, out y);
            if (EditPts.Count > 0)
            {
                Color lineColor = EditingColor;
                int lineWidth = 4;
                double z = 15;
                GL.Color3(lineColor.R / (double)255, lineColor.G / (double)255.0, lineColor.B / (double)255);
                GL.LineWidth(lineWidth);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(EditPts[EditPts.Count - 1].X, EditPts[EditPts.Count - 1].Y, z);
                GL.Vertex3(x, y, z);
                GL.End();
            }
        }

        /// <summary>
        /// 拡大縮小
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        private bool DoZoom(double delta)
        {
            bool executed = false;

            if (CadMode != CadModeType.None)
            {
                return executed;
            }

            double scale = Camera.Scale;
            scale *= Math.Pow(1.1, delta / 120.0);
            Camera.Scale = scale;

            GLControl.MakeCurrent();
            CadPanelResize();

            executed = true;

            return executed;
        }

        /// <summary>
        /// マウスによる物体の移動/拡大に使用する移動量計測用の点の位置
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="movX"></param>
        /// <param name="movY"></param>
        private static void GetMovPt(Point pt, out double movX, out double movY)
        {
            // ウィンドウの幅、高さの取得
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int winW = viewport[2];
            int winH = viewport[3];
            // 移動量計測用のポイントに変換する
            movX = (2.0 * pt.X - winW) / (double)winW;
            movY = (winH - 2.0 * pt.Y) / (double)winH;
        }

        /// <summary>
        /// カメラを水平方向に回転させる(パン:Panoramic Viewing = パノラマのように見る)
        /// </summary>
        /// <param name="prevpt"></param>
        /// <param name="pt"></param>
        private bool DoMousePan(Point prevpt, Point pt)
        {
            bool executed = false;

            if (CadMode == CadModeType.None && DragFlg)
            {
                // OK
            }
            else
            {
                return executed;
            }

            double movBeginX;
            double movBeginY;
            double movEndX;
            double movEndY;
            GetMovPt(prevpt, out movBeginX, out movBeginY);
            GetMovPt(pt, out movEndX, out movEndY);
            Camera.MousePan(movBeginX, movBeginY, movEndX, movEndY);

            executed = true;
            return executed;
        }

        /// <summary>
        /// 回転
        /// </summary>
        /// <param name="prevpt"></param>
        /// <param name="pt"></param>
        private bool DoMouseRotation(Point prevpt, Point pt)
        {
            bool executed = false;

            if (CadMode == CadModeType.None && DragFlg)
            {
                // OK
            }
            else
            {
                return executed;
            }

            double movBeginX;
            double movBeginY;
            double movEndX;
            double movEndY;
            GetMovPt(prevpt, out movBeginX, out movBeginY);
            GetMovPt(pt, out movEndX, out movEndY);
            Camera.MouseRotation(movBeginX, movBeginY, movEndX, movEndY);

            executed = true;
            return executed;
        }

        /// <summary>
        /// プロパティの表示
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool DoShowProperty(Point pt)
        {
            bool executed = false;
            if (CadMode != CadModeType.None)
            {
                return executed;
            }
            CadElementType cadElemType;
            uint cadId;
            bool hit = HitTest(pt, out cadElemType, out cadId);
            if (hit)
            {
                ShowPropertyEventArgs eventArgs = new ShowPropertyEventArgs(cadElemType, cadId);
                ShowProperty(this, eventArgs);
                executed = true;
            }
            return executed;
        }

        /// <summary>
        /// Cadオブジェクトの移動
        /// </summary>
        /// <param name="elemType"></param>
        /// <param name="objId"></param>
        /// <param name="screenPt"></param>
        private static bool MoveObject(
            Cad2DMove cad,
            Camera Camera,
            CadElementType elemType,
            uint objId,
            Point startPt,
            Point endPt)
        {
            bool executed = false;
            if (objId == 0)
            {
                return executed;
            }
            double movBeginX = 0.0;
            double movBeginY = 0.0;
            double movEndX = 0.0;
            double movEndY = 0.0;
            // デバイス座標系に変換
            OpenGLUtils.ScreenPointToCoord(startPt, out movBeginX, out movBeginY);
            OpenGLUtils.ScreenPointToCoord(endPt, out movEndX, out movEndY);

            if (elemType == CadElementType.Vertex)
            {
                uint vId = objId;
                bool ret = cad.MoveVertex(vId, new OpenTK.Vector2d(movEndX, movEndY));
                if (ret)
                {
                    executed = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "failed: MoveVertex {0}, {1}, {2}", vId, movEndX, movEndY);
                }
            }
            else if (elemType == CadElementType.Edge)
            {
                uint eId = objId;
                bool ret = cad.MoveEdge(eId, new OpenTK.Vector2d(movEndX - movBeginX, movEndY - movBeginY));
                if (ret)
                {
                    executed = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "failed: MoveEdge {0}, {1}, {2}", eId, movEndX - movBeginX, movEndY - movBeginY);
                }
            }
            else if (elemType == CadElementType.Loop)
            {
                uint lId = objId;
                bool ret = cad.MoveLoop(lId, new OpenTK.Vector2d(movEndX - movBeginX, movEndY - movBeginY));
                if (ret)
                {
                    executed = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "failed: MoveLoop {0}, {1}, {2}", lId, movEndX - movBeginX, movEndY - movBeginY);
                }
            }
            return executed;
        }

        /// <summary>
        /// Cadオブジェクト移動処理
        /// </summary>
        /// <param name="isDragging"></param>
        /// <returns></returns>
        private bool DoMoveObject(bool isDragging, ref Point startPt, Point endPt)
        {
            bool executed = false;
            if (CadMode == CadModeType.Move)
            {
                // Cadオブジェクトの移動
                executed = MoveObject(Cad,
                    Camera,
                    MovElemType,
                    MovObjId,
                    startPt,
                    endPt);
                startPt = endPt;
                if (executed && !IsDirty)
                {
                    IsDirty = true;
                }
            }
            return executed;
        }

        /// <summary>
        /// 領域を作成する
        ///   領域の頂点追加/領域確定
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool DoMakeDisconArea(Point pt)
        {
            bool executed = false;
            if (CadMode != CadModeType.Polygon)
            {
                return executed;
            }

            // スクリーンの位置をデバイス座標に変換
            OpenTK.Vector2d pp = ScreenPointToCoord(pt);

            double th = MinDistance; //!!!!!

            //if (EditPts.Count > 0)
            //{
            //    System.Diagnostics.Debug.WriteLine("d = " + OpenTK.Vector2d.Distance(EditPts[0], pp));
            //}

            // 追加されたループIDを格納するリスト
            IList<uint> addLoopIds = new List<uint>();

            // これから追加する点の事前チェック
            bool handled = false; // 処理した？
            {
                // 編集中のCadの頂点や辺はヒットテストで捕捉する
                // ヒットテスト実行
                CadElementType partElemType = CadElementType.NotSet;
                uint partId = 0;
                HitTest(pt, out partElemType, out partId);
                if (partId != 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "hitTest for doMakeDisconArea: " + partElemType + " " + partId);
                }
                if (partId != 0 && partElemType == CadElementType.Edge)
                {
                    uint parentEdgeId = partId;
                    uint sVid;
                    uint eVId;
                    Cad.GetEdgeVertexId(parentEdgeId, out sVid, out eVId);
                    if (EditVertexIds.IndexOf(sVid) >= 0 ||
                        EditVertexIds.IndexOf(eVId) >= 0)
                    {
                        // 編集中の辺上に頂点を追加しようとした
                        MessageBoxShowError("編集中の辺上に頂点を追加できません。", "");
                        executed = false;
                        return executed;
                    }
                    else
                    {
                        /////////////////////////////////////////////////
                        // 孤立した辺1かどうかをチェックする

                        // ループに属している辺の辺上にあるかどうかチェックし、辺上ならば辺IDを返す
                        uint hitEId = GetEdgeIdIncludingPoint(Cad, pp, LoopIds);
                        if (hitEId != 0)
                        {
                            // ループに属している辺の辺上
                            // 通常の処理を行う
                            System.Diagnostics.Debug.Assert(partId == hitEId);
                        }
                        else
                        {
                            /////////////////////////////////////////////////
                            // 管理外の（ループを形成していない)頂点
                            // イレギュラーな処理

                            // 通常の頂点の追加処理はできないので処理済みにする
                            handled = true;

                            // 孤立した辺の処理を行う(頂点の追加)
                            hitEId = partId;  // !!!!!!!
                            int indexPP = EditPts.Count;
                            executed = AddVertexAndEdgeAtStandAloneEdge(
                                Cad, pp, hitEId, indexPP, LoopIds,
                                EditPts, EditVertexIds, EditEdgeIds, addLoopIds, true);
                        }

                    }
                }
                else if (partId != 0 && partElemType == CadElementType.Vertex)
                {
                    if (EditPts.Count >= 3 && OpenTK.Vector2d.Distance(EditPts[0], pp) < th)
                    {
                        // ループ確定
                        pp = EditPts[0]; //!!!!!!1
                        System.Diagnostics.Debug.WriteLine("loop will be made!!!!!!!!!!");
                    }
                    else
                    {
                        // 編集中の頂点上？
                        bool isEditingVertex = false;
                        foreach (OpenTK.Vector2d workPP in EditPts)
                        {
                            if (OpenTK.Vector2d.Distance(workPP, pp) < th)
                            {
                                isEditingVertex = true;
                                break;
                            }
                        }
                        if (isEditingVertex)
                        {
                            // 編集中の頂点上に頂点を追加しようとした
                            MessageBoxShowError("編集中の頂点上に頂点を追加できません。", "");
                            executed = false;
                            return executed;
                        }
                        else
                        {
                            /////////////////////////////////////////////////
                            // 孤立した頂点かどうかをチェックする

                            // 前に追加したループと共有している頂点かチェックする
                            IList<uint> hitLoopIds = null;
                            uint hitVId = 0;
                            GetVertexIdBelongToLoopByCoord(
                                Cad, pp, LoopIds, out hitVId, out hitLoopIds);
                            if (hitVId != 0 && hitLoopIds != null && hitLoopIds.Count > 0)
                            {
                                // 前に追加したループと共有している点
                                // 通常処理を行う
                                System.Diagnostics.Debug.Assert(partId == hitVId);
                            }
                            else
                            {
                                /////////////////////////////////////////////////
                                // 管理外の（ループを形成していない)頂点
                                // イレギュラーな処理

                                // 通常の辺の追加処理はできないので処理済みにする
                                handled = true;

                                // 孤立した頂点の処理を行う(辺の追加)
                                hitVId = partId;  // !!!!!!!
                                int indexPP = EditPts.Count;
                                executed = AddEdgeConnectedToStandAloneVertex(
                                    Cad, pp, hitVId, indexPP, LoopIds,
                                    EditPts, EditVertexIds, EditEdgeIds, addLoopIds, true);
                            }
                        }
                    }
                }
            }

            /////////////////
            if (!handled)
            {
                // 以下の処理は孤立した頂点、辺を考慮していません。
                // それらはイレギュラーな処理としてすでに処理済みとします。（ここにはこないようにする）
                int indexPP = EditPts.Count; // これから追加する点のインデックス
                executed = DoMakeDisconAreaCore(Cad, pp, indexPP, LoopIds,
                    EditPts, EditVertexIds, EditEdgeIds, addLoopIds, true);
            }
            if (addLoopIds.Count > 0)
            {
                //MessageBox.Show("領域の分割確定");
                foreach (uint lId in addLoopIds)
                {
                    //// ループの色を指定（指定しなければ(0.9,0.8,0.8)になる
                    Color backColor = LoopBackColor;
                    SetupCadObjectsColorForOneLoop(Cad, lId, backColor);
                    // ループ情報の追加
                    LoopIds.Add(lId);

                    // ループの内側にあるループを子ループに設定する
                    ReconstructLoopsInsideLoopAsChild(
                        Cad, lId, LoopIds, PortEdges);

                    //ループの色をすべて再設定する
                    SetupCadObjectsColorForAllLoops(Cad, LoopIds);

                    executed = true;
                }
                //
                EditPts.Clear();
                EditVertexIds.Clear();
                EditEdgeIds.Clear();
            }

            if (executed && !IsDirty)
            {
                IsDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// 孤立した頂点と接続して辺を作成する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="pp"></param>
        /// <param name="hitVId"></param>
        /// <param name="indexPP"></param>
        /// <param name="LoopIds"></param>
        /// <param name="EditPts"></param>
        /// <param name="EditVertexIds"></param>
        /// <param name="EditEdgeIds"></param>
        /// <param name="addLoopIds"></param>
        /// <param name="showErrorFlg"></param>
        /// <returns></returns>
        private static bool AddEdgeConnectedToStandAloneVertex(
            Cad2D cad, OpenTK.Vector2d pp, uint hitVId, int indexPP, IList<uint> LoopIds,
            IList<OpenTK.Vector2d> EditPts, IList<uint> EditVertexIds, IList<uint> EditEdgeIds,
            IList<uint> addLoopIds, bool showErrorFlg)
        {
            bool executed = false;
            uint parentLoopId = 0;
            int addedCnt = 0;
            // 頂点は作成せず、辺だけ作成する
            // 辺の作成に関して、既存の辺が存在するかチェック
            uint existEId = 0;
            {
                // １つ前の点に関して、前に追加したループと共有している頂点かチェックする
                IList<uint> prevHitLoopIds = null;
                uint prevHitVId = 0;
                if (indexPP >= 1)
                {
                    //CVector2D prevPt = pps[indexPP - 1];
                    OpenTK.Vector2d prevPt = EditPts[EditPts.Count - 1];
                    GetVertexIdBelongToLoopByCoord(
                        cad, prevPt, LoopIds, out prevHitVId, out prevHitLoopIds);
                    if (prevHitVId != 0)
                    {
                        existEId = GetEdgeIdByVertexIds(cad, hitVId, prevHitVId);
                    }
                }
            }

            // 頂点は作成しない。リストに追加するだけ。
            EditPts.Add(pp);
            addedCnt++;
            System.Diagnostics.Debug.Assert(EditVertexIds.IndexOf(hitVId) == -1);
            EditVertexIds.Add(hitVId);

            // 辺の追加
            if (EditVertexIds.Count >= 2 && existEId == 0)
            {
                // １つ前の点と現在の点が既に辺を作っていなければ辺を追加

                uint eId = 0;
                uint loopIdAddByConnectVertex = 0;
                bool ret = AddEdgeByLastEditPts(
                    cad, parentLoopId,
                    EditVertexIds, EditEdgeIds, out eId, out loopIdAddByConnectVertex, 
                    showErrorFlg);
                if (ret)
                {
                    executed = true;
                    if (loopIdAddByConnectVertex != 0)
                    {
                        addLoopIds.Add(loopIdAddByConnectVertex);
                    }
                }
                else
                {
                    //System.Diagnostics.Debug.Assert(false);
                    System.Diagnostics.Debug.WriteLine("[ERROR]parentElemType:{0}, parentId:{1}",
                        CadElementType.Loop, parentLoopId);
                    System.Diagnostics.Debug.WriteLine("[ERROR] add edge pp: {0},{1} indexPP: {2}",
                        pp.X, pp.Y, indexPP);
                }
            }
            return executed;
        }

        /// <summary>
        /// 孤立した辺上に頂点を追加する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="pp"></param>
        /// <param name="hitEId"></param>
        /// <param name="indexPP"></param>
        /// <param name="LoopIds"></param>
        /// <param name="EditPts"></param>
        /// <param name="EditVertexIds"></param>
        /// <param name="EditEdgeIds"></param>
        /// <param name="addLoopIds"></param>
        /// <param name="showErrorFlg"></param>
        /// <returns></returns>
        private static bool AddVertexAndEdgeAtStandAloneEdge(
            Cad2D cad, OpenTK.Vector2d pp, uint hitEId, int indexPP, IList<uint> LoopIds,
            IList<OpenTK.Vector2d> EditPts, IList<uint> EditVertexIds, IList<uint> EditEdgeIds,
            IList<uint> addLoopIds, bool showErrorFlg)
        {
            bool executed = false;

            CadElementType parentElemType = CadElementType.Edge;
            uint parentId = hitEId;

            int addedCnt = 0;

            // 頂点と辺を追加
            uint vertexIdAdd = 0;
            uint edgeIdAdd = 0;
            uint edgeIdAddByAddVertex = 0;
            uint loopIdAddByConnectVertex = 0;
            bool ret = AddVertexAndEdge(cad, parentElemType, parentId, pp,
                EditPts, EditVertexIds, EditEdgeIds, out vertexIdAdd, out edgeIdAdd,
                out edgeIdAddByAddVertex, out loopIdAddByConnectVertex, showErrorFlg);
            if (ret)
            {
                executed = true;
                addedCnt++;
                if (edgeIdAddByAddVertex != 0)
                {
                    //System.Diagnostics.Debug.Assert(false);
                    if (edgeIdAdd != 0)
                    {
                        // 辺が作成されている場合、頂点の作成によってできた（分割された）辺は作成された辺の１つ前に挿入-->現在の最後の位置に挿入
                        EditEdgeIds.Insert(EditEdgeIds.Count - 1, edgeIdAddByAddVertex);
                    }
                    else
                    {
                        EditEdgeIds.Add(edgeIdAddByAddVertex);
                    }
                }
                if (loopIdAddByConnectVertex != 0)
                {
                    addLoopIds.Add(loopIdAddByConnectVertex);
                }
            }
            else
            {
                //System.Diagnostics.Debug.Assert(false);
                System.Diagnostics.Debug.WriteLine(
                    "[ERROR]parentElemType:{0}, parentId:{1}", parentElemType, parentId);
                System.Diagnostics.Debug.WriteLine(
                    "[ERROR] add vertex and edge pp: {0},{1} indexPP: {2}", pp.X, pp.Y, indexPP);
            }
            return executed;
        }


        /// <summary>
        /// 領域作成コア処理
        ///   Note: ループに属さない孤立した頂点や辺を考慮していない
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="pp"></param>
        /// <param name="indexPP"></param>
        /// <param name="LoopIds"></param>
        /// <param name="EditPts"></param>
        /// <param name="EditVertexIds"></param>
        /// <param name="EditEdgeIds"></param>
        /// <param name="addLoopIds"></param>
        /// <param name="showErrorFlg"></param>
        /// <returns></returns>
        private static bool DoMakeDisconAreaCore(
            Cad2D cad, OpenTK.Vector2d pp, int indexPP, IList<uint> LoopIds,
            IList<OpenTK.Vector2d> EditPts, IList<uint> EditVertexIds, IList<uint> EditEdgeIds,
            IList<uint> addLoopIds, bool showErrorFlg)
        {
            bool executed = false;

            int addedCnt = 0;
            bool addVertexAndEdgeFlg = true;
            double th = Constants.PrecisionLowerLimit;

            if (EditPts.Count >= 3 && 
                OpenTK.Vector2d.Distance(pp, EditPts[0]) < th) // loopFixFlg
            {
                // ループ確定
                OpenTK.Vector2d prevPP = EditPts[EditPts.Count - 1];
                // 最後の辺の追加
                // 前に追加したループと共有している頂点かチェックする
                IList<uint> hitLoopIds = null;
                uint hitVId = 0;
                GetVertexIdBelongToLoopByCoord(
                    cad, prevPP, LoopIds, out hitVId, out hitLoopIds);

                uint parentLoopId = 0;

                // 先頭の頂点と接続する
                EditPts.Add(EditPts[0]);
                addedCnt++;
                EditVertexIds.Add(EditVertexIds[0]);
                parentLoopId = GetVertexLoopId(cad, EditVertexIds[0]);

                uint existEId = 0;
                {
                    // 先頭の点に関して、前に追加したループと共有している頂点かチェックする
                    IList<uint> nextHitLoopIds = null;
                    uint nextHitVId = 0;
                    if (indexPP >= 1)
                    {
                        GetVertexIdBelongToLoopByCoord(
                            cad, EditPts[0], LoopIds, out nextHitVId, out nextHitLoopIds);
                        if (nextHitVId != 0)
                        {
                            existEId = GetEdgeIdByVertexIds(cad, hitVId, nextHitVId);
                        }
                    }
                }

                if (hitVId == 0 || existEId == 0)
                {
                    // １つ前の点と現在の点が既に辺を作っていなければ辺を追加
                    uint eId = 0;
                    uint loopIdAddByConnectVertex = 0;
                    bool ret = AddEdgeByLastEditPts(
                        cad, parentLoopId,
                        EditVertexIds, EditEdgeIds, out eId, out loopIdAddByConnectVertex, showErrorFlg);
                    if (ret)
                    {
                        if (loopIdAddByConnectVertex != 0)
                        {
                            addLoopIds.Add(loopIdAddByConnectVertex);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                        System.Diagnostics.Debug.WriteLine(
                            "[ERROR]parentElemType:{0}, parentId:{1}", CadElementType.Loop, parentLoopId);
                        System.Diagnostics.Debug.WriteLine(
                            "[ERROR] add edge (last) pp: {0},{1} indexPP: {2}", pp, pp, indexPP);
                    }
                }
            }
            else
            {
                // 新しく作成しようとしている辺が既存の辺を含んでいる場合をチェック
                if (EditPts.Count >= 1)
                {
                    OpenTK.Vector2d prevPP = EditPts[EditPts.Count - 1];

                    // 前の点の頂点ID
                    uint prevVId = EditVertexIds[EditVertexIds.Count - 1]; //
                    System.Diagnostics.Debug.Assert(EditVertexIds.Count == EditPts.Count);

                    // これから作成する辺に含まれる辺の開始頂点、終了頂点を取得する
                    uint minVId = 0;
                    uint maxVId = 0;
                    GetIncludedEdgesSartEndVId(cad, prevPP, pp, LoopIds, out minVId, out maxVId);
                    if (minVId != 0 && maxVId != 0)
                    {
                        addVertexAndEdgeFlg = false;

                        OpenTK.Vector2d minVertexPt = cad.GetVertexCoord(minVId);
                        OpenTK.Vector2d maxVertexPt = cad.GetVertexCoord(maxVId);
                        System.Diagnostics.Debug.WriteLine(
                            "split Edge indexPP: {0} pts: ({1},{2}) - ({3},{4}) - ({5},{6}) - ({7},{8})",
                            indexPP, prevPP.X, prevPP.Y,
                            minVertexPt.X, minVertexPt.Y, 
                            maxVertexPt.X, maxVertexPt.Y,
                            pp.X, pp.Y);

                        // pps[indexPP - 1] と minVIdの間の辺の作成
                        if (OpenTK.Vector2d.Distance(prevPP, minVertexPt) < th)
                        {
                            System.Diagnostics.Debug.Assert(minVId == prevVId);
                        }
                        else
                        {
                            // 前の点と開始頂点の間の辺を作成
                            EditPts.Add(minVertexPt);
                            addedCnt++;
                            System.Diagnostics.Debug.Assert(EditVertexIds.IndexOf(minVId) == -1);
                            EditVertexIds.Add(minVId);

                            // 前の点を含むループIDを取得する
                            uint parentLoopId = GetLoopIdIncludingPoint(cad, prevPP, LoopIds);

                            // １つ前の点と現在の点が既に辺を作っていなければ辺を追加
                            uint eId = 0;
                            uint loopIdAddByConnectVertex = 0;
                            bool ret = AddEdgeByLastEditPts(
                                cad, parentLoopId,
                                EditVertexIds, EditEdgeIds, out eId, out loopIdAddByConnectVertex,
                                showErrorFlg);
                            if (ret)
                            {
                                executed = true;
                                if (loopIdAddByConnectVertex != 0)
                                {
                                    addLoopIds.Add(loopIdAddByConnectVertex);
                                }
                            }
                            else
                            {
                                //System.Diagnostics.Debug.Assert(false);
                                System.Diagnostics.Debug.WriteLine(
                                    "[ERROR]parentElemType:{0}, parentId:{1}", CadElementType.Loop, parentLoopId);
                                System.Diagnostics.Debug.WriteLine(
                                    "[ERROR] add edge (Split Behind) pp: {0},{1} indexPP: {2}",
                                    pp.X, pp.Y, indexPP);
                            }
                        }
                        // minVId - maxVId間は、既存の辺、頂点なので作成しない
                        // 頂点リストにだけ追加
                        EditPts.Add(maxVertexPt);
                        addedCnt++;
                        System.Diagnostics.Debug.Assert(EditVertexIds.IndexOf(maxVId) == -1);
                        EditVertexIds.Add(maxVId);

                        // maxVIdとpp[indexPP]の間
                        if (OpenTK.Vector2d.Distance(pp, maxVertexPt) < th)
                        {
                            // maxVIdがpp[indexPP]の場合、もうすることはない
                            addVertexAndEdgeFlg = false;
                        }
                        else
                        {
                            // 以下 pp[indexPP]を頂点追加、maxVIdとpp[indexPP]の間の辺を追加の処理へ進む
                            addVertexAndEdgeFlg = true;
                        }
                    }
                }

                if (!addVertexAndEdgeFlg)
                {
                    if (addedCnt == 0)
                    {
                        // 頂点の作成が１つも行われなかった場合
                        EditPts.Add(pp);
                    }
                    if (EditPts.Count > 0)
                    {
                        System.Diagnostics.Debug.Assert(
                            OpenTK.Vector2d.Distance(pp, EditPts[EditPts.Count - 1]) <
                            Constants.PrecisionLowerLimit);
                    }
                }
                else
                {
                    // 途中経過のチェック
                    if (EditPts.Count > 0 && addedCnt > 0)
                    {
                        //嘘   prevPP      min    max   pp
                        //     +------------+-------+----+   のとき EditPts[EditPts.Count - 1]はmaxが入っているはず
                        //System.Diagnostics.Debug.Assert(CVector2D.Distance(pp, EditPts[EditPts.Count - 1]) < Constants.PrecisionLowerLimit);
                    }
                    // 前に追加したループと共有している頂点かチェックする
                    IList<uint> hitLoopIds = null;
                    uint hitVId = 0;
                    GetVertexIdBelongToLoopByCoord(
                        cad, pp, LoopIds, out hitVId, out hitLoopIds);
                    if (hitVId > 0)
                    {
                        // 共有する頂点の場合
                        uint parentLoopId = 0;
                        if (hitLoopIds.Count > 0)
                        {
                            parentLoopId = hitLoopIds[0];
                        }

                        // 頂点は作成せず、辺だけ作成する
                        // 辺の作成に関して、既存の辺が存在するかチェック
                        uint existEId = 0;
                        {
                            // １つ前の点に関して、前に追加したループと共有している頂点かチェックする
                            IList<uint> prevHitLoopIds = null;
                            uint prevHitVId = 0;
                            if (indexPP >= 1)
                            {
                                //CVector2D prevPt = pps[indexPP - 1];
                                OpenTK.Vector2d prevPt = EditPts[EditPts.Count - 1];
                                GetVertexIdBelongToLoopByCoord(
                                    cad, prevPt, LoopIds, out prevHitVId, out prevHitLoopIds);
                                if (prevHitVId != 0)
                                {
                                    existEId = GetEdgeIdByVertexIds(cad, hitVId, prevHitVId);
                                }
                            }
                        }

                        // 頂点は作成しない。リストに追加するだけ。
                        EditPts.Add(pp);
                        addedCnt++;
                        System.Diagnostics.Debug.Assert(EditVertexIds.IndexOf(hitVId) == -1);
                        EditVertexIds.Add(hitVId);

                        // 辺の追加
                        if (EditVertexIds.Count >= 2 && existEId == 0)
                        {
                            // １つ前の点と現在の点が既に辺を作っていなければ辺を追加

                            uint eId = 0;
                            uint loopIdAddByConnectVertex = 0;
                            bool ret = AddEdgeByLastEditPts(
                                cad, parentLoopId, EditVertexIds, EditEdgeIds,
                                out eId, out loopIdAddByConnectVertex, showErrorFlg);
                            if (ret)
                            {
                                executed = true;
                                if (loopIdAddByConnectVertex != 0)
                                {
                                    addLoopIds.Add(loopIdAddByConnectVertex);
                                }
                            }
                            else
                            {
                                //System.Diagnostics.Debug.Assert(false);
                                System.Diagnostics.Debug.WriteLine(
                                    "[ERROR]parentElemType:{0}, parentId:{1}", CadElementType.Loop, parentLoopId);
                                System.Diagnostics.Debug.WriteLine(
                                    "[ERROR] add edge pp: {0},{1} indexPP: {2}",
                                    pp.X, pp.Y, indexPP);
                            }
                        }
                    }
                    else
                    {
                        // ループ内部、または新規の独立な点の場合

                        uint parentId = 0;
                        CadElementType parentElemType = CadElementType.Loop;

                        bool addEdgeFlg = true;
                        // 辺上にあるかどうかチェックし、辺上ならば辺IDを返す
                        uint parentEdgeId = GetEdgeIdIncludingPoint(cad, pp, LoopIds);

                        if (parentEdgeId != 0)
                        {
                            parentId = parentEdgeId;
                            parentElemType = CadElementType.Edge;

                            if (EditVertexIds.Count > 0)
                            {
                                // 前に追加した頂点が同じ辺上にあるかをチェック
                                uint prevVId = EditVertexIds[EditVertexIds.Count - 1];
                                OpenTK.Vector2d prevPtV = cad.GetVertexCoord(prevVId);
                                bool isOnEdgePrevPtV = IsPointOnEdge(cad, parentEdgeId, prevPtV);
                                if (isOnEdgePrevPtV)
                                {
                                    addEdgeFlg = false;
                                }
                            }
                        }
                        else
                        {
                            // 包含関係を調べる必要あり
                            // 点を含むループIDを取得する
                            uint parentLoopId = GetLoopIdIncludingPoint(cad, pp, LoopIds);
                            parentId = parentLoopId;
                            parentElemType = CadElementType.Loop;
                        }

                        if (addEdgeFlg)
                        {
                            // 頂点と辺を追加
                            uint vertexIdAdd = 0;
                            uint edgeIdAdd = 0;
                            uint edgeIdAddByAddVertex = 0;
                            uint loopIdAddByConnectVertex = 0;
                            bool ret = AddVertexAndEdge(
                                cad, parentElemType, parentId, pp,
                                EditPts, EditVertexIds, EditEdgeIds,
                                out vertexIdAdd, out edgeIdAdd, out edgeIdAddByAddVertex,
                                out loopIdAddByConnectVertex, showErrorFlg);
                            if (ret)
                            {
                                executed = true;
                                addedCnt++;
                                if (edgeIdAddByAddVertex != 0)
                                {
                                    //System.Diagnostics.Debug.Assert(false);
                                    if (edgeIdAdd != 0)
                                    {
                                        // 辺が作成されている場合、頂点の作成によってできた（分割された）辺は作成された辺の１つ前に挿入-->現在の最後の位置に挿入
                                        EditEdgeIds.Insert(EditEdgeIds.Count - 1, edgeIdAddByAddVertex);
                                    }
                                    else
                                    {
                                        EditEdgeIds.Add(edgeIdAddByAddVertex);
                                    }
                                }
                                if (loopIdAddByConnectVertex != 0)
                                {
                                    addLoopIds.Add(loopIdAddByConnectVertex);
                                }
                            }
                            else
                            {
                                //System.Diagnostics.Debug.Assert(false);
                                System.Diagnostics.Debug.WriteLine(
                                    "[ERROR]parentElemType:{0}, parentId:{1}", parentElemType, parentId);
                                System.Diagnostics.Debug.WriteLine(
                                    "[ERROR] add vertex and edge pp: {0},{1} indexPP: {2}",
                                    pp.X, pp.Y, indexPP);
                            }
                        }
                        else
                        {
                            // 頂点のみ追加
                            uint vertexIdAdd = 0;
                            uint edgeIdAddByAddVertex = 0;
                            bool ret = AddVertex(
                                cad, parentElemType, parentId, pp,
                                EditPts, EditVertexIds, EditEdgeIds,
                                out vertexIdAdd, out edgeIdAddByAddVertex,
                                showErrorFlg);
                            if (ret)
                            {
                                executed = true;
                                addedCnt++;
                                if (edgeIdAddByAddVertex != 0)
                                {
                                    EditEdgeIds.Add(edgeIdAddByAddVertex);
                                }
                            }
                            else
                            {
                                //System.Diagnostics.Debug.Assert(false);
                                System.Diagnostics.Debug.WriteLine(
                                    "[ERROR]parentElemType:{0}, parentId:{1}", parentElemType, parentId);
                                System.Diagnostics.Debug.WriteLine(
                                    "[ERROR] add vertex pp: {0},{1}", pp.X, pp.Y);
                            }
                        } // addEdgeFlg
                    } // hitVId

                    if (addedCnt == 0)
                    {
                        // 頂点の作成が１つも行われなかった場合
                        //addPts.Add(pps[indexPP]);
                        System.Diagnostics.Debug.Assert(false);
                    }
                    if (EditPts.Count > 0)
                    {
                        System.Diagnostics.Debug.Assert(
                            OpenTK.Vector2d.Distance(pp, EditPts[EditPts.Count - 1]) <
                            Constants.PrecisionLowerLimit);
                    }
                } // addVertexAndEdgeFlg
            } // loopFixFlg

            return executed;
        }

        /// <summary>
        /// 頂点と辺を追加する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="parentElemType"></param>
        /// <param name="parentId"></param>
        /// <param name="pp"></param>
        /// <param name="editPts"></param>
        /// <param name="editVertexIds"></param>
        /// <param name="editEdgeIds"></param>
        /// <param name="vertexIdAdd"></param>
        /// <param name="edgeIdAdd"></param>
        /// <param name="edgeIdAddByAddVertex"></param>
        /// <param name="loopIdAddByConnectVertex"></param>
        /// <returns></returns>
        private static bool AddVertexAndEdge(
            Cad2D cad, CadElementType parentElemType, uint parentId, OpenTK.Vector2d pp,
            IList<OpenTK.Vector2d> editPts, IList<uint> editVertexIds, IList<uint> editEdgeIds,
            out uint vertexIdAdd, out uint edgeIdAdd, out uint edgeIdAddByAddVertex,
            out uint loopIdAddByConnectVertex,
            bool showErrorFlg)
        {
            bool success = false;
            uint vId = 0; // 作成された頂点のID
            uint eIdAddByAddVertex = 0; // 頂点作成で辺が分割された場合に格納
            uint eId = 0; // 作成された辺のID
            uint lIdAddByConnectVertex = 0; // 辺の作成でループが作成された場合ループIDを格納 

            // 頂点を作成
            success = AddVertex(cad, parentElemType, parentId, pp,
                editPts, editVertexIds, editEdgeIds,
                out vId, out eIdAddByAddVertex,
                showErrorFlg);
            if (!success || vId == 0)
            {
                success = false;
            }
            else
            {
                success = true;
                uint lIdOfAddVertex = 0; // 追加された頂点の属するループID
                if (parentElemType == CadElementType.Loop)
                {
                    lIdOfAddVertex = parentId; // 図面が空の状態だと0が入る、それ以外は頂点を追加したループのID
                }
                else if (parentElemType == CadElementType.Edge && eIdAddByAddVertex != 0)
                {
                    lIdOfAddVertex = 0;
                    lIdOfAddVertex = GetEdgeLoopId(cad, eIdAddByAddVertex);

                    // Note: 今回作成した頂点が辺上にある場合、前の頂点も辺上ならば辺の作成はできない
                }
                else
                {
                    // ロジックエラー
                    System.Diagnostics.Debug.Assert(false);
                }

                if (editVertexIds.Count >= 2)
                {
                    // 辺を作成
                    success = AddEdgeByLastEditPts(cad, lIdOfAddVertex,
                        editVertexIds,
                        editEdgeIds,
                        out eId, out lIdAddByConnectVertex,
                        showErrorFlg);
                    if (!success)
                    {
                        // 失敗
                        // 頂点を削除する
                        cad.RemoveElement(CadElementType.Vertex, vId);
                        editVertexIds.RemoveAt(editVertexIds.Count - 1);
                        eIdAddByAddVertex = 0;
                        editPts.RemoveAt(editPts.Count - 1);
                        ////EditParentLoopId = 0;
                    }
                }
            }
            vertexIdAdd = vId;
            edgeIdAdd = eId;
            edgeIdAddByAddVertex = eIdAddByAddVertex;
            loopIdAddByConnectVertex = lIdAddByConnectVertex;
            return success;
        }

        /// <summary>
        /// 頂点を作成する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="parentElemType"></param>
        /// <param name="parentId"></param>
        /// <param name="pp"></param>
        /// <param name="editPts"></param>
        /// <param name="editVertexIds"></param>
        /// <param name="editEdgeIds"></param>
        /// <param name="vertexIdAdd"></param>
        /// <param name="edgeIdAddByAddVertex"></param>
        /// <param name="showErrorFlg"></param>
        /// <returns></returns>
        private static bool AddVertex(Cad2D cad, CadElementType parentElemType, uint parentId,
            OpenTK.Vector2d pp,
            IList<OpenTK.Vector2d> editPts, IList<uint> editVertexIds, IList<uint> editEdgeIds,
            out uint vertexIdAdd, out uint edgeIdAddByAddVertex,
            bool showErrorFlg)
        {
            bool success = false;
            uint vId = 0; // 作成された頂点のID
            uint eIdAddByAddVertex = 0; // 頂点作成で辺が分割された場合に格納

            // 頂点を作成
            //vId = cad.AddVertex(parentElemType, parentId, pp).AddVId;
            AddVertexRes addVertexRes = cad.AddVertex(parentElemType, parentId, pp);
            vId = addVertexRes.AddVId;
            eIdAddByAddVertex = addVertexRes.AddEId;
            if (vId == 0)
            {
                // 頂点の作成に失敗
                if (showErrorFlg)
                {
                    MessageBoxShowError("頂点の作成に失敗しました", "");
                }
                addVertexRes.AddVId = 0;
                success = false;
            }
            else
            {
                success = true;
                /*
                // 頂点の追加で辺が分割された場合は、辺のIDを記録する
                if (eIdAddByAddVertex != 0)
                {
                    editEdgeIds.Add(eId_AddByAddVertex);
                }
                 */
                // 頂点の作成に成功
                // 点を追加
                editPts.Add(pp);

                // 頂点をリストに追加
                editVertexIds.Add(vId);

                // 一時作成の辺の色をセットする
                if (eIdAddByAddVertex != 0)
                {
                    cad.SetEdgeColor(eIdAddByAddVertex, 
                        ColorToColorDouble(TmpEdgeColor));
                }
            }
            vertexIdAdd = vId;
            edgeIdAddByAddVertex = eIdAddByAddVertex;
            return success;
        }

        /// <summary>
        /// 辺を作成する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="lIdOfAddVertex"></param>
        /// <param name="editVertexIds"></param>
        /// <param name="editEdgeIds"></param>
        /// <param name="eId"></param>
        /// <param name="lIdAddByConnectVertex"></param>
        /// <returns></returns>
        private static bool AddEdgeByLastEditPts(Cad2D cad, uint lIdOfAddVertex,
            IList<uint> editVertexIds, IList<uint> editEdgeIds,
            out uint eId, out uint lIdAddByConnectVertex, bool showErrorFlg)
        {
            bool success = false;

            // 出力の初期化
            eId = 0;
            lIdAddByConnectVertex = 0;

            // 作成しようとしている辺がすでに作成されているかチェックする
            uint existedEId = GetEdgeIdByVertexIds(
                cad, editVertexIds[editVertexIds.Count - 2], editVertexIds[editVertexIds.Count - 1]);
            if (existedEId != 0)
            {
                // すでに追加しようとしている辺が存在する場合は追加しない
                if (showErrorFlg)
                {
                    MessageBoxShowError("すでに辺は作成されています", "");
                }
                return success;
            }

            // 辺を作成
            //eId = cad.ConnectVertex_Line(EditVertexIds[EditVertexIds.Count - 2], EditVertexIds[EditVertexIds.Count - 1]).id_e_add;
            ConnectVertexRes connectVertexRes = cad.ConnectVertexLine(
                editVertexIds[editVertexIds.Count - 2], editVertexIds[editVertexIds.Count - 1]);
            eId = connectVertexRes.AddEId;  // 追加された辺のIDを格納
            if (connectVertexRes.AddLId != 0)
            {
                System.Diagnostics.Debug.WriteLine("lIdOfAddVertex:{0}", lIdOfAddVertex);
                System.Diagnostics.Debug.WriteLine("AddLId:{0}", connectVertexRes.AddLId);
                System.Diagnostics.Debug.WriteLine("LId:{0}", connectVertexRes.LId);

                lIdAddByConnectVertex = 
                    (connectVertexRes.AddLId != lIdOfAddVertex) ?
                    connectVertexRes.AddLId :
                    connectVertexRes.LId;  // 辺の作成でループが作成された場合ループIDを格納
                System.Diagnostics.Debug.WriteLine("lIdAddByConnectVertex:{0}", lIdAddByConnectVertex);
            }
            else
            {
                lIdAddByConnectVertex = 0;
            }
            if (eId == 0)
            {
                // 辺の作成に失敗
                if (showErrorFlg)
                {
                    MessageBoxShowError("辺の作成に失敗しました", "");
                }
                lIdAddByConnectVertex = 0;

                success = false;
            }
            else
            {
                // 辺の作成に成功
                editEdgeIds.Add(eId);

                // 一時作成の辺の色をセットする
                cad.SetEdgeColor(eId, ColorToColorDouble(TmpEdgeColor));

                success = true;
            }
            return success;
        }

        /// <summary>
        /// ヒットテスト
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool HitTest(Point pt, out CadElementType partElemType, out uint partId)
        {
            bool hit = false;
            partElemType = CadElementType.NotSet;
            partId = 0;

            int sizeBuffer = 2048;
            int[] pickSelectBuffer = new int[sizeBuffer];
            OpenGLUtils.PickPre(sizeBuffer, pickSelectBuffer, (uint)pt.X, (uint)pt.Y, 5, 5, Camera);
            DrawerArray.DrawSelection();
            List<SelectedObject> selecObjs = 
                (List<SelectedObject>)
                OpenGLUtils.PickPost(pickSelectBuffer, (uint)pt.X, (uint)pt.Y, Camera);

            DrawerArray.ClearSelected();
            if (selecObjs.Count > 0)
            {
                if (DrawerArray.Drawers.Count >= 2)
                {
                    Cad2DDrawer drawer = DrawerArray.Drawers[1] as Cad2DDrawer;
                    if (drawer != null)
                    {
                        int ctrlIndex;
                        drawer.GetPartCadId(selecObjs[0].Name, out partElemType, out partId, out ctrlIndex);
                        //System.Diagnostics.Debug.WriteLine("partElemType:{0} partId:{1}", partElemType, partId);
                    }
                }
                //int index = 0;
                //foreach (int ival in selecObjs[0].name)
                //{
                //    System.Diagnostics.Debug.WriteLine("selecObjs[0].name[{0}] = {1}", index, ival);
                //    index++;
                //}
                if (CadMode == CadModeType.None &&
                        (partElemType == CadElementType.Loop ||
                        partElemType == CadElementType.Edge ||
                        partElemType == CadElementType.Vertex) ||
                    (CadMode == CadModeType.Polygon &&
                        ((!IsEditing && partElemType == CadElementType.Loop) ||
                        partElemType == CadElementType.Edge ||
                        partElemType == CadElementType.Vertex)) ||
                    (CadMode == CadModeType.Move &&
                        (partElemType == CadElementType.Loop ||
                        partElemType == CadElementType.Edge ||
                        partElemType == CadElementType.Vertex)) ||
                    (CadMode == CadModeType.Erase &&
                        (partElemType == CadElementType.Loop ||
                        partElemType == CadElementType.Edge ||
                        partElemType == CadElementType.Vertex)))
                {
                    // 選択表示設定に追加する
                    DrawerArray.AddSelected(selecObjs[0].Name);
                }
                else
                {
                    // 選択表示にはしない
                }
                // ヒットフラグは選択表示する/しないに関係なくヒットしたらフラグを立てる
                hit = true;
            }
            return hit;
        }

        /// <summary>
        /// 領域を削除する
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool DoEraseDisconArea(Point pt)
        {
            bool executed = false;
            if (CadMode != CadModeType.Erase)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CadElementType partElemType;
            uint partId;
            bool hit = HitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CadElementType.Loop && partId != 0 &&
                    Cad.IsElementId(CadElementType.Loop, partId))
                {
                    uint tagtLoopId = partId;
                    // ループの削除処理
                    executed = DelLoop(Cad, tagtLoopId, LoopIds, PortEdges);
                }
            }
            if (executed && !IsDirty)
            {
                IsDirty = true;
            }
            return executed;
        }

        /// <summary>
        /// ループ削除処理
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="tagtLoopId"></param>
        /// <param name="loopIds"></param>
        /// <param name="portEdges"></param>
        /// <returns></returns>
        private static bool DelLoop(
            Cad2D cad, uint tagtLoopId,
            IList<uint> loopIds, IList<PortEdge> portEdges)
        {
            bool executed = false;
            if (tagtLoopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return executed;
            }

            // ループの頂点、辺のIDのリストを取得する
            IList<uint> vIds = null;
            IList<uint> eIds = null;
            GetLoopVertexsAndEdges(cad, tagtLoopId, out vIds, out eIds);

            IList<uint> otherLoopVIds = new List<uint>();
            IList<uint> otherLoopEIds = new List<uint>();
            foreach (uint workLoopId in loopIds)
            {
                if (workLoopId != tagtLoopId)
                {
                    IList<uint> workVIds = null;
                    IList<uint> workEIds = null;
                    GetLoopVertexsAndEdges(cad, workLoopId, out workVIds, out workEIds);
                    foreach (uint workVId in workVIds)
                    {
                        otherLoopVIds.Add(workVId);
                    }
                    foreach (uint workEId in workEIds)
                    {
                        otherLoopEIds.Add(workEId);
                    }
                }
            }

            // ポート境界があれば削除する
            DelPortBelongToLoop(cad, tagtLoopId, portEdges);

            // エラーチェック用
            Dictionary<uint, IList<uint>> saveLoopEdgess = null;
            ChkLoopEdgesPreProc(cad, loopIds, out saveLoopEdgess);

            // ループ削除
            //   辺と頂点も削除してくれているはず? -->してくれない-->というかループの場合なにもしないらしい
            //cad.RemoveElement(CadElementType.Loop, tagtLoopId);
            // ループを構成する辺と頂点を削除
            // 辺を削除
            foreach (uint eId in eIds)
            {
                if (otherLoopEIds.IndexOf(eId) >= 0)
                {
                    // 他のループと共有している辺の場合(領域分割で作成された辺)
                    continue;
                }

                // 辺を削除
                cad.RemoveElement(CadElementType.Edge, eId);

                // 領域すべてが他のループに囲まれていて削除できない場合がある
                // そのため、ここで辺の削除が行われていることを示すためにフラグを立てる
                executed = true;
            }
            if (executed)
            {
                // 頂点削除
                foreach (uint vId in vIds)
                {
                    if (otherLoopVIds.IndexOf(vId) >= 0)
                    {
                        // 他のループと共有している頂点の場合(領域分割で作成された頂点)
                        continue;
                    }
                    cad.RemoveElement(CadElementType.Vertex, vId);
                }
            }

            if (executed)
            {
                // ループリストから削除
                loopIds.Remove(tagtLoopId);

                // チェック用
                // ループIDが変更されているかチェックし、変更されていればループ情報を更新する
                ChkLoopEdgesPostProc(cad, saveLoopEdgess, tagtLoopId, 0, loopIds);

                // 全ループの色を再設定する
                SetupCadObjectsColorForAllLoops(cad, loopIds);

            }

            return executed;
        }

        /// <summary>
        /// エラーチェック用前処理
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="loopIds">ループ情報リスト</param>
        /// <param name="saveLoopEdgess"></param>
        private static void ChkLoopEdgesPreProc(
            Cad2D cad, IList<uint> loopIds, out Dictionary<uint, IList<uint>> saveLoopEdgess)
        {
            saveLoopEdgess = new Dictionary<uint, IList<uint>>();
            foreach (uint workLoopId in loopIds)
            {
                IList<uint> workVIds = null;
                IList<uint> workEIds = null;
                GetLoopVertexsAndEdges(cad, workLoopId, out workVIds, out workEIds);
                saveLoopEdgess.Add(workLoopId, workEIds);
            }
        }
        /// <summary>
        /// ループIDが変更されているかチェックし、変更されていればループ情報を更新する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="saveLoopEdgess"></param>
        /// <param name="delLoopId">削除されたループID</param>
        /// <param name="delEId">削除された辺ID</param>
        /// <param name="loopIds">ループ情報リスト</param>
        private static void ChkLoopEdgesPostProc(
            Cad2D cad, Dictionary<uint, IList<uint>> saveLoopEdgess, uint delLoopId, uint delEId,
            IList<uint> loopIds)
        {
            if (delLoopId != 0)
            {
                // まず削除されたループは除外するためマップから削除
                saveLoopEdgess.Remove(delLoopId);
            }

            // エラーチェック：ループIDが変更される??
            foreach (var loopEdgesPair in saveLoopEdgess)
            {
                uint prevLoopId = loopEdgesPair.Key;
                IList<uint> eIds = loopEdgesPair.Value;

                // ループIDが変わってないか検査する
                Dictionary<uint, int> loopIdsHash = new Dictionary<uint, int>();
                foreach (uint eId in eIds)
                {
                    if (eId == delEId)
                    {
                        continue;
                    }
                    uint curLoopId = GetEdgeLoopId(cad, eId);
                    if (curLoopId == 0)
                    {
                        continue;
                    }
                    /*
                    if (curLoopId != prevLoopId)
                    {
                    }
                     */
                    int hitCnt = 0;
                    if (loopIdsHash.ContainsKey(curLoopId))
                    {
                        hitCnt = loopIdsHash[curLoopId];
                        hitCnt++;
                        loopIdsHash[curLoopId] = hitCnt;
                    }
                    else
                    {
                        hitCnt = 1;
                        loopIdsHash.Add(curLoopId, hitCnt);
                    }
                }
                if (loopIdsHash.ContainsKey(prevLoopId))
                {
                    // 以前のループIDがある
                    // 変更されていないとする
                    System.Diagnostics.Debug.WriteLine("edge cnt = {0}, hit cnt = {1}",
                        eIds.Count, loopIdsHash[prevLoopId]);
                }
                else
                {
                    // 変更された
                    // 一番ヒット数の多いループIDを新しいループIDとする
                    uint curLoopId = 0;
                    int maxHits = 0;
                    foreach (var loopIdsHPair in loopIdsHash)
                    {
                        uint workLoopId = loopIdsHPair.Key;
                        int hitCnt = loopIdsHPair.Value;
                        if (maxHits < hitCnt)
                        {
                            curLoopId = workLoopId;
                            maxHits = hitCnt;
                        }
                    }
                    System.Diagnostics.Debug.Assert(curLoopId != 0);
                    // ループ情報を更新
                    int index = loopIds.IndexOf(prevLoopId);
                    loopIds[index] = curLoopId;

                    System.Diagnostics.Debug.WriteLine("loopId changed {0} → {1}", prevLoopId, curLoopId);
                    //MessageBox.Show(string.Format("loopId changed {0} → {1}", prevLoopId, curLoopId));
                }
            }
        }

        /// <summary>
        /// ループの辺に属するポートを削除する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="tagtLoopId"></param>
        /// <param name="portEdges"></param>
        /// <returns>実行された場合true、実行されなかった場合false</returns>
        public static bool DelPortBelongToLoop(Cad2D cad, uint tagtLoopId,
            IList<PortEdge> portEdges)
        {
            bool executed = false;
            if (tagtLoopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return executed;
            }

            // ループの頂点、辺のIDのリストを取得する
            IList<uint> vIds = null;
            IList<uint> eIds = null;
            GetLoopVertexsAndEdges(cad, tagtLoopId, out vIds, out eIds);

            // ポート境界があれば削除する
            IList<PortEdge> delEdges = new List<PortEdge>();
            // すべてのポートについて検査
            foreach (PortEdge work in portEdges)
            {
                // 削除対象の辺集合か調べる
                foreach (uint eId in eIds)
                {
                    if (work.EdgeId == eId)
                    {
                        delEdges.Add(work);
                        break;
                    }
                }
            }
            // ポート削除処理
            foreach (PortEdge deltarget in delEdges)
            {
                // 削除前検査：ポート削除対象が、ほかのループと共有しているか検査する
                bool isSharedPort = false;
                uint eId = deltarget.EdgeId;
                if (IsEdgeSharedByLoops(cad, eId))
                {
                    isSharedPort = true;
                }
                if (isSharedPort)
                {
                    // 削除しない
                    continue;
                }
                // ポート削除
                DoErasePortCore(cad, deltarget, portEdges);
                executed = true;
            }
            return executed;
        }

        /// <summary>
        /// 円弧⇔直線切り替え
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool DoToggleArcLine(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Arc)
            {
                return executed;
            }
            if (DragArcFlg)
            {
                return executed;
            }

            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CadElementType partElemType;
            uint partId;
            bool hit = HitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CadElementType.Edge && partId != 0 &&
                    Cad.IsElementId(CadElementType.Edge, partId))
                {
                    uint eId = partId;
                    Edge2D edge = Cad.GetEdge(eId);
                    if (edge.CurveType == CurveType.CurveArc)
                    {
                        edge.SetCurveLine();
                    }
                    else
                    {
                        //uint sVId;
                        //uint eVId;
                        //Cad.GetEdgeVertexId(out sVId, out eVId, eId);
                        //OpenTK.Vector2d sPt = Cad.GetVertexCoord(sVId);
                        //OpenTK.Vector2d ePt = Cad.GetVertexCoord(eVId);
                        //double dist = OpenTK.Vector2d.Distance(sPt, ePt) * 10.0;
                        double dist = 0.0;
                        Cad.SetCurveArc(eId, false, dist);
                    }
                    executed = true;
                }
            }

            if (executed && !IsDirty)
            {
                IsDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// 円弧のドラッグ開始
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool BeginDragArc(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Arc)
            {
                return executed;
            }

            DragArcFlg = false;
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CadElementType partElemType;
            uint partId;
            bool hit = HitTest(pt, out partElemType, out partId);
            {
                if (partElemType == CadElementType.Edge && partId != 0 &&
                    Cad.IsElementId(CadElementType.Edge, partId))
                {
                    uint eId = partId;
                    Edge2D edge = Cad.GetEdge(eId);
                    if (edge.CurveType == CurveType.CurveArc)
                    {
                        MovElemType = CadElementType.Edge;
                        MovObjId = eId;

                        executed = true;
                    }
                }
            }
            return executed;
        }

        /// <summary>
        /// 円弧のドラッグ
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public bool DoDragArc(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Arc)
            {
                return executed;
            }

            if (!DragFlg)
            {
                return executed;
            }

            uint eId = MovObjId;

            OpenTK.Vector2d pp = ScreenPointToCoord(pt);
            // 円弧をドラッグ
            bool ret = Cad.DragArc(eId, pp);
            if (ret)
            {
                DragArcFlg = true;
                executed = true;
            }
            if (executed && !IsDirty)
            {
                IsDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// 円弧のドラッグ終了
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool EndDragArc(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Arc)
            {
                return executed;
            }
            if (DragArcFlg)
            {
                DragArcFlg = false;
                executed = true;
            }
            return executed;
        }

        /// <summary>
        /// 入出力ポートの選択処理
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool DoSelectPort(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Port)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CadElementType partElemType;
            uint partId;
            bool hit = HitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CadElementType.Edge && partId != 0 &&
                    Cad.IsElementId(CadElementType.Edge, partId))
                {
                    uint tagtEdgeId = partId;
                    // 辺がループに属しているかチェック
                    uint parentLoopId = GetEdgeLoopId(Cad, tagtEdgeId);
                    if (parentLoopId == 0)
                    {
                        // 辺がループに属していない
                        return executed;
                    }
                    // すでに対象の辺がポート境界に指定されていないかチェックする
                    PortEdge otherEdge = GetPortEdgeByEdgeId(tagtEdgeId);
                    if (otherEdge == null)
                    {
                        // 新規の辺

                        PortEdge edge = null;
                        {
                            // 新規のポート境界
                            edge = new PortEdge();
                            edge.No = PortEdges.Count + 1;
                            edge.EdgeId = tagtEdgeId;
                            // ポート境界の追加
                            PortEdges.Add(edge);
                            executed = true;
                        }
                        if (executed)
                        {
                            // 辺に色を付ける
                            Color portColor = PortColor;
                            double[] portColorDouble = ColorToColorDouble(portColor);
                            uint eId = edge.EdgeId;
                            Cad.SetEdgeColor(eId, portColorDouble);
                        }
                    }
                }
            }
            if (executed && !IsDirty)
            {
                IsDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// 入出力ポートの削除処理
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool DoErasePort(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Erase)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CadElementType partElemType;
            uint partId;
            bool hit = HitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CadElementType.Edge && partId != 0 &&
                    Cad.IsElementId(CadElementType.Edge, partId))
                {
                    uint tagtEdgeId = partId;
                    PortEdge hitEdge = GetPortEdgeByEdgeId(tagtEdgeId);
                    if (hitEdge != null)
                    {
                        DoErasePortCore(Cad, hitEdge, PortEdges);

                        executed = true;
                    }
                }
            }
            if (executed && !IsDirty)
            {
                IsDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// 辺のIDからポートを取得する
        /// </summary>
        /// <param name="tagtEdgeId"></param>
        /// <returns></returns>
        private PortEdge GetPortEdgeByEdgeId(uint tagtEdgeId)
        {
            PortEdge hitEdge = null;
            foreach (PortEdge work in PortEdges)
            {
                if (work.EdgeId == tagtEdgeId)
                {
                    hitEdge = work;
                    break;
                }
            }
            return hitEdge;
        }

        /// <summary>
        /// ポート境界の消去コア処理
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="hitEdge"></param>
        /// <param name="portEdges"></param>
        private static void DoErasePortCore(
            Cad2D cad, PortEdge hitEdge, IList<PortEdge> portEdges)
        {
            // ヒットしたポート番号
            int hitPortNo = hitEdge.No;

            System.Diagnostics.Debug.Assert(hitEdge != null);
            // 辺ID
            uint eId = hitEdge.EdgeId;

            //辺が属するループを取得する
            uint lId = GetEdgeLoopId(cad, eId);
            // ループの背景色を取得する
            double[] loopColorDouble = cad.GetLoopColor(lId);
            Color loopColor = ColorDoubleToColor(loopColorDouble);
            Color loopLineColor = GetLoopLineColor(loopColor);
            double[] loopLineColorDouble = ColorToColorDouble(loopLineColor);
            cad.SetEdgeColor(eId, loopLineColorDouble);

            // ポートを削除
            portEdges.Remove(hitEdge);

            // ポート番号振り直し
            for (int portIndex = 0; portIndex < portEdges.Count; portIndex++)
            {
                PortEdge work = portEdges[portIndex];
                int no = work.No;
                work.No = portIndex + 1;
            }
        }

        /// <summary>
        /// 辺の削除処理
        ///   危険　テスト実装
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool DoEraseCadEdge(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Erase)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CadElementType partElemType;
            uint partId;
            bool hit = HitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CadElementType.Edge && partId != 0 && 
                    Cad.IsElementId(CadElementType.Edge, partId))
                {
                    uint tagtEdgeId = partId;
                    // まずポート境界があれば削除
                    PortEdge hitEdge = GetPortEdgeByEdgeId(tagtEdgeId);
                    if (hitEdge != null)
                    {
                        DoErasePortCore(Cad, hitEdge, PortEdges);
                        executed = true;
                    }
                    // 辺を削除するとループが壊れるので壊れるループIDを記録する
                    uint brokenLoopId = 0;
                    {
                        // 辺のループIDを取得する
                        uint workLoopId = GetEdgeLoopId(Cad, tagtEdgeId);
                        // 辺を削除するとループが壊れるかチェックする
                        // 辺の両端に別の辺が接続していれば壊れる
                        // ＜壊れない例（除外したい）＞
                        //   +-------------+
                        //   |             |
                        //   |    +        |
                        //   |    |        |
                        //   +----+--------+
                        //
                        if (workLoopId != 0)
                        {
                            uint workVId1;
                            uint workVId2;
                            GetEdgeVertexId(Cad, tagtEdgeId, out workVId1, out workVId2);
                            IList<uint> workEIds1 = GetEdgeIdsByVertexId(Cad, workVId1);
                            IList<uint> workEIds2 = GetEdgeIdsByVertexId(Cad, workVId2);
                            if (workEIds1.Count >= 2 && workEIds2.Count >= 2)
                            {
                                brokenLoopId = workLoopId;
                            }
                        }
                    }
                    // 辺の頂点を取得する
                    uint vId1 = 0;
                    uint vId2 = 0;
                    GetEdgeVertexId(Cad, tagtEdgeId, out vId1, out vId2);

                    // エラーチェック用
                    // ループの辺IDを１つ取得しておく
                    uint exceptEId = tagtEdgeId; // 削除対象辺IDは除外して取得
                    // エラーチェック用
                    Dictionary<uint, IList<uint>> saveLoopEdgess = null;
                    ChkLoopEdgesPreProc(Cad, LoopIds, out saveLoopEdgess);

                    if (brokenLoopId != 0)
                    {
                        // 壊れるループにポート境界があれば削除する
                        bool delportExceuted = DelPortBelongToLoop(
                            Cad, brokenLoopId, PortEdges);
                        if (delportExceuted)
                        {
                            if (!executed)
                            {
                                executed = true;
                            }
                        }
                    }

                    // 辺を削除
                    bool ret = Cad.RemoveElement(CadElementType.Edge, tagtEdgeId);
                    if (!ret)
                    {
                        // 失敗
                        MessageBoxShowError("辺の削除に失敗しました", "");
                    }
                    else
                    {
                        if (!executed)
                        {
                            executed = true;
                        }

                        // 頂点を削除（辺に属していなけれは)
                        if (!IsVertexOwnedByEdges(Cad, vId1))
                        {
                            bool retRmVertex = Cad.RemoveElement(CadElementType.Vertex, vId1);
                            System.Diagnostics.Debug.Assert(retRmVertex);
                        }
                        if (!IsVertexOwnedByEdges(Cad, vId2))
                        {
                            bool retRmVertex = Cad.RemoveElement(CadElementType.Vertex, vId2);
                            System.Diagnostics.Debug.Assert(retRmVertex);
                        }

                        if (brokenLoopId != 0)
                        {
                            // 壊れたループのループ情報を削除する
                            //  Note:情報のみ削除する。ループの残骸の頂点や辺はCadオブジェクトから消去しない
                            LoopIds.Remove(brokenLoopId);
                        }

                        // チェック用
                        // ループIDが変更されているかチェックし、変更されていればループ情報を更新する
                        ChkLoopEdgesPostProc(Cad, saveLoopEdgess, brokenLoopId, tagtEdgeId, LoopIds);
                    }
                    if (executed)
                    {
                        // 全ループの色を再設定する
                        SetupCadObjectsColorForAllLoops(Cad, LoopIds);
                    }
                }
            }
            if (executed && !IsDirty)
            {
                IsDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// ループの線分の色を取得する
        /// </summary>
        /// <param name="loopColor"></param>
        /// <returns></returns>
        private static Color GetLoopLineColor(Color loopColor)
        {
            //Color loopLineColor = Color.FromArgb(0xff & (loopColor.R - 0x20), 0xff & (loopColor.G - 0x20), 0xff & (loopColor.B - 0x20));
            Color loopLineColor = Color.Gray;
            return loopLineColor;
        }

        /// <summary>
        /// Colorをdoule[]の規格化カラーに変換する
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static double[] ColorToColorDouble(Color color)
        {
            double[] colorDouble = new double[] { color.R / (double)255, color.G / (double)255, color.B / (double)255 };
            return colorDouble;
        }

        /// <summary>
        /// 規格化カラーをカラーに変換する
        /// </summary>
        /// <param name="colorDouble"></param>
        /// <returns></returns>
        private static Color ColorDoubleToColor(double[] colorDouble)
        {
            return Color.FromArgb((int)(colorDouble[0] * 255), (int)(colorDouble[1] * 255), (int)(colorDouble[2] * 255));
        }

        /// <summary>
        /// ループの色と辺の色を全ループについて設定する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="loopIds"></param>
        private static void SetupCadObjectsColorForAllLoops(Cad2D cad, IList<uint> loopIds)
        {
            //ループの色をすべて再設定する
            foreach (uint workLoopId in loopIds)
            {
                Color backColor = LoopBackColor;
                SetupCadObjectsColorForOneLoop(cad, workLoopId, backColor);
            }
        }

        /// <summary>
        /// Cadオブジェクトのループとその辺、頂点の色をセットする
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="lId"></param>
        /// <param name="backColor"></param>
        private static void SetupCadObjectsColorForOneLoop(Cad2D cad, uint lId, Color backColor)
        {
            // ループの頂点と辺のリストを取得する
            IList<uint> vIds = null;
            IList<uint> eIds = null;
            GetLoopVertexsAndEdges(cad, lId, out vIds, out eIds);

            // ループの色を指定（指定しなければ(0.9,0.8,0.8)になる
            double[] backColorDouble = ColorToColorDouble(backColor);
            cad.SetLoopColor(lId, backColorDouble);
            // 辺、頂点の色の変更
            Color loopLineColor = GetLoopLineColor(backColor);
            double[] lineColorDouble = ColorToColorDouble(loopLineColor);
            // 辺の色
            foreach (uint eId in eIds)
            {
                cad.SetEdgeColor(eId, lineColorDouble);
            }
            // 頂点の色
            //foreach (uint vId in vIds)
            //{
            //    cad.SetVertexColor(vId, lineColorDouble);
            //}
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // CadObj2Dのユーティリティ
        /// <summary>
        /// ループの内側にあるループを子ループに設定する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="loopId"></param>
        /// <param name="loopIds"></param>
        /// <param name="edgeCollections"></param>
        private static void ReconstructLoopsInsideLoopAsChild(Cad2D cad, uint loopId,
            IList<uint> loopIds, IList<PortEdge> edgeCollections)
        {
            if (loopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            //   内側にあるループIDのリストを取得する
            IList<uint> insideLoopIds = GetLoopIdsInsideLoop(cad, loopId, loopIds);
            //  子ループに設定
            foreach (uint childLoopId in insideLoopIds)
            {
                SetLoopParentLoopId(cad, childLoopId, loopId, loopIds, edgeCollections);
            }
        }

        /// <summary>
        /// ループの内側にあるループのIDのリストを取得する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="loopId"></param>
        /// <param name="loopIds"></param>
        /// <returns></returns>
        private static IList<uint> GetLoopIdsInsideLoop(Cad2D cad, uint loopId, IList<uint> loopIds)
        {
            IList<uint> hitLoopIds = new List<uint>();
            if (loopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return hitLoopIds;
            }
            foreach (uint workLoopId in loopIds)
            {
                if (loopId == workLoopId)
                {
                    continue;
                }
                // ワークループの１点をヒットテストする
                //   先ず頂点を取得
                uint vId = 0;
                {
                    IList<uint> vIds = null;
                    IList<uint> eIds = null;
                    GetLoopVertexsAndEdges(cad, workLoopId, out vIds, out eIds);
                    if (vIds.Count > 0)
                    {
                        vId = vIds[0];
                    }
                }
                System.Diagnostics.Debug.Assert(vId != 0);
                // ワークループの頂点の座標を取得
                OpenTK.Vector2d vPP = cad.GetVertexCoord(vId);
                // ワークループの頂点が、指定ループの内側の点か？
                bool inside = cad.CheckIsPointInsideLoop(loopId, vPP);
                if (inside)
                {
                    hitLoopIds.Add(workLoopId);
                }
            }
            return hitLoopIds;
        }

        /// <summary>
        /// ループに親ループIDを設定する
        ///   子ループを削除して、親ループの子ループとして再作成する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="childLoopId">子ループID</param>
        /// <param name="parentLoopId">親ループID</param>
        /// <param name="loopIds">ループ情報リスト</param>
        /// <param name="edgeCollections">ポート境界エッジコレクションのリスト</param>
        private static void SetLoopParentLoopId(
            Cad2D cad, uint childLoopId, uint parentLoopId,
            IList<uint> loopIds, IList<PortEdge> edgeCollections)
        {
            if (childLoopId == 0 || parentLoopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            // 方針：子ループを削除して再設定する

            // 子ループのループ情報を退避
            uint childLoopIdTmp = childLoopId;
            // 子ループの頂点座標を取得する
            IList<OpenTK.Vector2d> vPPs = new List<OpenTK.Vector2d>();
            {
                IList<uint> vIds = null;
                IList<uint> eIds = null;
                GetLoopVertexsAndEdges(cad, childLoopId, out vIds, out eIds);
                foreach (uint vId in vIds)
                {
                    OpenTK.Vector2d vPP = cad.GetVertexCoord(vId);
                    vPPs.Add(vPP);
                }
            }
            // 子ループを削除
            bool delRet = DelLoop(cad, childLoopId, loopIds, edgeCollections);
            if (!delRet)
            {
                MessageBoxShowError("子ループの設定に失敗しました", "");
            }
            else
            {
                // ループを再追加
                //uint id_l_add = cad.AddPolygon(v_pps, parentLoopId).id_l_add;
                uint addLId = MakeLoop(cad, vPPs, loopIds, true);
                if (addLId == 0)
                {
                    MessageBoxShowError("子ループの設定に失敗しました", "");
                }
                else
                {
                    // 子ループの作成に成功

                    // ループ情報を再登録
                    loopIds.Add(addLId);

                    // ループの色を設定
                    Color backColor = LoopBackColor;
                    SetupCadObjectsColorForOneLoop(cad, addLId, backColor);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 隣り合う辺か？
        /// </summary>
        /// <param name="eId1"></param>
        /// <param name="eId2"></param>
        /// <returns></returns>
        private static bool IsNextDoorEdge(Cad2D cad, uint eId1, uint eId2)
        {
            bool isNextDoor = false;
            // ここで隣の辺のみに限定したい
            Edge2D tagtEdge = cad.GetEdge(eId1);
            uint[] tagtVtx = new uint[] { tagtEdge.GetVertexId(true), tagtEdge.GetVertexId(false) };
            Edge2D prevEdge = cad.GetEdge(eId2);
            uint[] prevVtx = new uint[] { prevEdge.GetVertexId(true), prevEdge.GetVertexId(false) };
            if (tagtVtx[0] == prevVtx[0] || tagtVtx[0] == prevVtx[1] ||
                tagtVtx[1] == prevVtx[0] || tagtVtx[1] == prevVtx[1])
            {
                isNextDoor = true;
            }
            return isNextDoor;
        }

        /// <summary>
        /// ループの頂点と辺のIDのリストを取得する
        /// </summary>
        /// <param name="lId"></param>
        /// <param name="vIds"></param>
        /// <param name="eIds"></param>
        public void GetLoopVertexsAndEdges(uint lId, out IList<uint> vIds, out IList<uint> eIds)
        {
            GetLoopVertexsAndEdges(Cad, lId, out vIds, out eIds);
        }

        private static void GetLoopVertexsAndEdges(
            Cad2D cad, uint lId, out IList<uint> vIds, out IList<uint> eIds)
        {
            vIds = new List<uint>();
            eIds = new List<uint>();
            if (lId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }
            for (LoopEdgeItr lItr = cad.GetLoopEdgeItr(lId); !lItr.IsChildEnd; lItr.ShiftChildLoop())
            {
                if (!lItr.IsParent())
                {
                    // 親でないならスキップする
                    continue;
                }
                for (lItr.Begin(); !lItr.IsEnd(); lItr.Next())
                {
                    uint vId = lItr.GetVertexId();
                    vIds.Add(vId);

                    uint eId;
                    bool isSameDir;
                    lItr.GetEdgeId(out eId, out isSameDir);
                    if (!cad.IsElementId(CadElementType.Edge, eId))
                    {
                        continue;
                    }
                    eIds.Add(eId);
                }
            }
        }

        /// <summary>
        /// 頂点IDからループIDを取得する
        /// </summary>
        /// <param name="vId"></param>
        /// <returns></returns>
        private static uint GetVertexLoopId(Cad2D cad, uint vId)
        {
            uint loopId = 0;

            for (VertexEdgeItr vItr = cad.GetVertexEdgeItr(vId); !vItr.IsEnd(); vItr.Next())
            {
                uint lId = vItr.GetLoopId();
                if (lId != 0)
                {
                    loopId = lId;
                    break;
                }
            }

            return loopId;
        }

        /// <summary>
        /// 辺IDからループIDを取得する
        /// </summary>
        /// <param name="eId">辺ID</param>
        /// <returns>ループID</returns>
        private static uint GetEdgeLoopId(Cad2D cad, uint eId)
        {
            uint loopId = 0;
            uint lLId;
            uint rLId;
            cad.GetEdgeLoopId(eId, out lLId, out rLId);
            // ループから見て孤立した辺は除外する
            if (lLId != 0 && IsStandAloneEdgeInsideLoop(cad, eId, lLId))
            {
                lLId = 0;
            }
            if (rLId != 0 && IsStandAloneEdgeInsideLoop(cad, eId, rLId))
            {
                rLId = 0;
            }

            //System.Diagnostics.Debug.Assert(lLId != 0 || rLId != 0);
            if (lLId != 0)
            {
                loopId = lLId;
            }
            else if (rLId != 0)
            {
                loopId = rLId;
            }
            else
            {
                // 不完全な辺の作成も許可するようにしたので、Assertを外す
                System.Diagnostics.Debug.WriteLine("[Warn]getLoopIdOfEdge: lLId == 0 && rLId == 0");
                //System.Diagnostics.Debug.Assert(false);
                loopId = 0;
            }

            return loopId;
        }

        /// <summary>
        /// 辺がループ内部の孤立した辺か？
        ///   GetIdLoop_Edgeがループ内部の孤立した辺の場合も含んでいるのでそれを除外したい場合に使用する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="eId"></param>
        /// <param name="loopId"></param>
        /// <returns></returns>
        private static bool IsStandAloneEdgeInsideLoop(Cad2D cad, uint eId, uint loopId)
        {
            bool standAlone = false;
            if (eId == 0 || loopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return standAlone;
            }

            IList<uint> workVIds = null;
            IList<uint> workEIds = null;
            GetLoopVertexsAndEdges(cad, loopId, out workVIds, out workEIds);
            if (workEIds.IndexOf(eId) == -1)
            {
                standAlone = true;
            }
            return standAlone;
        }

        /// <summary>
        /// 辺が２つのループに共有されているか?
        /// </summary>
        /// <param name="eId"></param>
        /// <returns></returns>
        private static bool IsEdgeSharedByLoops(Cad2D cad, uint eId)
        {
            uint lLId;
            uint rLId;
            cad.GetEdgeLoopId(eId, out lLId, out rLId);
            /*これは孤立した辺でもよい --> 包含関係にある場合、親ループから見て子ループの辺は孤立した辺なので
            // ループから見て孤立した辺は除外する
            if (lLId != 0 && IsStandAloneEdgeInsideLoop(cad, eId, lLId))
            {
                lLId = 0;
            }
            if (rLId != 0 && IsStandAloneEdgeInsideLoop(cad, eId, rLId))
            {
                rLId = 0;
            }
            */
            return (lLId != 0 && rLId != 0);
        }

        /// <summary>
        /// 隣のループのIDを取得する
        /// </summary>
        /// <param name="tagtEdgeId"></param>
        /// <param name="tagtLoopId"></param>
        /// <returns></returns>
        private static uint GetNextDoorLoopId(Cad2D cad, uint tagtEdgeId, uint tagtLoopId)
        {
            uint nextDoorLoopId = 0;
            uint lLId;
            uint rLId;
            cad.GetEdgeLoopId(tagtEdgeId, out lLId, out rLId);
            /*これは孤立した辺でもよい --> 包含関係にある場合、親ループから見て子ループの辺は孤立した辺なので
            // ループから見て孤立した辺は除外する
            if (lLId != 0 && IsStandAloneEdgeInsideLoop(cad, tagtEdgeId, lLId))
            {
                lLId = 0;
            }
            if (rLId != 0 && IsStandAloneEdgeInsideLoop(cad, tagtEdgeId, rLId))
            {
                rLId = 0;
            }
            */

            if (lLId != 0 && lLId != tagtLoopId)
            {
                nextDoorLoopId = lLId;
            }
            else if (rLId != 0 && rLId != tagtLoopId)
            {
                nextDoorLoopId = rLId;
            }
            return nextDoorLoopId;
        }

        /// <summary>
        /// 指定された座標がループの頂点なら頂点ID、ループIDのペアをリストで返却する
        /// </summary>
        /// <param name="cad">Cadオブジェクト</param>
        /// <param name="chkPt">チェックする点</param>
        /// <param name="loopIds">これまでに追加されたループリスト</param>
        private static void GetVertexIdBelongToLoopByCoord(
            Cad2D cad, OpenTK.Vector2d chkPt, IList<uint> loopIds, 
            out uint outVertexId, out IList<uint> outLoopIds)
        {
            outLoopIds = new List<uint>();
            outVertexId = 0;
            foreach (uint workLoopId in loopIds)
            {
                // ループの頂点、辺のIDのリストを取得する
                IList<uint> vIds = null;
                IList<uint> eIds = null;
                GetLoopVertexsAndEdges(cad, workLoopId, out vIds, out eIds);

                foreach (uint vId in vIds)
                {
                    OpenTK.Vector2d vertexPt = cad.GetVertexCoord(vId);
                    if (OpenTK.Vector2d.Distance(vertexPt, chkPt) < Constants.PrecisionLowerLimit)
                    {
                        if (outVertexId == 0)
                        {
                            outVertexId = vId;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(outVertexId == vId);
                        }
                        outLoopIds.Add(workLoopId);
                    }
                }
            }
        }

        /// <summary>
        /// 頂点IDから辺IDのリストを取得する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="vId"></param>
        /// <returns></returns>
        private static IList<uint> GetEdgeIdsByVertexId(Cad2D cad, uint vId)
        {
            IList<uint> eIds = new List<uint>();
            if (vId != 0)
            {
                for (VertexEdgeItr vItr = cad.GetVertexEdgeItr(vId); !vItr.IsEnd(); vItr.Next())
                {
                    bool ret;
                    uint eId;
                    bool isSameDir;
                    eId = 0;
                    ret = vItr.GetBehindEdgeId(out eId, out isSameDir);
                    if (ret && eId != 0)
                    {
                        if (eIds.IndexOf(eId) == -1)
                        {
                            eIds.Add(eId);
                        }
                    }
                    eId = 0;
                    ret = vItr.GetAheadEdgeId(out eId, out isSameDir);
                    if (ret && eId != 0)
                    {
                        if (eIds.IndexOf(eId) == -1)
                        {
                            eIds.Add(eId);
                        }
                    }
                }
            }
            return eIds;
        }

        /// <summary>
        /// ２つの頂点IDからなる辺IDを取得する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="vId1"></param>
        /// <param name="vId2"></param>
        /// <returns></returns>
        private static uint GetEdgeIdByVertexIds(Cad2D cad, uint vId1, uint vId2)
        {
            uint retEId = 0;

            System.Diagnostics.Debug.Assert(vId1 != vId2);
            if (vId1 == vId2)
            {
                return retEId;
            }

            IList<uint> eIds1 = GetEdgeIdsByVertexId(cad, vId1);
            IList<uint> eIds2 = GetEdgeIdsByVertexId(cad, vId2);

            foreach (uint workEId in eIds1)
            {
                if (eIds2.IndexOf(workEId) >= 0)
                {
                    retEId = workEId;
                    break;
                }
            }

            // check
            if (retEId != 0)
            {
                uint workVId1 = 0;
                uint workVId2 = 0;
                GetEdgeVertexId(cad, retEId, out workVId1, out workVId2);
                System.Diagnostics.Debug.Assert(
                    (workVId1 == vId1 && workVId2 == vId2) || (workVId1 == vId2 && workVId2 == vId1));
            }


            return retEId;
        }

        /// <summary>
        /// 辺IDから頂点ID２つを取得する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="eId"></param>
        /// <param name="vId1"></param>
        /// <param name="vId2"></param>
        internal static void GetEdgeVertexId(Cad2D cad, uint eId, out uint vId1, out uint vId2)
        {
            cad.GetEdgeVertexId(eId, out vId1, out vId2);
        }

        /// <summary>
        /// 頂点が辺に所有されているか？
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="vId"></param>
        /// <returns></returns>
        private static bool IsVertexOwnedByEdges(Cad2D cad, uint vId)
        {
            bool owned = false;
            if (vId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return owned;
            }

            VertexEdgeItr vItr = cad.GetVertexEdgeItr(vId);
            int edgeCnt = 0;
            for (vItr.Begin(); !vItr.IsEnd(); vItr.Next())
            {
                uint eId;
                bool isSameDir;
                vItr.GetAheadEdgeId(out eId, out isSameDir);
                if (eId == 0)
                {
                    vItr.GetBehindEdgeId(out eId, out isSameDir);
                }
                if (eId != 0)
                {
                    edgeCnt++;
                }
            }
            System.Diagnostics.Debug.Assert(edgeCnt == vItr.GetEdgeCount());
            if (edgeCnt > 0)
            {
                owned = true;
            }

            return owned;
        }

        /// <summary>
        /// 指定された点を包含するループのIDを取得する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="chkPt">チェックする点</param>
        /// <param name="loopIds">これまでに追加されたループのリスト</param>
        /// <returns></returns>
        private static uint GetLoopIdIncludingPoint(Cad2D cad, OpenTK.Vector2d chkPt, IList<uint> loopIds)
        {
            uint hitLId = 0;
            foreach (uint lId in loopIds)
            {
                bool ret = cad.CheckIsPointInsideLoop(lId, chkPt);
                if (ret)
                {
                    hitLId = lId;
                    break;
                }
            }
            return hitLId;
        }

        /// <summary>
        /// 指定ポイントが辺上にあれば辺IDを返却する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="chkPt">チェックするポイント</param>
        /// <param name="loopList">これまでに追加されたループのリスト</param>
        /// <returns>辺ID</returns>
        private static uint GetEdgeIdIncludingPoint(
            Cad2D cad, OpenTK.Vector2d chkPt, IList<uint> loopIds)
        {
            uint hitEId = 0;
            foreach (uint lId in loopIds)
            {
                IList<uint> vIds = null;
                IList<uint> eIds = null;
                GetLoopVertexsAndEdges(cad, lId, out vIds, out eIds);

                foreach (uint eId in eIds)
                {
                    bool isOnEdge = IsPointOnEdge(cad, eId, chkPt);
                    if (isOnEdge)
                    {
                        hitEId = eId;
                        break;
                    }
                }
            }
            return hitEId;
        }

        /// <summary>
        /// 指定ポイントが辺上にある？
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="eId">辺ID</param>
        /// <param name="chkPt">チェックするポイント</param>
        /// <returns></returns>
        private static bool IsPointOnEdge(Cad2D cad, uint eId, OpenTK.Vector2d chkPt)
        {
            bool isOnEdge = false;
            uint vId1 = 0;
            uint vId2 = 0;
            GetEdgeVertexId(cad, eId, out vId1, out vId2);
            OpenTK.Vector2d ppV1 = cad.GetVertexCoord(vId1);
            OpenTK.Vector2d ppV2 = cad.GetVertexCoord(vId2);
            isOnEdge = IsPointOnEdge(ppV1, ppV2, chkPt);
            return isOnEdge;
        }

        /// <summary>
        /// 指定ポイントが辺上にある？
        /// </summary>
        /// <param name="ppV1">辺の始点の座標</param>
        /// <param name="ppV2">辺の終点の座標</param>
        /// <param name="chkPt">チェックするポイント</param>
        /// <returns></returns>
        private static bool IsPointOnEdge(OpenTK.Vector2d ppV1, OpenTK.Vector2d ppV2, OpenTK.Vector2d chkPt)
        {
            bool isOnEdge = false;
            // 辺上だったらその点がつくる２つの辺の長さの和が元の辺の長さのはず
            double le = OpenTK.Vector2d.Distance(ppV1, ppV2);
            double le1 = OpenTK.Vector2d.Distance(ppV1, chkPt);
            double le2 = OpenTK.Vector2d.Distance(ppV2, chkPt);
            if (Math.Abs(le - (le1 + le2)) < Constants.PrecisionLowerLimit)
            {
                isOnEdge = true;
            }
            return isOnEdge;
        }

        /// <summary>
        /// 辺が辺を含む？
        /// </summary>
        /// <param name="cad">Cadオブジェクト</param>
        /// <param name="ppV1">辺の始点</param>
        /// <param name="ppV2">辺の終点</param>
        /// <param name="chkEId">含むかチェックする辺のID</param>
        /// <returns></returns>
        private static bool IsEdgeIncludingEdge(
            Cad2D cad, OpenTK.Vector2d ppV1, OpenTK.Vector2d ppV2, uint chkEId)
        {
            bool isIncluding = false;

            // チェックする辺の始点と終点を取得
            uint chkVId1 = 0;
            uint chkVId2 = 0;
            GetEdgeVertexId(cad, chkEId, out chkVId1, out chkVId2);
            OpenTK.Vector2d chkPPV1 = cad.GetVertexCoord(chkVId1);
            OpenTK.Vector2d chkPPV2 = cad.GetVertexCoord(chkVId2);

            // チェックする辺の始点と終点がどちらも元の辺の辺上なら、チェックする辺は元の辺に含まれる
            bool chk1 = IsPointOnEdge(ppV1, ppV2, chkPPV1);
            bool chk2 = IsPointOnEdge(ppV1, ppV2, chkPPV2);
            if (chk1 && chk2)
            {
                isIncluding = true;
            }
            return isIncluding;
        }

        /// <summary>
        /// これから作成する辺が他の辺を含んでいるかチェックし、含んでいればその辺IDを返却する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="newEdgeVId">これから作成する辺の始点</param>
        /// <param name="chkPt">これから作成する辺の終点ポイント</param>
        /// <param name="loopIds">これまでに追加されたループのリスト</param>
        /// <returns>辺IDのリスト</returns>
        private static IList<uint> GetEdgeIdsIncludedByNewEdge(
            Cad2D cad, OpenTK.Vector2d chkPPV1, OpenTK.Vector2d chkPPV2, IList<uint> loopIds)
        {
            IList<uint> hitEIds = new List<uint>();

            foreach (uint lId in loopIds)
            {
                IList<uint> vIds = null;
                IList<uint> eIds = null;
                GetLoopVertexsAndEdges(cad, lId, out vIds, out eIds);

                foreach (uint eId in eIds)
                {
                    bool isIncluding = IsEdgeIncludingEdge(cad, chkPPV1, chkPPV2, eId);
                    if (isIncluding)
                    {
                        hitEIds.Add(eId);
                    }
                }
            }
            return hitEIds;
        }

        /// <summary>
        /// これから作成する辺が既存の辺を含んでいるかチェックし、含んでいれば（複数の辺の可能性あり）開始頂点ID、終了頂点IDを返却する
        /// </summary>
        /// <param name="cad"></param>
        /// <param name="ppV1">これから作成する辺の始点</param>
        /// <param name="ppV2">これから作成する辺の終点</param>
        /// <param name="loopIds">これまでに追加したループのリスト</param>
        /// <param name="minDistanceVId">開始頂点ID</param>
        /// <param name="maxDistanceVId">終了頂点ID</param>
        private static void GetIncludedEdgesSartEndVId(
            Cad2D cad, OpenTK.Vector2d ppV1, OpenTK.Vector2d ppV2, IList<uint> loopIds,
            out uint minDistanceVId, out uint maxDistanceVId)
        {
            minDistanceVId = 0;
            maxDistanceVId = 0;

            IList<uint> includedEIds = GetEdgeIdsIncludedByNewEdge(cad, ppV1, ppV2, loopIds);
            if (includedEIds.Count != 0)
            {
                // 下記のみを考慮
                // indexPP -1                                     indexPP
                // +----------------+--------既存の辺------+----------+
                //              minDistanceVId        maxDistanceVId
                // 既存の辺がさらに歯抜けになる場合は、....エラーとして返せるか？
                double minDistance = double.MaxValue;
                double maxDistance = double.MinValue;
                foreach (uint includedEId in includedEIds)
                {
                    // 含まれる辺の頂点IDを取得
                    uint vId1OfIncludedEdge = 0;
                    uint vId2OfIncludedEdge = 0;
                    GetEdgeVertexId(cad, includedEId, out vId1OfIncludedEdge, out vId2OfIncludedEdge);
                    OpenTK.Vector2d ppV1OfIncludedEdge = cad.GetVertexCoord(vId1OfIncludedEdge);
                    OpenTK.Vector2d ppV2OfIncludedEdge = cad.GetVertexCoord(vId2OfIncludedEdge);
                    // これから作成する辺の始点から頂点までの距離を取得
                    double d1 = OpenTK.Vector2d.Distance(ppV1, ppV1OfIncludedEdge);
                    double d2 = OpenTK.Vector2d.Distance(ppV1, ppV2OfIncludedEdge);
                    if (d1 > maxDistance)
                    {
                        maxDistance = d1;
                        maxDistanceVId = vId1OfIncludedEdge;
                    }
                    if (d1 < minDistance)
                    {
                        minDistance = d1;
                        minDistanceVId = vId1OfIncludedEdge;
                    }
                    if (d2 > maxDistance)
                    {
                        maxDistance = d2;
                        maxDistanceVId = vId2OfIncludedEdge;
                    }
                    if (d2 < minDistance)
                    {
                        minDistance = d2;
                        minDistanceVId = vId2OfIncludedEdge;
                    }
                }
            }
        }

        /// <summary>
        /// ループを作成する
        /// </summary>
        /// <param name="cad">Cadオブジェクト</param>
        /// <param name="pps">追加するループの多角形の頂点リスト(ループを閉じる終点は含まない)</param>
        /// <param name="loopIds">これまでに追加されたループのリスト</param>
        /// <returns></returns>
        private static uint MakeLoop(
            Cad2D cad, IList<OpenTK.Vector2d> pps, IList<uint> loopIds, bool showErrorFlg)
        {
            // 多角形でループを作成するのを止める
            //uint id_l = out_cad.AddPolygon(pps, baseLoopId).id_l_add;

            uint lId = 0;

            IList<OpenTK.Vector2d> addPts = new List<OpenTK.Vector2d>();
            IList<uint> addVertexIds = new List<uint>();
            IList<uint> addEdgeIds = new List<uint>();
            IList<uint> addLoopIds = new List<uint>();
            for (int indexPP = 0; indexPP < pps.Count; indexPP++)
            {
                int prevAddLoopCnt = addLoopIds.Count;
                /////////////////
                OpenTK.Vector2d pp = pps[indexPP];
                int workIndexPP = indexPP;
                bool executed;
                executed = DoMakeDisconAreaCore(cad, pp, workIndexPP, loopIds,
                    addPts, addVertexIds, addEdgeIds, addLoopIds, showErrorFlg);
                if (addLoopIds.Count != prevAddLoopCnt)
                {
                    //
                    //addPts.Clear();
                    //addVertexIds.Clear();
                    //addEdgeIds.Clear();
                }
                // 最後の辺の追加
                if (indexPP == pps.Count - 1 && addVertexIds.Count > 0)
                {
                    prevAddLoopCnt = addLoopIds.Count;
                    // 先頭の頂点と接続する
                    pp = pps[0];
                    workIndexPP = pps.Count;
                    executed = DoMakeDisconAreaCore(cad, pp, workIndexPP, loopIds,
                        addPts, addVertexIds, addEdgeIds, addLoopIds, showErrorFlg);
                    if (addLoopIds.Count != prevAddLoopCnt)
                    {
                        //
                        //addPts.Clear();
                        //addVertexIds.Clear();
                        //addEdgeIds.Clear();
                    }
                }
            }
            if (addLoopIds.Count > 0)
            {
                lId = addLoopIds[0];
                System.Diagnostics.Debug.Assert(addLoopIds.Count == 1);
            }

            return lId;
        }

    }
}
