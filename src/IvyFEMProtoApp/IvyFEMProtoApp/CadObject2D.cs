﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;


namespace IvyFEM
{
    class ResAddVertex
    {
        public uint AddVId { get; set; } = 0;
        public uint AddEId { get; set; } = 0;

        public string Dump()
        {
            string CRLF = System.Environment.NewLine;
            string ret = "";

            ret += "AddVId = " + AddVId + CRLF;
            ret += "AddEId = " + AddEId + CRLF;
            return ret;
        }
    }

    class ResAddPolygon
    {
        public uint AddLId { get; set; } = 0;
        public IList<uint> VIds { get; } = new List<uint>();
        public IList<uint> EIds { get; } = new List<uint>();

        public ResAddPolygon()
        {

        }

        public ResAddPolygon(ResAddPolygon src)
        {
            this.AddLId = src.AddLId;
            this.VIds.Clear();
            foreach (var id in src.VIds)
            {
                this.VIds.Add(id);
            }
            this.EIds.Clear();
            foreach (var id in src.EIds)
            {
                this.EIds.Add(id);
            }
        }

        public string Dump()
        {
            string CRLF = System.Environment.NewLine;
            string ret = "";

            ret += "AddId = " + AddLId + CRLF;
            ret += "VIds:" + CRLF;
            for (int i = 0; i < VIds.Count; i++)
            {
                ret += "    VIds[" + i + "] = " + VIds[i] + CRLF;
            }
            ret += "EIds:" + CRLF;
            for (int i = 0; i < EIds.Count; i++)
            {
                ret += "    EIds[" + i + "] = " + EIds[i] + CRLF;
            }

            return ret;
        }
    }

    class CadObject2D
    {
        private ObjectArray<Loop2D> LoopArray = new ObjectArray<Loop2D>();
        private ObjectArray<Edge2D> EdgeArray = new ObjectArray<Edge2D>();
        private ObjectArray<Vertex2D> VertexArray = new ObjectArray<Vertex2D>();
        private BRep2D BRep = new BRep2D();
        private double MinClearance = 1.0e-3;

        public CadObject2D()
        {

        }

        ~CadObject2D()
        {
            Clear();
        }

        public void Clear()
        {
            LoopArray.Clear();
            EdgeArray.Clear();
            VertexArray.Clear();
            BRep.Clear();
        }

        public string Dump()
        {
            string ret = "";
            string CRLF = System.Environment.NewLine;

            ret += "■CadObject2D" + CRLF;
            ret += "LoopArray" + CRLF;
            var lIds = LoopArray.GetObjectIds();
            for (int i = 0; i < lIds.Count; i++)
            {
                var lId = lIds[i];
                ret += "-------------------------" + CRLF;
                ret += "lId = " + lId + CRLF;
                var l = LoopArray.GetObject(lId);
                ret += l.Dump();
            }
            ret += "EdgeArray" + CRLF;
            var eIds = EdgeArray.GetObjectIds();
            for (int i = 0; i < eIds.Count; i++)
            {
                var eId = eIds[i];
                ret += "-------------------------" + CRLF;
                ret += "eId = " + eId + CRLF;
                var e = EdgeArray.GetObject(eId);
                ret += e.Dump();
            }
            ret += "VertexArray" + CRLF;
            var vIds = VertexArray.GetObjectIds();
            for (int i = 0; i < vIds.Count; i++)
            {
                var vId = vIds[i];
                ret += "-------------------------" + CRLF;
                ret += "vId = " + vId + CRLF;
                var v = VertexArray.GetObject(vId);
                ret += v.Dump();
            }
            ret += "BRep" + CRLF;
            ret += BRep.Dump();

            return ret;
        }

        public ResAddVertex AddVertex(CadElementType type, uint id, Vector2 vec)
        {
            ResAddVertex res = new ResAddVertex();
            if (type == CadElementType.NOT_SET || id == 0)
            {
                uint addVId = BRep.AddVertexToLoop(0);
                uint tmpId = VertexArray.AddObject(new KeyValuePair<uint, Vertex2D>(addVId, new Vertex2D(vec)));
                System.Diagnostics.Debug.Assert(tmpId == addVId);
                res.AddVId = addVId;
                return res;
            }
            else if (type == CadElementType.LOOP)
            {
                uint lId = id;
                System.Diagnostics.Debug.Assert(LoopArray.IsObjectId(lId));
                if (!LoopArray.IsObjectId(lId))
                {
                    return res;
                }
                {
                    double dist = SignedDistancePointLoop(lId, vec);
                    if (dist < this.MinClearance) { return res; }
                }
                uint addVId = BRep.AddVertexToLoop(lId);
                uint tmpId = VertexArray.AddObject(new KeyValuePair<uint, Vertex2D>(addVId, new Vertex2D(vec)));
                System.Diagnostics.Debug.Assert(tmpId == (int)addVId);
                System.Diagnostics.Debug.Assert(AssertValid() == 0);
                res.AddVId = addVId;
                return res;
            }
            else if (type == CadElementType.EDGE)
            {
                uint eId = id;
                if (!EdgeArray.IsObjectId(eId))
                {
                    return res;
                }
                Edge2D oldEdge = GetEdge(eId);
                Vector2 addVec = oldEdge.GetNearestPoint(vec);
                if (CadUtils.SquareLength(addVec - oldEdge.GetVertex(false)) < 1.0e-20 ||
                    CadUtils.SquareLength(addVec - oldEdge.GetVertex(true)) < 1.0e-20)
                {
                    return res;
                }

                uint addVId = BRep.AddVertexToEdge(eId);
                uint addEId;
                {
                    ItrVertex itrv = BRep.GetItrVertex(addVId);
                    bool isSameDir0;
                    bool isSameDir1;
                    uint bEId;
                    uint aEId;
                    itrv.GetBehindEdgeId(out bEId, out isSameDir0);
                    itrv.GetAheadEdgeId(out aEId, out isSameDir1);
                    addEId = (bEId == eId) ? aEId : bEId;
                }
                {
                    uint tmpId = VertexArray.AddObject(new KeyValuePair<uint, Vertex2D>(addVId,
                        new Vertex2D(addVec)));
                    System.Diagnostics.Debug.Assert(tmpId == addVId);
                }
                {
                    uint tmpId = EdgeArray.AddObject(new KeyValuePair<uint, Edge2D>(addEId,
                        new Edge2D(addVId, oldEdge.GetVertexId(false))));
                    System.Diagnostics.Debug.Assert(tmpId == addEId);
                }
                {
                    Edge2D addEdge = GetEdge(addEId);

                    oldEdge.Split(addEdge, addVec);

                    Edge2D edge = GetEdge(eId);

                    System.Diagnostics.Debug.Assert(edge.GetVertexId(false) == addVId);
                    edge.Copy(oldEdge);
                }
                System.Diagnostics.Debug.Assert(AssertValid() == 0);
                res.AddVId = addVId;
                res.AddEId = addEId;
                return res;
            }
            return res;
        }

        public ResAddPolygon AddPolygon(IList<Vector2> points, uint lId = 0)
        {
            ResAddPolygon res = new ResAddPolygon();

            int ptCnt = points.Count;
            if (ptCnt < 3)
            {
                return res;
            }

            try
            {
                IList<Vector2> points1 = new List<Vector2>(points);
                {
                    uint n = (uint)points.Count;
                    IList<Edge2D> edges = new List<Edge2D>();
                    for (uint i = 0; i < n - 1; i++)
                    {
                        Edge2D e = new Edge2D(i, i + 1);
                        e.SetVertexs(points[(int)i], points[(int)i + 1]);
                        edges.Add(e);
                    }
                    {
                        Edge2D e = new Edge2D(n - 1, 0);
                        e.SetVertexs(points[(int)n - 1], points[0]);
                        edges.Add(e);
                    }
                    if (CadUtils.CheckEdgeIntersection(edges) != 0)
                    {
                        return res;
                    }
                }
                for (uint i = 0; i < ptCnt; i++)
                {
                    uint vId0 = AddVertex(CadElementType.LOOP, lId, points1[(int)i]).AddVId;
                    if (vId0 == 0)
                    {
                        throw new InvalidOperationException("FAIL_ADD_POLYGON_INSIDE_LOOP");
                    }
                    res.VIds.Add(vId0);
                }
                for (uint iEdge = 0; iEdge < ptCnt - 1; iEdge++)
                {
                    uint eId0 = ConnectVertexLine(res.VIds[(int)iEdge], res.VIds[(int)iEdge + 1]).AddEId;
                    if (eId0 == 0)
                    {
                        throw new InvalidOperationException("FAIL_ADD_POLYGON_INSIDE_LOOP");
                    }
                    res.EIds.Add(eId0);
                }
                {
                    uint eId0 = ConnectVertexLine(res.VIds[ptCnt - 1], res.VIds[0]).AddEId;
                    if (eId0 == 0)
                    {
                        throw new InvalidOperationException("FAIL_ADD_POLYGON_INSIDE_LOOP");
                    }
                    res.EIds.Add(eId0);
                }

                System.Diagnostics.Debug.Assert(AssertValid() == 0);

                {
                    uint eId0 = res.EIds[ptCnt - 1];
                    uint lId0 = 0;
                    uint lId1 = 0;
                    BRep.GetEdgeLoopId(eId0, out lId0, out lId1);
                    res.AddLId = (lId0 == lId) ? lId1 : lId0;
                }
                return res;
            }
            catch (InvalidOperationException exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.ToString());
                for (uint iEId = 0; iEId < res.EIds.Count; iEId++)
                {
                    uint eId0 = res.EIds[(int)iEId];
                    RemoveElement(CadElementType.EDGE, eId0);
                }
                for (uint iVId = 0; iVId < res.VIds.Count; iVId++)
                {
                    uint vId0 = res.VIds[(int)iVId];
                    RemoveElement(CadElementType.VERTEX, vId0);
                }
                System.Diagnostics.Debug.Assert(AssertValid() == 0);
            }

            //　失敗したとき
            return new ResAddPolygon();

        }

        public bool IsElemId(CadElementType type, uint id)
        {
            if (type == CadElementType.NOT_SET)
            {
                return false;
            }
            else if (type == CadElementType.VERTEX)
            {
                return VertexArray.IsObjectId(id);
            }
            else if (type == CadElementType.EDGE)
            {
                return EdgeArray.IsObjectId(id);
            }
            else if (type == CadElementType.LOOP)
            {
                return LoopArray.IsObjectId(id);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return false;
        }

        public IList<uint> GetElemIds(CadElementType type)
        {
            if (type == CadElementType.VERTEX)
            {
                return VertexArray.GetObjectIds();
            }
            else if (type == CadElementType.EDGE)
            {
                return EdgeArray.GetObjectIds();
            }
            else if (type == CadElementType.LOOP)
            {
                return LoopArray.GetObjectIds();
            }

            System.Diagnostics.Debug.Assert(false);
            IList<uint> nullVec = new List<uint>();
            return nullVec;
        }

        public Vector2 GetVertex(uint vId)
        {
            System.Diagnostics.Debug.Assert(VertexArray.IsObjectId(vId));
            Vertex2D v = VertexArray.GetObject(vId);
            return v.Point;
        }

        public bool GetVertexColor(uint vId, double[] color)
        {
            if (!BRep.IsElemId(CadElementType.VERTEX, vId))
            {
                return false;
            }
            if (!VertexArray.IsObjectId(vId))
            {
                return false;
            }
            Vertex2D v = VertexArray.GetObject(vId);
            for (int i = 0; i < 3; i++)
            {
                color[i] = v.Color[i];
            }
            return true;
        }

        public bool SetVertexColor(uint vId, double[] color)
        {
            if (!BRep.IsElemId(CadElementType.VERTEX, vId))
            {
                return false;
            }
            if (!VertexArray.IsObjectId(vId))
            {
                return false;
            }
            Vertex2D v = VertexArray.GetObject(vId);
            for (int i = 0; i < 3; i++)
            {
                v.Color[i] = color[i];
            }
            return true;
        }

        public Edge2D GetEdge(uint eId)
        {
            System.Diagnostics.Debug.Assert(BRep.IsElemId(CadElementType.EDGE, eId));
            System.Diagnostics.Debug.Assert(EdgeArray.IsObjectId(eId));
            Edge2D e = EdgeArray.GetObject(eId);
            uint sVId;
            uint eVId;
            BRep.GetEdgeVertexIds(eId, out sVId, out eVId);
            e.SetVertexIds(sVId, eVId);
            System.Diagnostics.Debug.Assert(BRep.IsElemId(CadElementType.VERTEX, sVId));
            System.Diagnostics.Debug.Assert(BRep.IsElemId(CadElementType.VERTEX, eVId));
            System.Diagnostics.Debug.Assert(VertexArray.IsObjectId(sVId));
            System.Diagnostics.Debug.Assert(VertexArray.IsObjectId(eVId));
            e.SetVertexs(GetVertex(sVId), GetVertex(eVId));
            return e;
        }

        public bool GetEdgeColor(uint eId, double[] color)
        {
            if (!BRep.IsElemId(CadElementType.EDGE, eId))
            {
                return false;
            }
            if (!EdgeArray.IsObjectId(eId))
            {
                return false;
            }
            Edge2D e = EdgeArray.GetObject(eId);
            e.GetColor(color);
            return true;
        }

        public bool SetEdgeColor(uint eId, double[] color)
        {
            if (!BRep.IsElemId(CadElementType.EDGE, eId))
            {
                return false;
            }
            if (!EdgeArray.IsObjectId(eId))
            {
                return false;
            }
            Edge2D e = EdgeArray.GetObject(eId);
            e.SetColor(color);
            return true;
        }

        public bool GetEdgeVertexId(out uint sVId, out uint eVId, uint eId)
        {
            System.Diagnostics.Debug.Assert(BRep.IsElemId(CadElementType.EDGE, eId));
            return BRep.GetEdgeVertexIds(eId, out sVId, out eVId);
        }

        public uint GetEdgeVertexId(uint eId, bool isS)
        {
            System.Diagnostics.Debug.Assert(BRep.IsElemId(CadElementType.EDGE, eId));
            return BRep.GetEdgeVertexId(eId, isS);
        }

        public bool GetEdgeLoopId(out uint lLId, out uint rLId, uint eId)
        {
            return BRep.GetEdgeLoopId(eId, out lLId, out rLId);
        }

        public CurveType GetEdgeCurveType(uint eId)
        {
            System.Diagnostics.Debug.Assert(EdgeArray.IsObjectId(eId));
            Edge2D e = EdgeArray.GetObject(eId);
            return e.CurveType;
        }

        public bool GetCurveAsPolyline(uint eId, out IList<Vector2> points, double elen = -1)
        {
            points = new List<Vector2>();

            if (!EdgeArray.IsObjectId(eId))
            {
                return false;
            }
            Edge2D e = GetEdge(eId);
            double len = e.GetCurveLength();
            if (elen > 0)
            {
                uint ndiv = (uint)((len / elen) + 1);
                return e.GetCurveAsPolyline(out points, (int)ndiv);
            }
            return e.GetCurveAsPolyline(out points, -1);
        }

        public int GetLayer(CadElementType type, uint id)
        {
            if (type == CadElementType.LOOP)
            {
                if (!LoopArray.IsObjectId(id))
                {
                    return 0;
                }
                Loop2D l = LoopArray.GetObject(id);
                return (int)l.Layer;
            }
            else if (type == CadElementType.EDGE)
            {
                uint lLId;
                uint rLId;
                GetEdgeLoopId(out lLId, out rLId, id);

                bool bl = IsElemId(CadElementType.LOOP, lLId);
                bool br = IsElemId(CadElementType.LOOP, rLId);
                if (!bl && !br) { return 0; }
                if (bl && !br) { return GetLayer(CadElementType.LOOP, lLId); }
                if (!bl && br) { return GetLayer(CadElementType.LOOP, rLId); }
                int ilayer_l = GetLayer(CadElementType.LOOP, lLId);
                int ilayer_r = GetLayer(CadElementType.LOOP, rLId);
                return (ilayer_l > ilayer_r) ? ilayer_l : ilayer_r;
            }
            else if (type == CadElementType.VERTEX)
            {
                int layer = 0;
                bool iflg = true;
                for (ItrVertex itrv = BRep.GetItrVertex(id); !itrv.IsEnd(); itrv++)
                {
                    uint lId0 = itrv.GetLoopId();
                    if (!IsElemId(CadElementType.LOOP, lId0))
                    {
                        continue;
                    }
                    int layer0 = GetLayer(CadElementType.LOOP, lId0);
                    if (iflg == true)
                    {
                        layer = layer0;
                        iflg = false;
                    }
                    else
                    {
                        layer = (layer0 > layer) ? layer0 : layer;
                    }
                }
                return layer;
            }
            return 0;
        }

        public void GetLayerMinMax(out int minLayer, out int maxLayer)
        {
            IList<uint> lIds = GetElemIds(CadElementType.LOOP);
            if (lIds.Count == 0)
            {
                minLayer = 0;
                maxLayer = 0;
                return;
            }

            {
                System.Diagnostics.Debug.Assert(lIds.Count > 0);
                uint lId0 = lIds[0];
                minLayer = GetLayer(CadElementType.LOOP, lId0);
                maxLayer = minLayer;
            }
            for (int i = 0; i < lIds.Count; i++)
            {
                uint lId = lIds[i];
                int layer = GetLayer(CadElementType.LOOP, lId);
                minLayer = (layer < minLayer) ? layer : minLayer;
                maxLayer = (layer > maxLayer) ? layer : maxLayer;
            }
        }

        public bool GetLoopColor(uint id_l, double[] color)
        {
            if (!LoopArray.IsObjectId(id_l))
            {
                return false;
            }
            Loop2D l = LoopArray.GetObject(id_l);
            for (int i = 0; i < 3; i++)
            {
                color[i] = l.Color[i];
            }
            return true;
        }

        private int AssertValid()
        {
            {
                IList<uint> lIds = LoopArray.GetObjectIds();
                for (uint i = 0; i < lIds.Count; i++)
                {
                    uint lId = lIds[(int)i];
                    int res = CheckLoop(lId);
                    if (res != 0)
                    {
                        if (res == 1)
                        {
                            System.Diagnostics.Debug.WriteLine("Intersectoin in the loop");
                        }
                        else if (res == 2)
                        {
                            System.Diagnostics.Debug.WriteLine("Check area parent plus, childe minus");
                        }
                        else if (res == 3)
                        {
                            System.Diagnostics.Debug.WriteLine("Check whether childe loop included in parent loop");
                        }
                        else if (res == 4)
                        {
                            System.Diagnostics.Debug.WriteLine("Check childe loop excluded from other child loop");
                        }
                        else if (res == 5)
                        {
                            System.Diagnostics.Debug.WriteLine("Check positive angle around vertex on the loop");
                        }
                        return res;
                    }
                }
            }
            if (!BRep.AssertValid())
            {
                return 6;
            }
            {
                IList<uint> lIds = BRep.GetElemIds(CadElementType.LOOP);
                for (uint i = 0; i < lIds.Count; i++)
                {
                    if (!LoopArray.IsObjectId(lIds[(int)i]))
                    {
                        //System.Diagnostics.Debug.WriteLine(lIds[(int)i]);
                        return 7;
                    }
                }
            }
            {
                IList<uint> eIds = BRep.GetElemIds(CadElementType.EDGE);
                for (uint i = 0; i < eIds.Count; i++)
                {
                    if (!EdgeArray.IsObjectId(eIds[(int)i]))
                    {
                        return 7;
                    }
                }
            }
            {
                IList<uint> vIds = BRep.GetElemIds(CadElementType.VERTEX);
                for (uint i = 0; i < vIds.Count; i++)
                {
                    if (!VertexArray.IsObjectId(vIds[(int)i]))
                    {
                        return 7;
                    }
                }
            }
            return 0;
        }

        private bool CheckIsPointInsideItrLoop(ItrLoop itrl, Vector2 point)
        {
            // 29 is handy prim number
            for (uint i = 1; i < 29; i++)
            {
                uint crossCounter = 0;
                bool iflg = true;
                Vector2 dir = new Vector2((float)Math.Sin(6.28 * i / 29.0), (float)Math.Cos(6.28 * i / 29.0));
                for (itrl.Begin(); !itrl.IsEnd(); itrl++)
                {
                    uint eId;
                    bool isSameDir;
                    itrl.GetEdgeId(out eId, out isSameDir);
                    if (eId == 0)
                    {
                        return false;
                    }
                    System.Diagnostics.Debug.Assert(EdgeArray.IsObjectId(eId));
                    Edge2D e = GetEdge(eId);
                    int ires = e.NumIntersectAgainstHalfLine(point, dir);
                    if (ires == -1)
                    {
                        iflg = false;
                        break;
                    }
                    crossCounter += (uint)ires;
                }
                if (iflg == true)
                {
                    if (crossCounter % 2 == 0) return false;
                    return true;
                }
            }
            System.Diagnostics.Debug.Assert(false);
            return false;
        }

        private uint CheckInOutItrLoopPointItrLoop(ItrLoop itrl1, ItrLoop itrl2)
        {
            uint outCount = 0;
            uint inCount = 0;
            for (itrl1.Begin(); !itrl1.IsEnd(); itrl1++)
            {
                uint vId = itrl1.GetVertexId();
                Vertex2D v = VertexArray.GetObject(vId);
                double dist = DistancePointItrLoop(itrl2, v.Point);
                if (Math.Abs(dist) < MinClearance)
                {
                    return 1;
                } 
                if (CheckIsPointInsideItrLoop(itrl2, v.Point))
                {
                    if (outCount != 0)
                    {
                        return 1;
                    }
                    inCount++;
                }
                else
                {
                    if (inCount != 0)
                    {
                        return 1;
                    }
                    outCount++;
                }
            }
            if (inCount == 0)
            {
                System.Diagnostics.Debug.Assert(outCount != 0);
                return 2;
            }
            System.Diagnostics.Debug.Assert(outCount == 0);
            System.Diagnostics.Debug.Assert(inCount != 0);
            return 0;
        }

        public bool CheckIsPointInsideLoop(uint lId1, Vector2 point)
        {
            System.Diagnostics.Debug.Assert(LoopArray.IsObjectId(lId1));
            for (ItrLoop itrl = BRep.GetItrLoop(lId1); !itrl.IsChildEnd; itrl.ShiftChildLoop())
            {
                if (itrl.IsParent())
                {
                    if (!CheckIsPointInsideItrLoop(itrl, point))
                    {
                        return false;
                    }
                }
                else
                {
                    if (CheckIsPointInsideItrLoop(itrl, point))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public double GetLoopArea(uint lId)
        {
            System.Diagnostics.Debug.Assert(LoopArray.IsObjectId(lId));
            double area = 0.0;
            for (ItrLoop itrl = BRep.GetItrLoop(lId); !itrl.IsChildEnd; itrl.ShiftChildLoop())
            {
                area += GetItrLoopArea(itrl);
            }
            return area;
        }

        private double GetItrLoopArea(ItrLoop itrl)
        {
            double area = 0.0;
            for (itrl.Begin(); !itrl.IsEnd(); itrl++)
            {
                uint eId;
                bool isSameDir;
                itrl.GetEdgeId(out eId, out isSameDir);
                if (!IsElemId(CadElementType.EDGE, eId))
                {
                    return 0;
                }
                System.Diagnostics.Debug.Assert(IsElemId(CadElementType.EDGE, eId));
                Edge2D e = GetEdge(eId);
                System.Diagnostics.Debug.Assert(e.GetVertexId(isSameDir) == itrl.GetVertexId());
                System.Diagnostics.Debug.Assert(e.GetVertexId(!isSameDir) == itrl.GetAheadVertexId());
                double earea = CadUtils.TriArea(
                    e.GetVertex(true), e.GetVertex(false), new Vector2(0, 0)) + e.EdgeArea();
                if (isSameDir)
                {
                    area += earea;
                }
                else
                {
                    area -= earea;
                }
            }
            return area;
        }

        private double DistancePointItrLoop(ItrLoop itrl, Vector2 point)
        {
            double minDist = -1;
            for (itrl.Begin(); !itrl.IsEnd(); itrl++)
            {
                uint eId;
                bool isSameDir;
                itrl.GetEdgeId(out eId, out isSameDir);
                if (eId == 0)
                {
                    uint vId0 = itrl.GetVertexId();
                    System.Diagnostics.Debug.Assert(IsElemId(CadElementType.VERTEX, vId0));
                    Vector2 p1 = GetVertex(vId0);
                    return Vector2.Distance(point, p1);
                }
                System.Diagnostics.Debug.Assert(EdgeArray.IsObjectId(eId));
                Edge2D e = GetEdge(eId);
                Vector2 v = e.GetNearestPoint(point);
                double d0 = Vector2.Distance(v, point);
                if (minDist < 0 || d0 < minDist)
                {
                    minDist = d0;
                }
            }
            return minDist;
        }

        public double SignedDistancePointLoop(uint lId1, Vector2 point, uint ignoreVId = 0)
        {
            double minSd = 0;
            System.Diagnostics.Debug.Assert(LoopArray.IsObjectId(lId1));
            for (ItrLoop itrl = BRep.GetItrLoop(lId1); !itrl.IsChildEnd; itrl.ShiftChildLoop())
            {
                if (itrl.IsParent())
                {
                    minSd = DistancePointItrLoop(itrl, point);
                    System.Diagnostics.Debug.Assert(minSd >= 0);
                    if (!CheckIsPointInsideItrLoop(itrl, point))
                    {
                        minSd = -minSd;
                    }
                }
                else
                {
                    if (itrl.GetVertexId() == itrl.GetAheadVertexId())
                    {
                        uint vId = itrl.GetVertexId();
                        if (vId == ignoreVId)
                        {
                            continue;
                        }
                    }
                    double sd0 = DistancePointItrLoop(itrl, point);
                    if (sd0 < 0)
                    {
                        continue;
                    }
                    if (CheckIsPointInsideItrLoop(itrl, point))
                    {
                        sd0 = -sd0;
                    }
                    if (Math.Abs(sd0) < Math.Abs(minSd))
                    {
                        minSd = sd0;
                    }
                }
            }
            return minSd;
        }

        public ResConnectVertex ConnectVertexLine(uint vId1, uint vId2)
        {
            Edge2D e = new Edge2D(vId1, vId2);
            return ConnectVertex(e);
        }

        public ResConnectVertex ConnectVertex(Edge2D edge)
        {
            uint vId1 = edge.GetVertexId(true);
            uint vId2 = edge.GetVertexId(false);
            ResConnectVertex res = new ResConnectVertex();
            res.VId1 = vId1;
            res.VId2 = vId2;
            ////  
            if (!VertexArray.IsObjectId(vId1))
            {
                return res;
            }
            if (!VertexArray.IsObjectId(vId2))
            {
                return res;
            }
            if (vId1 == vId2)
            {
                return res;
            }

            if (edge.CurveType == CurveType.CURVE_LINE)
            {
                IList<uint> eIds = EdgeArray.GetObjectIds();
                for (uint i = 0; i < eIds.Count; i++)
                {
                    uint eId = eIds[(int)i];
                    System.Diagnostics.Debug.Assert(EdgeArray.IsObjectId(eId));
                    Edge2D e = GetEdge(eId);
                    if (e.CurveType != CurveType.CURVE_LINE)
                    {
                        continue;
                    }
                    uint sVId = e.GetVertexId(true);
                    uint eVId = e.GetVertexId(false);
                    if ((sVId - vId1) * (sVId - vId2) != 0)
                    {
                        continue;
                    }
                    if ((eVId - vId1) * (eVId - vId2) != 0)
                    {
                        continue;
                    }
                    return res;
                }
            }
            edge.SetVertexs(VertexArray.GetObject(vId1).Point, VertexArray.GetObject(vId2).Point);
            if (edge.IsCrossEdgeSelf())
            {
                return res;
            }

            {
                ItrVertex itrv1 = FindCornerHalfLine(vId1, edge.GetTangentEdge(true));
                ItrVertex itrv2 = FindCornerHalfLine(vId2, edge.GetTangentEdge(false));
                if (itrv1.GetLoopId() != itrv2.GetLoopId())
                {
                    return res;
                }
                uint lId = itrv1.GetLoopId();
                if (CheckEdgeAgainstLoopIntersection(edge, lId))
                {
                    return res;
                }
                bool isLeftAddL = false;
                if (itrv1.IsSameUseLoop(itrv2) && (!itrv1.IsParent() || lId == 0))
                {
                    IList<KeyValuePair<uint, bool>> eId2Dir = BRep.GetItrLoopConnectVertex(itrv1, itrv2);
                    System.Diagnostics.Debug.Assert(eId2Dir.Count != 0);
                    double area = CadUtils.TriArea(
                        edge.GetVertex(true), edge.GetVertex(false), new Vector2(0, 0)) + edge.EdgeArea();
                    for (uint i = 0; i < eId2Dir.Count; i++)
                    {
                        uint eId = eId2Dir[(int)i].Key;
                        System.Diagnostics.Debug.Assert(IsElemId(CadElementType.EDGE, eId));
                        Edge2D e = GetEdge(eId);
                        double earea = e.EdgeArea() +
                            CadUtils.TriArea(e.GetVertex(true), e.GetVertex(false), new Vector2(0, 0));
                        if (eId2Dir[(int)i].Value)
                        {
                            area += earea;
                        }
                        else
                        {
                            area -= earea;
                        }
                    }
                    isLeftAddL = (area > 0);
                }
                res = BRep.ConnectVertex(itrv1, itrv2, isLeftAddL);
            }
            {
                uint tmpId = EdgeArray.AddObject(new KeyValuePair<uint, Edge2D>(res.AddEId, edge));
                System.Diagnostics.Debug.Assert(tmpId == (int)res.AddEId);
            }

            {
                uint lLId;
                uint rLId;
                BRep.GetEdgeLoopId(res.AddEId, out lLId, out rLId);
                if (lLId == rLId)
                {
                    System.Diagnostics.Debug.Assert(AssertValid() == 0);
                    return res;
                }
                res.AddLId = (res.IsLeftAddL) ? lLId : rLId;
                System.Diagnostics.Debug.Assert(res.AddLId != res.LId || res.LId == 0);
                System.Diagnostics.Debug.Assert(((res.IsLeftAddL) ? lLId : rLId) == res.AddLId);
            }

            if (!BRep.IsElemId(CadElementType.LOOP, res.LId))
            {
                if (CheckLoopIntersection(res.AddLId))
                {
                    BRep.MakeHoleFromLoop(res.AddLId);
                    System.Diagnostics.Debug.Assert(AssertValid() == 0);
                    return res;
                }
            }

            if (BRep.IsElemId(CadElementType.LOOP, res.LId) && BRep.IsElemId(CadElementType.LOOP, res.AddLId))
            {
                for (;;)
                {
                    bool iflg = true;
                    ItrLoop itrlAddInner = BRep.GetItrLoopSideEdge(res.AddEId, res.IsLeftAddL);
                    ItrLoop itrlAddOuter = BRep.GetItrLoopSideEdge(res.AddEId, !res.IsLeftAddL);
                    for (ItrLoop itrlC = BRep.GetItrLoop(res.LId); !itrlC.IsChildEnd; itrlC.ShiftChildLoop())
                    {
                        if (itrlC.IsParent())
                        {
                            continue;
                        }
                        if (itrlC.IsSameUseLoop(itrlAddOuter))
                        {
                            continue;
                        }
                        uint ires = CheckInOutItrLoopPointItrLoop(itrlC, itrlAddInner);
                        System.Diagnostics.Debug.Assert(ires == 0 || ires == 2);
                        if (ires == 0)
                        {
                            BRep.SwapItrLoop(itrlC, res.AddLId);
                            iflg = false;
                            break;
                        }
                    }
                    if (iflg)
                    {
                        break;
                    }
                }
            }

            if (LoopArray.IsObjectId(res.LId))
            {
                Loop2D addLoop = LoopArray.GetObject(res.LId);
                LoopArray.AddObject(new KeyValuePair<uint, Loop2D>(res.AddLId, addLoop));
            }
            else
            {
                LoopArray.AddObject(new KeyValuePair<uint, Loop2D>(res.AddLId, new Loop2D()));
            }

            System.Diagnostics.Debug.Assert(AssertValid() == 0);
            return res;
        }

        private ItrVertex FindCornerHalfLine(uint vId, Vector2 dir1) 
        {
            System.Diagnostics.Debug.Assert(VertexArray.IsObjectId(vId));
            Vector2 dir = dir1;
            dir = CadUtils.Normalize(dir);
            Vector2 zeroVec = new Vector2(0, 0);
            ItrVertex itrv = BRep.GetItrVertex(vId);
            if (itrv.CountEdge() < 2)
            {
                return itrv;
            }
            for (; !itrv.IsEnd(); itrv++)
            {
                uint eId0;
                bool isSameDir0;
                itrv.GetBehindEdgeId(out eId0, out isSameDir0);
                System.Diagnostics.Debug.Assert(EdgeArray.IsObjectId(eId0));
                Edge2D e0 = GetEdge(eId0);
                System.Diagnostics.Debug.Assert(e0.GetVertexId(isSameDir0) == vId);
                Vector2 tan0 = e0.GetTangentEdge(isSameDir0);

                uint eId1;
                bool isSameDir1;
                itrv.GetAheadEdgeId(out eId1, out isSameDir1);
                System.Diagnostics.Debug.Assert(EdgeArray.IsObjectId(eId1));
                Edge2D e1 = GetEdge(eId1);
                System.Diagnostics.Debug.Assert(e1.GetVertexId(isSameDir1) == vId);
                Vector2 tan1 = e1.GetTangentEdge(isSameDir1);
                System.Diagnostics.Debug.Assert(eId0 != eId1);

                double area0 = CadUtils.TriArea(tan1, zeroVec, tan0);
                double area1 = CadUtils.TriArea(tan1, zeroVec, dir);
                double area2 = CadUtils.TriArea(dir, zeroVec, tan0);
                if (area0 > 0.0)
                {
                    if (area1 > 0.0 && area2 > 0.0)
                    {
                        return itrv;
                    }
                }
                else
                {
                    if (area1 > 0.0 || area2 > 0.0)
                    {
                        return itrv;
                    }
                }
            }
            return itrv;
        }

        public bool RemoveElement(CadElementType type, uint id)
        {
            if (!IsElemId(type, id))
            {
                return false;
            }
            if (type == CadElementType.EDGE)
            {
                ItrLoop itrlL = BRep.GetItrLoopSideEdge(id, true);
                ItrLoop itrlR = BRep.GetItrLoopSideEdge(id, false);
                uint lLId = itrlL.GetLoopId();
                uint rLId = itrlR.GetLoopId();
                uint vId1;
                uint vId2;
                BRep.GetEdgeVertexIds(id, out vId1, out vId2);
                ItrVertex itrv1 = BRep.GetItrVertex(vId1);
                ItrVertex itrv2 = BRep.GetItrVertex(vId2);
                bool isDelCP = false;
                if (itrlL.IsSameUseLoop(itrlR) && itrlL.IsParent() && itrlL.GetLoopId() != 0 &&
                    itrv1.CountEdge() > 1 && itrv2.CountEdge() > 1)
                {
                    IList<KeyValuePair<uint, bool>> eId2Dir = BRep.GetItrLoopRemoveEdge(id);
                    System.Diagnostics.Debug.Assert(eId2Dir.Count != 0);
                    {
                        int iE = 0;
                        for (; iE < eId2Dir.Count; iE++)
                        {
                            int jE = 0;
                            for (; jE < eId2Dir.Count; jE++)
                            {
                                if (iE == jE)
                                {
                                    continue;
                                }
                                if (eId2Dir[iE].Key == eId2Dir[jE].Key)
                                {
                                    System.Diagnostics.Debug.Assert(eId2Dir[iE].Value != eId2Dir[jE].Value);
                                    break;
                                }
                            }
                            if (jE == eId2Dir.Count)
                            {
                                break;
                            }
                        }
                        isDelCP = (iE == eId2Dir.Count);
                    }
                    if (!isDelCP)
                    {
                        double area = 0.0;
                        for (int iE = 0; iE < eId2Dir.Count; iE++)
                        {
                            uint eId = eId2Dir[iE].Key;
                            System.Diagnostics.Debug.Assert(IsElemId(CadElementType.EDGE, eId));
                            Edge2D e = GetEdge(eId);
                            double earea = e.EdgeArea() + 
                                CadUtils.TriArea(e.GetVertex(true), e.GetVertex(false), new Vector2(0, 0));
                            if (eId2Dir[iE].Value)
                            {
                                area += earea;
                            }
                            else
                            {
                                area -= earea;
                            }
                        }
                        if (area < 0)
                        {
                            isDelCP = true;
                        }
                    }
                }
                if (!BRep.RemoveEdge(id, isDelCP))
                {
                    System.Diagnostics.Debug.WriteLine( "Remove Edge B-Rep unsuccessfull : " + id);
                    return false;
                }
                EdgeArray.DeleteObject(id);
                if (!BRep.IsElemId(CadElementType.LOOP, lLId))
                {
                    LoopArray.DeleteObject(lLId);
                }
                if (!BRep.IsElemId(CadElementType.LOOP, rLId))
                {
                    LoopArray.DeleteObject(rLId);
                }
                System.Diagnostics.Debug.Assert(AssertValid() == 0);
                return true;
            }
            else if (type == CadElementType.VERTEX)
            {
                ItrVertex itrv = BRep.GetItrVertex(id);
                if (itrv.CountEdge() == 2)
                {
                    uint eId1;
                    uint eId2;
                    bool isSameDir1;
                    bool isSameDir2;
                    itrv.GetAheadEdgeId(out eId1, out isSameDir1);
                    itrv.GetBehindEdgeId(out eId2, out isSameDir2);
                    Edge2D tmpEdge = GetEdge(eId1);
                    {
                        uint vId2 = BRep.GetEdgeVertexId(eId2, !isSameDir2);
                        System.Diagnostics.Debug.Assert(BRep.GetEdgeVertexId(eId1, isSameDir1) == id);
                        System.Diagnostics.Debug.Assert(BRep.GetEdgeVertexId(eId2, isSameDir2) == id);
                        tmpEdge.ConnectEdge(GetEdge(eId2), !isSameDir1, isSameDir1 != isSameDir2);
                        if (isSameDir1)
                        {
                            tmpEdge.SetVertexIds(vId2, tmpEdge.GetVertexId(false));
                            tmpEdge.SetVertexs(GetVertex(vId2), tmpEdge.GetVertex(false));
                        }
                        else
                        {
                            tmpEdge.SetVertexIds(tmpEdge.GetVertexId(true), vId2);
                            tmpEdge.SetVertexs(tmpEdge.GetVertex(true), GetVertex(vId2));
                        }
                    }
                    {
                        uint iPt0 = tmpEdge.GetVertexId(true);
                        uint iPt1 = tmpEdge.GetVertexId(false);
                        BoundingBox2D iBB = tmpEdge.GetBoundingBox();
                        IList<uint> eIds = BRep.GetElemIds(CadElementType.EDGE);
                        for (int ijE = 0; ijE < eIds.Count; ijE++)
                        {
                            uint jEId = eIds[ijE];
                            if (jEId == eId2 || jEId == eId1)
                            {
                                continue;
                            }
                            Edge2D jEdge = GetEdge(jEId);
                            uint jPt0 = jEdge.GetVertexId(true);
                            uint jPt1 = jEdge.GetVertexId(false);
                            if ((iPt0 - jPt0) * (iPt0 - jPt1) * (iPt1 - jPt0) * (iPt1 - jPt1) != 0)
                            {
                                BoundingBox2D jBB = jEdge.GetBoundingBox();
                                if (!iBB.IsIntersect(jBB, MinClearance))
                                {
                                    continue;
                                }
                                double dist = tmpEdge.Distance(jEdge);
                                if (dist > MinClearance)
                                {
                                    continue;
                                }
                                return true;
                            }
                            else if (iPt0 == jPt0 && iPt1 == jPt1)
                            {
                                if (tmpEdge.IsCrossEdgeShareBothPoints(jEdge, true))
                                {
                                    return false;
                                }
                            }
                            else if (iPt0 == jPt1 && iPt1 == jPt0)
                            {
                                if (tmpEdge.IsCrossEdgeShareBothPoints(jEdge, false))
                                {
                                    return false;
                                }
                            }
                            else if (iPt0 == jPt0)
                            {
                                if (tmpEdge.IsCrossEdgeShareOnePoint(jEdge, true, true))
                                {
                                    return false;
                                }
                            }
                            else if (iPt0 == jPt1)
                            {
                                if (tmpEdge.IsCrossEdgeShareOnePoint(jEdge, true, false))
                                {
                                    return false;
                                }
                            }
                            else if (iPt1 == jPt0)
                            {
                                if (tmpEdge.IsCrossEdgeShareOnePoint(jEdge, false, true))
                                {
                                    return false;
                                }
                            }
                            else if (iPt1 == jPt1)
                            {
                                if (tmpEdge.IsCrossEdgeShareOnePoint(jEdge, false, false))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    if (!BRep.RemoveVertex(id))
                    {
                        return false;
                    }
                    System.Diagnostics.Debug.Assert(BRep.IsElemId(CadElementType.EDGE, eId1));
                    System.Diagnostics.Debug.Assert(!BRep.IsElemId(CadElementType.EDGE, eId2));
                    EdgeArray.DeleteObject(eId2);
                    Edge2D e1 = GetEdge(eId1);
                    e1.Copy(tmpEdge);
                    VertexArray.DeleteObject(id);
                    System.Diagnostics.Debug.Assert(AssertValid() == 0);
                    return true;
                }
                else if (itrv.CountEdge() == 0)
                {
                    if (!BRep.RemoveVertex(id)) { return false; }
                    VertexArray.DeleteObject(id);
                    System.Diagnostics.Debug.Assert(AssertValid() == 0);
                    return true;
                }
            }
            return false;
        }

        private int CheckLoop(uint lId)
        {
            {
                if (CheckLoopIntersection(lId))
                {
                    return 1;
                }
            }
            {
                for (ItrLoop itrl = BRep.GetItrLoop(lId); !itrl.IsChildEnd; itrl.ShiftChildLoop())
                {
                    if (itrl.IsParent())
                    {
                        if (itrl.GetType() != 2)
                        {
                            return 2;
                        }
                        if (GetItrLoopArea(itrl) < 0)
                        {
                            return 2;
                        }
                    }
                    else if (itrl.GetType() == 2)
                    {
                        if (GetItrLoopArea(itrl) > 0)
                        {
                            return 2;
                        }
                    }
                }
            }
            {
                ItrLoop pItrl = BRep.GetItrLoop(lId);
                for (ItrLoop cItrl = BRep.GetItrLoop(lId); !cItrl.IsChildEnd; cItrl.ShiftChildLoop())
                {
                    if (cItrl.IsParent()) continue;
                    if (CheckInOutItrLoopPointItrLoop(cItrl, pItrl) != 0)
                    {
                        return 3;
                    }
                }
            }
            {
                for (ItrLoop itrl1 = BRep.GetItrLoop(lId); !itrl1.IsChildEnd; itrl1.ShiftChildLoop())
                {
                    if (itrl1.IsParent())
                    {
                        continue;
                    }
                    for (ItrLoop itrl2 = BRep.GetItrLoop(lId); !itrl2.IsChildEnd; itrl2.ShiftChildLoop())
                    {
                        if (itrl2.IsParent())
                        {
                            continue;
                        }
                        if (itrl1.IsSameUseLoop(itrl2))
                        {
                            continue;
                        }
                        if (CheckInOutItrLoopPointItrLoop(itrl1, itrl2) != 2)
                        {
                            return 4;
                        }
                    }
                }
            }
            {
                Vector2 zeroVec = new Vector2(0, 0);
                for (ItrLoop itrl = BRep.GetItrLoop(lId); !itrl.IsChildEnd; itrl.ShiftChildLoop())
                {
                    for (itrl.Begin(); !itrl.IsEnd(); itrl++)
                    {
                        uint vmId;
                        uint vfId;
                        Vector2 dir;
                        {
                            vmId = itrl.GetVertexId();
                            vfId = itrl.GetAheadVertexId();
                            uint eId0;
                            bool isSameDir0;
                            itrl.GetEdgeId(out eId0, out isSameDir0);
                            if (!EdgeArray.IsObjectId(eId0))
                            {
                                continue;
                            }
                            Edge2D e0 = GetEdge(eId0);
                            dir = e0.GetTangentEdge(isSameDir0);
                            System.Diagnostics.Debug.Assert(e0.GetVertexId(isSameDir0) == vmId);
                            System.Diagnostics.Debug.Assert(e0.GetVertexId(!isSameDir0) == vfId);
                        }
                        for (ItrVertex vItr = BRep.GetItrVertex(vmId); !vItr.IsEnd(); vItr++)
                        {   // 点周りの辺をめぐる
                            uint eId0;
                            bool isSameDir0;
                            vItr.GetBehindEdgeId(out eId0, out isSameDir0);
                            if (!EdgeArray.IsObjectId(eId0))
                            {
                                continue;
                            }
                            Edge2D e0 = GetEdge(eId0);
                            System.Diagnostics.Debug.Assert(e0.GetVertexId(isSameDir0) == vmId);
                            if (e0.GetVertexId(!isSameDir0) == vfId)
                            {
                                continue;
                            }
                            Vector2 tan0 = e0.GetTangentEdge(isSameDir0);
                            uint eId1;
                            bool isSameDir1;
                            vItr.GetAheadEdgeId(out eId1, out isSameDir1);
                            if (!EdgeArray.IsObjectId(eId1))
                            {
                                continue;
                            }
                            Edge2D e1 = GetEdge(eId1);
                            System.Diagnostics.Debug.Assert(e1.GetVertexId(isSameDir1) == vmId);
                            if (e1.GetVertexId(!isSameDir1) == vfId)
                            {
                                continue;
                            }
                            Vector2 tan1 = e1.GetTangentEdge(isSameDir1);
                            double area0 = CadUtils.TriArea(tan1, zeroVec, tan0);
                            double area1 = CadUtils.TriArea(tan1, zeroVec, dir);
                            double area2 = CadUtils.TriArea(dir, zeroVec, tan0);
                            if ((area0 > 0.0 && area1 > 0.0 && area2 > 0.0) ||
                                   (area0 < 0.0 && (area1 > 0.0 || area2 > 0.0)))
                            {
                                return 5;
                            }
                        }
                    }
                }
            }
            return 0;
        }

        private bool CheckLoopIntersection(uint lId)
        {
            IList<uint> eIds = new List<uint>();
            if (BRep.IsElemId(CadElementType.LOOP, lId))
            {
                for (ItrLoop itrl = BRep.GetItrLoop(lId); !itrl.IsChildEnd; itrl.ShiftChildLoop())
                {
                    for (itrl.Begin(); !itrl.IsEnd(); itrl++)
                    {
                        uint eId;
                        bool isSameDir;
                        if (!itrl.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;
                        }
                        if (itrl.IsEdgeBothSideSameLoop() && !isSameDir)
                        {
                            continue;
                        }
                        eIds.Add(eId);
                    }
                }
            }
            else
            {
                eIds = GetElemIds(CadElementType.EDGE);
            }

            int eIdCnt = eIds.Count;
            for (int iE = 0; iE < eIdCnt; iE++)
            {
                Edge2D edge = GetEdge(eIds[iE]);
                if (edge.IsCrossEdgeSelf())
                {
                    return true;
                }
                uint iPt0 = edge.GetVertexId(true);
                uint iPt1 = edge.GetVertexId(false);
                BoundingBox2D iBB = edge.GetBoundingBox();
                for (int jE = iE + 1; jE < eIdCnt; jE++)
                {
                    Edge2D jEdge = GetEdge(eIds[jE]);
                    uint jPt0 = jEdge.GetVertexId(true);
                    uint jPt1 = jEdge.GetVertexId(false);
                    if ((iPt0 - jPt0) * (iPt0 - jPt1) * (iPt1 - jPt0) * (iPt1 - jPt1) != 0)
                    {
                        BoundingBox2D jBB = jEdge.GetBoundingBox();
                        if (!iBB.IsIntersect(jBB, MinClearance))
                        {
                            continue;
                        }
                        double dist = edge.Distance(jEdge);
                        if (dist < MinClearance)
                        {
                            return true;
                        }
                        continue;
                    }
                    else if (iPt0 == jPt0 && iPt1 == jPt1)
                    {
                        if (edge.IsCrossEdgeShareBothPoints(jEdge, true))
                        {
                            return true;
                        }
                    }
                    else if (iPt0 == jPt1 && iPt1 == jPt0)
                    {
                        if (edge.IsCrossEdgeShareBothPoints(jEdge, false))
                        {
                            return true;
                        }
                    }
                    else if (iPt0 == jPt0)
                    {
                        if (edge.IsCrossEdgeShareOnePoint(jEdge, true, true))
                        {
                            return true;
                        }
                    }
                    else if (iPt0 == jPt1)
                    {
                        if (edge.IsCrossEdgeShareOnePoint(jEdge, true, false))
                        {
                            return true;
                        }
                    }
                    else if (iPt1 == jPt0)
                    {
                        if (edge.IsCrossEdgeShareOnePoint(jEdge, false, true))
                        {
                            return true;
                        }
                    }
                    else if (iPt1 == jPt1)
                    {
                        if (edge.IsCrossEdgeShareOnePoint(jEdge, false, false))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool CheckEdgeAgainstLoopIntersection(Edge2D edge, uint lId)
        {
            IList<uint> edgeIds = new List<uint>();
            if (BRep.IsElemId(CadElementType.LOOP, lId))
            {
                for (ItrLoop itrl = BRep.GetItrLoop(lId); !itrl.IsChildEnd; itrl.ShiftChildLoop())
                {
                    for (itrl.Begin(); !itrl.IsEnd(); itrl++)
                    {
                        uint eId;
                        bool isSameDir;
                        if (!itrl.GetEdgeId(out eId, out isSameDir))
                        {
                            continue;
                        }
                        if (itrl.IsEdgeBothSideSameLoop() && !isSameDir)
                        {
                            continue;
                        }
                        edgeIds.Add(eId);
                    }
                }
            }
            else
            {
                edgeIds = GetElemIds(CadElementType.EDGE);
            }

            uint edgeIdCnt = (uint)edgeIds.Count;
            if (edge.IsCrossEdgeSelf())
            {
                return true;
            }
            uint iPt0 = edge.GetVertexId(true);
            uint iPt1 = edge.GetVertexId(false);
            BoundingBox2D iBB = edge.GetBoundingBox();
            for (int jE = 0; jE < edgeIdCnt; jE++)
            {
                Edge2D jEdge = GetEdge(edgeIds[jE]);
                uint jPt0 = jEdge.GetVertexId(true);
                uint jPt1 = jEdge.GetVertexId(false);
                if ((iPt0 - jPt0) * (iPt0 - jPt1) * (iPt1 - jPt0) * (iPt1 - jPt1) != 0)
                {
                    BoundingBox2D jBB = jEdge.GetBoundingBox();
                    if (!iBB.IsIntersect(jBB, MinClearance))
                    {
                        continue;
                    }
                    double dist = edge.Distance(jEdge);
                    if (dist < MinClearance)
                    {
                        return true;
                    }
                    continue;
                }
                else if (iPt0 == jPt0 && iPt1 == jPt1)
                {
                    if (edge.IsCrossEdgeShareBothPoints(jEdge, true))
                    {
                        return true;
                    }
                }
                else if (iPt0 == jPt1 && iPt1 == jPt0)
                {
                    if (edge.IsCrossEdgeShareBothPoints(jEdge, false))
                    {
                        return true;
                    }
                }
                else if (iPt0 == jPt0)
                {
                    if (edge.IsCrossEdgeShareOnePoint(jEdge, true, true))
                    {
                        return true;
                    }
                }
                else if (iPt0 == jPt1)
                {
                    if (edge.IsCrossEdgeShareOnePoint(jEdge, true, false))
                    {
                        return true;
                    }
                }
                else if (iPt1 == jPt0)
                {
                    if (edge.IsCrossEdgeShareOnePoint(jEdge, false, true))
                    {
                        return true;
                    }
                }
                else if (iPt1 == jPt1)
                {
                    if (edge.IsCrossEdgeShareOnePoint(jEdge, false, false))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public ItrLoop GetItrLoop(uint lId)
        {
            return new ItrLoop(BRep, lId);
        }

    }
}
