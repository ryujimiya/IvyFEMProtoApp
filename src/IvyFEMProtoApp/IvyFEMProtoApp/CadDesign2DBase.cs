using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;

namespace IvyFEMProtoApp
{
    public class CadDesign2DBase
    {
        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Cadモード
        ///   None 操作なし
        ///   Move 物体の移動
        ///   Polygon 多角形作成
        ///   Arc 円弧⇔直線変換と円弧中心変更
        ///   Port ポート境界選択
        ///   Erase 消しゴム
        /// </summary>
        public enum CadModeType
        {
            None,
            Move,
            Polygon,
            Arc,
            Port,
            Erase
        }

        /// <summary>
        /// Cad
        /// </summary>
        public CadObject2DMove Cad { get; protected set; } = new CadObject2DMove();

        /// <summary>
        /// Cadのループのリスト
        /// </summary>
        public IList<uint> LoopIds { get; protected set; } = new List<uint>();

        /// <summary>
        /// 境界のリスト
        /// </summary>
        public IList<PortEdge> PortEdges { get; protected set; } = new List<PortEdge>();

        ////////////////////////////////////////////////////
        //編集中
        ////////////////////////////////////////////////////
        /// <summary>
        /// 編集中のVector2D 座標リスト
        /// </summary>
        protected IList<OpenTK.Vector2d> EditPts = new List<OpenTK.Vector2d>();
        /// <summary>
        /// 追加中の多角形の頂点IDのリスト
        /// </summary>
        protected IList<uint> EditVertexIds = new List<uint>();
        /// <summary>
        /// 追加中の多角形の辺IDのリスト
        /// </summary>
        protected IList<uint> EditEdgeIds = new List<uint>();

        /// <summary>
        /// Cadモード
        /// </summary>
        protected CadModeType _CadMode = CadModeType.None;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CadDesign2DBase()
        {
            Init();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Init()
        {
            _CadMode = CadModeType.None;

            Cad.Clear();
            LoopIds.Clear();
            PortEdges.Clear();

            //edit
            EditPts.Clear();
            EditVertexIds.Clear();
            EditEdgeIds.Clear();
        }

        /// <summary>
        /// Cadデータをコピーする
        /// </summary>
        /// <param name="src"></param>
        public void Copy(CadDesign2DBase src)
        {
            if (src == this)
            {
                System.Diagnostics.Debug.WriteLine("Why? another me exists!");
                //System.Diagnostics.Debug.Assert(false);
                return;
            }

            // CadモードもUndo/Redo対象に入れる
            _CadMode = src._CadMode;

            Cad.Copy(src.Cad);

            LoopIds.Clear();
            foreach (uint srcLoopId in src.LoopIds)
            {
                uint loopId = srcLoopId;
                LoopIds.Add(loopId);
            }
            PortEdges.Clear();
            foreach (PortEdge srcEdge in src.PortEdges)
            {
                PortEdge edge = new PortEdge(srcEdge);
                PortEdges.Add(edge);
            }

            // edit
            EditPts.Clear();
            foreach (OpenTK.Vector2d pp in src.EditPts)
            {
                EditPts.Add(pp);
            }
            // edit
            EditVertexIds.Clear();
            foreach (uint id_v in src.EditVertexIds)
            {
                EditVertexIds.Add(id_v);
            }
            // edit
            EditEdgeIds.Clear();
            foreach (uint id_e in src.EditEdgeIds)
            {
                EditEdgeIds.Add(id_e);
            }
        }
    }
}
