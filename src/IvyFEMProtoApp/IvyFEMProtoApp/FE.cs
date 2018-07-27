using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class FE : IObject
    {
        internal FEWorld World { get; set; }
        public ElementType Type { get; protected set; } = ElementType.NOT_SET;
        public uint NodeCount { get; protected set; } = 0;
        public int[] CoordIds { get; protected set; } = null;
        public uint MaterialId { get; set; } = 0;
        public uint MeshId { get; set; } = 0;
        public int MeshElemId { get; set; } = -1;

        public FE()
        {

        }

        public FE(FE src)
        {
            Copy(src);
        }

        public virtual void Copy(IObject src)
        {
            FE srcFE = src as FE;

            World = srcFE.World; // shallow copy
            Type = srcFE.Type;
            NodeCount = srcFE.NodeCount;
            MaterialId = srcFE.MaterialId;
            MeshId = srcFE.MeshId;
            MeshElemId = srcFE.MeshElemId;
        }

        public void SetCoordIndexs(int[] coordIndexs)
        {
            System.Diagnostics.Debug.Assert(NodeCount == coordIndexs.Length);
            CoordIds = new int[NodeCount];
            for (int iNode = 0; iNode < NodeCount; iNode++)
            {
                CoordIds[iNode] = coordIndexs[iNode];
            }
        }
    }
}
