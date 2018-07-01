using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    enum MshType
    {
        VERTEX,
        BAR,
        TRI,
        QUAD,
        TET,
        HEX
    }

    class Mesher2D
    {
        private HashSet<uint> CutMeshLCadIdSet = new HashSet<uint>();
        private uint MeshingMode;
        private double ELen;
        private uint ESize;

        private IList<int> ElemTypes = new List<int>();
        private IList<int> ElemLocs = new List<int>();
        private IList<IList<uint>> IncludeRelations = new List<IList<uint>>();

        private IList<Vertex> Vertexs = new List<Vertex>();
        private IList<BarArray> BarArrays = new List<BarArray>();
        private IList<TriArray2D> TriArrays = new List<TriArray2D>();
        private IList<QuadArray2D> QuadArrays = new List<QuadArray2D>();

        private IList<Vector2> Vec2Ds = new List<Vector2>();

        public Mesher2D()
        {
            MeshingMode = 1;
            ELen = 0.1;
            ESize = 1000;
        }

        public Mesher2D(CadObject2D cad2D) {
            MeshingMode = 0;
            ELen = 1;
            ESize = 1000;
            IList<uint> lIds = cad2D.GetElemIds(CadElemType.LOOP);
            for (uint i = 0; i < lIds.Count; i++)
            {
                CutMeshLCadIdSet.Add(lIds[(int)i]);
            }

            Meshing(cad2D);
        }

        public Mesher2D(Mesher2D src)
        {
            Clear();
            CutMeshLCadIdSet = new HashSet<uint>(src.CutMeshLCadIdSet);
            MeshingMode = src.MeshingMode;
            ELen = src.ELen;
            ESize = src.ESize;

            ElemTypes = new List<int>(src.ElemTypes);
            ElemLocs = new List<int>(src.ElemLocs);
            IncludeRelations = new List<IList<uint>>();
            for (int i = 0; i < src.IncludeRelations.Count; i++)
            {
                IncludeRelations.Add(new List<uint>(src.IncludeRelations[i]));
            }

            Vertexs = new List<Vertex>(src.Vertexs);
            BarArrays = new List<BarArray>(src.BarArrays);
            TriArrays = new List<TriArray2D>(src.TriArrays);
            QuadArrays = new List<QuadArray2D>(src.QuadArrays);

            Vec2Ds = new List<Vector2>(src.Vec2Ds);
        }

        public void Clear()
        {
            CutMeshLCadIdSet.Clear();
            MeshingMode = 1;
            ELen = 0.1;
            ESize = 1000;
            ClearMeshData();
        }

        private void ClearMeshData()
        {
            ElemTypes.Clear();
            ElemLocs.Clear();
            IncludeRelations.Clear();

            Vertexs.Clear();
            BarArrays.Clear();
            TriArrays.Clear();
            QuadArrays.Clear();

            Vec2Ds.Clear();
        }

        public IList<TriArray2D> GetTriArrays()
        {
            return TriArrays;
        }

        public IList<QuadArray2D> GetQuadArrays()
        {
            return QuadArrays;
        }

        public IList<BarArray> GetBarArrays()
        {
            return BarArrays;
        }

        public IList<Vertex> GetVertexs()
        {
            return Vertexs;
        }

        public IList<Vector2> GetVectors()
        {
            return Vec2Ds;
        }

        public bool IsId(uint id)
        {
            if (ElemLocs.Count <= id)
            {
                return false;
            }
            int loc = ElemLocs[(int)id];
            if (loc == -1)
            {
                return false;
            }

            System.Diagnostics.Debug.Assert(ElemTypes.Count > id);
            int type = ElemTypes[(int)id];
            System.Diagnostics.Debug.Assert(type >= 0);
            System.Diagnostics.Debug.Assert(loc >= 0);
            if (type == 0)
            {
                System.Diagnostics.Debug.Assert(Vertexs.Count > loc);
                System.Diagnostics.Debug.Assert(Vertexs[loc].Id == id);
            }
            else if (type == 1)
            {
                System.Diagnostics.Debug.Assert(BarArrays.Count > loc);
                System.Diagnostics.Debug.Assert(BarArrays[loc].Id == id);
            }
            else if (type == 2)
            {
                System.Diagnostics.Debug.Assert(TriArrays.Count > loc);
                System.Diagnostics.Debug.Assert(TriArrays[loc].Id == id);
            }
            else if (type == 3)
            {
                System.Diagnostics.Debug.Assert(QuadArrays.Count > loc);
                System.Diagnostics.Debug.Assert(QuadArrays[loc].Id == id);
            }
            return true;
        }

        public bool Meshing(CadObject2D cadD)
        {
            IList<uint> cutLIds = new List<uint>();
            {
                foreach (uint lId in CutMeshLCadIdSet)
                {
                    if (!cadD.IsElemId(CadElemType.LOOP, lId))
                    {
                        continue;
                    }
                    cutLIds.Add(lId);
                }
            }
            if (MeshingMode == 0)
            {
                return Tessellation(cadD, cutLIds);
            }
            else if (MeshingMode == 1)
            {
                // 後で実装する ryujimiya
                throw new NotImplementedException();
                //return Meshing_ElemSize(cad2D, ESize, cutLIds);
            }
            else if (MeshingMode == 2)
            {
                // 後で実装する ryujimiya
                throw new NotImplementedException();
                //return Meshing_ElemLength(cad2D, ELen, cutLIds);
            }
            return false;
        }

        private bool Tessellation(CadObject2D cad2D, IList<uint> loopIds)
        {
            {
                IList<uint> vIds = cad2D.GetElemIds(CadElemType.VERTEX);
                for (uint iv = 0; iv < vIds.Count; iv++)
                {
                    uint vId = vIds[(int)iv];
                    System.Diagnostics.Debug.Assert(GetElemIdFromCadId(vId, CadElemType.VERTEX) == 0);
                    uint addId = GetFreeObjectId();
                    Vector2 vec2d = cad2D.GetVertex(vId);
                    Vec2Ds.Add(vec2d);
                    {
                        Vertex tmpVer = new Vertex();
                        tmpVer.Id = addId;
                        tmpVer.VCadId = vId;
                        tmpVer.V = (uint)(Vec2Ds.Count - 1);
                        Vertexs.Add(tmpVer);
                    }
                    {
                        for (int i = ElemLocs.Count; i < addId + 1; i++)
                        {
                            ElemLocs.Add(-1);
                        }
                        for (int i = ElemTypes.Count; i < addId + 1; i++)
                        {
                            ElemTypes.Add(0);
                        }
                        ElemLocs[(int)addId] = Vertexs.Count - 1;
                        ElemTypes[(int)addId] = 0;
                    }
                    System.Diagnostics.Debug.Assert(CheckMesh() == 0);
                }
                System.Diagnostics.Debug.Assert(Vec2Ds.Count <= cad2D.GetElemIds(CadElemType.VERTEX).Count * 10);
            }
            {
                IList<uint> eIds = cad2D.GetElemIds(CadElemType.EDGE);
                for (uint ie = 0; ie < eIds.Count; ie++)
                {
                    uint eId = eIds[(int)ie];

                    TessellateEdge(cad2D, eId);

                    System.Diagnostics.Debug.Assert(CheckMesh() == 0);
                }
            }
            {
                for (uint il = 0; il < loopIds.Count; il++)
                {
                    uint lId = loopIds[(int)il];

                    TessellateLoop(cad2D, lId);

                    System.Diagnostics.Debug.Assert(CheckMesh() == 0);
                }
            }

            MakeIncludeRelation(cad2D);

            return true;
        }

        private bool TessellateEdge(CadObject2D cad2D, uint eId)
        {
            uint sVId;
            uint eVId;
            System.Diagnostics.Debug.Assert(cad2D.IsElemId(CadElemType.EDGE, eId));
            if (!cad2D.GetEdgeVertexId(out sVId, out eVId, eId))
            {
                System.Diagnostics.Debug.WriteLine("error edge : " + eId);
                System.Diagnostics.Debug.Assert(false);
            }

            uint iSP;
            uint iEP;
            uint sMshId;
            uint eMshId;
            {
                uint loc;
                uint type;
                if (!FindElemLocTypeFromCadIdType(out loc, out type, sVId, CadElemType.VERTEX))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                System.Diagnostics.Debug.Assert(type == 0 && loc < Vertexs.Count);
                Vertex sVer = Vertexs[(int)loc];
                iSP = sVer.V;
                sMshId = sVer.Id;
                if (!FindElemLocTypeFromCadIdType(out loc, out type, eVId, CadElemType.VERTEX))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                System.Diagnostics.Debug.Assert(type == 0 && loc < Vertexs.Count);

                Vertex eVer = Vertexs[(int)loc];
                iEP = eVer.V;
                eMshId = eVer.Id;
            }

            uint newElemArrayId = GetFreeObjectId();
            uint ibarary0 = (uint)BarArrays.Count;
            {
                int locsCnt = ElemLocs.Count;
                for (int i = locsCnt; i < newElemArrayId + 1; i++)
                {
                    ElemLocs.Add(-1);
                }
                int typesCnt = ElemTypes.Count;
                for (int i = typesCnt; i < newElemArrayId + 1; i++)
                {
                    ElemTypes.Add(-1);
                }
                ElemLocs[(int)newElemArrayId] = (int)ibarary0;
                ElemTypes[(int)newElemArrayId] = 1;
            }
            int barArrayCnt = BarArrays.Count;
            for (int i = barArrayCnt; i < barArrayCnt + 1; i++)
            {
                BarArrays.Add(new BarArray());
            }
            BarArray barArray = BarArrays[(int)ibarary0];
            IList<Vector2> pts;
            cad2D.GetCurveAsPolyline(eId, out pts, -1);

            uint ndiv = (uint)(pts.Count + 1);
            IList<uint> iPts = new List<uint>();
            {
                for (int i = 0; i < ndiv + 1; i++)
                {
                    iPts.Add(0);
                }
                iPts[0] = iSP;
                for (uint i = 1; i < ndiv; i++)
                {
                    iPts[(int)i] = (uint)Vec2Ds.Count;
                    Vec2Ds.Add(pts[(int)(i - 1)]);
                }
                iPts[(int)ndiv] = iEP;
            }
            {
                barArray.Id = newElemArrayId;
                barArray.ECadId = eId;
                barArray.Layer = cad2D.GetLayer(CadElemType.EDGE, eId);
                int barsCnt = barArray.Bars.Count;
                for (int i = barsCnt; i < ndiv; i++)
                {
                    barArray.Bars.Add(new Bar());
                }
                barArray.SEId[0] = sMshId;
                barArray.SEId[1] = eMshId;
                barArray.LRId[0] = 0;
                barArray.LRId[1] = 0;
                for (uint ibar = 0; ibar < ndiv; ibar++)
                {
                    barArray.Bars[(int)ibar].V[0] = iPts[(int)ibar];
                    barArray.Bars[(int)ibar].V[1] = iPts[(int)(ibar + 1)];
                    barArray.Bars[(int)ibar].S2[0] = 0;
                    barArray.Bars[(int)ibar].S2[1] = 0;
                    barArray.Bars[(int)ibar].R2[0] = 0;
                    barArray.Bars[(int)ibar].R2[1] = 0;
                }
            }
            System.Diagnostics.Debug.Assert(CheckMesh() == 0);

            return true;
        }

        private bool TessellateLoop(CadObject2D cad2D, uint lId)
        {
            IList<Point2D> points = new List<Point2D>();
            IList<int> vec2Pt = new List<int>();
            {
                // 要素分割する領域の節点　Pt2Dsを作成
                // 辺に属する節点の全体番号から、要素分割する領域のローカル番号への対応(vec2Pt)を作成

                ////////////////////////////////
                for (int i = 0; i < Vec2Ds.Count; i++)
                {
                    vec2Pt.Add(-1);
                }
                {
                    // このループで使用される節点のフラグを立てる
                    ItrLoop itrEdgeLoop = cad2D.GetItrLoop(lId);
                    for (;;)
                    {
                        // ループをめぐる
                        for (; !itrEdgeLoop.IsEnd(); itrEdgeLoop++)
                        {
                            // このループの中のエッジをめぐる
                            {
                                uint vId = itrEdgeLoop.GetVertexId();
                                uint locTmp;
                                uint typeTmp;
                                if (!FindElemLocTypeFromCadIdType(out locTmp, out typeTmp, vId, CadElemType.VERTEX))
                                {
                                    System.Diagnostics.Debug.Assert(false);
                                }
                                System.Diagnostics.Debug.Assert(typeTmp == 0 && locTmp < Vertexs.Count);
                                Vertex ver = Vertexs[(int)locTmp];
                                vec2Pt[(int)ver.V] = 1;
                            }
                            uint eId;
                            bool isSameDir;
                            if (!itrEdgeLoop.GetEdgeId(out eId, out isSameDir))
                            {
                                continue; // 浮遊点は飛ばす
                            }
                            uint type;
                            uint loc;
                            if (!FindElemLocTypeFromCadIdType(out loc, out type, eId, CadElemType.EDGE))
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                            System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                            System.Diagnostics.Debug.Assert(type == 1);
                            BarArray barArray = BarArrays[(int)loc];
                            System.Diagnostics.Debug.Assert(barArray.ECadId == eId);
                            IList<Bar> bars = barArray.Bars;
                            for (uint ibar = 0; ibar < bars.Count; ibar++)
                            {
                                vec2Pt[(int)bars[(int)ibar].V[0]] = 1;
                                vec2Pt[(int)bars[(int)ibar].V[1]] = 1;
                            }
                        }
                        if (!itrEdgeLoop.ShiftChildLoop())
                        {
                            break;
                        }
                    }
                }
                {
                    // vec2Ptを作る、pointsを確保する
                    int ipt = 0;
                    for (uint ivec = 0; ivec < vec2Pt.Count; ivec++)
                    {
                        if (vec2Pt[(int)ivec] != -1)
                        {
                            vec2Pt[(int)ivec] = ipt;
                            ipt++;
                        }
                    }
                    int cntPts = points.Count;
                    for (int i = cntPts; i < ipt; i++)
                    {
                        points.Add(new Point2D());
                    }
                }
                for (uint ivec = 0; ivec < vec2Pt.Count; ivec++)
                {
                    if (vec2Pt[(int)ivec] != -1)
                    {
                        uint ip = (uint)vec2Pt[(int)ivec];
                        System.Diagnostics.Debug.Assert(ip < points.Count);
                        points[(int)ip].Point = new Vector2(Vec2Ds[(int)ivec].X, Vec2Ds[(int)ivec].Y);
                        points[(int)ip].Elem = -1;
                        points[(int)ip].Dir = 0;
                    }
                }
            }

            IList<Tri2D> tris = new List<Tri2D>();
            {
                // 与えられた点群を内部に持つ、大きな三角形を作る
                System.Diagnostics.Debug.Assert(Vec2Ds.Count >= 3);
                double maxLen;
                double[] center = new double[2];
                {
                    double[] bound2d = new double[4];
                    bound2d[0] = points[0].Point.X;
                    bound2d[1] = points[0].Point.X;
                    bound2d[2] = points[0].Point.Y;
                    bound2d[3] = points[0].Point.Y;
                    for (uint ipoin = 1; ipoin < points.Count; ipoin++)
                    {
                        if (points[(int)ipoin].Point.X < bound2d[0])
                        {
                            bound2d[0] = points[(int)ipoin].Point.X;
                        }
                        if (points[(int)ipoin].Point.X > bound2d[1])
                        {
                            bound2d[1] = points[(int)ipoin].Point.X;
                        }
                        if (points[(int)ipoin].Point.Y < bound2d[2])
                        {
                            bound2d[2] = points[(int)ipoin].Point.Y;
                        }
                        if (points[(int)ipoin].Point.Y > bound2d[3])
                        {
                            bound2d[3] = points[(int)ipoin].Point.Y;
                        }
                    }
                    maxLen = (bound2d[1] - bound2d[0] > bound2d[3] - bound2d[2]) ?
                        bound2d[1] - bound2d[0] : bound2d[3] - bound2d[2];
                    center[0] = (bound2d[1] + bound2d[0]) * 0.5;
                    center[1] = (bound2d[3] + bound2d[2]) * 0.5;
                }

                double triLen = maxLen * 8.0;
                double tmpLen = triLen * Math.Sqrt(3.0) / 6.0;

                int npo = points.Count;
                for (int i = npo; i < npo + 3; i++)
                {
                    points.Add(new Point2D());
                }
                points[npo + 0].Point = new Vector2(
                    (float)center[0],
                    (float)(center[1] + 2.0 * tmpLen));
                points[npo + 0].Elem = 0;
                points[npo + 0].Dir = 0;
                points[npo + 1].Point = new Vector2(
                    (float)(center[0] - 0.5 * triLen),
                    (float)(center[1] - tmpLen));
                points[npo + 1].Elem = 0;
                points[npo + 1].Dir = 1;
                points[npo + 2].Point = new Vector2(
                    (float)(center[0] + 0.5 * triLen),
                    (float)(center[1] - tmpLen));
                points[npo + 2].Elem = 0;
                points[npo + 2].Dir = 2;

                int trisCnt = tris.Count;
                for (int i = trisCnt; i < 1; i++)
                {
                    tris.Add(new Tri2D());
                }
                tris[0].V[0] = (uint)(npo + 0);
                tris[0].V[1] = (uint)(npo + 1);
                tris[0].V[2] = (uint)(npo + 2);
                tris[0].G2[0] = -1;
                tris[0].G2[1] = -1;
                tris[0].G2[2] = -1;
                tris[0].S2[0] = 0;
                tris[0].S2[1] = 0;
                tris[0].S2[2] = 0;
                tris[0].R2[0] = 0;
                tris[0].R2[1] = 0;
                tris[0].R2[2] = 0;
            }
            /*
            // DEBUG
            {
                System.Diagnostics.Debug.WriteLine("■TessellateLoop (1)");
                for (int i = 0; i < points.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine("points[{0}]", i);
                    System.Diagnostics.Debug.WriteLine(points[i].Dump());

                }
                for (int i = 0; i < tris.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine("tris[{0}]", i);
                    System.Diagnostics.Debug.WriteLine(tris[i].Dump());
                }
            }
            */

            // Make Delaunay Division
            for (uint ipoin = 0; ipoin < points.Count; ipoin++)
            {
                if (points[(int)ipoin].Elem >= 0)
                {
                    continue;  // 既にメッシュの一部である。
                }
                Vector2 addPo = points[(int)ipoin].Point;
                int iTriIn = -1;
                int iEdge = -1;
                uint iflg1 = 0;
                uint iflg2 = 0;
                for (uint itri = 0; itri < tris.Count; itri++)
                {
                    iflg1 = 0;
                    iflg2 = 0;
                    Tri2D tri = tris[(int)itri];
                    if (CadUtils.TriArea(addPo,
                        points[(int)tri.V[1]].Point, points[(int)tri.V[2]].Point) > CadUtils.MinTriArea)
                    {
                        iflg1++;
                        iflg2 += 0;
                    }
                    if (CadUtils.TriArea(addPo,
                        points[(int)tri.V[2]].Point, points[(int)tri.V[0]].Point) > CadUtils.MinTriArea)
                    {
                        iflg1++;
                        iflg2 += 1;
                    }
                    if (CadUtils.TriArea(addPo,
                        points[(int)tri.V[0]].Point, points[(int)tri.V[1]].Point) > CadUtils.MinTriArea)
                    {
                        iflg1++; iflg2 += 2;
                    }
                    if (iflg1 == 3)
                    {
                        iTriIn = (int)itri;
                        break;
                    }
                    else if (iflg1 == 2)
                    {
                        uint iEd0 = 3 - iflg2;
                        uint ipoE0 = tri.V[MeshUtils.NoELTriEdge[iEd0][0]];
                        uint ipoE1 = tri.V[MeshUtils.NoELTriEdge[iEd0][1]];
                        uint[] rel = MeshUtils.RelTriTri[tri.R2[iEd0]];
                        uint itri_s = tri.S2[iEd0];
                        System.Diagnostics.Debug.Assert(
                            tris[(int)itri_s].V[rel[MeshUtils.NoELTriEdge[iEd0][0]]] == ipoE0);
                        System.Diagnostics.Debug.Assert(
                            tris[(int)itri_s].V[rel[MeshUtils.NoELTriEdge[iEd0][1]]] == ipoE1);
                        uint inoel_d = rel[iEd0];
                        System.Diagnostics.Debug.Assert(tris[(int)itri_s].S2[inoel_d] == itri);
                        uint ipo_d = tris[(int)itri_s].V[inoel_d];
                        System.Diagnostics.Debug.Assert(
                            CadUtils.TriArea(addPo, points[(int)ipoE1].Point,
                            points[(int)tris[(int)itri].V[iEd0]].Point) > CadUtils.MinTriArea);
                        System.Diagnostics.Debug.Assert(
                            CadUtils.TriArea(addPo,
                            points[(int)tris[(int)itri].V[iEd0]].Point,
                            points[(int)ipoE0].Point) > CadUtils.MinTriArea);
                        if (CadUtils.TriArea(addPo,
                            points[(int)ipoE0].Point, points[(int)ipo_d].Point) < CadUtils.MinTriArea)
                        {
                            continue;
                        }
                        if (CadUtils.TriArea(addPo,
                            points[(int)ipo_d].Point, points[(int)ipoE1].Point) < CadUtils.MinTriArea)
                        {
                            continue;
                        }
                        int detD = MeshUtils.DetDelaunay(addPo,
                            points[(int)ipoE0].Point, points[(int)ipoE1].Point, points[(int)ipo_d].Point);
                        if (detD == 2 || detD == 1)
                        {
                            continue;
                        }
                        iTriIn = (int)itri;
                        iEdge = (int)iEd0;
                        break;
                    }
                }
                if (iTriIn == -1)
                {
                    System.Diagnostics.Debug.WriteLine("Super Triangle Failure " + ipoin + " (" +
                        addPo.X + " " + addPo.Y + ")");
                    System.Diagnostics.Debug.WriteLine(tris.Count);
                    return false;
                }
                if (iEdge == -1)
                {
                    MeshUtils.InsertPointElem(ipoin, (uint)iTriIn, points, tris);
                }
                else
                {
                    MeshUtils.InsertPointElemEdge(ipoin, (uint)iTriIn, (uint)iEdge, points, tris);
                }
                MeshUtils.DelaunayAroundPoint(ipoin, points, tris);
            }
            System.Diagnostics.Debug.Assert(MeshUtils.CheckTri(points, tris));
            /*
            // DEBUG
            {
                System.Diagnostics.Debug.WriteLine("■TessellateLoop (2)");
                for (int i = 0; i < points.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine("points[{0}]", i);
                    System.Diagnostics.Debug.WriteLine(points[i].Dump());

                }
                for (int i = 0; i < tris.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine("tris[{0}]", i);
                    System.Diagnostics.Debug.WriteLine(tris[i].Dump());
                }
            }
            */

            uint newTriId = GetFreeObjectId();

            {
                // エッジを回復する
                ItrLoop itrEdgeLoop = cad2D.GetItrLoop(lId);
                for (; ; )
                {
                    // 子ループのためのループ
                    for (; !itrEdgeLoop.IsEnd(); itrEdgeLoop++)
                    {
                        uint eId;
                        bool isSameDir;
                        if (!itrEdgeLoop.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;  // ループの中の点
                        }
                        uint loc;
                        uint type;
                        if (!FindElemLocTypeFromCadIdType(out loc, out type, eId, CadElemType.EDGE))
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                        System.Diagnostics.Debug.Assert(type == 1);
                        BarArray barArray = BarArrays[(int)loc];
                        System.Diagnostics.Debug.Assert(eId == barArray.ECadId);
                        IList<Bar> bars = barArray.Bars;
                        uint barArrayId = barArray.Id;
                        System.Diagnostics.Debug.Assert(barArrayId != newTriId);
                        for (uint ibar = 0; ibar < bars.Count; ibar++)
                        {
                            for (;;)
                            {
                                // EdgeをFlipしたら同じ辺について繰り返す				

                                // ipoi0は左周りのbarの始点、ipoi1は終点
                                uint ipoi0;
                                uint ipoi1;
                                if (isSameDir)
                                {
                                    ipoi0 = (uint)vec2Pt[(int)bars[(int)ibar].V[0]];
                                    ipoi1 = (uint)vec2Pt[(int)bars[(int)ibar].V[1]];
                                }
                                else
                                {
                                    ipoi0 = (uint)vec2Pt[(int)bars[(int)ibar].V[1]];
                                    ipoi1 = (uint)vec2Pt[(int)bars[(int)ibar].V[0]];
                                }
                                System.Diagnostics.Debug.Assert(ipoi0 < points.Count);
                                System.Diagnostics.Debug.Assert(ipoi1 < points.Count);

                                uint itri0;
                                uint inotri0;
                                uint inotri1;
                                if (MeshUtils.FindEdge(ipoi0, ipoi1, out itri0, out inotri0, out inotri1,
                                    points, tris))
                                {
                                    // ループの内側に接する要素を見つける
                                    // Split Triangle
                                    System.Diagnostics.Debug.Assert(inotri0 != inotri1);
                                    System.Diagnostics.Debug.Assert(inotri0 < 3);
                                    System.Diagnostics.Debug.Assert(inotri1 < 3);
                                    System.Diagnostics.Debug.Assert(tris[(int)itri0].V[inotri0] == ipoi0);
                                    System.Diagnostics.Debug.Assert(tris[(int)itri0].V[inotri1] == ipoi1);
                                    uint ied0 = 3 - inotri0 - inotri1;
                                    {
                                        uint itri1 = tris[(int)itri0].S2[ied0];
                                        uint ied1 = (uint)MeshUtils.RelTriTri[(int)tris[(int)itri0].R2[ied0]][ied0];
                                        System.Diagnostics.Debug.Assert(tris[(int)itri1].S2[ied1] == itri0);
                                        tris[(int)itri1].G2[ied1] = -3;
                                        tris[(int)itri0].G2[ied0] = -3;
                                    }
                                    break;  // 次のBarへ　for(;;)を抜ける
                                }
                                else
                                {
                                    double ratio;
                                    if (!MeshUtils.FindEdgePointAcrossEdge(ipoi0, ipoi1,
                                        out itri0, out inotri0, out inotri1, out ratio,
                                        points, tris))
                                    {
                                        System.Diagnostics.Debug.WriteLine("歪んだメッシュ");
                                        return false;
                                    }
                                    // return false if degeneration
                                    if (ratio < -1.0e-20 || ratio > 1.0 + 1.0e-20)
                                    {
                                        return false;
                                    }
                                    if (CadUtils.TriArea(
                                        points[(int)ipoi0].Point,
                                        points[(int)tris[(int)itri0].V[inotri0]].Point,
                                        points[(int)ipoi1].Point) < 1.0e-20)
                                    {
                                        return false;
                                    }
                                    if (CadUtils.TriArea(
                                        points[(int)ipoi0].Point,
                                        points[(int)ipoi1].Point,
                                        points[(int)tris[(int)itri0].V[inotri1]].Point) < 1.0e-20)
                                    {
                                        return false;
                                    }
                                    /*
                                    System.Diagnostics.Debug.Assert(ratio > -1.0e-20 && ratio < 1.0 + 1.0e-20);
                                    System.Diagnostics.Debug.Assert(CadUtils.TriArea(
                                        pt2Ds[(int)ipoi0].Point,
                                        pt2Ds[(int)tris[(int)itri0].V[inotri0]].Point,
                                        pt2Ds[(int)ipoi1].Point) > 1.0e-20);
                                    System.Diagnostics.Debug.Assert(CadUtils.TriArea(
                                        pt2Ds[(int)ipoi0].Point,
                                        pt2Ds[(int)ipoi1].Point,
                                        pt2Ds[(int)tris[(int)itri0].V[inotri1]].Point) > 1.0e-20);
                                    */

                                    if (ratio < 1.0e-20)
                                    {
                                        // "未実装 辺上に点がある場合"
                                        return false;
                                    }
                                    else if (ratio > 1.0 - 1.0e-10)
                                    {
                                        //	"未実装 辺上に点がある場合"
                                        return false;
                                    }
                                    else
                                    {
                                        uint ied0 = 3 - inotri0 - inotri1;
                                        if (tris[(int)itri0].G2[ied0] != -2)
                                        {
                                            return false;
                                        }
                                        System.Diagnostics.Debug.Assert(tris[(int)itri0].G2[ied0] == -2);
                                        uint itri1 = tris[(int)itri0].S2[ied0];
                                        uint ied1 = (uint)MeshUtils.RelTriTri[tris[(int)itri0].R2[ied0]][ied0];
                                        System.Diagnostics.Debug.Assert(tris[(int)itri1].S2[ied1] == itri0);
                                        System.Diagnostics.Debug.Assert(tris[(int)itri1].G2[ied1] == -2);
                                        MeshUtils.FlipEdge(itri0, ied0, points, tris);
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    if (!itrEdgeLoop.ShiftChildLoop())
                    {
                        break;
                    }
                }
                System.Diagnostics.Debug.Assert(MeshUtils.CheckTri(points, tris));
            }

            ////////////////////////////////////////////////
            // ここからはクラスの内容を変更する
            // エラーを出して戻るなら、ここ以前にすること
            ////////////////////////////////////////////////

            // ここから辺要素の隣接関係を変更する．３角形についてはそのまま

            {
                // 辺要素から３角形要素への隣接情報を作成
                ItrLoop itrEdgeLoop = cad2D.GetItrLoop(lId);
                for (; ; )
                {   // 子ループのためのループ
                    for (; !itrEdgeLoop.IsEnd(); itrEdgeLoop++)
                    {
                        uint eId;
                        bool isSameDir;
                        if (!itrEdgeLoop.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;  // ループの中の点
                        }
                        uint loc;
                        uint type;
                        if (!FindElemLocTypeFromCadIdType(out loc, out type, eId, CadElemType.EDGE))
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                        System.Diagnostics.Debug.Assert(type == 1);
                        BarArray barArray = BarArrays[(int)loc];
                        System.Diagnostics.Debug.Assert(eId == barArray.ECadId);
                        IList<Bar> bars = barArray.Bars;
                        uint barArrayId = barArray.Id;
                        System.Diagnostics.Debug.Assert(barArrayId != newTriId);
                        for (uint ibar = 0; ibar < bars.Count; ibar++)
                        {
                            // ipoi0は左周りのbarの始点、ipoi1は終点
                            uint ipoi0;
                            uint ipoi1;
                            if (isSameDir)
                            {
                                ipoi0 = (uint)vec2Pt[(int)bars[(int)ibar].V[0]];
                                ipoi1 = (uint)vec2Pt[(int)bars[(int)ibar].V[1]];
                            }
                            else
                            {
                                ipoi0 = (uint)vec2Pt[(int)bars[(int)ibar].V[1]];
                                ipoi1 = (uint)vec2Pt[(int)bars[(int)ibar].V[0]];
                            }
                            System.Diagnostics.Debug.Assert(ipoi0 < points.Count);
                            System.Diagnostics.Debug.Assert(ipoi1 < points.Count);
                            //
                            uint itri0;
                            uint inotri0;
                            uint inotri1;
                            // ループの内側に接する要素を見つける
                            if (!MeshUtils. FindEdge(ipoi0, ipoi1, out itri0, out inotri0, out inotri1,
                                points, tris))
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                            System.Diagnostics.Debug.Assert(inotri0 != inotri1);
                            System.Diagnostics.Debug.Assert(inotri0 < 3);
                            System.Diagnostics.Debug.Assert(inotri1 < 3);
                            System.Diagnostics.Debug.Assert(tris[(int)itri0].V[inotri0] == ipoi0);
                            System.Diagnostics.Debug.Assert(tris[(int)itri0].V[inotri1] == ipoi1);
                            uint ied0 = 3 - inotri0 - inotri1;
                            // 辺要素の隣接情報を作る
                            if (isSameDir)
                            {
                                System.Diagnostics.Debug.Assert(barArray.LRId[0] == newTriId ||
                                    barArray.LRId[0] == 0);
                                barArray.LRId[0] = newTriId;
                                bars[(int)ibar].S2[0] = itri0; bars[(int)ibar].R2[0] = ied0;
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(barArray.LRId[1] == newTriId ||
                                    barArray.LRId[1] == 0);
                                barArray.LRId[1] = newTriId;
                                bars[(int)ibar].S2[1] = itri0;
                                bars[(int)ibar].R2[1] = ied0;
                            }
                        }
                    }
                    if (!itrEdgeLoop.ShiftChildLoop())
                    {
                        break;
                    }
                }
                System.Diagnostics.Debug.Assert(MeshUtils.CheckTri(points, tris));
            }

            // 今後は辺要素を変更するのは，TriAryの番号付けを変化させるとき

            /*
            // DEBUG
            {
                System.Diagnostics.Debug.WriteLine("■TessellateLoop (3)");
                for (int i = 0; i < points.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine("points[{0}]", i);
                    System.Diagnostics.Debug.WriteLine(points[i].Dump());

                }
                for (int i = 0; i < tris.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine("tris[{0}]", i);
                    System.Diagnostics.Debug.WriteLine(tris[i].Dump());
                }
            }
            */

            {
                // 辺との隣接番号の整合性をとる
                ItrLoop itrEdgeLoop = cad2D.GetItrLoop(lId);
                for (; ; )
                {
                    // 子ループのためのループ
                    for (; !itrEdgeLoop.IsEnd(); itrEdgeLoop++)
                    {
                        uint eId;
                        bool isSameDir;
                        if (!itrEdgeLoop.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;  // 子ループが点の場合
                        }
                        uint loc;
                        uint type;
                        if (!FindElemLocTypeFromCadIdType(out loc, out type, eId, CadElemType.EDGE))
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                        System.Diagnostics.Debug.Assert(type == 1);
                        BarArray barArray = BarArrays[(int)loc];
                        System.Diagnostics.Debug.Assert(eId == barArray.ECadId);
                        IList<Bar> bars = barArray.Bars;
                        uint barArrayId = barArray.Id;
                        System.Diagnostics.Debug.Assert(barArrayId != newTriId);
                        if (barArray.LRId[0] == newTriId)
                        {
                            // 左側を切り離す
                            for (uint ibar = 0; ibar < bars.Count; ibar++)
                            {
                                Bar bar = bars[(int)ibar];
                                uint itri0 = bar.S2[0];
                                uint ied0 = bar.R2[0];
                                System.Diagnostics.Debug.Assert(itri0 < tris.Count);
                                System.Diagnostics.Debug.Assert(ied0 < 3);
                                System.Diagnostics.Debug.Assert(tris[(int)itri0].V[MeshUtils.NoELTriEdge[ied0][0]] ==
                                    vec2Pt[(int)bar.V[0]] ||
                                    tris[(int)itri0].V[MeshUtils.NoELTriEdge[ied0][0]] ==
                                    vec2Pt[(int)bar.V[1]]);
                                System.Diagnostics.Debug.Assert(tris[(int)itri0].V[MeshUtils.NoELTriEdge[ied0][1]] ==
                                    vec2Pt[(int)bar.V[0]] ||
                                    tris[(int)itri0].V[MeshUtils.NoELTriEdge[ied0][1]] ==
                                    vec2Pt[(int)bar.V[1]]);
                                if (tris[(int)itri0].G2[ied0] == barArrayId)
                                {
                                    continue; // すでに切り離されてる
                                }
                                {   // 向かい側の要素の処理
                                    uint itri1 = tris[(int)itri0].S2[ied0];
                                    uint ied1 = MeshUtils.RelTriTri[tris[(int)itri0].R2[ied0]][ied0];
                                    System.Diagnostics.Debug.Assert(tris[(int)itri1].S2[ied1] == itri0);
                                    if (barArray.LRId[1] != newTriId)
                                    {
                                        // 外側の要素を切り離す
                                        System.Diagnostics.Debug.Assert(tris[(int)itri1].S2[ied1] == itri0);
                                        tris[(int)itri1].G2[ied1] = -1;
                                    }
                                    else
                                    {
                                        // 辺をはさんで向かい側の要素も内側だから辺にくっつける
                                        tris[(int)itri1].G2[ied1] = (int)barArrayId;
                                        tris[(int)itri1].S2[ied1] = ibar;
                                        tris[(int)itri1].R2[ied1] = 1;
                                    }
                                }
                                {   // 内側の要素を辺にくっつける
                                    tris[(int)itri0].G2[ied0] = (int)barArrayId;
                                    tris[(int)itri0].S2[ied0] = ibar;
                                    tris[(int)itri0].R2[ied0] = 0;
                                }
                            }
                        }
                        if (barArray.LRId[1] == newTriId)
                        {
                            // 辺の右側を切り離す
                            for (uint ibar = 0; ibar < bars.Count; ibar++)
                            {
                                Bar bar = bars[(int)ibar];
                                uint itri0 = bar.S2[1];
                                uint ied0 = bar.R2[1];
                                if (tris[(int)itri0].G2[ied0] == barArrayId)
                                {
                                    continue; // すでに切り離されてる
                                }
                                {
                                    // 外側の要素を切り離す
                                    uint itri1 = tris[(int)itri0].S2[ied0];
                                    uint ied1 = MeshUtils.RelTriTri[tris[(int)itri0].R2[ied0]][ied0];
                                    System.Diagnostics.Debug.Assert(itri1 < tris.Count);
                                    System.Diagnostics.Debug.Assert(ied1 < 3);
                                    System.Diagnostics.Debug.Assert(tris[(int)itri0].V[MeshUtils.NoELTriEdge[ied0][0]] ==
                                        vec2Pt[(int)bar.V[1]]);
                                    System.Diagnostics.Debug.Assert(tris[(int)itri0].V[MeshUtils.NoELTriEdge[ied0][1]] ==
                                        vec2Pt[(int)bar.V[0]]);
                                    if (barArray.LRId[0] != newTriId)
                                    {
                                        // 外側の要素を切り離す
                                        System.Diagnostics.Debug.Assert(tris[(int)itri1].S2[ied1] == itri0);
                                        tris[(int)itri1].G2[ied1] = -1;
                                    }
                                    else
                                    {
                                        // 辺をはさんで向かい側の要素も内側だから辺にくっつける
                                        tris[(int)itri1].G2[ied1] = (int)barArrayId;
                                        tris[(int)itri1].S2[ied1] = ibar;
                                        tris[(int)itri1].R2[ied1] = 0;
                                    }
                                }
                                {
                                    // 内側の要素を辺にくっつける
                                    tris[(int)itri0].G2[ied0] = (int)barArrayId;
                                    tris[(int)itri0].S2[ied0] = ibar;
                                    tris[(int)itri0].R2[ied0] = 1;
                                }
                            }
                        }
                    }
                    if (!itrEdgeLoop.ShiftChildLoop())
                    {
                        break;
                    }
                }   // ループのfor文終わり

                // ここから先はFlip禁止フラグ(隣接要素配列番号-3)はないはず
                System.Diagnostics.Debug.Assert(MeshUtils.CheckTri(points, tris));
            }

            // 外側の３角形の消去
            ////////////////////////////////////////////////

            IList<Tri2D> inTris = new List<Tri2D>();    // 内側の三角形
            {
                // 外側の三角形の除去
                // 内側にある三角形をひとつ(iKerTri0)見つける
                uint iKerTri0 = (uint)tris.Count;
                {
                    ItrLoop itrEdgeLoop = cad2D.GetItrLoop(lId);
                    for (; !itrEdgeLoop.IsEnd(); itrEdgeLoop++)
                    {
                        uint eId;
                        bool isSameDir;
                        itrEdgeLoop.GetEdgeId(out eId, out isSameDir);
                        uint loc;
                        uint type;
                        if (!FindElemLocTypeFromCadIdType(out loc, out type, eId, CadElemType.EDGE))
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        System.Diagnostics.Debug.Assert(type == 1);
                        System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                        BarArray barArray = BarArrays[(int)loc];
                        System.Diagnostics.Debug.Assert(eId == barArray.ECadId);
                        IList<Bar> bars = barArray.Bars;
                        if (barArray.LRId[0] == newTriId)
                        {
                            for (uint ibar = 0; ibar < bars.Count; ibar++)
                            {
                                iKerTri0 = bars[(int)ibar].S2[0];
                                break;
                            }
                        }
                        else if (barArray.LRId[1] == newTriId)
                        {
                            for (uint ibar = 0; ibar < bars.Count; ibar++)
                            {
                                iKerTri0 = bars[(int)ibar].S2[1];
                                break;
                            }
                        }
                    }
                }
                System.Diagnostics.Debug.Assert(iKerTri0 < tris.Count);

                // 領域の外の要素ならフラグが-1、そうでなければフラグは昇順の要素番号が入った配列inoutFlgsを作る
                uint nInTri;
                // フラグ配列
                IList<int> inoutFlgs = new List<int>();
                {   // 上で見つけた内側の三角形を核として内側の三角形を周囲に拡大していく
                    for (int i = 0; i < tris.Count; i++)
                    {
                        inoutFlgs.Add(-1);
                    }
                    inoutFlgs[(int)iKerTri0] = 0;
                    nInTri = 1;
                    // 周囲が探索されていない三角形
                    Stack<uint> indStack = new Stack<uint>();
                    indStack.Push(iKerTri0);
                    for (; ; )
                    {
                        if (indStack.Count == 0)
                        {
                            break;
                        }
                        uint iCurTri = indStack.Pop();
                        for (uint inotri = 0; inotri < 3; inotri++)
                        {
                            if (tris[(int)iCurTri].G2[inotri] != -2)
                            {
                                continue;
                            }
                            uint iSTri = tris[(int)iCurTri].S2[inotri];
                            if (inoutFlgs[(int)iSTri] == -1)
                            {
                                inoutFlgs[(int)iSTri] = (int)nInTri;
                                nInTri++;
                                indStack.Push(iSTri);
                            }
                        }
                    }
                }

                // フラグ配列に沿って内側の三角形を集めた配列in_Triを作る
                for (int i = 0; i < nInTri; i++)
                {
                    inTris.Add(new Tri2D());
                }
                for (uint itri = 0; itri < tris.Count; itri++)
                {
                    if (inoutFlgs[(int)itri] != -1)
                    {
                        int iInTri = inoutFlgs[(int)itri];
                        System.Diagnostics.Debug.Assert(iInTri >= 0 && iInTri < nInTri);
                        inTris[iInTri] = tris[(int)itri];
                    }
                }
                // 内側の三角形配列のの隣接情報を作る
                for (uint itri = 0; itri < inTris.Count; itri++)
                {
                    for (uint ifatri = 0; ifatri < 3; ifatri++)
                    {
                        if (inTris[(int)itri].G2[ifatri] != -2)
                        {
                            continue;
                        }
                        int iSTri0 = (int)inTris[(int)itri].S2[ifatri];
                        System.Diagnostics.Debug.Assert(iSTri0 >= 0 && iSTri0 < tris.Count);
                        int iSInTri0 = inoutFlgs[iSTri0];
                        System.Diagnostics.Debug.Assert(iSInTri0 >= 0 && iSInTri0 < inTris.Count);
                        inTris[(int)itri].S2[ifatri] = (uint)iSInTri0;
                    }
                }
                {   // 辺の隣接情報を更新
                    ItrLoop itrEdgeLoop = cad2D.GetItrLoop(lId);
                    for (; ; )
                    {
                        // 子ループのためのループ
                        for (; !itrEdgeLoop.IsEnd(); itrEdgeLoop++)
                        {
                            uint eId;
                            bool isSameDir;
                            if (!itrEdgeLoop.GetEdgeId(out eId, out isSameDir))
                            {
                                continue;  // ループの中の点
                            }
                            uint loc;
                            uint type;
                            if (!FindElemLocTypeFromCadIdType(out loc, out type, eId, CadElemType.EDGE))
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                            System.Diagnostics.Debug.Assert(loc < BarArrays.Count);
                            System.Diagnostics.Debug.Assert(type == 1);
                            BarArray barArray = BarArrays[(int)loc];
                            System.Diagnostics.Debug.Assert(eId == barArray.ECadId);
                            IList<Bar> bars = barArray.Bars;
                            int barArrayId = (int)barArray.Id;
                            System.Diagnostics.Debug.Assert(barArrayId != newTriId);
                            uint iside = (isSameDir) ? 0 : 1u;
                            System.Diagnostics.Debug.Assert(barArray.LRId[iside] == newTriId);
                            for (uint ibar = 0; ibar < bars.Count; ibar++)
                            {
                                Bar bar = bars[(int)ibar];
                                int iSTri0 = (int)bar.S2[(int)iside];
                                System.Diagnostics.Debug.Assert(iSTri0 >= 0 && iSTri0 < tris.Count);
                                int iSInTri0 = inoutFlgs[iSTri0];
                                System.Diagnostics.Debug.Assert(iSInTri0 >= 0 && iSInTri0 < inTris.Count);
                                bar.S2[iside] = (uint)iSInTri0;
                            }
                        }
                        if (!itrEdgeLoop.ShiftChildLoop())
                        {
                            break;
                        }
                    }
                }
                inoutFlgs.Clear();
                for (uint ipo = 0; ipo < points.Count; ipo++)
                {
                    points[(int)ipo].Elem = -1;
                }
                System.Diagnostics.Debug.Assert(MeshUtils.CheckTri(points, inTris));
            }
            {
                // Remove not used point
                IList<int> pt2Vec = new List<int>();
                for (int i = 0; i < points.Count; i++)
                {
                    pt2Vec.Add(-2);
                }
                for (uint itri = 0; itri < inTris.Count; itri++)
                {
                    pt2Vec[(int)inTris[(int)itri].V[0]] = -1;
                    pt2Vec[(int)inTris[(int)itri].V[1]] = -1;
                    pt2Vec[(int)inTris[(int)itri].V[2]] = -1;
                }
                for (uint ivec = 0; ivec < vec2Pt.Count; ivec++)
                {
                    if (vec2Pt[(int)ivec] != -1)
                    {
                        uint ipo0 = (uint)vec2Pt[(int)ivec];
                        if (pt2Vec[(int)ipo0] != -1)
                        {
                            System.Diagnostics.Debug.WriteLine("対応しない点");
                            return false;
                        }
                        System.Diagnostics.Debug.Assert(pt2Vec[(int)ipo0] == -1);
                        pt2Vec[(int)ipo0] = (int)ivec;
                    }
                }
                for (uint ipo = 0; ipo < pt2Vec.Count; ipo++)
                {
                    if (pt2Vec[(int)ipo] == -1)
                    {
                        System.Diagnostics.Debug.WriteLine(ipo + " (" + points[(int)ipo].Point.X +
                            " " + points[(int)ipo].Point.Y);
                        //"未実装  Ｌｏｏｐに新しい節点の追加したときの処理"
                        return false;
                    }
                }
                for (uint itri = 0; itri < inTris.Count; itri++)
                {
                    for (uint inotri = 0; inotri < 3; inotri++)
                    {
                        int ipo0 = (int)inTris[(int)itri].V[inotri];
                        System.Diagnostics.Debug.Assert(ipo0 >= 0 && ipo0 < points.Count);
                        int ivec0 = pt2Vec[ipo0];
                        System.Diagnostics.Debug.Assert(ivec0 >= 0 && ivec0 < Vec2Ds.Count);
                        inTris[(int)itri].V[inotri] = (uint)ivec0;
                    }
                }
            }
            // DEBUG
            {
                System.Diagnostics.Debug.WriteLine("■TessellateLoop (4)");
                for (int i = 0; i < Vec2Ds.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine("Vec2Ds[{0}]", i);
                    System.Diagnostics.Debug.WriteLine(CadUtils.Dump(Vec2Ds[i]));

                }
                for (int i = 0; i < inTris.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine("inTris[{0}]", i);
                    System.Diagnostics.Debug.WriteLine(inTris[i].Dump());
                }
            }

            {
                uint itriary = (uint)TriArrays.Count;
                for (int i = (int)itriary; i < itriary + 1; i++)
                {
                    TriArrays.Add(new TriArray2D());
                }
                TriArrays[(int)itriary].Tris = inTris;
                TriArrays[(int)itriary].LCadId = lId;
                TriArrays[(int)itriary].Id = newTriId;
                TriArrays[(int)itriary].Layer = cad2D.GetLayer(CadElemType.LOOP, lId);

                int typesCnt = ElemTypes.Count;
                for (int i = typesCnt; i < newTriId + 1; i++)
                {
                    ElemTypes.Add(-1);
                }
                int locsCnt = ElemLocs.Count;
                for (int i = locsCnt; i < newTriId + 1; i++)
                {
                    ElemLocs.Add(0);
                }
                ElemTypes[(int)newTriId] = 2;   // TRI
                ElemLocs[(int)newTriId] = (int)itriary;
            }

            System.Diagnostics.Debug.Assert(CheckMesh() == 0);
            return true;
        }

        private uint FindMaxId()
        {
            uint maxId = 0;
            {
                for (uint iver = 0; iver < Vertexs.Count; iver++)
                {
                    if (maxId < Vertexs[(int)iver].Id)
                    {
                        maxId = Vertexs[(int)iver].Id;
                    }
                }
                for (uint ibarary = 0; ibarary < BarArrays.Count; ibarary++)
                {
                    if (maxId < BarArrays[(int)ibarary].Id)
                    {
                        maxId = BarArrays[(int)ibarary].Id;
                    }
                }
                for (uint itriary = 0; itriary < TriArrays.Count; itriary++)
                {
                    if (maxId < TriArrays[(int)itriary].Id)
                    {
                        maxId = TriArrays[(int)itriary].Id;
                    }
                }
                for (uint iquadary = 0; iquadary < QuadArrays.Count; iquadary++)
                {
                    if (maxId < QuadArrays[(int)iquadary].Id)
                    {
                        maxId = QuadArrays[(int)iquadary].Id;
                    }
                }
            }
            return maxId;
        }

        private uint GetFreeObjectId()
        {
            uint maxId = FindMaxId();
            IList<uint> isUsedFlgs = new List<uint>();
            {
                for (uint iuse = 0; iuse < maxId + 1; iuse++)
                {
                    isUsedFlgs.Add(0);
                }
                for (uint iver = 0; iver < Vertexs.Count; iver++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)Vertexs[(int)iver].Id] == 0);
                    System.Diagnostics.Debug.Assert(Vertexs[(int)iver].Id >= 1 && Vertexs[(int)iver].Id <= maxId);
                    isUsedFlgs[(int)Vertexs[(int)iver].Id] = 1;
                }
                for (uint ibarary = 0; ibarary < BarArrays.Count; ibarary++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)BarArrays[(int)ibarary].Id] == 0);
                    System.Diagnostics.Debug.Assert(BarArrays[(int)ibarary].Id >= 1 && BarArrays[(int)ibarary].Id <= maxId);
                    isUsedFlgs[(int)BarArrays[(int)ibarary].Id] = 1;
                }
                for (uint itriary = 0; itriary < TriArrays.Count; itriary++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)TriArrays[(int)itriary].Id] == 0);
                    System.Diagnostics.Debug.Assert(TriArrays[(int)itriary].Id >= 1 && TriArrays[(int)itriary].Id <= maxId);
                    isUsedFlgs[(int)TriArrays[(int)itriary].Id] = 1;
                }
                for (uint iquadary = 0; iquadary < QuadArrays.Count; iquadary++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)QuadArrays[(int)iquadary].Id] == 0);
                    System.Diagnostics.Debug.Assert(QuadArrays[(int)iquadary].Id >= 1 && QuadArrays[(int)iquadary].Id <= maxId);
                    isUsedFlgs[(int)QuadArrays[(int)iquadary].Id] = 1;
                }
            }
            System.Diagnostics.Debug.Assert(isUsedFlgs[0] == 0);
            for (uint i = 1; i < isUsedFlgs.Count; i++)
            {
                if (isUsedFlgs[(int)i] == 0)
                {
                    return i;
                }
            }
            return maxId + 1;
        }

        public uint GetElemIdFromCadId(uint cadId, CadElemType cadType)
        {
            switch (cadType)
            {
                case CadElemType.VERTEX:
                    for (uint iver = 0; iver < Vertexs.Count; iver++)
                    {
                        if (Vertexs[(int)iver].VCadId == cadId)
                        {
                            uint mshid = Vertexs[(int)iver].Id;
                            System.Diagnostics.Debug.Assert(ElemLocs[(int)mshid] == iver);
                            System.Diagnostics.Debug.Assert(ElemTypes[(int)mshid] == 0);
                            return mshid;
                        }
                    }
                    break;

                case CadElemType.EDGE:
                    for (uint ibar = 0; ibar < BarArrays.Count; ibar++)
                    {
                        if (BarArrays[(int)ibar].ECadId == cadId)
                        {
                            uint mshid = BarArrays[(int)ibar].Id;
                            System.Diagnostics.Debug.Assert(ElemLocs[(int)mshid] == ibar);
                            System.Diagnostics.Debug.Assert(ElemTypes[(int)mshid] == 1);
                            return mshid;
                        }
                    }
                    break;

                case CadElemType.LOOP:
                    for (uint itri = 0; itri < TriArrays.Count; itri++)
                    {
                        if (TriArrays[(int)itri].LCadId == cadId)
                        {
                            uint mshid = TriArrays[(int)itri].Id;
                            System.Diagnostics.Debug.Assert(ElemLocs[(int)mshid] == itri);
                            System.Diagnostics.Debug.Assert(ElemTypes[(int)mshid] == 2);
                            return mshid;
                        }
                    }
                    for (uint iquad = 0; iquad < QuadArrays.Count; iquad++)
                    {
                        if (QuadArrays[(int)iquad].LCadId == cadId)
                        {
                            uint mshid = TriArrays[(int)iquad].Id;
                            System.Diagnostics.Debug.Assert(ElemLocs[(int)mshid] == iquad);
                            System.Diagnostics.Debug.Assert(ElemTypes[(int)mshid] == 3);
                            return mshid;
                        }
                    }
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return 0;
            }
            return 0;
        }

        private bool FindElemLocTypeFromCadIdType(out uint loc, out uint type, uint cadId, CadElemType cadType)
        {
            loc = 0;
            type = 0;

            switch (cadType)
            {
                case CadElemType.VERTEX:
                    for (uint iver = 0; iver < Vertexs.Count; iver++)
                    {
                        if (Vertexs[(int)iver].VCadId == cadId)
                        {
                            loc = iver;
                            type = 0;
                            uint mshId = Vertexs[(int)iver].Id;
                            System.Diagnostics.Debug.Assert(ElemLocs[(int)mshId] == loc);
                            System.Diagnostics.Debug.Assert(ElemTypes[(int)mshId] == type);
                            return true;
                        }
                    }
                    break;

                case CadElemType.EDGE:
                    for (uint ibar = 0; ibar < BarArrays.Count; ibar++)
                    {
                        if (BarArrays[(int)ibar].ECadId == cadId)
                        {
                            loc = ibar;
                            type = 1;
                            uint mshId = BarArrays[(int)ibar].Id;
                            System.Diagnostics.Debug.Assert(ElemLocs[(int)mshId] == loc);
                            System.Diagnostics.Debug.Assert(ElemTypes[(int)mshId] == type);
                            return true;
                        }
                    }
                    break;

                case CadElemType.LOOP:
                    for (uint itri = 0; itri < TriArrays.Count; itri++)
                    {
                        if (TriArrays[(int)itri].LCadId == cadId)
                        {
                            loc = itri;
                            type = 2;
                            uint mshId = TriArrays[(int)itri].Id;
                            System.Diagnostics.Debug.Assert(ElemLocs[(int)mshId] == loc);
                            System.Diagnostics.Debug.Assert(ElemTypes[(int)mshId] == type);
                            return true;
                        }
                    }
                    for (uint iquad = 0; iquad < QuadArrays.Count; iquad++)
                    {
                        if (QuadArrays[(int)iquad].LCadId == cadId)
                        {
                            loc = iquad;
                            type = 3;
                            uint mshId = QuadArrays[(int)iquad].Id;
                            System.Diagnostics.Debug.Assert(ElemLocs[(int)mshId] == loc);
                            System.Diagnostics.Debug.Assert(ElemTypes[(int)mshId] == type);
                            return true;
                        }
                    }
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
            return false;
        }

        private int CheckMesh()
        {
            {
                uint maxId = 0;
                for (uint iver = 0; iver < Vertexs.Count; iver++)
                {
                    uint id0 = Vertexs[(int)iver].Id;
                    if (maxId < id0)
                    {
                        maxId = id0;
                    }
                    System.Diagnostics.Debug.Assert(ElemTypes.Count > id0);
                    System.Diagnostics.Debug.Assert(ElemTypes[(int)id0] == 0);
                    System.Diagnostics.Debug.Assert(ElemLocs.Count > id0);
                    int loc0 = ElemLocs[(int)id0];
                    System.Diagnostics.Debug.Assert(loc0 == (int)iver);
                    System.Diagnostics.Debug.Assert(Vertexs.Count > loc0);
                    Vertex ver0 = Vertexs[(int)loc0];
                    System.Diagnostics.Debug.Assert(ver0.Id == id0);
                }
                for (uint ibarary = 0; ibarary < BarArrays.Count; ibarary++)
                {
                    uint id0 = BarArrays[(int)ibarary].Id;
                    if (maxId < id0)
                    {
                        maxId = id0;
                    }
                    System.Diagnostics.Debug.Assert(ElemTypes.Count > id0);
                    System.Diagnostics.Debug.Assert(ElemTypes[(int)id0] == 1);
                    System.Diagnostics.Debug.Assert(ElemLocs.Count > id0);
                    int loc0 = ElemLocs[(int)id0];
                    System.Diagnostics.Debug.Assert(loc0 == (int)ibarary);
                    System.Diagnostics.Debug.Assert(BarArrays.Count > loc0);
                    BarArray bar0 = BarArrays[(int)loc0];
                    System.Diagnostics.Debug.Assert(bar0.Id == id0);
                }
                for (uint itriary = 0; itriary< TriArrays.Count; itriary++)
                {
                    uint id0 = TriArrays[(int)itriary].Id;
                    if (maxId < id0)
                    {
                        maxId = id0;
                    }
                    System.Diagnostics.Debug.Assert(ElemTypes.Count > id0);
                    System.Diagnostics.Debug.Assert(ElemTypes[(int)id0] == 2);
                    System.Diagnostics.Debug.Assert(ElemLocs.Count > id0);
                    int loc0 = ElemLocs[(int)id0];
                    System.Diagnostics.Debug.Assert(loc0 == (int)itriary);
                    System.Diagnostics.Debug.Assert(TriArrays.Count > loc0);
                    TriArray2D tri0 = TriArrays[(int)loc0];
                    System.Diagnostics.Debug.Assert(tri0.Id == id0);
                }
                for (uint iquadary = 0; iquadary< QuadArrays.Count;iquadary++)
                {
                    uint id0 = QuadArrays[(int)iquadary].Id;
                    if (maxId < id0)
                    {
                        maxId = id0;
                    }
                    System.Diagnostics.Debug.Assert(ElemTypes.Count > id0);
                    System.Diagnostics.Debug.Assert(ElemTypes[(int)id0] == 3);
                    System.Diagnostics.Debug.Assert(ElemLocs.Count > id0);
                    int loc0 = ElemLocs[(int)id0];
                    System.Diagnostics.Debug.Assert(loc0 == (int)iquadary);
                    System.Diagnostics.Debug.Assert(QuadArrays.Count > loc0);
                    QuadArray2D quad0 = QuadArrays[loc0];
                    System.Diagnostics.Debug.Assert(quad0.Id == id0);
                }
                System.Diagnostics.Debug.Assert(maxId == FindMaxId());
                IList<uint> isUsedFlgs = new List<uint>();
                for (int i = 0; i < maxId + 1; i++)
                {
                    isUsedFlgs.Add(0);
                }
                for (uint iver = 0; iver < Vertexs.Count; iver++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)Vertexs[(int)iver].Id] == 0);
                    isUsedFlgs[(int)Vertexs[(int)iver].Id] = 1;
                }
                for (uint ibarary = 0; ibarary< BarArrays.Count; ibarary++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)BarArrays[(int)ibarary].Id] == 0);
                    isUsedFlgs[(int)BarArrays[(int)ibarary].Id] = 1;
                }
                for (uint itriary = 0; itriary < TriArrays.Count; itriary++)
                {
                    System.Diagnostics.Debug.Assert(isUsedFlgs[(int)TriArrays[(int)itriary].Id] == 0);
                    isUsedFlgs[(int)TriArrays[(int)itriary].Id] = 1;
                }
                System.Diagnostics.Debug.Assert(isUsedFlgs[0] == 0 );
            }
            for (uint iver = 0; iver < Vertexs.Count; iver++)
            {
                System.Diagnostics.Debug.Assert(IsId(Vertexs[(int)iver].Id));
            }
            for (uint ibarary = 0; ibarary< BarArrays.Count;ibarary++)
            {
                System.Diagnostics.Debug.Assert(IsId(BarArrays[(int)ibarary].Id));
            }
            for (uint itriary = 0; itriary< TriArrays.Count; itriary++)
            {
                System.Diagnostics.Debug.Assert(IsId(TriArrays[(int)itriary].Id));
            }
            {
                for (uint ind = 0; ind < ElemLocs.Count; ind++)
                {
                    if (ElemLocs[(int)ind] == -1)
                    {
                        continue;
                    }
                    System.Diagnostics.Debug.Assert(ElemLocs[(int)ind] >= 0);
                    System.Diagnostics.Debug.Assert(IsId(ind));
                }
            }
            ////////////////////////////////	
            for (uint ibarary = 0; ibarary < BarArrays.Count; ibarary++)
            {
                uint mshBarId = BarArrays[(int)ibarary].Id;
                int barLoc = ElemLocs[(int)mshBarId];
                IList<Bar> bars = BarArrays[barLoc].Bars;
                for (uint isidebar = 0; isidebar < 2; isidebar++)
                {
                    int mshAdjId = (int)BarArrays[barLoc].LRId[isidebar];
                    if (mshAdjId <= 0)
                    {
                        continue;  // 外部と接している場合
                    }
                    System.Diagnostics.Debug.Assert(mshAdjId < ElemLocs.Count);
                    int adjLoc = ElemLocs[mshAdjId];
                    if (ElemTypes[mshAdjId] == 2)
                    {
                        // 三角形と接している場合
                        IList<Tri2D> tris = TriArrays[adjLoc].Tris;
                        for (uint ibar = 0; ibar < bars.Count; ibar++)
                        {
                            uint itri = bars[(int)ibar].S2[isidebar];
                            uint inotri = bars[(int)ibar].R2[isidebar];
                            System.Diagnostics.Debug.Assert(tris[(int)itri].G2[inotri] == (int)mshBarId);
                            System.Diagnostics.Debug.Assert(tris[(int)itri].S2[inotri] == ibar);
                            System.Diagnostics.Debug.Assert(tris[(int)itri].R2[inotri] == isidebar);
                        }
                    }
                }
            }
            return 0;
        }

        private void MakeIncludeRelation(CadObject2D cad2D)
        {
            IncludeRelations.Clear();

            if (ElemLocs.Count == 0 || ElemTypes.Count == 0)
            {
                return;
            }

            uint maxId = FindMaxId();
            for (int i = 0; i < maxId + 1; i++)
            {
                IncludeRelations.Add(new List<uint>());
            }
            IList<uint> lIds = cad2D.GetElemIds(CadElemType.LOOP);
            for (uint ilid = 0; ilid < lIds.Count; ilid++)
            {
                uint lId = lIds[(int)ilid];
                uint triId = GetElemIdFromCadId(lId, CadElemType.LOOP);
                if (!IsId(triId))
                {
                    continue;
                }
                ItrLoop itrL = cad2D.GetItrLoop(lId);
                for (;;)
                {
                    for (; !itrL.IsEnd(); itrL++)
                    {
                        uint vCadId = itrL.GetVertexId();
                        uint mshVId = GetElemIdFromCadId(vCadId, CadElemType.VERTEX);
                        IncludeRelations[(int)triId].Add(mshVId);
                        System.Diagnostics.Debug.Assert(IsId(mshVId));

                        uint eId;
                        bool isSameDir;
                        if (!itrL.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;
                        }
                        uint barId = GetElemIdFromCadId(eId, CadElemType.EDGE);
                        System.Diagnostics.Debug.Assert(IsId(barId));
                        IncludeRelations[(int)triId].Add(barId);
                    }
                    if (!itrL.ShiftChildLoop())
                    {
                        break;
                    }
                }
            }

            IList<uint> eIds = cad2D.GetElemIds(CadElemType.EDGE);
            for (uint ieid = 0; ieid < eIds.Count; ieid++)
            {
                uint eId = eIds[(int)ieid];
                System.Diagnostics.Debug.Assert(cad2D.IsElemId(CadElemType.EDGE, eId));
                uint barId = GetElemIdFromCadId(eId, CadElemType.EDGE);
                if (!IsId(barId))
                {
                    // 浮いている辺があって，辺メッシュが切られなかった場合
                    continue;
                }
                uint sVId;
                uint eVId;
                if (!cad2D.GetEdgeVertexId(out sVId, out eVId, eId))
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                uint mshSVId = GetElemIdFromCadId(sVId, CadElemType.VERTEX);
                uint mshEVId = GetElemIdFromCadId(eVId, CadElemType.VERTEX);
                System.Diagnostics.Debug.Assert(IsId(mshSVId));
                System.Diagnostics.Debug.Assert(IsId(mshEVId));
                IncludeRelations[(int)barId].Add(mshSVId);
                IncludeRelations[(int)barId].Add(mshEVId);
            }
        }


    }
}
