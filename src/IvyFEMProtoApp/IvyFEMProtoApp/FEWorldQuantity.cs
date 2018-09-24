using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class FEWorldQuantity
    {
        public uint Id { get; set; } = 0;
        public uint Dimension { get; set; } = 0;
        public uint Dof { get; set; } = 1;
        public uint FEOrder { get; set; } = 1;
        internal IList<double> Coords = new List<double>();
        internal IList<Dictionary<int, int>> PortCo2Nodes = new List<Dictionary<int, int>>();
        internal IList<Dictionary<int, int>> PortNode2Cos = new List<Dictionary<int, int>>();
        internal IList<IList<uint>> PortLineFEIdss = new List<IList<uint>>();
        internal Dictionary<int, int> Co2Node = new Dictionary<int, int>();
        internal Dictionary<int, int> Node2Co = new Dictionary<int, int>();
        internal Dictionary<string, uint> Mesh2LineFE = new Dictionary<string, uint>();
        internal Dictionary<string, uint> Mesh2TriangleFE = new Dictionary<string, uint>();
        internal ObjectArray<LineFE> LineFEArray = new ObjectArray<LineFE>();
        internal ObjectArray<TriangleFE> TriangleFEArray = new ObjectArray<TriangleFE>();

        public FEWorldQuantity(uint id, uint dimension, uint dof, uint feOrder)
        {
            Id = id;
            Dimension = dimension;
            Dof = dof;
            FEOrder = feOrder;
        }

        public void ClearElements()
        {
            Coords.Clear();
            foreach (var portCo2Node in PortCo2Nodes)
            {
                portCo2Node.Clear();
            }
            PortCo2Nodes.Clear();
            foreach (var portNode2Co in PortNode2Cos)
            {
                portNode2Co.Clear();
            }
            PortNode2Cos.Clear();
            Co2Node.Clear();
            Node2Co.Clear();
            Mesh2LineFE.Clear();
            Mesh2TriangleFE.Clear();
            LineFEArray.Clear();
            foreach (var portLineFEIds in PortLineFEIdss)
            {
                portLineFEIds.Clear();
            }
            PortLineFEIdss.Clear();
            TriangleFEArray.Clear();
        }

        internal uint GetCoordCount()
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
            if (!Node2Co.ContainsKey(nodeId))
            {
                return -1;
            }
            return Node2Co[nodeId];
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
            var portNode2Co = PortNode2Cos[(int)portId];
            if (!portNode2Co.ContainsKey(nodeId))
            {
                return -1;
            }
            return portNode2Co[nodeId];
        }

        public IList<int> GetCoordIdsFromCadId(FEWorld world, uint cadId, CadElementType cadElemType)
        {
            Mesher2D mesh = world.Mesh;
            IList<int> coIds = null;
            if (cadElemType == CadElementType.Vertex)
            {
                uint meshId = mesh.GetIdFromCadId(cadId, cadElemType);
                uint elemCnt;
                MeshType meshType;
                int loc;
                uint cadIdTmp;
                mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadIdTmp);
                MeshType dummyMeshType;
                int[] vertexs;
                mesh.GetConnectivity(meshId, out dummyMeshType, out vertexs);
                System.Diagnostics.Debug.Assert(meshType == dummyMeshType);

                coIds = vertexs.ToList();
            }
            else if (cadElemType == CadElementType.Edge)
            {
                coIds = new List<int>();
                IList<uint> feIds = LineFEArray.GetObjectIds();
                foreach (uint feId in feIds)
                {
                    LineFE lineFE = LineFEArray.GetObject(feId);
                    uint cadIdTmp;
                    {
                        uint meshId = lineFE.MeshId;
                        uint elemCnt;
                        MeshType meshType;
                        int loc;
                        mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadIdTmp);
                        System.Diagnostics.Debug.Assert(meshType == MeshType.Bar);
                    }
                    if (cadIdTmp == cadId)
                    {
                        foreach (int coId in lineFE.NodeCoordIds)
                        {
                            if (coIds.IndexOf(coId) == -1)
                            {
                                coIds.Add(coId);
                            }
                        }
                    }
                }
            }
            else if (cadElemType == CadElementType.Loop)
            {
                coIds = new List<int>();
                IList<uint> feIds = TriangleFEArray.GetObjectIds();
                foreach (uint feId in feIds)
                {
                    TriangleFE triFE = TriangleFEArray.GetObject(feId);
                    uint cadIdTmp;
                    {
                        uint meshId = triFE.MeshId;
                        uint elemCnt;
                        MeshType meshType;
                        int loc;
                        mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadIdTmp);
                        System.Diagnostics.Debug.Assert(meshType == MeshType.Tri);
                    }
                    if (cadIdTmp == cadId)
                    {
                        foreach (int coId in triFE.NodeCoordIds)
                        {
                            if (coIds.IndexOf(coId) == -1)
                            {
                                coIds.Add(coId);
                            }
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }

            return coIds;
        }

        public uint GetLineFEIdFromMesh(uint meshId, uint iElem)
        {
            string key = meshId + "_" + iElem;
            if (Mesh2LineFE.ContainsKey(key))
            {
                uint feId = Mesh2LineFE[key];
                return feId;
            }
            return 0;
        }

        public uint GetTriangleFEIdFromMesh(uint meshId, uint iElem)
        {
            string key = meshId + "_" + iElem;
            if (Mesh2TriangleFE.ContainsKey(key))
            {
                uint feId = Mesh2TriangleFE[key];
                return feId;
            }
            return 0;
        }

        public IList<uint> GetLineFEIds()
        {
            return LineFEArray.GetObjectIds();
        }

        public LineFE GetLineFE(uint feId)
        {
            System.Diagnostics.Debug.Assert(LineFEArray.IsObjectId(feId));
            return LineFEArray.GetObject(feId);
        }

        public IList<uint> GetPortLineFEIds(uint portId)
        {
            System.Diagnostics.Debug.Assert(portId < PortLineFEIdss.Count);
            return PortLineFEIdss[(int)portId];
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

        public void MakeElements(FEWorld world)
        {
            ClearElements();

            Mesher2D mesh = world.Mesh;

            System.Diagnostics.Debug.Assert(mesh != null);

            IList<double> vertexCoords = world.VertexCoords;
            if (FEOrder == 1)
            {
                Coords = new List<double>(vertexCoords);
            }
            else if (FEOrder == 2)
            {
                Coords = new List<double>(vertexCoords);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }

            IList<uint> meshIds = mesh.GetIds();

            //////////////////////////////////////////////////
            // 領域の三角形要素
            // まず要素を作る
            // この順番で生成した要素は隣接していない
            IList<TriangleFE> triFEs = new List<TriangleFE>();
            Dictionary<string, IList<int>> edge2MidPt = new Dictionary<string, IList<int>>();
            foreach (uint meshId in meshIds)
            {
                uint elemCnt;
                MeshType meshType;
                int loc;
                uint cadId;
                mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadId);
                if (meshType != MeshType.Tri)
                {
                    continue;
                }

                if (!world.CadLoop2Material.ContainsKey(cadId))
                {
                    throw new IndexOutOfRangeException();
                }
                uint maId = world.CadLoop2Material[cadId];

                int elemVertexCnt = 3;
                int elemNodeCnt = 0;
                if (FEOrder == 1)
                {
                    elemNodeCnt = 3;
                }
                else if (FEOrder == 2)
                {
                    elemNodeCnt = 6;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                MeshType dummyMeshType;
                int[] vertexs;
                mesh.GetConnectivity(meshId, out dummyMeshType, out vertexs);
                System.Diagnostics.Debug.Assert(meshType == dummyMeshType);
                System.Diagnostics.Debug.Assert(elemVertexCnt * elemCnt == vertexs.Length);

                for (int iElem = 0; iElem < elemCnt; iElem++)
                {
                    int[] vertexCoIds = new int[elemVertexCnt];
                    for (int iPt = 0; iPt < elemVertexCnt; iPt++)
                    {
                        int coId = vertexs[iElem * elemVertexCnt + iPt];
                        vertexCoIds[iPt] = coId;
                    }
                    int[] nodeCoIds = new int[elemNodeCnt];
                    if (FEOrder == 1)
                    {
                        System.Diagnostics.Debug.Assert(nodeCoIds.Length == vertexCoIds.Length);
                        vertexCoIds.CopyTo(nodeCoIds, 0);
                    }
                    else if (FEOrder == 2)
                    {
                        for (int i = 0; i < elemVertexCnt; i++)
                        {
                            nodeCoIds[i] = vertexCoIds[i];

                            {
                                int v1 = vertexCoIds[i];
                                int v2 = vertexCoIds[(i + 1) % elemVertexCnt];
                                if (v1 > v2)
                                {
                                    int tmp = v1;
                                    v1 = v2;
                                    v2 = tmp;
                                }
                                string edgeKey = v1 + "_" + v2;
                                int midPtCoId = -1;
                                if (edge2MidPt.ContainsKey(edgeKey))
                                {
                                    midPtCoId = edge2MidPt[edgeKey][0];
                                }
                                else
                                {
                                    double[] vPt1 = world.GetVertexCoord(v1);
                                    double[] vPt2 = world.GetVertexCoord(v2);
                                    double[] midPt = { (vPt1[0] + vPt2[0]) / 2.0, (vPt1[1] + vPt2[1]) / 2.0 };
                                    midPtCoId = (int)(Coords.Count / Dimension);
                                    Coords.Add(midPt[0]);
                                    Coords.Add(midPt[1]);
                                    var list = new List<int>();
                                    list.Add(midPtCoId);
                                    edge2MidPt[edgeKey] = list;
                                }

                                nodeCoIds[i + elemVertexCnt] = midPtCoId;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    TriangleFE fe = new TriangleFE((int)FEOrder);
                    fe.World = world;
                    fe.SetVertexCoordIds(vertexCoIds);
                    fe.SetNodeCoordIds(nodeCoIds);
                    fe.MaterialId = maId;
                    fe.MeshId = meshId;
                    fe.MeshElemId = iElem;
                    triFEs.Add(fe);
                }
            }

            //////////////////////////////////////////////////
            // 境界の線要素
            foreach (uint meshId in meshIds)
            {
                uint elemCnt;
                MeshType meshType;
                int loc;
                uint cadId;
                mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadId);
                if (meshType != MeshType.Bar)
                {
                    continue;
                }

                int elemVertexCnt = 2;
                int elemNodeCnt = 0;
                if (FEOrder == 1)
                {
                    elemNodeCnt = 2;
                }
                else if (FEOrder == 2)
                {
                    elemNodeCnt = 3;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                MeshType dummyMeshType;
                int[] vertexs;
                mesh.GetConnectivity(meshId, out dummyMeshType, out vertexs);
                System.Diagnostics.Debug.Assert(meshType == dummyMeshType);
                System.Diagnostics.Debug.Assert(elemVertexCnt * elemCnt == vertexs.Length);

                //System.Diagnostics.Debug.Assert(CadEdge2Material.ContainsKey(cadId));
                //if (!CadEdge2Material.ContainsKey(cadId))
                //{
                //    throw new IndexOutOfRangeException();
                //}
                // 未指定のマテリアルも許容する
                uint maId = world.CadEdge2Material.ContainsKey(cadId) ? world.CadEdge2Material[cadId] : 0;

                for (int iElem = 0; iElem < elemCnt; iElem++)
                {
                    int[] vertexCoIds = new int[elemVertexCnt];
                    for (int iPt = 0; iPt < elemVertexCnt; iPt++)
                    {
                        int coId = vertexs[iElem * elemVertexCnt + iPt];
                        vertexCoIds[iPt] = coId;
                    }
                    int[] nodeCoIds = new int[elemNodeCnt];
                    if (FEOrder == 1)
                    {
                        System.Diagnostics.Debug.Assert(nodeCoIds.Length == vertexCoIds.Length);
                        vertexCoIds.CopyTo(nodeCoIds, 0);
                    }
                    else if (FEOrder == 2)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            nodeCoIds[i] = vertexCoIds[i];
                        }
                        // 線要素上の中点
                        int v1 = vertexCoIds[0];
                        int v2 = vertexCoIds[1];
                        if (v1 > v2)
                        {
                            int tmp = v1;
                            v1 = v2;
                            v2 = tmp;
                        }
                        string edgeKey = v1 + "_" + v2;
                        if (!edge2MidPt.ContainsKey(edgeKey))
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        int midPtCoId = edge2MidPt[edgeKey][0];
                        nodeCoIds[2] = midPtCoId;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    LineFE fe = new LineFE((int)FEOrder);
                    fe.World = world;
                    fe.SetVertexCoordIds(vertexCoIds);
                    fe.SetNodeCoordIds(nodeCoIds);
                    fe.MaterialId = maId;
                    fe.MeshId = meshId;
                    fe.MeshElemId = iElem;
                    uint freeId = LineFEArray.GetFreeObjectId();
                    uint feId = LineFEArray.AddObject(new KeyValuePair<uint, LineFE>(freeId, fe));
                    System.Diagnostics.Debug.Assert(feId == freeId);

                    string key = string.Format(meshId + "_" + iElem);
                    Mesh2LineFE.Add(key, feId);
                }
            }

            // Note: 線要素生成後でないと点を特定できない
            IList<int> zeroCoordIds = world.GetZeroCoordIds(Id);

            // ポート上の線要素の抽出と節点ナンバリング
            foreach (var portEIds in world.PortEIdss)
            {
                var lineFEIds = new List<uint>();
                PortLineFEIdss.Add(lineFEIds);

                var portCo2Node = new Dictionary<int, int>();
                PortCo2Nodes.Add(portCo2Node);
                int portNodeId = 0;

                IList<uint> feIds = LineFEArray.GetObjectIds();
                foreach (var feId in feIds)
                {
                    LineFE lineFE = LineFEArray.GetObject(feId);
                    uint cadId;
                    {
                        uint meshId = lineFE.MeshId;
                        uint elemCnt;
                        MeshType meshType;
                        int loc;
                        mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadId);
                        System.Diagnostics.Debug.Assert(meshType == MeshType.Bar);
                    }
                    if (portEIds.Contains(cadId))
                    {
                        // ポート上の線要素
                        lineFEIds.Add(feId);

                        int[] coIds = lineFE.NodeCoordIds;

                        foreach (int coId in coIds)
                        {
                            if (!portCo2Node.ContainsKey(coId) &&
                                zeroCoordIds.IndexOf(coId) == -1)
                            {
                                portCo2Node[coId] = portNodeId;
                                portNodeId++;
                            }
                        }
                    }
                }
            }

            ////////////////////////////////////////
            Dictionary<TriangleFE, IList<int>> triFECoIds = new Dictionary<TriangleFE, IList<int>>();
            IList<IList<int>> portCoIdss = new List<IList<int>>();
            uint portCnt = world.GetPortCount();
            for (uint portId = 0; portId < portCnt; portId++)
            {
                var portCoIds = new List<int>();
                portCoIdss.Add(portCoIds);
                IList<uint> feIds = GetPortLineFEIds(portId);
                foreach (uint feId in feIds)
                {
                    LineFE lineFE = GetLineFE(feId);
                    int[] vertexCoIds = lineFE.VertexCoordIds;
                    foreach (int coId in vertexCoIds)
                    {
                        if (!portCoIds.Contains(coId))
                        {
                            portCoIds.Add(coId);
                        }
                    }
                }
            }
            // 共有とみなす節点を生成
            foreach (TriangleFE fe in triFEs)
            {
                // 要素の節点
                IList<int> coIds = fe.VertexCoordIds.ToList();

                //（EMWaveguideの場合、ポート上の節点は隣りあわせの要素以外でも関係がある)
                bool isInclude = false;
                for (int portId = 0; portId < portCnt; portId++)
                {
                    var portCoIds = portCoIdss[portId];
                    foreach (int portCoId in portCoIds)
                    {
                        if (coIds.Contains(portCoId))
                        {
                            isInclude = true;
                            break;
                        }
                    }
                    if (isInclude)
                    {
                        foreach (int portCoId in portCoIds)
                        {
                            coIds.Add(portCoId);
                        }
                    }
                }
                triFECoIds.Add(fe, coIds);
            }
            // 節点を共有している要素を探す
            IList<TriangleFE> sortedTriFEs = new List<TriangleFE>();
            IList<TriangleFE> queueTriFE = new List<TriangleFE>();
            while (triFEs.Count > 0 || queueTriFE.Count > 0)
            {
                TriangleFE checkFE = null;
                if (queueTriFE.Count == 0 && triFEs.Count > 0)
                {
                    checkFE = triFEs[0];
                    triFEs.Remove(checkFE);
                    System.Diagnostics.Debug.Assert(!sortedTriFEs.Contains(checkFE));
                    sortedTriFEs.Add(checkFE);
                }
                else if (queueTriFE.Count > 0)
                {
                    checkFE = queueTriFE[0];
                    queueTriFE.RemoveAt(0);
                    System.Diagnostics.Debug.Assert(!sortedTriFEs.Contains(checkFE));
                    sortedTriFEs.Add(checkFE);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                var includeFEs = GetCoordIncludeTriFEs(checkFE, triFEs, triFECoIds);
                foreach (TriangleFE includeFE in includeFEs)
                {
                    if (!sortedTriFEs.Contains(includeFE) &&
                        !queueTriFE.Contains(includeFE))
                    {
                        queueTriFE.Add(includeFE);
                        triFEs.Remove(includeFE);
                    }
                }
            }

            // ナンバリング
            int nodeId = 0;
            foreach (TriangleFE fe in sortedTriFEs)
            {
                int elemPtCnt = fe.NodeCoordIds.Length;
                int[] coIds = fe.NodeCoordIds;
                for (int iPt = 0; iPt < elemPtCnt; iPt++)
                {
                    int coId = coIds[iPt];
                    if (!Co2Node.ContainsKey(coId) &&
                        zeroCoordIds.IndexOf(coId) == -1)
                    {
                        Co2Node[coId] = nodeId;
                        nodeId++;
                    }
                }

                uint freeId = TriangleFEArray.GetFreeObjectId();
                uint feId = TriangleFEArray.AddObject(new KeyValuePair<uint, TriangleFE>(freeId, fe));
                System.Diagnostics.Debug.Assert(feId == freeId);

                uint meshId = fe.MeshId;
                int iElem = fe.MeshElemId;
                uint elemCnt;
                MeshType meshType;
                int loc;
                uint cadId;
                mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadId);
                System.Diagnostics.Debug.Assert(meshType == MeshType.Tri);
                var triArray = mesh.GetTriArrays();
                var tri = triArray[loc].Tris[iElem];
                tri.FEId = (int)feId;

                string key = string.Format(meshId + "_" + iElem);
                Mesh2TriangleFE.Add(key, feId);
            }

            //////////////////////////////////////////////////////
            // 逆参照
            foreach (var portCo2Node in PortCo2Nodes)
            {
                var portNode2Co = new Dictionary<int, int>();
                PortNode2Cos.Add(portNode2Co);
                foreach (var pair in portCo2Node)
                {
                    int tmpPortCoId = pair.Key;
                    int tmpPortNodeId = pair.Value;
                    portNode2Co[tmpPortNodeId] = tmpPortCoId;
                }
            }
            foreach (var pair in Co2Node)
            {
                int tmpCoId = pair.Key;
                int tmpNodeId = pair.Value;
                Node2Co[tmpNodeId] = tmpCoId;
            }
        }

        private IList<TriangleFE> GetCoordIncludeTriFEs(TriangleFE fe, IList<TriangleFE> triFEs,
            Dictionary<TriangleFE, IList<int>> triFECoIds)
        {
            IList<int> coIds = triFECoIds[fe];
            IList<TriangleFE> includeTriFEs = new List<TriangleFE>();
            foreach (TriangleFE tmpFE in triFEs)
            {
                bool isInclude = false;
                IList<int> tmpCoIds = triFECoIds[tmpFE];
                foreach (int tmpCoId in tmpCoIds)
                {
                    foreach (int coId in coIds)
                    {
                        if (tmpCoId == coId)
                        {
                            isInclude = true;
                            break;
                        }
                    }
                }
                if (isInclude)
                {
                    includeTriFEs.Add(tmpFE);
                }
            }
            return includeTriFEs;
        }

        public IList<LineFE> MakeBoundOfElements(FEWorld world)
        {
            IList<LineFE> boundOfTriangelFEs = new List<LineFE>();
            HashSet<string> edges = new HashSet<string>();

            var feIds = GetTriangleFEIds();
            foreach (uint feId in feIds)
            {
                TriangleFE triFE = GetTriangleFE(feId);
                System.Diagnostics.Debug.Assert(triFE.Order == FEOrder);
                int[][] vertexCoIds =
                {
                    new int[] { triFE.VertexCoordIds[0], triFE.VertexCoordIds[1] },
                    new int[] { triFE.VertexCoordIds[1], triFE.VertexCoordIds[2] },
                    new int[] { triFE.VertexCoordIds[2], triFE.VertexCoordIds[0] }
                };
                int[][] nodeCoIds = null;
                if (triFE.Order == 1)
                {
                    int[][] nodeCoIds1 =
                    {
                    new int[] { triFE.NodeCoordIds[0], triFE.NodeCoordIds[1] },
                    new int[] { triFE.NodeCoordIds[1], triFE.NodeCoordIds[2] },
                    new int[] { triFE.NodeCoordIds[2], triFE.NodeCoordIds[0] }
                    };
                    nodeCoIds = nodeCoIds1;
                }
                else if (triFE.Order == 2)
                {
                    int[][] nodeCoIds2 =
                    {
                    new int[] { triFE.NodeCoordIds[0], triFE.NodeCoordIds[1], triFE.NodeCoordIds[3] },
                    new int[] { triFE.NodeCoordIds[1], triFE.NodeCoordIds[2], triFE.NodeCoordIds[4] },
                    new int[] { triFE.NodeCoordIds[2], triFE.NodeCoordIds[0], triFE.NodeCoordIds[5] }
                    };
                    nodeCoIds = nodeCoIds2;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                for (int iEdge = 0; iEdge < 3; iEdge++)
                {
                    int v1 = vertexCoIds[iEdge][0];
                    int v2 = vertexCoIds[iEdge][1];
                    if (v1 > v2)
                    {
                        int tmp = v1;
                        v1 = v2;
                        v2 = tmp;
                    }
                    string edgeKey = v1 + "_" + v2;
                    if (edges.Contains(edgeKey))
                    {
                        continue;
                    }
                    else
                    {
                        edges.Add(edgeKey);
                    }
                    var lineFE = new LineFE((int)FEOrder);
                    lineFE.World = world;
                    lineFE.SetVertexCoordIds(vertexCoIds[iEdge]);
                    lineFE.SetNodeCoordIds(nodeCoIds[iEdge]);
                    // MeshId等は対応するものがないのでセットしない
                    boundOfTriangelFEs.Add(lineFE);
                }
            }
            return boundOfTriangelFEs;
        }
    }
}
