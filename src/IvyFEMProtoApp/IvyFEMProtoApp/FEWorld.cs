using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class FEWorld
    {
        // 当面2D固定
        public Mesher2D Mesh { get; set; } = null;
        public uint Dimension { get; private set; } = 2;
        private IList<double> Coords = new List<double>();
        private ObjectArray<Material> MaterialArray = new ObjectArray<Material>();
        private Dictionary<uint, uint> CadEdge2Material = new Dictionary<uint, uint>();
        private Dictionary<uint, uint> CadLoop2Material = new Dictionary<uint, uint>();

        public IList<uint> ZeroECadIds { get; private set; } = new List<uint>();
        public IList<FieldFixedCad> FieldFixedCads { get; private set; } = new List<FieldFixedCad>();
        public int IncidentPotId { get; set; } = -1;
        public int IncidentModeId { get; set; } = -1;
        public IList<IList<uint>> PortEIdss { get; } = new List<IList<uint>>();
        private IList<Dictionary<int, int>> PortCo2Nodes = new List<Dictionary<int, int>>();

        private Dictionary<int, int> Co2Node = new Dictionary<int, int>();
        private IList<ObjectArray<LineFE>> PortLineFEArrays = new List<ObjectArray<LineFE>>();
        private ObjectArray<TriangleFE> TriangleFEArray = new ObjectArray<TriangleFE>();
        private ObjectArray<FieldValue> FieldValueArray = new ObjectArray<FieldValue>();

        public FEWorld()
        {

        }

        public void Clear()
        {
            Mesh = null;
            Coords.Clear();
            MaterialArray.Clear();
            CadEdge2Material.Clear();
            CadLoop2Material.Clear();
            ZeroECadIds.Clear();
            IncidentPotId = -1;
            IncidentModeId = -1;
            foreach (var portEIds in PortEIdss)
            {
                portEIds.Clear();
            }
            PortEIdss.Clear();
            FieldValueArray.Clear();

            ClearElements();
        }

        private void ClearElements()
        {
            foreach (var portCo2Node in PortCo2Nodes)
            {
                portCo2Node.Clear();
            }
            PortCo2Nodes.Clear();
            Co2Node.Clear();
            foreach (var lineFEArray in PortLineFEArrays)
            {
                lineFEArray.Clear();
            }
            PortLineFEArrays.Clear();
            TriangleFEArray.Clear();
        }

        public uint GetCoordCount()
        {
            return (uint)Coords.Count / Dimension;
        }

        public double[] GetCoord(int coId)
        {
            System.Diagnostics.Debug.Assert(coId * Dimension + (Dimension - 1) < Coords.Count);
            double[] coord = new double[Dimension];
            for (int iDim = 0; iDim < Dimension; iDim++)
            {
                coord[iDim] = Coords[(int)(coId * Dimension + iDim)];
            }
            return coord;
        }

        public uint GetNodeCount()
        {
            return (uint)Co2Node.Count;
        }

        public int Coord2Node(int coId)
        {
            if (!Co2Node.ContainsKey(coId))
            {
                return -1;
            }
            return Co2Node[coId];
        }

        public int Node2Coord(int nodeId)
        {
            var coId = (
                from pair in Co2Node
                where pair.Value == nodeId
                select pair.Key).First();
            return coId;
        }

        public uint GetPortCount()
        {
            return (uint)PortEIdss.Count;
        }

        public uint GetPortNodeCount(uint portId)
        {
            System.Diagnostics.Debug.Assert(portId < PortCo2Nodes.Count);
            return (uint)PortCo2Nodes[(int)portId].Count;
        }

        public int PortCoord2Node(uint portId, int coId)
        {
            var portCo2Node = PortCo2Nodes[(int)portId];
            if (!portCo2Node.ContainsKey(coId))
            {
                return -1;
            }
            return portCo2Node[coId];
        }

        public int PortNode2Coord(uint portId, int nodeId)
        {
            var portCo2Node = PortCo2Nodes[(int)portId];
            var coId = (
                from pair in portCo2Node
                where pair.Value == nodeId
                select pair.Key).First();
            return coId;
        }

        public IList<uint> GetMaterialIds()
        {
            return MaterialArray.GetObjectIds();
        }

        public Material GetMaterial(uint maId)
        {
            System.Diagnostics.Debug.Assert(MaterialArray.IsObjectId(maId));
            return MaterialArray.GetObject(maId);
        }

        public uint AddMaterial(Material material)
        {
            uint freeId = MaterialArray.GetFreeObjectId();
            uint maId = MaterialArray.AddObject(new KeyValuePair<uint, Material>(freeId, material));
            System.Diagnostics.Debug.Assert(maId == freeId);
            return maId;
        }

        public void ClearMaterial()
        {
            MaterialArray.Clear();
        }

        public void SetCadEdgeMaterial(uint eCadId, uint maId)
        {
            CadEdge2Material.Add(eCadId, maId);
        }

        public uint GetCadEdgeMaterial(uint eCadId)
        {
            System.Diagnostics.Debug.Assert(CadEdge2Material.ContainsKey(eCadId));
            uint maId = CadEdge2Material[eCadId];
            return maId;
        }

        public void ClearCadEdgeMaterial()
        {
            CadEdge2Material.Clear();
        }

        public void SetCadLoopMaterial(uint lCadId, uint maId)
        {
            CadLoop2Material.Add(lCadId, maId);
        }

        public uint GetCadLoopMaterial(uint lCadId)
        {
            System.Diagnostics.Debug.Assert(CadLoop2Material.ContainsKey(lCadId));
            uint maId = CadLoop2Material[lCadId];
            return maId;
        }

        public void ClearCadLoopMaterial()
        {
            CadLoop2Material.Clear();
        }

        public IList<int> GetCoordIdFromCadId(uint cadId, CadElementType cadElemType)
        {
            // TODO: 要素の節点数を変えた場合(高次要素)に対応していない
            uint meshId = Mesh.GetIdFromCadId(cadId, cadElemType);
            uint elemCnt;
            MeshType meshType;
            int loc;
            uint cadIdTmp;
            Mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadIdTmp);
            MeshType dummyMeshType;
            int[] vertexs;
            Mesh.GetConnectivity(meshId, out dummyMeshType, out vertexs);
            System.Diagnostics.Debug.Assert(meshType == dummyMeshType);

            return vertexs.ToList();
        }

        public Dictionary<int, IList<FieldFixedCad>> GetFixedCoordIdFixedCad()
        {
            var fixedCoIdFixedCad = new Dictionary<int, IList<FieldFixedCad>>();
            foreach (var fixedCad in FieldFixedCads)
            {
                IList<int> coIds = fixedCad.GetCoordIds(this);
                foreach (int coId in coIds)
                {
                    IList<FieldFixedCad> fixedCads = null;
                    if (!fixedCoIdFixedCad.ContainsKey(coId))
                    {
                        fixedCads = new List<FieldFixedCad>();
                        fixedCoIdFixedCad[coId] = fixedCads;
                    }
                    else
                    {
                        fixedCads = fixedCoIdFixedCad[coId];
                    }
                    if (fixedCads.IndexOf(fixedCad) == -1)
                    {
                        fixedCads.Add(fixedCad);
                    }
                }
            }
            return fixedCoIdFixedCad;
        }

        public IList<uint> GetPortLineFEIds(uint portId)
        {
            System.Diagnostics.Debug.Assert(portId < PortLineFEArrays.Count);
            ObjectArray<LineFE> portLineFEArray = PortLineFEArrays[(int)portId];
            return portLineFEArray.GetObjectIds();
        }
        public LineFE GetPortLineFE(uint portId, uint feId)
        {
            System.Diagnostics.Debug.Assert(portId < PortLineFEArrays.Count);
            ObjectArray<LineFE> portLineFEArray = PortLineFEArrays[(int)portId];
            System.Diagnostics.Debug.Assert(portLineFEArray.IsObjectId(feId));
            return portLineFEArray.GetObject(feId);
        }

        public IList<uint> GetTriangleFEIds()
        {
            return TriangleFEArray.GetObjectIds();
        }

        public TriangleFE GetTriangleFE(uint feId)
        {
            System.Diagnostics.Debug.Assert(TriangleFEArray.IsObjectId(feId));
            return TriangleFEArray.GetObject(feId);
        }

        public void MakeElements()
        {
            ClearElements();

            System.Diagnostics.Debug.Assert(Mesh != null);

            Mesh.GetCoords(out Coords);

            IList<uint> meshIds = Mesh.GetIds();
            IList<int> zeroCoordIds = GetZeroCoordIds(meshIds);

            int nodeId = 0;

            foreach (var portEIds in PortEIdss)
            {
                var lineFEArray = new ObjectArray<LineFE>();
                PortLineFEArrays.Add(lineFEArray);

                var portCo2Node = new Dictionary<int, int>();
                PortCo2Nodes.Add(portCo2Node);
                int portNodeId = 0;

                // ポート上のバー要素
                foreach (uint meshId in meshIds)
                {
                    uint elemCnt;
                    MeshType meshType;
                    int loc;
                    uint cadId;
                    Mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadId);
                    if (meshType != MeshType.BAR)
                    {
                        continue;
                    }

                    const int elemPtCnt = 2;
                    MeshType dummyMeshType;
                    int[] vertexs;
                    Mesh.GetConnectivity(meshId, out dummyMeshType, out vertexs);
                    System.Diagnostics.Debug.Assert(meshType == dummyMeshType);
                    System.Diagnostics.Debug.Assert(elemPtCnt * elemCnt == vertexs.Length);

                    if (portEIds.Contains(cadId))
                    {
                        System.Diagnostics.Debug.Assert(CadEdge2Material.ContainsKey(cadId));
                        if (!CadEdge2Material.ContainsKey(cadId))
                        {
                            throw new IndexOutOfRangeException();
                        }
                        uint maId = CadEdge2Material[cadId];

                        for (int iElem = 0; iElem < elemCnt; iElem++)
                        {
                            int[] coIds = new int[elemPtCnt];
                            for (int iPt = 0; iPt < elemPtCnt; iPt++)
                            {
                                int coId = vertexs[iElem * elemPtCnt + iPt];
                                coIds[iPt] = coId;
                                if (!Co2Node.ContainsKey(coId) &&
                                    zeroCoordIds.IndexOf(coId) == -1)
                                {
                                    Co2Node[coId] = nodeId;
                                    portCo2Node[coId] = portNodeId;
                                    nodeId++;
                                    portNodeId++;
                                }
                            }

                            LineFE fe = new LineFE();
                            fe.World = this;
                            fe.SetCoordIndexs(coIds);
                            fe.MaterialId = maId;
                            fe.MeshId = meshId;
                            fe.MeshElemId = iElem;
                            uint freeId = lineFEArray.GetFreeObjectId();
                            uint feId = lineFEArray.AddObject(new KeyValuePair<uint, LineFE>(freeId, fe));
                            System.Diagnostics.Debug.Assert(feId == freeId);
                        }
                    }
                }
            }

            // 領域の三角形要素
            foreach (uint meshId in meshIds)
            {
                uint elemCnt;
                MeshType meshType;
                int loc;
                uint cadId;
                Mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadId);
                if (meshType != MeshType.TRI)
                {
                    continue;
                }

                if (!CadLoop2Material.ContainsKey(cadId))
                {
                    throw new IndexOutOfRangeException();
                }
                uint maId = CadLoop2Material[cadId];

                const int elemPtCnt = 3;
                MeshType dummyMeshType;
                int[] vertexs;
                Mesh.GetConnectivity(meshId, out dummyMeshType, out vertexs);
                System.Diagnostics.Debug.Assert(meshType == dummyMeshType);
                System.Diagnostics.Debug.Assert(elemPtCnt * elemCnt == vertexs.Length);

                for (int iElem = 0; iElem < elemCnt; iElem++)
                {
                    int[] coIds = new int[elemPtCnt];
                    for (int iPt = 0; iPt < elemPtCnt; iPt++)
                    {
                        int coId = vertexs[iElem * elemPtCnt + iPt];
                        coIds[iPt] = coId;
                        if (!Co2Node.ContainsKey(coId) &&
                            zeroCoordIds.IndexOf(coId) == -1)
                        {
                            Co2Node[coId] = nodeId;
                            nodeId++;
                        }
                    }

                    TriangleFE fe = new TriangleFE();
                    fe.World = this;
                    fe.SetCoordIndexs(coIds);
                    fe.MaterialId = maId;
                    fe.MeshId = meshId;
                    fe.MeshElemId = iElem;
                    uint freeId = TriangleFEArray.GetFreeObjectId();
                    uint feId = TriangleFEArray.AddObject(new KeyValuePair<uint, TriangleFE>(freeId, fe));
                    System.Diagnostics.Debug.Assert(feId == freeId);

                    var triArray = Mesh.GetTriArrays();
                    var tri = triArray[loc].Tris[iElem];
                    tri.FEId = (int)feId;
                }
            }
        }

        private IList<int> GetZeroCoordIds(IList<uint> meshIds)
        {
            IList<int> zeroCoIds = new List<int>();

            foreach (uint eCadId in ZeroECadIds)
            {
                IList<int> coIds = GetCoordIdsFromECadId(meshIds, eCadId);
                foreach (int coId in coIds)
                {
                    zeroCoIds.Add(coId);
                }
            }
            return zeroCoIds;
        }

        private IList<int> GetCoordIdsFromECadId(IList<uint> meshIds, uint eCadId)
        {
            Dictionary<int, int> coIdSet = new Dictionary<int, int>();
            foreach (uint meshId in meshIds)
            {
                uint elemCnt;
                MeshType meshType;
                int loc;
                uint cadId;
                Mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadId);
                if (meshType != MeshType.BAR)
                {
                    continue;
                }
                if (cadId == eCadId)
                {
                    int elemPtCnt = 2;
                    MeshType dummyMeshType;
                    int[] vertexs;
                    Mesh.GetConnectivity(meshId, out dummyMeshType, out vertexs);
                    System.Diagnostics.Debug.Assert(meshType == dummyMeshType);
                    System.Diagnostics.Debug.Assert(elemPtCnt * elemCnt == vertexs.Length);
                    for (int iElem = 0; iElem < elemCnt; iElem++)
                    {
                        for (int iPt = 0; iPt < elemPtCnt; iPt++)
                        {
                            int coId = vertexs[iElem * elemPtCnt + iPt];
                            coIdSet[coId] = 1;
                        }
                    }

                }
            }
            return coIdSet.Keys.ToList();
        }

        public bool IsFieldValueId(uint valueId)
        {
            return FieldValueArray.IsObjectId(valueId);
        }

        public IList<uint> GetFieldValueIds()
        {
            return FieldValueArray.GetObjectIds();
        }

        public FieldValue GetFieldValue(uint valueId)
        {
            System.Diagnostics.Debug.Assert(FieldValueArray.IsObjectId(valueId));
            return FieldValueArray.GetObject(valueId);
        }

        public void ClearFieldValue()
        {
            FieldValueArray.Clear();
        }

        public uint AddFieldValue(FieldValueType fieldType, FieldDerivationType derivationType,
            FieldShowType showType, uint dof, 
            double[] values, double[] velocityValues, double[] accelerationValues)
        {
            FieldValue fv = new FieldValue();
            fv.Type = fieldType;
            fv.DerivationType = derivationType;
            fv.ShowType = showType;
            fv.Dof = dof;
            fv.Values = values;
            fv.VelocityValues = velocityValues;
            fv.AccelerationValues = accelerationValues;

            uint freeId = FieldValueArray.GetFreeObjectId();
            uint valueId = FieldValueArray.AddObject(new KeyValuePair<uint, FieldValue>(freeId, fv));
            System.Diagnostics.Debug.Assert(valueId == freeId);
            return valueId;
        }

        public IList<LineFE> MakeBoundOfElements()
        {
            IList<LineFE> boundOfTriangelFEs = new List<LineFE>();
            IList<KeyValuePair<int, int>> edges = new List<KeyValuePair<int, int>>();

            IList<uint> meshIds = Mesh.GetIds();

            // 三角形要素
            foreach (uint meshId in meshIds)
            {
                uint elemCnt;
                MeshType meshType;
                int loc;
                uint cadId;
                Mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadId);
                if (meshType != MeshType.TRI)
                {
                    continue;
                }

                const int elemPtCnt = 3;
                MeshType dummyMeshType;
                int[] vertexs;
                Mesh.GetConnectivity(meshId, out dummyMeshType, out vertexs);
                System.Diagnostics.Debug.Assert(meshType == dummyMeshType);
                System.Diagnostics.Debug.Assert(elemPtCnt * elemCnt == vertexs.Length);

                for (int iElem = 0; iElem < elemCnt; iElem++)
                {
                    var triArray = Mesh.GetTriArrays();
                    var tri = triArray[loc].Tris[iElem];
                    int feId = tri.FEId;
                    TriangleFE triFE = null;
                    if (feId == -1)
                    {

                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(TriangleFEArray.IsObjectId((uint)feId));
                        triFE = TriangleFEArray.GetObject((uint)feId);
                    }

                    int[] elemCoIds = new int[elemPtCnt];
                    for (int iPt = 0; iPt < elemPtCnt; iPt++)
                    {
                        elemCoIds[iPt] = vertexs[iElem * elemPtCnt + iPt];
                    }
                    for (int iEdge = 0; iEdge < MeshUtils.TriEdNo; iEdge++)
                    {
                        uint v1 = MeshUtils.TriElEdgeNo[iEdge][0];
                        uint v2 = MeshUtils.TriElEdgeNo[iEdge][1];
                        int[] coIds = new int[2];
                        coIds[0] = elemCoIds[v1];
                        coIds[1] = elemCoIds[v2];
                        Array.Sort(coIds, (a, b) => (a - b));
                        {
                            var l = (
                                from pair in edges
                                where pair.Key == coIds[0] && pair.Value == coIds[1]
                                select pair
                                ).ToList();
                            if (l.Count > 0)
                            {
                                System.Diagnostics.Debug.Assert(l.Count == 1);
                                continue;
                            }
                        }

                        var lineFE = new LineFE();
                        lineFE.World = this;
                        lineFE.SetCoordIndexs(coIds);
                        // MeshId等は対応するものがないのでセットしない
                        boundOfTriangelFEs.Add(lineFE);
                    }
                }
            }
            return boundOfTriangelFEs;
        }

    }
}
