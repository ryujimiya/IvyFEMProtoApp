using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;

namespace IvyFEMProtoApp
{
    /// <summary>
    /// ポート-エッジ対応
    /// </summary>
    class PortEdge
    {
        /// <summary>
        /// ポート番号
        /// </summary>
        public int No { get; set; }

        /// <summary>
        ///  Cadオブジェクトの辺のID
        /// </summary>
        public uint EdgeId { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public PortEdge()
        {
            init();
        }

        private void init()
        {
            No = 0;
            EdgeId = 0;
        }

        /// <summary>
        /// コピー
        /// </summary>
        /// <param name="src"></param>
        public void CP(PortEdge src)
        {
            if (this == src)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }
            init();

            No = src.No;
            EdgeId = src.EdgeId;
        }
    }
}
