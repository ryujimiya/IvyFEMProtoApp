using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;

namespace IvyFEMProtoApp
{
    /// <summary>
    /// 辺の集合(1つのポート境界に対応)
    /// </summary>
    class EdgeCollection : IComparable<EdgeCollection>
    {
        /// <summary>
        /// 辺の集合の番号
        /// </summary>
        public int No
        {
            get;
            set;
        }

        /// <summary>
        ///  Cadオブジェクトの辺のID
        /// </summary>
        public IList<uint> EdgeIds
        {
            get;
            private set;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public EdgeCollection()
        {
            init();
        }

        private void init()
        {
            No = 0;
            EdgeIds = new List<uint>();
        }

        /// <summary>
        /// コピー
        /// </summary>
        /// <param name="src"></param>
        public void CP(EdgeCollection src)
        {
            if (this == src)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }
            //System.Diagnostics.WriteLine("    CP");
            //System.Diagnostics.WriteLine("        prev. No:{0}, cnt:{1}", No, EdgeIds.Count);
            //System.Diagnostics.WriteLine("        src. No:{0}, cnt:{1}", src.No, src.EdgeIds.Count);
            init();

            No = src.No;
            EdgeIds.Clear();
            foreach (uint eId in src.EdgeIds)
            {
                EdgeIds.Add(eId);
            }
            //System.Diagnostics.WriteLine("        set. No:{0}, cnt:{1}", No, EdgeIds.Count);
            //System.Diagnostics.WriteLine("    CP end");
        }

        /// <summary>
        /// 辺番号比較
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(EdgeCollection other)
        {
            int diff = this.No - other.No;
            return diff;
        }

        /// <summary>
        /// 空?
        /// 辺IDのリストが空の場合空とみなす
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return (EdgeIds.Count == 0);
        }

        /// <summary>
        /// 辺IDがこの境界に含まれる?
        /// </summary>
        /// <param name="eId"></param>
        /// <returns></returns>
        public bool ContainsEdgeId(uint eId)
        {
            if (IsEmpty())
            {
                return false;
            }
            return EdgeIds.IndexOf(eId) >= 0;
        }

        /// <summary>
        /// 辺IDを追加する
        /// </summary>
        /// <param name="eId"></param>
        /// <returns></returns>
        public bool AddEdgeId(uint eId, CadObject2D cad2d, bool chkFlg = true)
        {
            bool success = false;

            //System.Diagnostics.WriteLine("addEdgeId");
            // 重複登録チェック
            if (EdgeIds.IndexOf(eId) >= 0)
            {
                return success;
            }

            // 先ず追加
            EdgeIds.Add(eId);
            //System.Diagnostics.WriteLine("eId Added. EdgeCollection No:{0}, eId:{1} cnt:{2}", No, EdgeIds[EdgeIds.Count - 1], EdgeIds.Count);

            if (chkFlg)
            {
                // ソートする
                success = SortEdgeIds(cad2d);
                if (!success)
                {
                    // ソートできなかったら辺が連続でないということ
                    EdgeIds.Remove(eId);
                }
            }
            else
            {
                success = true;
            }

            //System.Diagnostics.Debug.WriteLine("addEdgeId end");
            return success;
        }

        /// <summary>
        /// 辺IDをすべてクリアする
        /// </summary>
        public void ClearEdges()
        {
            EdgeIds.Clear();
        }

        /// <summary>
        /// 辺IDを削除する
        /// </summary>
        /// <param name="eId"></param>
        /// <returns></returns>
        public bool RemoveEdgeId(uint eId)
        {
            bool success = false;

            // 辺IDがこの境界に含まれるかチェック
            if (!ContainsEdgeId(eId))
            {
                return success;
            }

            // TODO:
            // 辺の連続性が失われないかチェックする必要あり

            // 削除
            EdgeIds.Remove(eId);

            success = true;
            return success;
        }

        public bool SortEdgeIds(CadObject2D cad2d)
        {
            bool success = false;
            if (EdgeIds.Count == 0 || EdgeIds.Count == 1)
            {
                // 何もしない
                success = true;
                return success;
            }
            //System.Diagnostics.WriteLine("=========old========");
            // チェック用に退避する
            IList<uint> oldEIdList = new List<uint>();
            foreach (uint eId in EdgeIds)
            {
                oldEIdList.Add(eId);
                //System.Diagnostics.WriteLine("{0}", eId);
            }
            //System.Diagnostics.WriteLine("=================");

            IList<uint> eIdList = new List<uint>();
            eIdList.Add(oldEIdList[0]);
            oldEIdList.Remove(oldEIdList[0]);
            while (oldEIdList.Count > 0)
            {
                uint workEId = eIdList[eIdList.Count - 1]; // 最後を参照
                uint id_v1 = 0;
                uint id_v2 = 0;
                CadDesign.getVertexIdsOfEdgeId(cad2d, workEId, out id_v1, out id_v2);
                uint nextdoor_eId = 0;
                foreach (uint chkEId in oldEIdList)
                {
                    uint chk_id_v1 = 0;
                    uint chk_id_v2 = 0;
                    CadDesign.getVertexIdsOfEdgeId(cad2d, chkEId, out chk_id_v1, out chk_id_v2);
                    // 隣の辺かチェック
                    if (id_v1 == chk_id_v1 || id_v1 == chk_id_v2
                        || id_v2 == chk_id_v1 || id_v2 == chk_id_v2)
                    {
                        nextdoor_eId = chkEId;
                        break;
                    }
                }
                if (nextdoor_eId != 0)
                {
                    eIdList.Add(nextdoor_eId); // 最後に追加
                    oldEIdList.Remove(nextdoor_eId);
                }
                else
                {
                    break;
                }
            }
            while (oldEIdList.Count > 0)
            {
                uint workEId = eIdList[0];// 先頭を参照
                uint id_v1 = 0;
                uint id_v2 = 0;
                CadDesign.getVertexIdsOfEdgeId(cad2d, workEId, out id_v1, out id_v2);
                uint nextdoor_eId = 0;
                foreach (uint chkEId in oldEIdList)
                {
                    uint chk_id_v1 = 0;
                    uint chk_id_v2 = 0;
                    CadDesign.getVertexIdsOfEdgeId(cad2d, chkEId, out chk_id_v1, out chk_id_v2);
                    // 隣の辺かチェック
                    if (id_v1 == chk_id_v1 || id_v1 == chk_id_v2
                        || id_v2 == chk_id_v1 || id_v2 == chk_id_v2)
                    {
                        nextdoor_eId = chkEId;
                        break;
                    }
                }
                if (nextdoor_eId != 0)
                {
                    eIdList.Insert(0, nextdoor_eId); // 先頭に追加
                    oldEIdList.Remove(nextdoor_eId);
                }
                else
                {
                    break;
                }
            }

            //System.Diagnostics.Debug.Assert(oldEIdList.Count == 0);
            if (oldEIdList.Count != 0)
            {
                // ソート失敗
                return success;
            }

            // ソート成功
            success = true;
            EdgeIds.Clear();
            //System.Diagnostics.WriteLine("=========new========");
            foreach (uint eId in eIdList)
            {
                EdgeIds.Add(eId);
                //System.Diagnostics.WriteLine("{0}", eId);
            }
            //System.Diagnostics.WriteLine("=================");
            return success;
        }


    }
}
