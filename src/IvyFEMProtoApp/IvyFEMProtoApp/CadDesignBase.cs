using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;

namespace IvyFEMProtoApp
{
    public class CadDesignBase
    {
        public enum CadModeType
        {
            None,
            Move,
            Polygon,
            Port,
            Erase
        }

        /// <summary>
        /// Cadのループのリスト
        /// </summary>
        protected IList<uint> LoopIds = new List<uint>();

        /// <summary>
        /// 境界のリスト
        /// </summary>
        protected IList<PortEdge> PortEdges = new List<PortEdge>();

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
        public CadDesignBase()
        {
            init();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected void init()
        {
            _CadMode = CadModeType.None;

            LoopIds.Clear();
            PortEdges.Clear();

            //edit
            EditPts.Clear();
            EditVertexIds.Clear();
            EditEdgeIds.Clear();
        }
    }
}
