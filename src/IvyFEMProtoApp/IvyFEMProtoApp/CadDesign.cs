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
    class CadDesign : CadDesignBase
    {
        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 変更通知デリゲート
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="prevCadMode"></param>
        public delegate void ChangeDeleagte(object sender, CadModeType prevCadMode);

        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////
        /// 編集中対象の描画色(ラバーバンド描画に使用)
        ///     Note: ヒットテストのときの色もColor.Yellow(DelFEM側で設定)だが Ver1.2と同じにする
        /// </summary>
        private static readonly Color EditingColor = Color.Yellow;
        /// <summary>
        /// 図形作成中に生成した辺の描画色
        /// </summary>
        private static Color TmpEdgeColor = Color.Black;
        /// <summary>
        /// ポート境界の色
        /// </summary>
        private static readonly Color PortColor = Color.Cyan;
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
        public event ChangeDeleagte Change = null;

        /// <summary>
        /// Cadパネル
        /// </summary>
        private OpenTK.GLControl glControl = null;
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
        private DrawerArray DrawerAry = new DrawerArray();
        /// <summary>
        /// マウス移動位置
        /// </summary>
        private Point MouseMovePt = new Point();

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
        /// 編集図面Cad
        /// </summary>
        public CadObject2DMove Cad2D { get; private set; } = null;
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
        /// 作成中ののポート境界番号
        /// Control + LButtonUpで連続した境界を選択
        /// </summary>
        private int EditPortNo = 0;  // 番号にしたのは、この派生クラスCadLogicでCadBaseのフィールドのポインタを格納すると、MementoCommand実行後、別のものを指してしまうため

        /// <summary>
        /// 図面背景
        /// </summary>
        private BackgroundDrawer BackgroundDrawer = null;

        /// <summary>
        /// Cad図面が変更された?
        /// </summary>
        public bool IsDirty { get; private set; } = false;

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
        public CadDesign(OpenTK.GLControl glControl, double width, double height)
            : base()
        {
            this.glControl = glControl;

            BackgroundWidth = width;
            BackgroundHeight = height;
            //BackgroundHeight = BackgroundWidth * glControl.Height / (double)glControl.Width;
            //MinDistance = width / 100.0 * 1.25;
            MinDistance = width / 100.0 * 2.0;

            // 色の設定
            CadDesign.TmpEdgeColor = this.glControl.ForeColor;

            // Cadオブジェクトを生成
            Cad2D = new CadObject2DMove();

            // 図面背景
            BackgroundDrawer = new BackgroundDrawer(BackgroundWidth, BackgroundHeight);

            // 領域を決定する
            SetupRegionSize();

            // 初期化処理
            init();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected new void init()
        {
            base.init();

            Cad2D.Clear();
            CadMode = CadModeType.None;
            EditPortNo = 0;

            refreshDrawerAry();

            IsDirty = false;
        }

        /// <summary>
        /// データの初期化
        /// </summary>
        public void InitData()
        {
            init();
        }

        /// <summary>
        /// 描画オブジェクトのリストを更新する
        /// </summary>
        private void refreshDrawerAry()
        {
            DrawerAry.Clear();
            // 背景を追加する
            DrawerAry.Add(BackgroundDrawer);
            // Cad図面
            CadObject2DDrawer drawer = new CadObject2DDrawer(Cad2D);
            uint lineWidth = (uint)(CadDesign.LineWidth * glControl.Width / (double)400);
            drawer.LineWidth = lineWidth;
            DrawerAry.Add(drawer);
        }

        /// <summary>
        /// 領域を決定する
        /// </summary>
        public void SetupRegionSize(double offsetX = 0, double offsetY = 0, double scale = 1.4)
        {
            // 描画オブジェクトを更新する
            refreshDrawerAry();
            // 描画オブジェクトのバウンディングボックスを使ってカメラの変換行列を初期化する
            Camera.Fit(DrawerAry.GetBoundingBox(Camera.RotMatrix33()));
            // カメラのスケール調整
            // DrawerArrayのInitTransを実行すると、物体のバウンディングボックス + マージン分(×1.5)がとられる。
            // マージンを表示上をなくすためスケールを拡大して調整する
            Camera.Scale = scale;
            // カメラをパンニングさせ位置を調整
            Camera.MousePan(0.0, 0.0, offsetX, offsetY);

            int w = glControl.Width;
            int h = glControl.Height;
            resizeScene(w, h);
        }

        /// <summary>
        /// SimpleOpenGlControlのシーンの描画
        /// </summary>
        private void renderScene()
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
                DrawerAry.Draw();
                drawEditPtsTemporayLine();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBoxShowError(exception.Message, "");
            }

            glControl.SwapBuffers();
        }

        /// <summary>
        /// SimpleOpenGlControlのリサイズ処理
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        private void resizeScene(int w, int h)
        {
            Camera.WindowAspect = (double)w / h;
            GL.Viewport(0, 0, w, h);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            OpenGLUtils.SetProjectionTransform(Camera);

            // 線のサイズを画面に合わせて変更させる為、サイズ変更時にDrawerArrayを更新する
            refreshDrawerAry();
        }

        /// <summary>
        /// CadPanelのリサイズイベントハンドラ
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelResize()
        {
            //System.Diagnostics.Debug.WriteLine("CadPanelResize");
            int scrollPosX = glControl.AutoScrollPosition.X;
            int scrollPosY = glControl.AutoScrollPosition.Y;
            int w = glControl.Width;
            int h = glControl.Height;
            resizeScene(w, h);
        }

        /// <summary>
        /// Cadパネル描画イベント処理
        /// </summary>
        /// <param name="g"></param>
        public void CadPanelPaint()
        {
            renderScene();
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
                executed = mouseLeftButtonDown(e);
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                executed = mouseRightButtonDown(e);
            }

            if (executed)
            {
                // 描画オブジェクトアレイを更新する
                refreshDrawerAry();
                // Cadパネルの再描画
                glControl.Invalidate();
            }
        }

        /// <summary>
        /// マウス左ボタンが押された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool mouseLeftButtonDown(System.Windows.Forms.MouseEventArgs e)
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
                bool hit = hitTest(pt, out partElemType, out partId);
                if (hit)
                {
                    MovElemType = partElemType;
                    MovObjId = partId;
                }
                executed = true; // 常に実行される
            }
            return executed;
        }

        /// <summary>
        /// マウス左ボタンが押された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool mouseRightButtonDown(System.Windows.Forms.MouseEventArgs e)
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

            /*
            if (KeyModifiers.HasFlag(Keys.Control))
            {
                MouseRotation(prevpt, pt);
            }
            else if (KeyModifiers.HasFlag(Keys.Shift))
            {
                MousePan(prevpt, pt);
            }
            */
            if (CadMode == CadModeType.Move)
            {
                if (DragFlg)
                {
                    // Cadオブジェクトの移動
                    executed = doMoveObject(true, ref StartPt, EndPt);
                }
                else
                {
                    CadElementType partElemType;
                    uint partId;
                    bool hit = hitTest(pt, out partElemType, out partId);
                    executed = true; // 常に実行される
                }

            }
            else if (CadMode != CadModeType.None)
            {
                CadElementType partElemType;
                uint partId;
                bool hit = hitTest(pt, out partElemType, out partId);
                executed = true; // 常に実行される
            }

            if (executed)
            {
                // 描画オブジェクトアレイを更新する
                refreshDrawerAry();
                // Cadパネルの再描画
                glControl.Invalidate();
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
                executed = mouseLeftButtonUp(e);
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                executed = mouseRightButtonUp(e);
            }

            if (executed)
            {
                // 描画オブジェクトアレイを更新する
                refreshDrawerAry();
                // Cadパネルの再描画
                glControl.Invalidate();
            }
        }

        /// <summary>
        /// マウス左ボタンが離された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool mouseLeftButtonUp(System.Windows.Forms.MouseEventArgs e)
        {
            bool executed = false;
            Point pt = e.Location;
            EndPt = e.Location;
            DragFlg = false;
            /*
            Point minPt = new Point();
            Point maxPt = new Point();
            if (StartPt.X <= EndPt.X)
            {
                minPt.X = StartPt.X;
                maxPt.X = EndPt.X;
            }
            else
            {
                minPt.X = EndPt.X;
                maxPt.X = StartPt.X;
            }
            if (StartPt.Y <= EndPt.Y)
            {
                minPt.Y = StartPt.Y;
                maxPt.Y = EndPt.Y;
            }
            else
            {
                minPt.Y = EndPt.Y;
                maxPt.Y = StartPt.Y;
            }
             */

            // Cadオブジェクトの移動
            executed = doMoveObject(false, ref StartPt, EndPt);
            if (!executed)
            {
                // 領域作成
                executed = doMakeDisconArea(pt);
            }
            if (!executed)
            {
                // 領域削除
                executed = doEraseDisconArea(pt);
            }
            if (!executed)
            {
                // ポート追加
                executed = doSelectPort(pt);
            }
            if (!executed)
            {
                // ポート削除
                executed = doErasePort(pt);
            }
            if (!executed)
            {
                // 辺削除
                executed = doEraseCadEdge(pt);
            }

            return executed;
        }

        /// <summary>
        /// マウス右ボタンが離された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool mouseRightButtonUp(System.Windows.Forms.MouseEventArgs e)
        {
            bool executed = false;

            return executed;
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
                ScreenPointToCoord(pt, Camera, out ox, out oy);
                coord = new OpenTK.Vector2d(ox, oy);
            }
            return coord;
        }

        /// <summary>
        /// マウスで指定したウインドウ座標をOpenGL座標に変換
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private static void ScreenPointToCoord(Point pt, Camera camera, out double ox, out double oy)
        {
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int winW = viewport[2];
            int winH = viewport[3];

            //モデルビュー行列、射影行列を格納する配列
            double[] modelviewMatrix = new double[16];
            double[] projectionMatrix = new double[16];

            int glY;
            double depth = 0.887; //デプス値(何でもよい)
            GL.GetDouble(GetPName.ProjectionMatrix, projectionMatrix);
            GL.GetDouble(GetPName.ModelviewMatrix, modelviewMatrix);
            glY = winH - pt.Y;

            double oz;
            OpenGLUtils.GluUnProject((double)pt.X, (double)glY, depth,
                modelviewMatrix, projectionMatrix, viewport,
                out ox, out oy, out oz);
            //System.Diagnostics.Debug.WriteLine("{0},{1},{2}", ox, oy, oz);
        }

        /// <summary>
        /// マウスで指定したウインドウ座標を正規化デバイス座標に変換
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private static void ScreenPointToNormalizedCoord(Point pt, Camera camera, out double ox, out double oy)
        {
            ScreenPointToCoord(pt, camera, out ox, out oy);
            double invScale = 1.0 / camera.Scale;
            ox *= invScale;
            oy *= invScale;
            //System.Diagnostics.Debug.WriteLine("NormalizedCoord {0},{1}", ox, oy);
        }

        /// <summary>
        /// OpenGL座標をウインドウ座標に変換
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private static Point CoordToScreenPoint(Camera camera, double x, double y)
        {
            Point pt = new Point();
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int winW = viewport[2];
            int winH = viewport[3];

            //モデルビュー行列、射影行列を格納する配列
            double[] modelviewMatrix = new double[16];
            double[] projectionMatrix = new double[16];

            double depth = 0.887; //デプス値(何でもよい)
            GL.GetDouble(GetPName.ProjectionMatrix, projectionMatrix);
            GL.GetDouble(GetPName.ModelviewMatrix, modelviewMatrix);

            double[] windowCoord = new double[3];
            OpenGLUtils.GluProject(x, y, depth,
                modelviewMatrix, projectionMatrix, viewport, windowCoord);
            double ox = windowCoord[0];
            double oy = windowCoord[1];
            double oz = windowCoord[2];
            //System.Diagnostics.Debug.WriteLine("{0},{1},{2}", ox, oy, oz);
            pt.X = (int)ox;
            pt.Y = (int)(winH - oy);
            return pt;
        }

        /// <summary>
        /// 図形作成途中の一時的な線分を描画
        /// </summary>
        private void drawEditPtsTemporayLine()
        {
            Point pt = MouseMovePt;
            double x;
            double y;
            ScreenPointToCoord(pt, Camera, out x, out y);
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
        /// Cadオブジェクトの移動
        /// </summary>
        /// <param name="elemType"></param>
        /// <param name="objId"></param>
        /// <param name="screenPt"></param>
        private static bool moveObject(
            CadObject2DMove EditCad2D,
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
            CadDesign.ScreenPointToCoord(startPt, Camera, out movBeginX, out movBeginY);
            CadDesign.ScreenPointToCoord(endPt, Camera, out movEndX, out movEndY);

            if (elemType == CadElementType.Vertex)
            {
                uint vId = objId;
                bool ret = EditCad2D.MoveVertex(vId, new OpenTK.Vector2d(movEndX, movEndY));
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
                bool ret = EditCad2D.MoveEdge(eId, new OpenTK.Vector2d(movEndX - movBeginX, movEndY - movBeginY));
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
                bool ret = EditCad2D.MoveLoop(lId, new OpenTK.Vector2d(movEndX - movBeginX, movEndY - movBeginY));
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
        private bool doMoveObject(bool isDragging, ref Point startPt, Point endPt)
        {
            bool executed = false;
            if (CadMode == CadModeType.Move)
            {
                // Cadオブジェクトの移動
                executed = moveObject(Cad2D,
                    Camera,
                    MovElemType,
                    MovObjId,
                    startPt,
                    endPt);
                startPt = endPt;
                if (executed)
                {
                    if (isDragging)
                    {
                        // Undo対象にはしない
                    }
                    else
                    {
                        if (Change != null)
                        {
                            Change(this, CadMode);
                        }
                    }
                }
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
        private bool doMakeDisconArea(Point pt)
        {
            bool executed = false;
            if (CadMode != CadModeType.Polygon)
            {
                return executed;
            }

            // スクリーンの位置をデバイス座標に変換
            double x;
            double y;
            CadDesign.ScreenPointToCoord(pt, Camera, out x, out y);

            double th = MinDistance; //!!!!!

            OpenTK.Vector2d pp = new OpenTK.Vector2d(x, y);

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
                /*方眼紙の場合の処理、今回はいらない
                //BUGFIX
                //ヒットテストは修正された位置で行う必要がある
                //hitTest(pt, out partElemType, out partId);
                Point ptModified = CoordToScreenPoint(Camera, pp.X, pp.Y);
                hitTest(ptModified, out partElemType, out partId);
                */
                hitTest(pt, out partElemType, out partId);
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
                    Cad2D.GetEdgeVertexId(out sVid, out eVId, parentEdgeId);
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
                        uint hitEId = getEdgeIdIncludingPoint(Cad2D, pp, LoopIds);
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
                            executed = addVertexAndEdgeAtStandAloneEdge(
                                Cad2D, pp, hitEId, indexPP, LoopIds,
                                ref EditPts, ref EditVertexIds, ref EditEdgeIds, ref addLoopIds, true);
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
                            CadDesign.getVertexIdBelongToLoopByCoord(
                                Cad2D, pp, LoopIds, out hitVId, out hitLoopIds);
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
                                executed = addEdgeConnectedToStandAloneVertex(
                                    Cad2D, pp, hitVId, indexPP, LoopIds,
                                    ref EditPts, ref EditVertexIds, ref EditEdgeIds, ref addLoopIds, true);
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
                executed = doMakeDisconAreaCore(Cad2D, pp, indexPP, LoopIds,
                    ref EditPts, ref EditVertexIds, ref EditEdgeIds, ref addLoopIds, true);
            }
            if (addLoopIds.Count > 0)
            {
                //MessageBox.Show("領域の分割確定");
                foreach (uint lId in addLoopIds)
                {
                    //// ループの色を指定（指定しなければ(0.9,0.8,0.8)になる
                    //MediaInfo media = Medias[SelectedMediaIndex];
                    //Color backColor = media.BackColor;
                    //SetupColorOfCadObjectsForOneLoop(EditCad2D, lId, backColor);
                    // ループ情報の追加
                    LoopIds.Add(lId);

                    // ループの内側にあるループを子ループに設定する
                    reconstructLoopsInsideLoopAsChild(
                        Cad2D, lId, ref LoopIds, ref EdgeCollectionList);

                    //ループの色をすべて再設定する
                    SetupColorOfCadObjectsForAllLoops(Cad2D, LoopIds);

                    executed = true;
                }
                //
                EditPts.Clear();
                EditVertexIds.Clear();
                EditEdgeIds.Clear();
            }

            if (executed)
            {
                if (Change != null)
                {
                    Change(this, CadMode);
                }

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
        /// <param name="EditCad2D"></param>
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
        public static bool addEdgeConnectedToStandAloneVertex(
            CadObject2D EditCad2D, OpenTK.Vector2d pp, uint hitVId, int indexPP, IList<uint> LoopIds,
            ref IList<OpenTK.Vector2d> EditPts, ref IList<uint> EditVertexIds, ref IList<uint> EditEdgeIds,
            ref IList<uint> addLoopIds, bool showErrorFlg)
        {
            bool executed = false;
            uint parentLoopId = 0;
            int addedCnt = 0;
            // 頂点は作成せず、辺だけ作成する
            // 辺の作成に関して、既存の辺が存在するかチェック
            uint existEId = 0;
            {
                // １つ前の点に関して、前に追加したループと共有している頂点かチェックする
                IList<uint> prevHitLoopIdList = null;
                uint prevHitVId = 0;
                if (indexPP >= 1)
                {
                    //CVector2D prevPt = pps[indexPP - 1];
                    OpenTK.Vector2d prevPt = EditPts[EditPts.Count - 1];
                    CadDesign.getVertexIdBelongToLoopByCoord(
                        EditCad2D, prevPt, LoopIds, out prevHitVId, out prevHitLoopIdList);
                    if (prevHitVId != 0)
                    {
                        existEId = getEdgeIdOfVertexIds(EditCad2D, hitVId, prevHitVId);
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
                bool ret = CadDesign.addEdgeByLastEditPts(
                    EditCad2D, parentLoopId,
                    ref EditVertexIds, ref EditEdgeIds, out eId, out loopIdAddByConnectVertex, 
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
        /// <param name="EditCad2D"></param>
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
        public static bool addVertexAndEdgeAtStandAloneEdge(
            CadObject2D EditCad2D, OpenTK.Vector2d pp, uint hitEId, int indexPP, IList<uint> LoopIds,
            ref IList<OpenTK.Vector2d> EditPts, ref IList<uint> EditVertexIds, ref IList<uint> EditEdgeIds,
            ref IList<uint> addLoopIds, bool showErrorFlg)
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
            bool ret = CadDesign.addVertexAndEdge(EditCad2D, parentElemType, parentId, pp,
                ref EditPts, ref EditVertexIds, ref EditEdgeIds, out vertexIdAdd, out edgeIdAdd,
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
        /// <param name="EditCad2D"></param>
        /// <param name="pp"></param>
        /// <param name="indexPP"></param>
        /// <param name="LoopIds"></param>
        /// <param name="EditPts"></param>
        /// <param name="EditVertexIds"></param>
        /// <param name="EditEdgeIds"></param>
        /// <param name="addLoopIds"></param>
        /// <param name="showErrorFlg"></param>
        /// <returns></returns>
        public static bool doMakeDisconAreaCore(
            CadObject2D EditCad2D, OpenTK.Vector2d pp, int indexPP, IList<uint> LoopIds,
            ref IList<OpenTK.Vector2d> EditPts, ref IList<uint> EditVertexIds, ref IList<uint> EditEdgeIds,
            ref IList<uint> addLoopIds, bool showErrorFlg)
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
                IList<uint> hitLoopIdList = null;
                uint hitVId = 0;
                CadDesign.getVertexIdBelongToLoopByCoord(
                    EditCad2D, prevPP, LoopIds, out hitVId, out hitLoopIdList);

                uint parentLoopId = 0;

                // 先頭の頂点と接続する
                EditPts.Add(EditPts[0]);
                addedCnt++;
                EditVertexIds.Add(EditVertexIds[0]);
                parentLoopId = CadDesign.getLoopIdOfVertex(EditCad2D, EditVertexIds[0]);

                uint existEId = 0;
                {
                    // 先頭の点に関して、前に追加したループと共有している頂点かチェックする
                    IList<uint> nextHitLoopIdList = null;
                    uint nextHitVId = 0;
                    if (indexPP >= 1)
                    {
                        CadDesign.getVertexIdBelongToLoopByCoord(
                            EditCad2D, EditPts[0], LoopIds, out nextHitVId, out nextHitLoopIdList);
                        if (nextHitVId != 0)
                        {
                            existEId = getEdgeIdOfVertexIds(EditCad2D, hitVId, nextHitVId);
                        }
                    }
                }

                if (hitVId == 0 || existEId == 0)
                {
                    // １つ前の点と現在の点が既に辺を作っていなければ辺を追加
                    uint eId = 0;
                    uint loopIdAddByConnectVertex = 0;
                    bool ret = CadDesign.addEdgeByLastEditPts(
                        EditCad2D, parentLoopId,
                        ref EditVertexIds, ref EditEdgeIds, out eId, out loopIdAddByConnectVertex, showErrorFlg);
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
                    getIncludedEdgesStEndVId(EditCad2D, prevPP, pp, LoopIds, out minVId, out maxVId);
                    if (minVId != 0 && maxVId != 0)
                    {
                        addVertexAndEdgeFlg = false;

                        OpenTK.Vector2d minVertexPt = EditCad2D.GetVertex(minVId);
                        OpenTK.Vector2d maxVertexPt = EditCad2D.GetVertex(maxVId);
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
                            uint parentLoopId = getLoopIdIncludingPoint(EditCad2D, prevPP, LoopIds);

                            // １つ前の点と現在の点が既に辺を作っていなければ辺を追加
                            uint eId = 0;
                            uint loopIdAddByConnectVertex = 0;
                            bool ret = CadDesign.addEdgeByLastEditPts(
                                EditCad2D, parentLoopId,
                                ref EditVertexIds, ref EditEdgeIds, out eId, out loopIdAddByConnectVertex,
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
                    IList<uint> hitLoopIdList = null;
                    uint hitVId = 0;
                    CadDesign.getVertexIdBelongToLoopByCoord(
                        EditCad2D, pp, LoopIds, out hitVId, out hitLoopIdList);
                    if (hitVId > 0)
                    {
                        // 共有する頂点の場合
                        uint parentLoopId = 0;
                        if (hitLoopIdList.Count > 0)
                        {
                            parentLoopId = hitLoopIdList[0];
                        }

                        // 頂点は作成せず、辺だけ作成する
                        // 辺の作成に関して、既存の辺が存在するかチェック
                        uint existEId = 0;
                        {
                            // １つ前の点に関して、前に追加したループと共有している頂点かチェックする
                            IList<uint> prevHitLoopIdList = null;
                            uint prevHitVId = 0;
                            if (indexPP >= 1)
                            {
                                //CVector2D prevPt = pps[indexPP - 1];
                                OpenTK.Vector2d prevPt = EditPts[EditPts.Count - 1];
                                CadDesign.getVertexIdBelongToLoopByCoord(
                                    EditCad2D, prevPt, LoopIds, out prevHitVId, out prevHitLoopIdList);
                                if (prevHitVId != 0)
                                {
                                    existEId = getEdgeIdOfVertexIds(EditCad2D, hitVId, prevHitVId);
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
                            bool ret = CadDesign.addEdgeByLastEditPts(EditCad2D, parentLoopId, ref EditVertexIds, ref EditEdgeIds, out eId, out loopIdAddByConnectVertex, showErrorFlg);
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
                        uint parentEdgeId = getEdgeIdIncludingPoint(EditCad2D, pp, LoopIds);

                        if (parentEdgeId != 0)
                        {
                            parentId = parentEdgeId;
                            parentElemType = CadElementType.Edge;

                            if (EditVertexIds.Count > 0)
                            {
                                // 前に追加した頂点が同じ辺上にあるかをチェック
                                uint prevVId = EditVertexIds[EditVertexIds.Count - 1];
                                OpenTK.Vector2d prevPtV = EditCad2D.GetVertex(prevVId);
                                bool isOnEdgePrevPtV = isPointOnEdge(EditCad2D, parentEdgeId, prevPtV);
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
                            uint parentLoopId = getLoopIdIncludingPoint(EditCad2D, pp, LoopIds);
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
                            bool ret = CadDesign.addVertexAndEdge(
                                EditCad2D, parentElemType, parentId, pp,
                                ref EditPts, ref EditVertexIds, ref EditEdgeIds,
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
                            bool ret = CadDesign.addVertex(
                                EditCad2D, parentElemType, parentId, pp,
                                ref EditPts, ref EditVertexIds, ref EditEdgeIds,
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
        /// <param name="cad2d"></param>
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
        public static bool addVertexAndEdge(
            CadObject2D cad2d, CadElementType parentElemType, uint parentId, OpenTK.Vector2d pp,
            ref IList<OpenTK.Vector2d> editPts, ref IList<uint> editVertexIds, ref IList<uint> editEdgeIds,
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
            success = addVertex(cad2d, parentElemType, parentId, pp,
                ref editPts, ref editVertexIds, ref editEdgeIds,
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
                    lIdOfAddVertex = getLoopIdOfEdge(cad2d, eIdAddByAddVertex);

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
                    success = CadDesign.addEdgeByLastEditPts(cad2d, lIdOfAddVertex,
                        ref editVertexIds,
                        ref editEdgeIds,
                        out eId, out lIdAddByConnectVertex,
                        showErrorFlg);
                    if (!success)
                    {
                        // 失敗
                        // 頂点を削除する
                        cad2d.RemoveElement(CadElementType.Vertex, vId);
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
        /// <param name="cad2d"></param>
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
        public static bool addVertex(CadObject2D cad2d, CadElementType parentElemType, uint parentId,
            OpenTK.Vector2d pp,
            ref IList<OpenTK.Vector2d> editPts, ref IList<uint> editVertexIds, ref IList<uint> editEdgeIds,
            out uint vertexIdAdd, out uint edgeIdAddByAddVertex,
            bool showErrorFlg)
        {
            bool success = false;
            uint vId = 0; // 作成された頂点のID
            uint eIdAddByAddVertex = 0; // 頂点作成で辺が分割された場合に格納

            // 頂点を作成
            //vId = cad2d.AddVertex(parentElemType, parentId, pp).AddVId;
            ResAddVertex resAddVertex = cad2d.AddVertex(parentElemType, parentId, pp);
            vId = resAddVertex.AddVId;
            eIdAddByAddVertex = resAddVertex.AddEId;
            if (vId == 0)
            {
                // 頂点の作成に失敗
                if (showErrorFlg)
                {
                    MessageBoxShowError("頂点の作成に失敗しました", "");
                }
                resAddVertex.AddVId = 0;
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
                    cad2d.SetEdgeColor(eIdAddByAddVertex, 
                        CadDesign.ColorToColorDouble(CadDesign.TmpEdgeColor));
                }
            }
            vertexIdAdd = vId;
            edgeIdAddByAddVertex = eIdAddByAddVertex;
            return success;
        }

        /// <summary>
        /// 辺を作成する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="lIdOfAddVertex"></param>
        /// <param name="editVertexIds"></param>
        /// <param name="editEdgeIds"></param>
        /// <param name="eId"></param>
        /// <param name="lIdAddByConnectVertex"></param>
        /// <returns></returns>
        public static bool addEdgeByLastEditPts(CadObject2D cad2d, uint lIdOfAddVertex,
            ref IList<uint> editVertexIds, ref IList<uint> editEdgeIds,
            out uint eId, out uint lIdAddByConnectVertex, bool showErrorFlg)
        {
            bool success = false;

            // 出力の初期化
            eId = 0;
            lIdAddByConnectVertex = 0;

            // 作成しようとしている辺がすでに作成されているかチェックする
            uint existedEId = CadDesign.getEdgeIdOfVertexIds(
                cad2d, editVertexIds[editVertexIds.Count - 2], editVertexIds[editVertexIds.Count - 1]);
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
            //eId = EditCad2D.ConnectVertex_Line(EditVertexIds[EditVertexIds.Count - 2], EditVertexIds[EditVertexIds.Count - 1]).id_e_add;
            ResConnectVertex resConnectVertex = cad2d.ConnectVertexLine(
                editVertexIds[editVertexIds.Count - 2], editVertexIds[editVertexIds.Count - 1]);
            eId = resConnectVertex.AddEId;  // 追加された辺のIDを格納
            if (resConnectVertex.AddLId != 0)
            {
                System.Diagnostics.Debug.WriteLine("lIdOfAddVertex:{0}", lIdOfAddVertex);
                System.Diagnostics.Debug.WriteLine("AddLId:{0}", resConnectVertex.AddLId);
                System.Diagnostics.Debug.WriteLine("LId:{0}", resConnectVertex.LId);

                lIdAddByConnectVertex = 
                    (resConnectVertex.AddLId != lIdOfAddVertex) ?
                    resConnectVertex.AddLId :
                    resConnectVertex.LId;  // 辺の作成でループが作成された場合ループIDを格納
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
                cad2d.SetEdgeColor(eId, CadDesign.ColorToColorDouble(CadDesign.TmpEdgeColor));

                success = true;
            }
            return success;
        }

        /// <summary>
        /// ヒットテスト
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool hitTest(Point pt, out CadElementType partElemType, out uint partId)
        {
            bool hit = false;
            partElemType = CadElementType.NotSet;
            partId = 0;

            int sizeBuffer = 2048;
            int[] pickSelectBuffer = new int[sizeBuffer];
            OpenGLUtils.PickPre(sizeBuffer, pickSelectBuffer, (uint)pt.X, (uint)pt.Y, 5, 5, Camera);
            DrawerAry.DrawSelection();
            List<SelectedObject> selecObjs = 
                (List < SelectedObject >)
                OpenGLUtils.PickPost(pickSelectBuffer, (uint)pt.X, (uint)pt.Y, Camera);

            DrawerAry.ClearSelected();
            if (selecObjs.Count > 0)
            {
                if (DrawerAry.Drawers.Count >= 2)
                {
                    CadObject2DDrawer drawer = DrawerAry.Drawers[1] as CadObject2DDrawer;
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
                if ((CadMode == CadModeType.Polygon &&
                        ((!IsEditing && partElemType == CadElementType.Loop) ||
                        partElemType == CadElementType.Edge ||
                        partElemType == CadElementType.Vertex)) ||
                    (CadMode == CadModeType.Erase &&
                        (partElemType == CadElementType.Loop ||
                        partElemType == CadElementType.Edge ||
                        partElemType == CadElementType.Vertex)) ||
                    (CadMode == CadModeType.Move &&
                        (partElemType == CadElementType.Loop ||
                        partElemType == CadElementType.Edge ||
                        partElemType == CadElementType.Vertex))
                    )
                {
                    // 選択表示設定に追加する
                    DrawerAry.AddSelected(selecObjs[0].Name);
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
        /// ループの色と辺の色を全ループについて設定する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="loopIds"></param>
        public static void SetupColorOfCadObjectsForAllLoops(CadObject2D cad2d, IList<uint> loopIds)
        {
            /*
            //ループの色をすべて再設定する
            foreach (uint workLoopId in loopIds)
            {
                Color backColor = Color.Green;// !!!!
                SetupColorOfCadObjectsForOneLoop(cad2d, workLoopId, backColor);
            }
            */
        }

        /// <summary>
        /// 領域を削除する
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool doEraseDisconArea(Point pt)
        {
            bool executed = false;
            if (CadMode != CadModeType.Erase)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CadElementType partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CadElementType.Loop && partId != 0 &&
                    Cad2D.IsElemId(CadElementType.Loop, partId))
                {
                    uint tagtLoopId = partId;
                    // ループの削除処理
                    executed = delLoop(Cad2D, tagtLoopId, ref LoopIds, ref EdgeCollectionList);
                }
            }
            if (executed)
            {
                if (Change != null)
                {
                    Change(this, CadMode);
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
        /// <param name="cad2d"></param>
        /// <param name="tagtLoopId"></param>
        /// <param name="loopIds"></param>
        /// <param name="edgeCollectionList"></param>
        /// <param name="medias"></param>
        /// <param name="incidentPortNo"></param>
        /// <returns></returns>
        public static bool delLoop(
            CadObject2D cad2d, uint tagtLoopId,
            ref IList<uint> loopIds, ref IList<EdgeCollection> edgeCollectionList)
        {
            bool executed = false;
            if (tagtLoopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return executed;
            }

            // ループの頂点、辺のIDのリストを取得する
            IList<uint> vIdList = null;
            IList<uint> eIdList = null;
            CadDesign.GetEdgeVertexListOfLoop(cad2d, tagtLoopId, out vIdList, out eIdList);

            IList<uint> otherLoopVIdList = new List<uint>();
            IList<uint> otherLoopEIdList = new List<uint>();
            foreach (uint workLoopId in loopIds)
            {
                if (workLoopId != tagtLoopId)
                {
                    IList<uint> workVIdList = null;
                    IList<uint> workEIdList = null;
                    CadDesign.GetEdgeVertexListOfLoop(cad2d, workLoopId, out workVIdList, out workEIdList);
                    foreach (uint workVId in workVIdList)
                    {
                        otherLoopVIdList.Add(workVId);
                    }
                    foreach (uint workEId in workEIdList)
                    {
                        otherLoopEIdList.Add(workEId);
                    }
                }
            }

            // エラーチェック用
            Dictionary<uint, IList<uint>> saveLoopEdgesList = null;
            chkLoopEdgesPreProc(cad2d, loopIds, out saveLoopEdgesList);

            // ループ削除
            //   辺と頂点も削除してくれているはず? -->してくれない-->というかループの場合なにもしないらしい
            //cad2d.RemoveElement(CadElementType.Loop, tagtLoopId);
            // ループを構成する辺と頂点を削除
            // 辺を削除
            foreach (uint eId in eIdList)
            {
                if (otherLoopEIdList.IndexOf(eId) >= 0)
                {
                    // 他のループと共有している辺の場合(領域分割で作成された辺)
                    continue;
                }

                // 辺を削除
                cad2d.RemoveElement(CadElementType.Edge, eId);

                // 領域すべてが他のループに囲まれていて削除できない場合がある
                // そのため、ここで辺の削除が行われていることを示すためにフラグを立てる
                executed = true;
            }
            if (executed)
            {
                // 頂点削除
                foreach (uint vId in vIdList)
                {
                    if (otherLoopVIdList.IndexOf(vId) >= 0)
                    {
                        // 他のループと共有している頂点の場合(領域分割で作成された頂点)
                        continue;
                    }
                    cad2d.RemoveElement(CadElementType.Vertex, vId);
                }
            }

            if (executed)
            {
                // ループリストから削除
                loopIds.Remove(tagtLoopId);

                // チェック用
                // ループIDが変更されているかチェックし、変更されていればループ情報を更新する
                chkLoopEdgesPostProc(cad2d, saveLoopEdgesList, tagtLoopId, 0, ref loopIds);

                // 全ループの色を再設定する
                SetupColorOfCadObjectsForAllLoops(cad2d, loopIds);

            }

            return executed;
        }

        /// <summary>
        /// エラーチェック用前処理
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="loopIds">ループ情報リスト</param>
        /// <param name="saveLoopEdgesList"></param>
        private static void chkLoopEdgesPreProc(CadObject2D cad2d, IList<uint> loopIds, out Dictionary<uint, IList<uint>> saveLoopEdgesList)
        {
            saveLoopEdgesList = new Dictionary<uint, IList<uint>>();
            foreach (uint workLoopId in loopIds)
            {
                IList<uint> workVIdList = null;
                IList<uint> workEIdList = null;
                GetEdgeVertexListOfLoop(cad2d, workLoopId, out workVIdList, out workEIdList);
                saveLoopEdgesList.Add(workLoopId, workEIdList);
            }
        }
        /// <summary>
        /// ループIDが変更されているかチェックし、変更されていればループ情報を更新する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="saveLoopEdgesList"></param>
        /// <param name="delLoopId">削除されたループID</param>
        /// <param name="delEId">削除された辺ID</param>
        /// <param name="loopIds">ループ情報リスト</param>
        private static void chkLoopEdgesPostProc(
            CadObject2D cad2d, Dictionary<uint, IList<uint>> saveLoopEdgesList, uint delLoopId, uint delEId,
            ref IList<uint> loopIds)
        {
            if (delLoopId != 0)
            {
                // まず削除されたループは除外するためマップから削除
                saveLoopEdgesList.Remove(delLoopId);
            }

            // エラーチェック：ループIDが変更される??
            foreach (var loopEdgesPair in saveLoopEdgesList)
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
                    uint curLoopId = getLoopIdOfEdge(cad2d, eId);
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
        /// 入出力ポートの選択処理
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool doSelectPort(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Port)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CadElementType partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CadElementType.Edge && partId != 0 &&
                    Cad2D.IsElemId(CadElementType.Edge, partId))
                {
                    uint tagtEdgeId = partId;
                    // 辺がループに属しているかチェック
                    uint parentLoopId = getLoopIdOfEdge(Cad2D, tagtEdgeId);
                    if (parentLoopId == 0)
                    {
                        // 辺がループに属していない
                        return executed;
                    }
                    // すでに対象の辺がポート境界に指定されていないかチェックする
                    EdgeCollection otherEdge = getEdgeCollectionByEdgeId(tagtEdgeId);
                    if (otherEdge == null)
                    {
                        // 新規の辺

                        EdgeCollection edge = null;
                        /*
                        if (KeyModifiers.HasFlag(Keys.Control) && EditPortNo != 0)
                        {
                            // Controlが押されている場合は、直前に作成された境界に辺を追加する
                            ((List<EdgeCollection>)EdgeCollectionList).Sort(); // 順番に並んでいることを保証する
                            edge = EdgeCollectionList[EditPortNo - 1];
                            if (isNextDoorEdge(EditCad2D, tagtEdgeId, edge.EdgeIds[edge.EdgeIds.Count - 1]))
                            {
                                // 隣
                                bool ret = edge.AddEdgeId(tagtEdgeId, EditCad2D);
                                if (ret)
                                {
                                    executed = true;
                                }
                            }
                            else
                            {
                                // 隣でない
                                Console.WriteLine("not nextdoor edge");
                            }
                        }
                        else
                        */
                        {
                            // 新規のポート境界
                            edge = new EdgeCollection();
                            edge.No = EdgeCollectionList.Count + 1;
                            bool ret = edge.AddEdgeId(tagtEdgeId, Cad2D);
                            if (ret)
                            {
                                // ポート境界の追加
                                EdgeCollectionList.Add(edge);
                                /*
                                if (KeyModifiers.HasFlag(Keys.Control))
                                {
                                    EditPortNo = edge.No;
                                    Console.WriteLine("EditPortNo written!");
                                }
                                */
                                executed = true;
                            }
                        }
                        if (executed)
                        {
                            // 辺に色を付ける
                            Color portColor = PortColor;
                            double[] portColorDouble = CadDesign.ColorToColorDouble(portColor);
                            foreach (uint eId in edge.EdgeIds)
                            {
                                Cad2D.SetEdgeColor(eId, portColorDouble);
                            }
                        }
                    }
                }
            }
            if (executed)
            {
                if (Change != null)
                {
                    Change(this, CadMode);
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
        private bool doErasePort(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Erase)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CadElementType partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CadElementType.Edge && partId != 0 &&
                    Cad2D.IsElemId(CadElementType.Edge, partId))
                {
                    uint tagtEdgeId = partId;
                    EdgeCollection hitEdge = getEdgeCollectionByEdgeId(tagtEdgeId);
                    if (hitEdge != null)
                    {
                        doErasePortCore(Cad2D, hitEdge, ref EdgeCollectionList);

                        executed = true;
                    }
                }
            }
            if (executed)
            {
                if (Change != null)
                {
                    Change(this, CadMode);
                }
            }
            if (executed && !IsDirty)
            {
                IsDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// 辺のIDからエッジコレクションを取得する
        /// </summary>
        /// <param name="tagtEdgeId"></param>
        /// <returns></returns>
        private EdgeCollection getEdgeCollectionByEdgeId(uint tagtEdgeId)
        {
            EdgeCollection hitEdge = null;
            foreach (EdgeCollection work in EdgeCollectionList)
            {
                if (work.ContainsEdgeId(tagtEdgeId))
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
        /// <param name="cad2d"></param>
        /// <param name="hitEdge"></param>
        /// <param name="edgeCollectionList"></param>
        /// <param name="incidentPortNo"></param>
        public static void doErasePortCore(
            CadObject2D cad2d, EdgeCollection hitEdge, ref IList<EdgeCollection> edgeCollectionList)
        {
            // ヒットしたポート番号
            int hitPortNo = hitEdge.No;

            System.Diagnostics.Debug.Assert(!hitEdge.IsEmpty());
            // 辺ID(先頭の辺を採用)
            uint tagtEdgeId = hitEdge.EdgeIds[0];

            /*
            // ループの色
            double[] loopColorDouble = new double[3];
            //辺が属するループを取得する
            uint lId = getLoopIdOfEdge(cad2d, tagtEdgeId);
            // ループの背景色を取得する
            cad2d.GetLoopColor(lId, loopColorDouble);
            // 辺の色をループの色に戻す
            Color loopColor = CadDesign.ColorDoubleToColor(loopColorDouble);
            Color loopLineColor = CadDesign.GetLoopLineColor(loopColor);
            */
            Color loopLineColor = TmpEdgeColor;
            double[] loopLineColorDouble = CadDesign.ColorToColorDouble(loopLineColor);
            foreach (uint eId in hitEdge.EdgeIds)
            {
                cad2d.SetEdgeColor(eId, loopLineColorDouble);
            }
            // ポートを削除
            edgeCollectionList.Remove(hitEdge);
        }

        /// <summary>
        /// 辺の削除処理
        ///   危険　テスト実装
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool doEraseCadEdge(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Erase)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CadElementType partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CadElementType.Edge && partId != 0 && 
                    Cad2D.IsElemId(CadElementType.Edge, partId))
                {
                    uint tagtEdgeId = partId;
                    /*
                    // まずポート境界があれば削除
                    EdgeCollection hitEdge = getEdgeCollectionByEdgeId(tagtEdgeId);
                    if (hitEdge != null)
                    {
                        doErasePortCore(EditCad2D, hitEdge, ref EdgeCollectionList);
                        executed = true;
                    }
                    */
                    // 辺を削除するとループが壊れるので壊れるループIDを記録する
                    uint brokenLoopId = 0;
                    {
                        // 辺のループIDを取得する
                        uint workLoopId = getLoopIdOfEdge(Cad2D, tagtEdgeId);
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
                            getVertexIdsOfEdgeId(Cad2D, tagtEdgeId, out workVId1, out workVId2);
                            IList<uint> workEIdsOfVId1 = getEdgeIdsByVertexId(Cad2D, workVId1);
                            IList<uint> workEIdsOfVId2 = getEdgeIdsByVertexId(Cad2D, workVId2);
                            if (workEIdsOfVId1.Count >= 2 && workEIdsOfVId2.Count >= 2)
                            {
                                brokenLoopId = workLoopId;
                            }
                        }
                    }
                    // 辺の頂点を取得する
                    uint vId1 = 0;
                    uint vId2 = 0;
                    getVertexIdsOfEdgeId(Cad2D, tagtEdgeId, out vId1, out vId2);

                    // エラーチェック用
                    // ループの辺IDを１つ取得しておく
                    uint exceptEId = tagtEdgeId; // 削除対象辺IDは除外して取得
                    // エラーチェック用
                    Dictionary<uint, IList<uint>> saveLoopEdgesList = null;
                    chkLoopEdgesPreProc(Cad2D, LoopIds, out saveLoopEdgesList);

                    if (brokenLoopId != 0)
                    {
                        /*
                        // 壊れるループにポート境界があれば削除する
                        bool delport_exceuted = delPortBelongToLoop(EditCad2D, brokenLoopId, ref EdgeCollectionList, ref IncidentPortNo);
                        if (delport_exceuted)
                        {
                            if (!executed)
                            {
                                executed = true;
                            }
                        }
                        */
                    }

                    // 辺を削除
                    bool ret = Cad2D.RemoveElement(CadElementType.Edge, tagtEdgeId);
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
                        if (!isVertexOwnedByEdges(Cad2D, vId1))
                        {
                            bool retRmVertex = Cad2D.RemoveElement(CadElementType.Vertex, vId1);
                            System.Diagnostics.Debug.Assert(retRmVertex);
                        }
                        if (!isVertexOwnedByEdges(Cad2D, vId2))
                        {
                            bool retRmVertex = Cad2D.RemoveElement(CadElementType.Vertex, vId2);
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
                        chkLoopEdgesPostProc(Cad2D, saveLoopEdgesList, brokenLoopId, tagtEdgeId, ref LoopIds);
                    }
                    if (executed)
                    {
                        // 全ループの色を再設定する
                        SetupColorOfCadObjectsForAllLoops(Cad2D, LoopIds);
                    }
                }
            }
            if (executed)
            {
                if (Change != null)
                {
                    Change(this, CadMode);
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
            Color loopLineColor = Color.DarkGray;//Color.White;
            return loopLineColor;
        }

        /// <summary>
        /// Colorをdoule[]の規格化カラーに変換する
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static double[] ColorToColorDouble(Color color)
        {
            double[] colorDouble = new double[] { color.R / (double)255, color.G / (double)255, color.B / (double)255 };
            return colorDouble;
        }

        /// <summary>
        /// 規格化カラーをカラーに変換する
        /// </summary>
        /// <param name="colorDouble"></param>
        /// <returns></returns>
        public static Color ColorDoubleToColor(double[] colorDouble)
        {
            return Color.FromArgb((int)(colorDouble[0] * 255), (int)(colorDouble[1] * 255), (int)(colorDouble[2] * 255));
        }

        /// <summary>
        /// Cadオブジェクトのループとその辺、頂点の色をセットする
        /// </summary>
        /// <param name="cad2D"></param>
        /// <param name="id3l"></param>
        /// <param name="backColor"></param>
        public static void SetupColorOfCadObjectsForOneLoop(CadObject2D cad2d, uint lId, Color backColor)
        {
            // ループの頂点と辺のリストを取得する
            IList<uint> vIdList = null;
            IList<uint> eIdList = null;
            CadDesign.GetEdgeVertexListOfLoop(cad2d, lId, out vIdList, out eIdList);

            // ループの色を指定（指定しなければ(0.9,0.8,0.8)になる
            double[] backColorDouble = CadDesign.ColorToColorDouble(backColor);
            cad2d.SetLoopColor(lId, backColorDouble);
            // 辺、頂点の色の変更
            Color loopLineColor = CadDesign.GetLoopLineColor(backColor);
            double[] lineColorDouble = CadDesign.ColorToColorDouble(loopLineColor);
            // 辺の色
            foreach (uint eId in eIdList)
            {
                cad2d.SetEdgeColor(eId, lineColorDouble);
            }
            // 頂点の色
            //foreach (uint id_v in vIdList)
            //{
            //    cad2d.SetVertexColor(id_v, lineColorDouble);
            //}
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // CadObj2Dのユーティリティ
        /// <summary>
        /// ループの内側にあるループを子ループに設定する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="loopId"></param>
        /// <param name="loopIds"></param>
        /// <param name="edgeCollectionList"></param>
        public static void reconstructLoopsInsideLoopAsChild(CadObject2D cad2d, uint loopId,
            ref IList<uint> loopIds, ref IList<EdgeCollection> edgeCollectionList)
        {
            if (loopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            //   内側にあるループIDのリストを取得する
            IList<uint> insideLoopIds = getLoopIdsInsideLoop(cad2d, loopId, loopIds);
            //  子ループに設定
            foreach (uint childLoopId in insideLoopIds)
            {
                setLoopParentLoopId(cad2d, childLoopId, loopId, ref loopIds, ref edgeCollectionList);
            }
        }

        /// <summary>
        /// ループの内側にあるループのIDのリストを取得する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="loopId"></param>
        /// <param name="loopIds"></param>
        /// <returns></returns>
        public static IList<uint> getLoopIdsInsideLoop(CadObject2D cad2d, uint loopId, IList<uint> loopIds)
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
                    IList<uint> vIdList = null;
                    IList<uint> eIdList = null;
                    GetEdgeVertexListOfLoop(cad2d, workLoopId, out vIdList, out eIdList);
                    if (vIdList.Count > 0)
                    {
                        vId = vIdList[0];
                    }
                }
                System.Diagnostics.Debug.Assert(vId != 0);
                // ワークループの頂点の座標を取得
                OpenTK.Vector2d vPP = cad2d.GetVertex(vId);
                // ワークループの頂点が、指定ループの内側の点か？
                bool inside = cad2d.CheckIsPointInsideLoop(loopId, vPP);
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
        /// <param name="cad2d"></param>
        /// <param name="childLoopId">子ループID</param>
        /// <param name="parentLoopId">親ループID</param>
        /// <param name="loopIds">ループ情報リスト</param>
        /// <param name="edgeCollectionList">ポート境界エッジコレクションのリスト</param>
        public static void setLoopParentLoopId(
            CadObject2D cad2d, uint childLoopId, uint parentLoopId,
            ref IList<uint> loopIds, ref IList<EdgeCollection> edgeCollectionList)
        {
            if (childLoopId == 0 || parentLoopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            // 方針：子ループを削除して再設定する

            /* NG 再構築しないと駄目
            uint workParentId = getParentLoopId(cad2d, CAD_ELEM_TYPE.LOOP, childLoopId);
            if (workParentId != 0)
            {
                // どこかのループの子ループに設定済みの場合は何もしない
                return;
            }
             */

            // 子ループのループ情報を退避
            uint childLoopIdTmp = childLoopId;
            // 子ループの頂点座標を取得する
            IList<OpenTK.Vector2d> vPPs = new List<OpenTK.Vector2d>();
            {
                IList<uint> vIdList = null;
                IList<uint> eIdList = null;
                GetEdgeVertexListOfLoop(cad2d, childLoopId, out vIdList, out eIdList);
                foreach (uint vId in vIdList)
                {
                    OpenTK.Vector2d vPP = cad2d.GetVertex(vId);
                    vPPs.Add(vPP);
                }
            }
            // 子ループを削除
            bool delRet = delLoop(cad2d, childLoopId, ref loopIds, ref edgeCollectionList);
            if (!delRet)
            {
                MessageBoxShowError("子ループの設定に失敗しました", "");
            }
            else
            {
                // ループを再追加
                //uint id_l_add = cad2d.AddPolygon(v_pps, parentLoopId).id_l_add;
                uint addLId = makeLoop(cad2d, vPPs, loopIds, true);
                if (addLId == 0)
                {
                    MessageBoxShowError("子ループの設定に失敗しました", "");
                }
                else
                {
                    // 子ループの作成に成功

                    // ループ情報を再登録
                    loopIds.Add(addLId);

                    /*
                    // ループの色を設定
                    MediaInfo media = medias[childLoopTmp.MediaIndex];
                    Color backColor = media.BackColor;
                    SetupColorOfCadObjectsForOneLoop(cad2d, id_l_add, backColor);
                    */
                }
            }
        }

        /* 関数の名称からループIDが指定されたときその親が返却されるような誤解を生むので使用停止
        /// <summary>
        /// ヒットテスト結果のパーツID、要素タイプを用いて親ループIDを特定する
        /// </summary>
        /// <param name="partId"></param>
        /// <param name="partElemType"></param>
        public static uint getParentLoopId(CCadObj2D cad2d, CAD_ELEM_TYPE partElemType, uint partId)
        {
            uint parentLoopId = 0;
            if (partId != 0 && partElemType == CAD_ELEM_TYPE.LOOP)
            {
                parentLoopId = partId;
            }
            else if (partId != 0 && partElemType == CAD_ELEM_TYPE.EDGE)
            {
                uint id_e = partId;
                parentLoopId = getLoopIdOfEdge(cad2d, id_e);
            }
            else if (partId != 0 && partElemType == CAD_ELEM_TYPE.VERTEX)
            {
                uint id_v = partId;
                parentLoopId = getLoopIdOfVertex(cad2d, id_v);
            }
            else
            {
                parentLoopId = 0;
            }
            Console.WriteLine("parentLoopId:{0}", parentLoopId);
            return parentLoopId;
        }
         */

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 隣り合う辺か？
        /// </summary>
        /// <param name="eId1"></param>
        /// <param name="eId2"></param>
        /// <returns></returns>
        public static bool isNextDoorEdge(CadObject2D cad2d, uint eId1, uint eId2)
        {
            bool isNextDoor = false;
            // ここで隣の辺のみに限定したい
            Edge2D tagtEdge = cad2d.GetEdge(eId1);
            uint[] tagtVtx = new uint[] { tagtEdge.GetVertexId(true), tagtEdge.GetVertexId(false) };
            Edge2D prevEdge = cad2d.GetEdge(eId2);
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
        /// <param name="EditCad2D"></param>
        /// <param name="lId"></param>
        /// <param name="vIdList"></param>
        /// <param name="eIdList"></param>
        public static void GetEdgeVertexListOfLoop(
            CadObject2D cad2d, uint lId, out IList<uint> vIdList, out IList<uint> eIdList)
        {
            //Console.WriteLine("GetEdgeVertexListOfLoop:id_l = {0}", id_l);
            vIdList = new List<uint>();
            eIdList = new List<uint>();
            if (lId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }
            for (ItrLoop itrl = cad2d.GetItrLoop(lId); !itrl.IsChildEnd; itrl.ShiftChildLoop())
            {
                if (!itrl.IsParent())
                {
                    // 親でないならスキップする
                    continue;
                }
                for (itrl.Begin(); !itrl.IsEnd(); itrl++)
                {
                    uint eId;
                    bool isSameDir;
                    itrl.GetEdgeId(out eId, out isSameDir);
                    uint vId = itrl.GetVertexId();
                    eIdList.Add(eId);
                    vIdList.Add(vId);
                    //Console.WriteLine("    id_v = {0} id_e = {1}", id_v, id_e);
                }
            }
        }

        /// <summary>
        /// 頂点IDからループIDを取得する
        /// </summary>
        /// <param name="vId"></param>
        /// <returns></returns>
        public static uint getLoopIdOfVertex(CadObject2D cad2d, uint vId)
        {
            uint loopId = 0;

            for (ItrVertex itrv = cad2d.GetItrVertex(vId); !itrv.IsEnd(); itrv++)
            {
                uint lId = itrv.GetLoopId();
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
        public static uint getLoopIdOfEdge(CadObject2D cad2d, uint eId)
        {
            uint loopId = 0;
            uint lLId;
            uint rLId;
            cad2d.GetEdgeLoopId(out lLId, out rLId, eId);
            // ループから見て孤立した辺は除外する
            if (lLId != 0 && isStandAloneEdgeInsideLoop(cad2d, eId, lLId))
            {
                lLId = 0;
            }
            if (rLId != 0 && isStandAloneEdgeInsideLoop(cad2d, eId, rLId))
            {
                rLId = 0;
            }

            //System.Diagnostics.Debug.Assert(id_l_l != 0 || id_l_r != 0);
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
                System.Diagnostics.Debug.WriteLine("[Warn]getLoopIdOfEdge: id_l_l == 0 && id_l_r == 0");
                //System.Diagnostics.Debug.Assert(false);
                loopId = 0;
            }

            return loopId;
        }

        /// <summary>
        /// 辺がループ内部の孤立した辺か？
        ///   GetIdLoop_Edgeがループ内部の孤立した辺の場合も含んでいるのでそれを除外したい場合に使用する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="eId"></param>
        /// <param name="loopId"></param>
        /// <returns></returns>
        public static bool isStandAloneEdgeInsideLoop(CadObject2D cad2d, uint eId, uint loopId)
        {
            bool standAlone = false;
            if (eId == 0 || loopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return standAlone;
            }

            IList<uint> workVIdList = null;
            IList<uint> workEIdList = null;
            GetEdgeVertexListOfLoop(cad2d, loopId, out workVIdList, out workEIdList);
            if (workEIdList.IndexOf(eId) == -1)
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
        public static bool isEdgeSharedByLoops(CadObject2D cad2d, uint eId)
        {
            uint lLId;
            uint rLId;
            cad2d.GetEdgeLoopId(out lLId, out rLId, eId);
            /*これは孤立した辺でもよい --> 包含関係にある場合、親ループから見て子ループの辺は孤立した辺なので
            // ループから見て孤立した辺は除外する
            if (id_l_l != 0 && isStandAloneEdgeInsideLoop(cad2d, eId, id_l_l))
            {
                id_l_l = 0;
            }
            if (id_l_r != 0 && isStandAloneEdgeInsideLoop(cad2d, eId, id_l_r))
            {
                id_l_r = 0;
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
        public static uint getNextDoorLoopId(CadObject2D cad2d, uint tagtEdgeId, uint tagtLoopId)
        {
            uint nextDoorLoopId = 0;
            uint lLId;
            uint rLId;
            cad2d.GetEdgeLoopId(out lLId, out rLId, tagtEdgeId);
            /*これは孤立した辺でもよい --> 包含関係にある場合、親ループから見て子ループの辺は孤立した辺なので
            // ループから見て孤立した辺は除外する
            if (id_l_l != 0 && isStandAloneEdgeInsideLoop(cad2d, tagtEdgeId, id_l_l))
            {
                id_l_l = 0;
            }
            if (id_l_r != 0 && isStandAloneEdgeInsideLoop(cad2d, tagtEdgeId, id_l_r))
            {
                id_l_r = 0;
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
        /// <param name="cad2d">Cadオブジェクト</param>
        /// <param name="chkPt">チェックする点</param>
        /// <param name="loopIds">これまでに追加されたループリスト</param>
        public static void getVertexIdBelongToLoopByCoord(
            CadObject2D cad2d, OpenTK.Vector2d chkPt, IList<uint> loopIds, 
            out uint outVertexId, out IList<uint> outLoopIds)
        {
            outLoopIds = new List<uint>();
            outVertexId = 0;
            foreach (uint workLoopId in loopIds)
            {
                // ループの頂点、辺のIDのリストを取得する
                IList<uint> vIdList = null;
                IList<uint> eIdList = null;
                CadDesign.GetEdgeVertexListOfLoop(cad2d, workLoopId, out vIdList, out eIdList);

                foreach (uint vId in vIdList)
                {
                    OpenTK.Vector2d vertexPt = cad2d.GetVertex(vId);
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
        /// <param name="cad2d"></param>
        /// <param name="vId"></param>
        /// <returns></returns>
        public static IList<uint> getEdgeIdsByVertexId(CadObject2D cad2d, uint vId)
        {
            IList<uint> eIdList = new List<uint>();
            if (vId != 0)
            {
                for (ItrVertex itrv = cad2d.GetItrVertex(vId); !itrv.IsEnd(); itrv++)
                {
                    bool ret;
                    uint eId;
                    bool isSameDir;
                    eId = 0;
                    ret = itrv.GetBehindEdgeId(out eId, out isSameDir);
                    if (ret && eId != 0)
                    {
                        if (eIdList.IndexOf(eId) == -1)
                        {
                            eIdList.Add(eId);
                        }
                    }
                    eId = 0;
                    ret = itrv.GetAheadEdgeId(out eId, out isSameDir);
                    if (ret && eId != 0)
                    {
                        if (eIdList.IndexOf(eId) == -1)
                        {
                            eIdList.Add(eId);
                        }
                    }
                }
            }
            return eIdList;
        }

        /// <summary>
        /// ２つの頂点IDからなる辺IDを取得する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="vId1"></param>
        /// <param name="vId2"></param>
        /// <returns></returns>
        public static uint getEdgeIdOfVertexIds(CadObject2D cad2d, uint vId1, uint vId2)
        {
            uint retEId = 0;

            System.Diagnostics.Debug.Assert(vId1 != vId2);
            if (vId1 == vId2)
            {
                return retEId;
            }

            IList<uint> eIdList1 = getEdgeIdsByVertexId(cad2d, vId1);
            IList<uint> eIdList2 = getEdgeIdsByVertexId(cad2d, vId2);

            foreach (uint workEId in eIdList1)
            {
                if (eIdList2.IndexOf(workEId) >= 0)
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
                getVertexIdsOfEdgeId(cad2d, retEId, out workVId1, out workVId2);
                System.Diagnostics.Debug.Assert((workVId1 == vId1 && workVId2 == vId2) || (workVId1 == vId2 && workVId2 == vId1));
            }


            return retEId;
        }

        /// <summary>
        /// 辺IDから頂点ID２つを取得する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="eId"></param>
        /// <param name="vId1"></param>
        /// <param name="vId2"></param>
        public static void getVertexIdsOfEdgeId(CadObject2D cad2d, uint eId, out uint vId1, out uint vId2)
        {
            cad2d.GetEdgeVertexId(out vId1, out vId2, eId);
            /*
            id_v1 = 0;
            id_v2 = 0;
            Edge2D edge2d =cad2d.GetEdge(id_e);
            id_v1 = edge2d.GetVertexId(true);
            id_v2 = edge2d.GetVertexId(false);
             */
        }

        /// <summary>
        /// 頂点が辺に所有されているか？
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="vId"></param>
        /// <returns></returns>
        public static bool isVertexOwnedByEdges(CadObject2D cad2d, uint vId)
        {
            bool owned = false;
            if (vId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return owned;
            }

            ItrVertex itrv = cad2d.GetItrVertex(vId);
            int edgeCnt = 0;
            for (itrv.Begin(); !itrv.IsEnd(); itrv++)
            {
                uint eId;
                bool isSameDir;
                itrv.GetAheadEdgeId(out eId, out isSameDir);
                if (eId == 0)
                {
                    itrv.GetBehindEdgeId(out eId, out isSameDir);
                }
                if (eId != 0)
                {
                    edgeCnt++;
                }
            }
            System.Diagnostics.Debug.Assert(edgeCnt == itrv.CountEdge());
            if (edgeCnt > 0)
            {
                owned = true;
            }

            return owned;
        }

        /// <summary>
        /// 指定された点を包含するループのIDを取得する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="chkPt">チェックする点</param>
        /// <param name="loopIds">これまでに追加されたループのリスト</param>
        /// <returns></returns>
        public static uint getLoopIdIncludingPoint(CadObject2D cad2d, OpenTK.Vector2d chkPt, IList<uint> loopIds)
        {
            uint hitLId = 0;
            foreach (uint lId in loopIds)
            {
                bool ret = cad2d.CheckIsPointInsideLoop(lId, chkPt);
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
        /// <param name="cad2d"></param>
        /// <param name="chkPt">チェックするポイント</param>
        /// <param name="loopList">これまでに追加されたループのリスト</param>
        /// <returns>辺ID</returns>
        public static uint getEdgeIdIncludingPoint(
            CadObject2D cad2d, OpenTK.Vector2d chkPt, IList<uint> loopIds)
        {
            uint hitEId = 0;
            foreach (uint lId in loopIds)
            {
                IList<uint> vIdList = null;
                IList<uint> eIdList = null;
                GetEdgeVertexListOfLoop(cad2d, lId, out vIdList, out eIdList);

                foreach (uint eId in eIdList)
                {
                    bool isOnEdge = isPointOnEdge(cad2d, eId, chkPt);
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
        /// <param name="cad2d"></param>
        /// <param name="eId">辺ID</param>
        /// <param name="chkPt">チェックするポイント</param>
        /// <returns></returns>
        public static bool isPointOnEdge(CadObject2D cad2d, uint eId, OpenTK.Vector2d chkPt)
        {
            bool isOnEdge = false;
            uint vId1 = 0;
            uint vId2 = 0;
            getVertexIdsOfEdgeId(cad2d, eId, out vId1, out vId2);
            OpenTK.Vector2d ppV1 = cad2d.GetVertex(vId1);
            OpenTK.Vector2d ppV2 = cad2d.GetVertex(vId2);
            isOnEdge = isPointOnEdge(ppV1, ppV2, chkPt);
            return isOnEdge;
        }

        /// <summary>
        /// 指定ポイントが辺上にある？
        /// </summary>
        /// <param name="ppV1">辺の始点の座標</param>
        /// <param name="ppV2">辺の終点の座標</param>
        /// <param name="chkPt">チェックするポイント</param>
        /// <returns></returns>
        public static bool isPointOnEdge(OpenTK.Vector2d ppV1, OpenTK.Vector2d ppV2, OpenTK.Vector2d chkPt)
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
        /// <param name="cad2d">Cadオブジェクト</param>
        /// <param name="ppV1">辺の始点</param>
        /// <param name="ppV2">辺の終点</param>
        /// <param name="chkEId">含むかチェックする辺のID</param>
        /// <returns></returns>
        public static bool isEdgeIncludingEdge(
            CadObject2D cad2d, OpenTK.Vector2d ppV1, OpenTK.Vector2d ppV2, uint chkEId)
        {
            bool isIncluding = false;

            // チェックする辺の始点と終点を取得
            uint chkVId1 = 0;
            uint chkVId2 = 0;
            getVertexIdsOfEdgeId(cad2d, chkEId, out chkVId1, out chkVId2);
            OpenTK.Vector2d chkPPV1 = cad2d.GetVertex(chkVId1);
            OpenTK.Vector2d chkPPV2 = cad2d.GetVertex(chkVId2);

            // チェックする辺の始点と終点がどちらも元の辺の辺上なら、チェックする辺は元の辺に含まれる
            bool chk1 = isPointOnEdge(ppV1, ppV2, chkPPV1);
            bool chk2 = isPointOnEdge(ppV1, ppV2, chkPPV2);
            if (chk1 && chk2)
            {
                isIncluding = true;
            }
            return isIncluding;
        }

        /// <summary>
        /// これから作成する辺が他の辺を含んでいるかチェックし、含んでいればその辺IDを返却する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="newEdgeVId">これから作成する辺の始点</param>
        /// <param name="chkPt">これから作成する辺の終点ポイント</param>
        /// <param name="loopIds">これまでに追加されたループのリスト</param>
        /// <returns>辺IDのリスト</returns>
        public static IList<uint> getEdgeListIdIncludedByNewEdge(
            CadObject2D cad2d, OpenTK.Vector2d chkPPV1, OpenTK.Vector2d chkPPV2, IList<uint> loopIds)
        {
            IList<uint> hitEIdList = new List<uint>();

            foreach (uint lId in loopIds)
            {
                IList<uint> vIdList = null;
                IList<uint> eIdList = null;
                GetEdgeVertexListOfLoop(cad2d, lId, out vIdList, out eIdList);

                foreach (uint eId in eIdList)
                {
                    bool isIncluding = isEdgeIncludingEdge(cad2d, chkPPV1, chkPPV2, eId);
                    if (isIncluding)
                    {
                        hitEIdList.Add(eId);
                    }
                }
            }
            return hitEIdList;
        }

        /// <summary>
        /// これから作成する辺が既存の辺を含んでいるかチェックし、含んでいれば（複数の辺の可能性あり）開始頂点ID、終了頂点IDを返却する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="ppV1">これから作成する辺の始点</param>
        /// <param name="ppV2">これから作成する辺の終点</param>
        /// <param name="loopIds">これまでに追加したループのリスト</param>
        /// <param name="minDistanceVId">開始頂点ID</param>
        /// <param name="maxDistanceVId">終了頂点ID</param>
        public static void getIncludedEdgesStEndVId(
            CadObject2D cad2d, OpenTK.Vector2d ppV1, OpenTK.Vector2d ppV2, IList<uint> loopIds,
            out uint minDistanceVId, out uint maxDistanceVId)
        {
            minDistanceVId = 0;
            maxDistanceVId = 0;

            IList<uint> includedEIds = getEdgeListIdIncludedByNewEdge(cad2d, ppV1, ppV2, loopIds);
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
                    getVertexIdsOfEdgeId(cad2d, includedEId, out vId1OfIncludedEdge, out vId2OfIncludedEdge);
                    OpenTK.Vector2d ppV1OfIncludedEdge = cad2d.GetVertex(vId1OfIncludedEdge);
                    OpenTK.Vector2d ppV2OfIncludedEdge = cad2d.GetVertex(vId2OfIncludedEdge);
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
        /// <param name="cad2d">Cadオブジェクト</param>
        /// <param name="pps">追加するループの多角形の頂点リスト(ループを閉じる終点は含まない)</param>
        /// <param name="loopIds">これまでに追加されたループのリスト</param>
        /// <returns></returns>
        public static uint makeLoop(
            CadObject2D cad2d, IList<OpenTK.Vector2d> pps, IList<uint> loopIds, bool showErrorFlg)
        {
            // 多角形でループを作成するのを止める
            //uint id_l = out_cad2d.AddPolygon(pps, baseLoopId).id_l_add;

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
                executed = doMakeDisconAreaCore(cad2d, pp, workIndexPP, loopIds,
                    ref addPts, ref addVertexIds, ref addEdgeIds, ref addLoopIds, showErrorFlg);
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
                    executed = doMakeDisconAreaCore(cad2d, pp, workIndexPP, loopIds,
                        ref addPts, ref addVertexIds, ref addEdgeIds, ref addLoopIds, showErrorFlg);
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
