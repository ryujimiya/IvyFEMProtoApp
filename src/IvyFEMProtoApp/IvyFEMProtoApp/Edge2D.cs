using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    class Edge2D : IObject
    {
        public CurveType CurveType { get; private set; } = CurveType.CURVE_LINE;
        private double[] Color = new double[3];

        private bool IsLeftSide;
        private double Dist;
        private IList<double> RelCos = new List<double>();
        private uint SVId = 0;
        private uint EVId = 0;
        private Vector2 SPt;
        private Vector2 EPt;
        private BoundingBox2D BB = new BoundingBox2D();
        private IList<Vector2> Coords = new List<Vector2>();

        public Edge2D()
        {
            for (int i = 0; i < 3; i++)
            {
                Color[i] = 0.0;
            }
        }

        public Edge2D(uint sVId, uint eVId)
        {
            SVId = sVId;
            EVId = eVId;
            for (int i = 0; i < 3; i++)
            {
                Color[i] = 0.0;
            }
        }

        public Edge2D(Edge2D src)
        {
            Copy(src);
        }

        public void Copy(IObject src)
        {
            Edge2D srcEdge = src as Edge2D;
            CurveType = srcEdge.CurveType;
            IsLeftSide = srcEdge.IsLeftSide;
            Dist = srcEdge.Dist;
            RelCos.Clear();
            foreach (var relco in srcEdge.RelCos)
            {
                RelCos.Add(relco);
            }
            SVId = srcEdge.SVId;
            EVId = srcEdge.EVId;
            for (int i = 0; i < 3; i++)
            {
                Color[i] = srcEdge.Color[i];
            }
            SPt = new Vector2(srcEdge.SPt.X, srcEdge.SPt.Y);
            EPt = new Vector2(srcEdge.EPt.X, srcEdge.EPt.Y);
            BB.Copy(srcEdge.BB);
            Coords.Clear();
            foreach (var co in srcEdge.Coords)
            {
                Coords.Add(co);
            }
        }

        public string Dump()
        {
            string ret = "";
            string CRLF = System.Environment.NewLine;

            ret += "■Edge2D" + CRLF;
            ret += "Type = " + CurveType + CRLF;
            ret += "IsLeftSide = " + IsLeftSide + CRLF;
            ret += "Dist = " + Dist + CRLF;
            ret += "RelCos" + CRLF;
            for (int i = 0; i < RelCos.Count; i++)
            {
                var relco = RelCos[i];
                ret += "RelCo[" + i + "] = " + relco + CRLF;
            }
            ret += "SVId = " + SVId + CRLF;
            ret += "EVId = " + EVId + CRLF;
            ret += "Color" + CRLF;
            for (int i = 0; i < 3; i++)
            {
                ret += "Color[" + i + "] = " + Color[i] + CRLF;
            }
            ret += "SPt = (" + SPt.X + ", " + SPt.Y + ")" + CRLF;
            ret += "EPt = (" + EPt.X + ", " + EPt.Y + ")" + CRLF;
            ret += "BB" + CRLF;
            ret += "Cos" + CRLF;
            for (int i = 0; i < Coords.Count; i++)
            {
                var co = Coords[i];
                ret += "Cos[" + i + "] = " + co + CRLF;
            }
            ret += BB.Dump();

            return ret;
        }

        public void SetCurveLine()
        {
            CurveType = CurveType.CURVE_LINE;
        }

        public void SetCurveArc(bool isLeftSide, double dist)
        {
            CurveType = CurveType.CURVE_ARC;
            Dist = dist;
            IsLeftSide = isLeftSide;
        }

        public void SetCurvePolyline(IList<double> relCos)
        {
            CurveType = CurveType.CURVE_POLYLINE;
            SetCurveRelPoint(relCos);
        }

        public void SetCurveBezier(double cx0, double cy0, double cx1, double cy1)
        {
            CurveType = CurveType.CURVE_BEZIER;
            RelCos.Clear();
            for (int i = 0; i < 4; i++)
            {
                RelCos.Add(0);
            }
            RelCos[0] = cx0;
            RelCos[1] = cy0;
            RelCos[2] = cx1;
            RelCos[3] = cy1;
            Vector2 v0 = EPt - SPt;
            Vector2 v1 = new Vector2(-v0.Y, v0.X);
            for (uint i = 0; i < 2; i++)
            {
                Vector2 pt0 = SPt + v0 * (float)RelCos[(int)(i * 2 + 0)] +
                    v1 * (float)RelCos[(int)(i * 2 + 1)];
                Coords.Add(pt0);
            }
        }

        public void GetCurveArc(out bool isLeftSide, out double dist)
        {
            isLeftSide = IsLeftSide;
            dist = Dist;
        }

        public IList<double> GetCurveRelPoint()
        {
            return RelCos;
        }

        public void SetCurveRelPoint(IList<double> relCo0)
        {
            RelCos = relCo0;
            Vector2 h = EPt - SPt;
            Vector2 v = new Vector2(-h.Y, h.X);
            uint n = (uint)(RelCos.Count / 2);
            int coCnt = Coords.Count;
            for (int i = coCnt; i < n; i++)
            {
                Coords.Add(new Vector2());
            }
            for (uint i = 0; i < n; i++)
            {
                Vector2 p = SPt + h * (float)RelCos[(int)(i * 2 + 0)] +
                    v * (float)RelCos[(int)(i * 2 + 1)];
                Coords[(int)i] = p;
            }
        }

        public IList<Vector2> GetCurvePoint()
        {
            return Coords;
        }

        public void SetVertexs(Vector2 sPt, Vector2 ePt)
        {
            SPt = sPt;
            EPt = ePt;
            BB.IsntEmpty = false;

            uint n = (uint)(RelCos.Count / 2);
            int coCnt = Coords.Count;
            for (int i = coCnt; i < n; i++)
            {
                Coords.Add(new Vector2());
            }
            if (n > 0)
            {
                Vector2 gh = EPt - SPt;
                Vector2 gv = new Vector2(-gh.Y, gh.X);
                for (uint i = 0; i < n; i++)
                {
                    Vector2 scPt = SPt + gh * (float)RelCos[(int)(i * 2 + 0)] +
                        gv * (float)RelCos[(int)(i * 2 + 1)];
                    Coords[(int)i] = scPt;
                }
            }
        }

        public Vector2 GetVertex(bool isRoot)
        {
            return isRoot ? SPt : EPt;
        }

        public void SetVertexIds(uint vSId, uint vEId)
        {
            SVId = vSId;
            EVId = vEId;
        }

        public uint GetVertexId(bool isRoot)
        {
            return isRoot ? SVId : EVId;
        }

        public void GetColor(double[] color)
        {
            for (int i = 0; i < 3; i++)
            {
                color[i] = Color[i];
            }
        }

        public void SetColor(double[] color)
        {
            for (int i = 0; i < 3; i++)
            {
                Color[i] = color[i];
            }
        }

        public double Distance(Edge2D e1)
        {
            Vector2 sPt1 = e1.SPt;
            Vector2 ePt1 = e1.EPt;
            if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_LINE)
            {
                return CadUtils.GetDistanceLineSegLineSeg(SPt, EPt, sPt1, ePt1);
            }
            else if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_ARC)
            {
                Vector2 cPt1;
                double radius1 = 0;
                e1.GetCenterRadius(out cPt1, out radius1);
                return CadUtils.GetDistanceLineSegArc(SPt, EPt, e1.SPt, e1.EPt, cPt1, radius1, e1.IsLeftSide);
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_LINE)
            {
                return e1.Distance(this);
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_ARC)
            {
                Vector2 cPt0;
                double radius0 = 0;
                GetCenterRadius(out cPt0, out radius0);
                Vector2 cPt1;
                double radius1 = 0;
                e1.GetCenterRadius(out cPt1, out radius1);

                Vector2 pt0;
                Vector2 pt1;
                bool isCrossCircle01 = false;
                if (CadUtils.IsCrossCircleCircle(cPt0, radius0, cPt1, radius1, out pt0, out pt1))
                {
                    isCrossCircle01 = true;
                    if (IsDirectionArc(pt0) != 0 && e1.IsDirectionArc(pt0) != 0)
                    {
                        return -1;
                    }
                    if (IsDirectionArc(pt1) != 0 && e1.IsDirectionArc(pt1) != 0)
                    {
                        return -1;
                    }
                }
                double minDistS0 = CadUtils.GetDistancePointArc(SPt, e1.SPt, e1.EPt, cPt1, radius1, e1.IsLeftSide);
                double minDistE0 = CadUtils.GetDistancePointArc(EPt, e1.SPt, e1.EPt, cPt1, radius1, e1.IsLeftSide);
                double minDistS1 = CadUtils.GetDistancePointArc(e1.SPt, SPt, EPt, cPt0, radius0, IsLeftSide);
                double minDistE1 = CadUtils.GetDistancePointArc(e1.EPt, SPt, EPt, cPt0, radius0, IsLeftSide);
                double minDist0 = (minDistS0 < minDistE0) ? minDistS0 : minDistE0;
                double minDist1 = (minDistS1 < minDistE1) ? minDistS1 : minDistE1;
                double minDist = (minDist0 < minDist1) ? minDist0 : minDist1;
                if (!isCrossCircle01)
                {
                    bool isC0InsideC1 = Vector2.Distance(cPt0, cPt1) < radius1;
                    bool isC1InsideC0 = Vector2.Distance(cPt1, cPt0) < radius0;
                    if (!isC0InsideC1 && !isC1InsideC0)
                    {
                        Vector2 v1 = CadUtils.GetProjectedPointOnCircle(cPt1, radius1, cPt0);
                        Vector2 v0 = CadUtils.GetProjectedPointOnCircle(cPt0, radius0, cPt1);
                        if (e1.IsDirectionArc(v1) != 0 && IsDirectionArc(v0) != 0)
                        {
                            double d0 = Vector2.Distance(v0, v1);
                            minDist = (d0 < minDist) ? d0 : minDist;
                        }
                    }
                    else
                    {
                        if (radius0 < radius1)
                        {
                            Vector2 v1 = CadUtils.GetProjectedPointOnCircle(cPt1, radius1, cPt0);
                            Vector2 v0 = CadUtils.GetProjectedPointOnCircle(cPt0, radius0, v1);
                            if (e1.IsDirectionArc(v1) != 0 && IsDirectionArc(v0) != 0)
                            {
                                double d0 = Vector2.Distance(v0, v1);
                                minDist = (d0 < minDist) ? d0 : minDist;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(isC1InsideC0);
                            Vector2 v0 = CadUtils.GetProjectedPointOnCircle(cPt0, radius0, cPt1);
                            Vector2 v1 = CadUtils.GetProjectedPointOnCircle(cPt1, radius1, v0);
                            if (e1.IsDirectionArc(v1) != 0 && IsDirectionArc(v0) != 0)
                            {
                                double d0 = Vector2.Distance(v0, v1);
                                minDist = (d0 < minDist) ? d0 : minDist;
                            }
                        }
                    }
                }
                return minDist;
            }
            else if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div1 = (uint)(e1.Coords.Count + 1);
                double minDist = -1;
                for (uint iDiv = 0; iDiv < div1; iDiv++)
                {
                    Vector2 pt0 = (iDiv == 0) ? e1.SPt : e1.Coords[(int)(iDiv - 1)];
                    Vector2 pt1 = (iDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(iDiv + 0)];
                    double dist = CadUtils.GetDistanceLineSegLineSeg(SPt, EPt, pt0, pt1);
                    if (dist < -0.5)
                    {
                        return dist;
                    }
                    if (dist < minDist || minDist < -0.5)
                    {
                        minDist = dist;
                    }
                }
                return minDist;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_LINE)
            {
                return e1.Distance(this);
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_ARC)
            {
                return e1.Distance(this);
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                Vector2 cPt0;
                double radius0;
                GetCenterRadius(out cPt0, out radius0);

                uint div1 = (uint)(e1.Coords.Count + 1);
                double minDist = -1;
                for (uint iDiv = 0; iDiv < div1; iDiv++)
                {
                    Vector2 pt0 = (iDiv == 0) ? e1.SPt : e1.Coords[(int)(iDiv - 1)];
                    Vector2 pt1 = (iDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(iDiv + 0)];
                    double dist = CadUtils.GetDistanceLineSegArc(pt0, pt1, SPt, EPt, cPt0, radius0, IsLeftSide);
                    if (dist < -0.5)
                    {
                        return dist;
                    }
                    if (dist < minDist || minDist < -0.5)
                    {
                        minDist = dist;
                    }
                }
                return minDist;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div0 = (uint)(Coords.Count + 1);
                uint div1 = (uint)(e1.Coords.Count + 1);
                double minDist = -1;
                for (uint iDiv = 0; iDiv < div0; iDiv++)
                {
                    Vector2 iPt0 = (iDiv == 0) ? SPt : Coords[(int)(iDiv - 1)];
                    Vector2 iPt1 = (iDiv == div0 - 1) ? EPt : Coords[(int)(iDiv + 0)];
                    for (uint jDiv = 0; jDiv < div1; jDiv++)
                    {
                        Vector2 jPt0 = (jDiv == 0) ? e1.SPt : e1.Coords[(int)(jDiv - 1)];
                        Vector2 jPt1 = (jDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(jDiv + 0)];
                        double dist = CadUtils.GetDistanceLineSegLineSeg(iPt0, iPt1, jPt0, jPt1);
                        if (dist < -0.5)
                        {
                            return -1;
                        }
                        if (minDist < -0.5 || dist < minDist)
                        {
                            minDist = dist;
                        }
                    }
                }
                return minDist;
            }
            return 1;
        }

        public BoundingBox2D GetBoundingBox()
        {
            if (BB.IsntEmpty)
            {
                return BB;
            }
            double minX;
            double maxX;
            double minY;
            double maxY;
            GetBoundingBox(out minX, out maxX, out minY, out maxY);
            BB = new BoundingBox2D(minX, maxX, minY, maxY);
            return BB;
        }

        private void GetBoundingBox(out double minX, out double maxX, out double minY, out double maxY )
        {
            minX = (SPt.X < EPt.X) ? SPt.X : EPt.X;
            maxX = (SPt.X > EPt.X) ? SPt.X : EPt.X;
            minY = (SPt.Y < EPt.Y) ? SPt.Y : EPt.Y;
            maxY = (SPt.Y > EPt.Y) ? SPt.Y : EPt.Y;

            if (CurveType ==CurveType.CURVE_LINE)
            {
                return;
            }
            else if (CurveType == CurveType.CURVE_ARC)
            {
                Vector2 cPt;
                double radius;
                GetCenterRadius(out cPt, out radius);

                Vector2 tmpV;
                tmpV.X = cPt.X + (float)radius;
                tmpV.Y = cPt.Y;
                if (IsDirectionArc(tmpV) == 1)
                {
                    maxX = (tmpV.X > maxX) ? tmpV.X : maxX;
                }
                tmpV.X = cPt.X - (float)radius;
                tmpV.Y = cPt.Y;
                if (IsDirectionArc(tmpV) == 1)
                {
                    minX = (tmpV.X < minX) ? tmpV.X : minX;
                }

                tmpV.X = cPt.X;
                tmpV.Y = cPt.Y + (float)radius;
                if (IsDirectionArc(tmpV) == 1)
                {
                    maxY = (tmpV.Y > maxY) ? tmpV.Y : maxY;
                }

                tmpV.X = cPt.X;
                tmpV.Y = cPt.Y - (float)radius;
                if (IsDirectionArc(tmpV) == 1)
                {
                    minY = (tmpV.Y < minY) ? tmpV.Y : minY;
                }
            }
            else if (CurveType == CurveType.CURVE_POLYLINE || CurveType == CurveType.CURVE_BEZIER)
            {
                uint coCnt = (uint)Coords.Count;
                if (CurveType == CurveType.CURVE_BEZIER)
                {

                    System.Diagnostics.Debug.Assert(coCnt == 2);
                }

                for (int i = 0; i < coCnt; i++)
                {
                    Vector2 pt0 = Coords[i];

                    minX = (pt0.X < minX) ? pt0.X : minX;
                    maxX = (pt0.X > maxX) ? pt0.X : maxX;
                    minY = (pt0.Y < minY) ? pt0.Y : minY;
                    maxY = (pt0.Y > maxY) ? pt0.Y : maxY;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
                throw new InvalidOperationException();
            }
        }

        public bool IsCrossEdgeSelf()
        {
            if (CurveType == CurveType.CURVE_LINE || CurveType == CurveType.CURVE_ARC)
            {
                return false;
            }
            if (CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div = (uint)(Coords.Count + 1);
                for (uint iDiv = 0; iDiv < div; iDiv++)
                {
                    Vector2 iPt0 = (iDiv == 0) ? SPt : Coords[(int)(iDiv - 1)];
                    Vector2 iPt1 = (iDiv == div - 1) ? EPt : Coords[(int)(iDiv + 0)];
                    for (uint jDiv = iDiv + 2; jDiv < div; jDiv++)
                    {
                        Vector2 jPt0 = (jDiv == 0) ? SPt : Coords[(int)(jDiv - 1)];
                        Vector2 jPt1 = (jDiv == div - 1) ? EPt : Coords[(int)(jDiv + 0)];
                        if (CadUtils.IsCrossLineSegLineSeg(iPt0, iPt1, jPt0, jPt1))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_BEZIER)
            { 
                // TODO
                return false;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return false;
        }

        public bool IsCrossEdge(Edge2D e1)
        {
            Vector2 sPt1 = e1.SPt;
            Vector2 ePt1 = e1.EPt;
            if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_LINE)
            {
                return CadUtils.IsCrossLineSegLineSeg(SPt, EPt, sPt1, ePt1);
            }
            else if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_ARC)
            {
                Vector2 cPt1;
                double radius1;
                e1.GetCenterRadius(out cPt1, out radius1);
                double t0;
                double t1;
                if (!CadUtils.IsCrossLineCircle(cPt1, radius1, SPt, EPt, out t0, out t1))
                {
                    return false;
                }
                if (0 < t0 && t0 < 1 && e1.IsDirectionArc(SPt + (EPt - SPt) * (float)t0) == 1)
                {
                    return true;
                }
                if (0 < t1 && t1 < 1 && e1.IsDirectionArc(SPt + (EPt - SPt) * (float)t1) == 1)
                {
                    return true;
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdge(this);
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_ARC)
            {
                Vector2 cPt0;
                double radius0;
                GetCenterRadius(out cPt0, out radius0);
                Vector2 cPt1;
                double radius1;
                e1.GetCenterRadius(out cPt1, out radius1);

                Vector2 pt0;
                Vector2 pt1;
                if (!CadUtils.IsCrossCircleCircle(cPt0, radius0, cPt1, radius1, out pt0, out pt1))
                {
                    return false;
                }
                if (IsDirectionArc(pt0) != 0 && e1.IsDirectionArc(pt0) != 0)
                {
                    return true;
                }
                if (IsDirectionArc(pt1) != 0 && e1.IsDirectionArc(pt1) != 0)
                {
                    return true;
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div1 = (uint)(e1.Coords.Count + 1);
                for (uint iDiv = 0; iDiv < div1; iDiv++)
                {
                    Vector2 pt0 = (iDiv == 0) ? e1.SPt : e1.Coords[(int)(iDiv - 1)];
                    Vector2 pt1 = (iDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(iDiv + 0)];
                    if (CadUtils.IsCrossLineSegLineSeg(SPt, EPt, pt0, pt1))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdge(this);
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_ARC)
            {
                return e1.IsCrossEdge(this);
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                Vector2 cPt0;
                double radius0;
                GetCenterRadius(out cPt0, out radius0);

                uint div1 = (uint)(e1.Coords.Count + 1);
                for (uint iDiv = 0; iDiv < div1; iDiv++)
                {
                    Vector2 pt0 = (iDiv == 0) ? e1.SPt : e1.Coords[(int)(iDiv - 1)];
                    Vector2 pt1 = (iDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(iDiv + 0)];
                    double t0;
                    double t1;
                    if (!CadUtils.IsCrossLineCircle(cPt0, radius0, pt0, pt1, out t0, out t1))
                    {
                        continue;
                    }
                    if (0 < t0 && t0 < 1 && IsDirectionArc(pt0 + (pt1 - pt0) * (float)t0) == 1)
                    {
                        return true;
                    }
                    if (0 < t1 && t1 < 1 && IsDirectionArc(pt0 + (pt1 - pt0) * (float)t1) == 1)
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div0 = (uint)(Coords.Count + 1);
                uint div1 = (uint)(e1.Coords.Count + 1);

                for (uint iDiv = 0; iDiv < div0; iDiv++)
                {
                    Vector2 iPt0 = (iDiv == 0) ? SPt : Coords[(int)(iDiv - 1)];
                    Vector2 iPt1 = (iDiv == div0 - 1) ? EPt : Coords[(int)(iDiv + 0)];
                    for (uint jDiv = 0; jDiv < div1; jDiv++)
                    {
                        Vector2 jPt0 = (jDiv == 0) ? e1.SPt : e1.Coords[(int)(iDiv - 1)];
                        Vector2 jPt1 = (jDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(iDiv + 0)];
                        if (CadUtils.IsCrossLineSegLineSeg(iPt0, iPt1, jPt0, jPt1))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return true;
        }

        public bool IsCrossEdgeShareOnePoint(Edge2D e1, bool isShareS0, bool isShareS1)
        {

            Vector2 sPt1 = e1.SPt;
            Vector2 ePt1 = e1.EPt;
            if (isShareS0 && isShareS1)
            {
                System.Diagnostics.Debug.Assert(CadUtils.SquareLength(SPt, sPt1) < 1.0e-20);
            }
            if (isShareS0 && !isShareS1)
            {
                System.Diagnostics.Debug.Assert(CadUtils.SquareLength(SPt, ePt1) < 1.0e-20);
            }
            if (!isShareS0 && isShareS1)
            {
                System.Diagnostics.Debug.Assert(CadUtils.SquareLength(EPt, sPt1) < 1.0e-20);
            }
            if (!isShareS0 && !isShareS1)
            {
                System.Diagnostics.Debug.Assert(CadUtils.SquareLength(EPt, ePt1) < 1.0e-20);
            }
            if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_LINE)
            {
                return false;
            }
            else if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_ARC)
            {
                Vector2 cPt1;
                double radius1;
                e1.GetCenterRadius(out cPt1, out radius1);

                double t0, t1;
                if (!CadUtils.IsCrossLineCircle(cPt1, radius1, SPt, EPt, out t0, out t1))
                {
                    return false;
                }
                Vector2 p0 = SPt + (EPt - SPt) * (float)t0;
                Vector2 p1 = SPt + (EPt - SPt) * (float)t1;
                if (!isShareS0)
                {
                    t0 = 1 - t0;
                    t1 = 1 - t1;
                }
                if (Math.Abs(t0) < Math.Abs(t1) && 0 < t1 && t1 < 1 && e1.IsDirectionArc(p1) != 0)
                {
                    System.Diagnostics.Debug.Assert(Math.Abs(t0) < 1.0e-5);
                    return true;
                }
                if (Math.Abs(t0) > Math.Abs(t1) && 0 < t0 && t0 < 1 && e1.IsDirectionArc(p0) != 0)
                {
                    System.Diagnostics.Debug.Assert(Math.Abs(t1) < 1.0e-5);
                    return true;
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdgeShareOnePoint(this, isShareS1, isShareS0);
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_ARC)
            {
                Vector2 cPt0;
                double radius0;
                GetCenterRadius(out cPt0, out radius0);

                Vector2 cPt1;
                double radius1;
                e1.GetCenterRadius(out cPt1, out radius1);

                Vector2 pt0;
                Vector2 pt1;
                bool isCross = CadUtils.IsCrossCircleCircle(cPt0, radius0, cPt1, radius1, out pt0, out pt1);
                if (!isCross)
                {
                    return false;
                }

                Vector2 sharePt;
                if (isShareS0)
                {
                    sharePt = SPt;
                }
                else
                {
                    sharePt = EPt;
                }
                double sqDist0 = CadUtils.SquareLength(sharePt, pt0);
                double sqDist1 = CadUtils.SquareLength(sharePt, pt1);
                if (sqDist0 < sqDist1 && IsDirectionArc(pt1) != 0 && e1.IsDirectionArc(pt1) != 0)
                {
                    System.Diagnostics.Debug.Assert(sqDist0 < 1.0e-20);
                    return true;
                }
                if (sqDist0 > sqDist1 && IsDirectionArc(pt0) != 0 && e1.IsDirectionArc(pt0) != 0)
                {
                    System.Diagnostics.Debug.Assert(sqDist1 < 1.0e-20);
                    return true;
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div1 = (uint)(e1.Coords.Count + 1);
                uint iSDiv = (isShareS1) ? 1u : 0;
                uint iEDiv = (isShareS1) ? div1 : div1 - 1;
                for (uint iDiv = iSDiv; iDiv < iEDiv; iDiv++)
                {
                    Vector2 pt0 = (iDiv == 0) ? e1.SPt : e1.Coords[(int)(iDiv - 1)];
                    Vector2 pt1 = (iDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(iDiv + 0)];
                    if (CadUtils.IsCrossLineSegLineSeg(SPt, EPt, pt0, pt1))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdgeShareOnePoint(this, isShareS1, isShareS0);
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                Vector2 cPt0;
                double radius0;
                GetCenterRadius(out cPt0, out radius0);

                uint div1 = (uint)(e1.Coords.Count + 1);
                uint iSDiv = (isShareS1) ? 1u : 0;
                uint iEDiv = (isShareS1) ? div1 : div1 - 1;
                for (uint iDiv = iSDiv; iDiv < iEDiv; iDiv++)
                {
                    Vector2 pt0 = (iDiv == 0) ? e1.SPt : e1.Coords[(int)(iDiv - 1)];
                    Vector2 pt1 = (iDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(iDiv + 0)];
                    double t0;
                    double t1;
                    if (!CadUtils.IsCrossLineCircle(cPt0, radius0, pt0, pt1, out t0, out t1))
                    {
                        continue;
                    }
                    if (0 < t0 && t0 < 1 && IsDirectionArc(pt0 + (pt1 - pt0) * (float)t0) == 1)
                    {
                        return true;
                    }
                    if (0 < t1 && t1 < 1 && IsDirectionArc(pt0 + (pt1 - pt0) * (float)t1) == 1)
                    {
                        return true;
                    }
                }
                {
                    Vector2 pt0;
                    Vector2 pt1;
                    if (isShareS1)
                    {
                        pt0 = e1.SPt;
                        pt1 = e1.Coords[0]; 
                    }
                    else
                    {
                        pt1 = e1.EPt;
                        pt0 = e1.Coords[e1.Coords.Count - 1];
                    }
                    double t0, t1;
                    if (!CadUtils.IsCrossLineCircle(cPt0, radius0, pt0, pt1, out t0, out t1))
                    {
                        return false;
                    }
                    Vector2 r0 = pt0 + (pt1 - pt0) * (float)t0;
                    Vector2 r1 = pt0 + (pt1 - pt0) * (float)t1;
                    // この後t0,t1は共有点との距離計算に使われる
                    if (!isShareS1)
                    {
                        t0 = 1 - t0;
                        t1 = 1 - t1;
                    }
                    if (Math.Abs(t0) < Math.Abs(t1) && 0 < t1 && t1 < 1 && IsDirectionArc(r1) != 0)
                    {
                        System.Diagnostics.Debug.Assert(Math.Abs(t0) < 1.0e-5);
                        return true;
                    }
                    if (Math.Abs(t0) > Math.Abs(t1) && 0 < t0 && t0 < 1 && IsDirectionArc(r0) != 0)
                    {
                        System.Diagnostics.Debug.Assert(Math.Abs(t1) < 1.0e-5);
                        return true;
                    }
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_ARC)
            {
                return e1.IsCrossEdgeShareOnePoint(this, isShareS1, isShareS0);
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div0 = (uint)(Coords.Count + 1);
                uint div1 = (uint)(e1.Coords.Count + 1);
                uint iExcDiv0 = (isShareS0) ? 0 : div0 - 1;
                uint iExcDiv1 = (isShareS1) ? 0 : div1 - 1;

                for (uint iDiv = 0; iDiv < div0; iDiv++)
                {
                    Vector2 iPt0 = (iDiv == 0) ? SPt : Coords[(int)(iDiv - 1)];
                    Vector2 iPt1 = (iDiv == div0 - 1) ? EPt : Coords[(int)(iDiv + 0)];
                    for (uint jDiv = 0; jDiv < div1; jDiv++)
                    {
                        if (iDiv == iExcDiv0 && jDiv == iExcDiv1)
                        {
                            continue;
                        }
                        Vector2 jPt0 = (jDiv == 0) ? e1.SPt : e1.Coords[(int)(jDiv - 1)];
                        Vector2 jPt1 = (jDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(jDiv + 0)];
                        if (CadUtils.IsCrossLineSegLineSeg(iPt0, iPt1, jPt0, jPt1))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_BEZIER || e1.CurveType == CurveType.CURVE_BEZIER)
            {
                return false;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return false;
        }

        public bool IsCrossEdgeShareBothPoints(Edge2D e1, bool isShareS0S1)
        {
            if (isShareS0S1)
            {
                System.Diagnostics.Debug.Assert(CadUtils.SquareLength(SPt, e1.SPt) < 1.0e-20 &&
                    CadUtils.SquareLength(EPt, e1.EPt) < 1.0e-20);
            }
            else
            {
                System.Diagnostics.Debug.Assert(CadUtils.SquareLength(SPt, e1.EPt) < 1.0e-20 &&
                    CadUtils.SquareLength(EPt, e1.SPt) < 1.0e-20);
            }

            if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_LINE)
            {
                return true;
            }
            else if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_ARC)
            {
                return false;
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdgeShareBothPoints(this, isShareS0S1);
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_ARC)
            {
                Vector2 cPt0;
                double radius0;
                GetCenterRadius(out cPt0, out radius0);

                Vector2 cPt1;
                double radius1;
                e1.GetCenterRadius(out cPt1, out radius1);

                if (CadUtils.SquareLength(cPt0 - cPt1) < 1.0e-10)
                {
                    return true;
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_LINE && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div1 = (uint)(e1.Coords.Count + 1);
                for (uint iDiv = 1; iDiv < div1 - 1; iDiv++)
                {
                    Vector2 pt0 = (iDiv == 0) ? e1.SPt : e1.Coords[(int)(iDiv - 1)];
                    Vector2 pt1 = (iDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(iDiv + 0)];
                    if (CadUtils.IsCrossLineSegLineSeg(SPt, EPt, pt0, pt1))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdgeShareBothPoints(this, isShareS0S1);
            }
            else if (CurveType == CurveType.CURVE_ARC && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                Vector2 cPt0;
                double radius0;
                GetCenterRadius(out cPt0, out radius0);
                uint div1 = (uint)(e1.Coords.Count + 1);
                for (uint iDiv = 1; iDiv < div1 - 1; iDiv++)
                {
                    Vector2 pt0 = (iDiv == 0) ? e1.SPt : e1.Coords[(int)(iDiv - 1)];
                    Vector2 pt1 = (iDiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(iDiv + 0)];
                    double t0;
                    double t1;
                    if (!CadUtils.IsCrossLineCircle(cPt0, radius0, pt0, pt1, out t0, out t1))
                    {
                        continue;
                    }
                    if (0 < t0 && t0 < 1 && IsDirectionArc(pt0 + (pt1 - pt0) * (float)t0) == 1)
                    {
                        return true;
                    }
                    if (0 < t1 && t1 < 1 && IsDirectionArc(pt0 + (pt1 - pt0) * (float)t1) == 1)
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_ARC)
            {
                return e1.IsCrossEdgeShareBothPoints(this, isShareS0S1);
            }
            else if (CurveType == CurveType.CURVE_POLYLINE && e1.CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div0 = (uint)(Coords.Count + 1);
                uint div1 = (uint)(e1.Coords.Count + 1);
                for (uint idiv = 1; idiv < div0 - 1; idiv++)
                {
                    Vector2 iPt0 = (idiv == 0) ? SPt : Coords[(int)(idiv - 1)];
                    Vector2 iPt1 = (idiv == div0 - 1) ? EPt : Coords[(int)(idiv + 0)];
                    for (uint jdiv = 1; jdiv < div1 - 1; jdiv++)
                    {
                        Vector2 jPt0 = (jdiv == 0) ? e1.SPt : e1.Coords[(int)(idiv - 1)];
                        Vector2 jPt1 = (jdiv == div1 - 1) ? e1.EPt : e1.Coords[(int)(idiv + 0)];
                        if (CadUtils.IsCrossLineSegLineSeg(iPt0, iPt1, jPt0, jPt1))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return false;
        }

        private int NumCrossArcLineSeg(Vector2 sPt1, Vector2 ePt1)
        {
            if (CurveType != CurveType.CURVE_ARC)
            {
                return -1;
            }

            Vector2 cPt0;
            double radius0;
            GetCenterRadius(out cPt0, out radius0);

            bool isOut0 = CadUtils.SquareLength(cPt0, sPt1) > radius0 * radius0;
            bool isOut1 = CadUtils.SquareLength(cPt0, ePt1) > radius0 * radius0;
            if (!isOut0 && !isOut1)
            {
                return 0;
            }

            bool isArcSide0 = (CadUtils.TriArea(EPt, sPt1, SPt) > 0.0) == IsLeftSide;
            bool isArcSide1 = (CadUtils.TriArea(EPt, ePt1, SPt) > 0.0) == IsLeftSide;
            if (!isArcSide0 && !isArcSide1)
            {
                return 0;
            }

            if ((!isOut0 && isOut1) || (isOut0 && !isOut1))
            {
                if (isArcSide0 && isArcSide1)
                {
                    return 1;
                }
                bool isCross;
                if (CadUtils.IsCrossLineSegLineSeg(SPt, EPt, sPt1, ePt1))
                {
                    if (isOut0)
                    {
                        isCross = isArcSide0;
                    }
                    else
                    {
                        isCross = isArcSide1;
                    }
                }
                else
                {
                    if (isOut0)
                    {
                        isCross = !isArcSide0;
                    }
                    else
                    {
                        isCross = !isArcSide1;
                    }
                }
                if (isCross)
                {
                    return 1;
                }
                return 0;
            }

            if (isOut0 && isOut1)
            {
                if (CadUtils.IsCrossLineSegLineSeg(SPt, EPt, sPt1, ePt1))
                {
                    return 1;
                }
                Vector2 foot;
                {
                    Vector2 v01 = new Vector2(ePt1.X - sPt1.X, ePt1.Y - sPt1.Y);
                    Vector2 vc0 = new Vector2(sPt1.X - cPt0.X, sPt1.Y - cPt0.Y);
                    Vector2 vc1 = new Vector2(ePt1.X - cPt0.X, ePt1.Y - cPt0.Y);
                    double dc0 = Vector2.Dot(vc0, v01);
                    double dc1 = Vector2.Dot(vc1, v01);
                    if (dc0 * dc1 > 0.0)
                    {
                        return 0;
                    }
                    double r0 = -dc0 / (-dc0 + dc1);
                    double r1 = dc1 / (-dc0 + dc1);
                    foot = sPt1 * (float)r1 + ePt1 * (float)r0;
                }
                if (CadUtils.SquareLength(cPt0, foot) > radius0 * radius0)
                {
                    return 0;
                }
                if ((CadUtils.TriArea(SPt, foot, EPt) > 0.0) == IsLeftSide)
                {
                    return 0;
                }
                return 2;
            }
            System.Diagnostics.Debug.Assert(false);
            return 0;
        }

        public double GetCurveLength()
        {
            if (CurveType == CurveType.CURVE_LINE)
            {
                Vector2 h0 = SPt - EPt;
                return h0.Length();
            }
            else if (CurveType == CurveType.CURVE_ARC)
            {
                double radius;
                double theta;
                Vector2 cPt;
                Vector2 lx;
                Vector2 ly;
                GetCenterRadiusThetaLXY(out cPt, out radius, out theta, out lx, out ly);
                return radius * theta;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div = (uint)(Coords.Count + 1);
                double totLen = 0;
                for (uint iDiv = 0; iDiv < div; iDiv++)
                {
                    Vector2 pt0 = (iDiv == 0) ? SPt : Coords[(int)(iDiv - 1)];
                    Vector2 pt1 = (iDiv == div - 1) ? EPt : Coords[(int)(iDiv + 0)];
                    totLen += Vector2.Distance(pt0, pt1);
                }
                return totLen;
            }
            else if (CurveType == CurveType.CURVE_BEZIER)
            {
                // TODO : use the Sympthon's rule integration 
                Vector2 scPt = Coords[0];
                Vector2 ecPt = Coords[1];
                uint div = 16;
                double divT = 1.0 / (div);
                double edgeLen = 0;
                for (uint i = 0; i < div; i++)
                {
                    double t0 = (i + 0) * divT;
                    double t1 = (i + 1) * divT;
                    Vector2 vect0 = (float)(-3 * (1 - t0) * (1 - t0)) * SPt +
                        (float)(3 * (3 * t0 - 1) * (t0 - 1)) * scPt -
                        (float)(3 * t0 * (3 * t0 - 2)) * ecPt +
                        (float)(3 * t0 * t0) * EPt;
                    Vector2 vect1 = (float)(-3 * (1 - t1) * (1 - t1)) * SPt +
                        (float)(3 * (3 * t1 - 1) * (t1 - 1)) * scPt -
                        (float)(3 * t1 * (3 * t1 - 2)) * ecPt +
                        (float)(3 * t1 * t1) * EPt;
                    double aveLen = (vect0.Length() + vect1.Length()) * 0.5;
                    edgeLen += aveLen * divT;
                }
                return edgeLen;
            }
            return 0;
        }

        public bool GetCurveAsPolyline(out IList<Vector2> pts, int div)
        {
            pts = new List<Vector2>();
            if (div <= 0)
            {
                if (CurveType == CurveType.CURVE_LINE)
                {
                    return true;
                }
                else if (CurveType == CurveType.CURVE_ARC)
                {
                    double radius;
                    double theta;
                    Vector2 cPt;
                    Vector2 lx;
                    Vector2 ly;
                    GetCenterRadiusThetaLXY(out cPt, out radius, out theta, out lx, out ly);
                    uint div1 = (uint)(theta * 360 / (5.0 * 6.28) + 1);
                    return GetCurveAsPolyline(out pts, (int)div1);
                }
                else if (CurveType == CurveType.CURVE_POLYLINE)
                {
                    uint nPt = (uint)Coords.Count;
                    for (uint i = 0; i < nPt; i++)
                    {
                        pts.Add(Coords[(int)i]);
                    }
                    return true;
                }
                else if (CurveType == CurveType.CURVE_BEZIER)
                {
                    return GetCurveAsPolyline(out pts, 16);
                }
            }
            else
            { 
                if (CurveType == CurveType.CURVE_LINE)
                {
                    Vector2 tDiv = (EPt - SPt) * (float)(1.0 / div);
                    for (uint iDiv = 1; iDiv < div; iDiv++){
                        Vector2 vec0 = SPt + tDiv * iDiv;
                        pts.Add(vec0);
                    }
                    System.Diagnostics.Debug.Assert(pts.Count <= div);
                    return true;
                }
                else if (CurveType == CurveType.CURVE_ARC)
                {
                    double radius;
                    double theta;
                    Vector2 cPt;
                    Vector2 lx;
                    Vector2 ly;
                    GetCenterRadiusThetaLXY(out cPt, out radius, out theta, out lx, out ly);
                    double thetaDiv = theta / div;
                    for (uint i = 1; i < div; i++)
                    {
                        double curTheta = i * thetaDiv;
                        Vector2 vec0 = ((float)Math.Cos(curTheta) * lx +
                            (float)Math.Sin(curTheta) * ly) * (float)radius + cPt;
                        pts.Add(vec0);
                    }
                    System.Diagnostics.Debug.Assert(pts.Count <= div);
                    return true;
                }
                else if (CurveType == CurveType.CURVE_POLYLINE)
                {
                    // mesh
                    double totLen = GetCurveLength();
                    uint div0 = (uint)(Coords.Count + 1);
                    uint div1 = (uint)div;
                    uint ptCnt1 = (uint)(div - 1);
                    double lDiv = totLen / div1;
                    uint iCurDiv0 = 0;
                    double curRatio0 = 0;
                    for (uint iPt1 = 0; iPt1 < ptCnt1; iPt1++)
                    {
                        double curRestLen0 = lDiv;
                        for (;;)
                        {
                            System.Diagnostics.Debug.Assert(iCurDiv0 < Coords.Count + 1);
                            System.Diagnostics.Debug.Assert(curRatio0 >= 0 && curRatio0 <= 1);
                            Vector2 pt0 = (iCurDiv0 == 0) ? SPt :Coords[(int)(iCurDiv0 - 1)];
                            Vector2 pt1 = (iCurDiv0 == div0 - 1) ? EPt : Coords[(int)(iCurDiv0 + 0)];
                            double iDivLen0 = Vector2.Distance(pt0, pt1);
                            if (iDivLen0 * (1 - curRatio0) > curRestLen0)
                            {
                                curRatio0 += curRestLen0 / iDivLen0;
                                Vector2 p = pt0 * (float)(1.0 - curRatio0) + pt1 * (float)curRatio0;
                                pts.Add(p);
                                break;
                            }
                            curRestLen0 -= iDivLen0 * (1 - curRatio0);
                            iCurDiv0 += 1;
                            curRatio0 = 0;
                        }
                    }
                    return true;
                }
                else if (CurveType == CurveType.CURVE_BEZIER)
                {
                    System.Diagnostics.Debug.Assert(Coords.Count == 2);
                    Vector2 scPt = Coords[0];
                    Vector2 ecPt = Coords[1];
                    double divT = 1.0 / (div);
                    for (uint i = 1; i < div; i++)
                    {
                        double t = i * divT;
                        Vector2 vec0 = (float)((1 - t) * (1 - t) * (1 - t)) * SPt +
                            (float)(3 * (1 - t) * (1 - t) * t) * scPt +
                            (float)(3 * (1 - t) * t * t) * ecPt +
                            (float)(t * t * t) * EPt;
                        pts.Add(vec0);
                    }
                    return true;
                }
            }
            return false;
        }

        public bool GetCenterRadius(out Vector2 cPt, out double radius)
        {
            if (CurveType != CurveType.CURVE_ARC)
            {
                cPt = new Vector2();
                radius = 0;
                return false;
            }

            double edgeLen = Vector2.Distance(SPt, EPt);
            Vector2 hPt = new Vector2((SPt.X + EPt.X) * 0.5f, (SPt.Y + EPt.Y) * 0.5f);
            Vector2 vv = new Vector2(SPt.Y - hPt.Y, hPt.X - SPt.X);
            double vvLen = vv.Length();
            vv.X /= (float)vvLen;
            vv.Y /= (float)vvLen;
            cPt.X = hPt.X + vv.X * (float)(this.Dist * edgeLen);
            cPt.Y = hPt.Y + vv.Y * (float)(this.Dist * edgeLen);
            Vector2 vcs = new Vector2(SPt.X - cPt.X, SPt.Y - cPt.Y);
            radius = vcs.Length();
            return true;
        }

        private int IsDirectionArc(Vector2 pt)
        {
            if (CurveType != CurveType.CURVE_ARC)
            {
                return -1;
            }
            Vector2 cPt;
            double radius = 0;
            GetCenterRadius(out cPt, out radius);
            if (IsLeftSide)
            {
                if (CadUtils.TriArea(SPt, cPt, EPt) > 0.0)
                {
                    if (CadUtils.TriArea(SPt, cPt, pt) > 0.0 && CadUtils.TriArea(pt, cPt, EPt) > 0.0)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    if (CadUtils.TriArea(SPt, cPt, pt) > 0.0 || CadUtils.TriArea(pt, cPt, EPt) > 0.0)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            else
            {
                if (CadUtils.TriArea(EPt, cPt, SPt) > 0.0)
                {
                    if (CadUtils.TriArea(EPt, cPt, pt) > 0.0 && CadUtils.TriArea(pt, cPt, SPt) > 0.0)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    if (CadUtils.TriArea(EPt, cPt, pt) > 0.0 || CadUtils.TriArea(pt, cPt, SPt) > 0.0)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            return -1;
        }

        public bool GetCenterRadiusThetaLXY(out Vector2 cPt, out double radius,
            out double theta, out Vector2 lx, out Vector2 ly)
        {
            cPt = new Vector2(0, 0);
            radius = 0;
            theta = 0;
            lx = new Vector2(0, 0);
            ly = new Vector2(0, 0);

            if (CurveType != CurveType.CURVE_ARC)
            {
                return false;
            }

            GetCenterRadius(out cPt, out radius);
            Vector2 rsV = new Vector2(SPt.X - cPt.X, SPt.Y - cPt.Y);
            lx.X = rsV.X / (float)radius;
            lx.Y = rsV.Y / (float)radius;
            if (IsLeftSide)
            {
                ly.X = lx.Y;
                ly.Y = -lx.X;
            }
            else
            {
                ly.X = -lx.Y;
                ly.Y = lx.X;
            }
            Vector2 reV = new Vector2(EPt.X-cPt.X, EPt.Y - cPt.Y);
            System.Diagnostics.Debug.Assert(Math.Abs(CadUtils.SquareLength(rsV) - CadUtils.SquareLength(reV)) < 1.0e-10);
            double x = Vector2.Dot(reV, lx);
            double y = Vector2.Dot(reV, ly);
            theta = Math.Atan2(y, x);
            const double PI = Math.PI;
            if (theta < 0.0)
            {
                theta += 2.0 * PI;
            }
            return true;
        }

        public Vector2 GetNearestPoint(Vector2 pt)
        {
            if (CurveType == CurveType.CURVE_LINE)
            {
                double t = CadUtils.FindNearestPointParameterLinePoint(pt, SPt, EPt);
                if (t < 0)
                {
                    return SPt;
                }
                if (t > 1)
                {
                    return EPt;
                }
                else
                {
                    return SPt + (EPt - SPt) * (float)t;
                }
            }
            else if (CurveType == CurveType.CURVE_ARC)
            {
                double radius;
                Vector2 cPt;

                GetCenterRadius(out cPt, out radius);

                double len = Vector2.Distance(cPt, pt);
                if (len < 1.0e-10)
                {
                    return SPt;
                }

                Vector2 pPt = cPt * (float)(1 - radius / len) + pt * (float)(radius / len);
                if (IsDirectionArc(pPt) != 0)
                {
                    return pPt;
                }
                double sDist = CadUtils.SquareLength(pt, SPt);
                double eDist = CadUtils.SquareLength(pt, EPt);
                if (sDist < eDist)
                {
                    return SPt;
                }
                else
                {
                    return EPt;
                }
            }
            else if (CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div = (uint)(Coords.Count + 1);
                double minDist = Vector2.Distance(SPt, pt);
                Vector2 cand = new Vector2(SPt.X, SPt.Y);
                for (uint iDiv = 0; iDiv < div; iDiv++)
                {
                    Vector2 iPt0 = (iDiv == 0) ? SPt : Coords[(int)(iDiv - 1)];
                    Vector2 iPt1 = (iDiv == div - 1) ? EPt : Coords[(int)(iDiv + 0)];
                    if (Vector2.Distance(pt, iPt1) < minDist)
                    {
                        minDist = Vector2.Distance(pt, iPt1);
                        cand = iPt1;
                    }
                    double t = CadUtils.FindNearestPointParameterLinePoint(pt, iPt0, iPt1);
                    if (t < 0.01 || t > 0.99)
                    {
                        continue;
                    }
                    Vector2 midPt = iPt0 + (iPt1 - iPt0) * (float)t;
                    if (Vector2.Distance(pt, midPt) < minDist)
                    {
                        minDist = Vector2.Distance(pt, midPt);
                        cand = midPt;
                    }
                }
                return cand;
            }

            System.Diagnostics.Debug.Assert(false);
            Vector2 zeroV = new Vector2(0,0);
            return zeroV;
        }

        public Vector2 GetTangentEdge(bool isS)
        {
            if (CurveType == CurveType.CURVE_LINE)
            {
                Vector2 d = (isS) ? EPt - SPt : SPt - EPt;
                d = CadUtils.Normalize(d);
                return d;
            }
            else if (CurveType == CurveType.CURVE_ARC)
            {
                double radius;
                Vector2 cPt;
                GetCenterRadius(out cPt, out radius);
                Vector2 h = (isS) ? SPt - cPt : EPt - cPt;
                Vector2 v;
                if ((isS && !IsLeftSide) || (!isS && IsLeftSide))
                {
                    v.X = -h.Y;
                    v.Y = h.X;
                }
                else
                {
                    v.X = h.Y;
                    v.Y = -h.X;
                }
                v = CadUtils.Normalize(v);
                return v;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE)
            {
                if (isS)
                {
                    if (Coords.Count == 0)
                    {
                        return EPt - SPt;
                    }
                    Vector2 d = Coords[1] - SPt;
                    d = CadUtils.Normalize(d);
                    return d;
                }
                else
                {
                    if (Coords.Count == 0)
                    {
                        return SPt - EPt;
                    }
                    Vector2 d = Coords[Coords.Count - 1] - EPt;
                    d = CadUtils.Normalize(d);
                    return d;
                }
            }
            else if (CurveType == CurveType.CURVE_BEZIER)
            {
                Vector2 scPt = Coords[0];
                Vector2 ecPt = Coords[1];
                Vector2 d = (isS) ? scPt - SPt : ecPt - EPt;
                d = CadUtils.Normalize(d);
                return d;
            }

            System.Diagnostics.Debug.Assert(false);
            Vector2 zeroV = new Vector2(0,0);
            return zeroV;
        }

        public bool Split(Edge2D addEdge, Vector2 addPt)
        {
            if (CurveType == CurveType.CURVE_LINE)
            {

            }
            else if (CurveType == CurveType.CURVE_ARC)
            {
                Vector2 sPt = SPt;
                Vector2 ePt = EPt;
                Vector2 cPt;
                double r;
                GetCenterRadius(out cPt, out r);
                Dist = CadUtils.TriHeight(cPt, sPt, addPt) / Vector2.Distance(sPt, addPt);
                addEdge.CurveType = CurveType.CURVE_ARC;
                addEdge.IsLeftSide = IsLeftSide;
                addEdge.Dist = CadUtils.TriHeight(cPt, addPt, ePt) / Vector2.Distance(addPt, ePt);
            }
            else if (CurveType == CurveType.CURVE_POLYLINE)
            {
                Vector2 sPt = SPt;
                Vector2 ePt = EPt;
                IList<Vector2> cos = new List<Vector2>(Coords);
                int iEPt0;
                int iSPt1;
                {
                    bool isSegment = false;
                    int ind = -1;
                    double minDist = Vector2.Distance(sPt, addPt);
                    for (uint iDiv = 0; iDiv < cos.Count + 1; iDiv++)
                    {
                        Vector2 iPt0 = (iDiv == 0) ? sPt : cos[(int)(iDiv - 1)];
                        Vector2 iPt1 = (iDiv == cos.Count) ? ePt : cos[(int)iDiv];
                        if (Vector2.Distance(addPt, iPt1) < minDist)
                        {
                            isSegment = false;
                            ind = (int)iDiv;
                            minDist = Vector2.Distance(addPt, iPt1);
                        }
                        double t = CadUtils.FindNearestPointParameterLinePoint(addPt, iPt0, iPt1);
                        if (t < 0.01 || t > 0.99)
                        {
                            continue;
                        }
                        Vector2 midPt = iPt0 + (iPt1 - iPt0) * (float)t;
                        if (Vector2.Distance(addPt, midPt) < minDist)
                        {
                            isSegment = true;
                            minDist = Vector2.Distance(addPt, midPt);
                            ind = (int)iDiv;
                        }
                    }

                    if (isSegment)
                    {
                        iEPt0 = ind - 2;
                        iSPt1 = ind + 1;
                    }
                    else
                    {
                        iEPt0 = ind - 1;
                        iSPt1 = ind + 1;
                    }
                }

                if (iEPt0 > 0)
                {
                    CurveType = CurveType.CURVE_POLYLINE;
                    uint nPt = (uint)(iEPt0 + 1);
                    int nCos = Coords.Count;
                    for (int i = nCos; i < nPt; i++)
                    {
                        Coords.Add(new Vector2());
                    }
                    for (uint ipt = 0; ipt < nPt; ipt++)
                    {
                        Coords[(int)ipt] = cos[(int)ipt];
                    }
                }
                else
                {
                    CurveType = CurveType.CURVE_LINE;
                }

                if (iSPt1 < (int)cos.Count)
                {
                    addEdge.CurveType = CurveType.CURVE_POLYLINE;
                    uint nPt = (uint)(cos.Count - iSPt1);
                    int nCos = addEdge.Coords.Count;
                    for (int i = nCos; i < nPt; i++)
                    {
                        addEdge.Coords.Add(new Vector2());
                    }
                    for (uint ipt = 0; ipt < nPt; ipt++)
                    {
                        addEdge.Coords[(int)ipt] = cos[(int)ipt + iSPt1];
                    }
                }
                else
                {
                    addEdge.CurveType = CurveType.CURVE_LINE;
                }
            }
            return true;
        }

        public double EdgeArea()
        {
            if (CurveType == CurveType.CURVE_LINE)
            {
                return 0;
            }
            else if (CurveType == CurveType.CURVE_ARC)
            {
                Vector2 cPt;
                double radius;
                GetCenterRadius(out cPt, out radius);
                Vector2 scV = SPt - cPt;
                Vector2 ax = scV * (float)(1 / radius);
                Vector2 ay;
                if (IsLeftSide)
                {
                    ay.X = ax.Y;
                    ay.Y = -ax.X;
                }
                else
                {
                    ay.X = -ax.Y;
                    ay.Y = ax.X;
                }
                Vector2 ecV = EPt - cPt;
                double x = Vector2.Dot(ecV, ax);
                double y = Vector2.Dot(ecV, ay);
                double theta = Math.Atan2(y, x);
                const double PI = Math.PI;
                if (theta < 0.0)
                {
                    theta += 2.0 * PI;
                }
                double segmentArea = theta * radius * radius * 0.5;
                segmentArea -= Math.Abs(CadUtils.TriArea(SPt, cPt, EPt));
                if (IsLeftSide)
                {
                    segmentArea *= -1;
                }
                return segmentArea;
            }
            else if (CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div = (uint)(Coords.Count + 1);
                if (div == 1) { return 0; }
                Vector2 eh = EPt - SPt;
                eh = CadUtils.Normalize(eh);
                Vector2 ev = new Vector2(eh.Y, -eh.X);
                double area = 0;
                for (uint iDiv = 0; iDiv < div; iDiv++)
                {
                    Vector2 pt0 = (iDiv == 0) ? SPt : Coords[(int)(iDiv - 1)];
                    Vector2 pt1 = (iDiv == div - 1) ? EPt : Coords[(int)(iDiv + 0)];
                    double h0 = Vector2.Dot(pt0 - SPt, ev);
                    double h1 = Vector2.Dot(pt1 - SPt, ev);
                    double divx = Vector2.Dot(pt1 - pt0, eh);
                    area += 0.5 * divx * (h0 + h1);
                }
                return area;
            }
            else if (CurveType == CurveType.CURVE_BEZIER)
            {
                Vector2 scPt = Coords[0];
                Vector2 ecPt = Coords[1];
                uint ndiv = 32;
                double tdiv = 1.0 / (ndiv);
                double area = 0;
                for (uint i = 0; i < ndiv; i++){
                    double t0 = (i + 0) * tdiv;
                    double t1 = (i + 1) * tdiv;
                    Vector2 vec0 = (float)((1 - t0) * (1 - t0) * (1 - t0)) * SPt +
                        (float)(3 * (1 - t0) * (1 - t0) * t0) * scPt +
                        (float)(3 * (1 - t0) * t0 * t0) * ecPt +
                        (float)(t0 * t0 * t0) * EPt;
                    Vector2 vecT0 = (float)(-3 * (1 - t0) * (1 - t0)) * SPt +
                        (float)(3 * (3 * t0 - 1) * (t0 - 1)) * scPt -
                        (float)(3 * t0 * (3 * t0 - 2)) * ecPt +
                        (float)(3 * t0 * t0) * EPt;
                    double area0 = CadUtils.TriArea(SPt, vec0, vecT0 + vec0);
                    Vector2 vec1 = (float)((1 - t1) * (1 - t1) * (1 - t1)) * SPt +
                        (float)(3 * (1 - t1) * (1 - t1) * t1) * scPt +
                        (float)(3 * (1 - t1) * t1 * t1) * ecPt +
                        (float)(t1 * t1 * t1) * EPt;
                    Vector2 vecT1 = (float)(-3 * (1 - t1) * (1 - t1)) * SPt +
                        (float)(3 * (3 * t1 - 1) * (t1 - 1)) * scPt -
                        (float)(3 * t1 * (3 * t1 - 2)) * ecPt +
                        (float)(3 * t1 * t1) * EPt;
                    double area1 = CadUtils.TriArea(SPt, vec1, vecT1 + vec1);
                    double aveLen = (area0 + area1) * 0.5;
                    area += aveLen * tdiv;
                }
                return area;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return 0;
        }

        public int NumIntersectAgainstHalfLine(Vector2 bPt, Vector2 dir0)
        {
            Vector2 dir1 = dir0 * (float)(1.0 / dir0.Length());
            double longLen = 0;
            {
                double minX;
                double maxX;
                double minY;
                double maxY;
                GetBoundingBox(out minX, out maxX, out minY, out maxY);
                Vector2 tmpDPt = bPt + dir1;
                double area1 = CadUtils.TriArea(bPt, tmpDPt, new Vector2((float)minX, (float)minY));
                double area2 = CadUtils.TriArea(bPt, tmpDPt, new Vector2((float)minX, (float)maxY));
                double area3 = CadUtils.TriArea(bPt, tmpDPt, new Vector2((float)maxX, (float)minY));
                double area4 = CadUtils.TriArea(bPt, tmpDPt, new Vector2((float)maxX, (float)maxY));
                if (area1 < 0 && area2 < 0 && area3 < 0 && area4 < 0)
                {
                    return 0;
                }
                if (area1 > 0 && area2 > 0 && area3 > 0 && area4 > 0)
                {
                    return 0;
                }
                double len0 = Vector2.Distance(bPt,
                    new Vector2((float)(0.5 * (minX + maxX)), (float)(0.5 * (minY + maxY))));
                double len1 = maxX - minX;
                double len2 = maxY - minY;
                longLen = 2 * (len0 + len1 + len2);
            }
            Vector2 dPt = bPt + dir1 * (float)longLen;
            if (CurveType == CurveType.CURVE_LINE)
            {
                bool ret = CadUtils.IsCrossLineSegLineSeg(SPt, EPt, bPt, dPt);
                return ret ? 1 : 0;
            }
            else if (CurveType == CurveType.CURVE_ARC)
            {
                return NumCrossArcLineSeg(bPt, dPt);

            }
            else if (CurveType == CurveType.CURVE_POLYLINE)
            {
                uint div = (uint)(Coords.Count + 1);
                int iCnt = 0;
                for (uint iDiv = 0; iDiv < div; iDiv++)
                {
                    Vector2 iPt0 = (iDiv == 0) ? SPt : Coords[(int)(iDiv - 1)];
                    Vector2 iPt1 = (iDiv == div - 1) ? EPt : Coords[(int)(iDiv + 0)];
                    int res = 0;
                    if (CadUtils.IsCrossLineSegLineSeg(bPt, dPt, iPt0, iPt1))
                    {
                        res = 1;
                    }
                    iCnt += res;
                }
                return iCnt;
            }
            System.Diagnostics.Debug.Assert(false);
            return 0;
        }

        public bool ConnectEdge(Edge2D e1, bool isAheadAdd, bool isSameDir)
        {
            if (isAheadAdd && isSameDir)
            {
                System.Diagnostics.Debug.Assert(EVId == e1.SVId);
            }
            else if (isAheadAdd && !isSameDir)
            {
                System.Diagnostics.Debug.Assert(EVId == e1.EVId);
            }
            else if (!isAheadAdd && isSameDir)
            {
                System.Diagnostics.Debug.Assert(SVId == e1.EVId);
            }
            else if (!isAheadAdd && !isSameDir)
            {
                System.Diagnostics.Debug.Assert(SVId == e1.SVId);
            }
            if (CurveType == CurveType.CURVE_POLYLINE)
            {
                IList<Vector2> pts0 = Coords;

                double aveELen = GetCurveLength() / (pts0.Count + 1.0);
                IList<Vector2> pts1;
                {
                    uint div1 = (uint)(e1.GetCurveLength() / aveELen);
                    e1.GetCurveAsPolyline(out pts1, (int)div1);
                }
                IList<Vector2> pts2;
                if (isAheadAdd)
                {
                    pts2 = new List<Vector2>(pts0);
                    pts2.Add(EPt);
                    if (isSameDir)
                    {
                        for (uint i = 0; i < (uint)pts1.Count; i++)
                        {
                            pts2.Add(pts1[(int)i]);
                        }
                        EVId = e1.EVId;
                        EPt = e1.EPt;
                    }
                    else
                    {
                        for (int i = pts1.Count - 1; i >= 0; i--)
                        {
                            pts2.Add(pts1[i]);
                        }
                        EVId = e1.SVId;
                        EPt = e1.SPt;
                    }
                }
                else
                {
                    if (isSameDir)
                    {
                        pts2 = new List<Vector2>(pts1);
                        pts2.Add(SPt);
                        for (uint i = 0; i < pts0.Count; i++)
                        {
                            pts2.Add(pts0[(int)i]);
                        }
                        SVId = e1.SVId;
                        SPt = e1.SPt;
                    }
                    else
                    {
                        pts2 = new List<Vector2>();
                        for (int i = pts1.Count - 1; i >= 0; i--)
                        {
                            pts2.Add(pts1[i]);
                        }
                        pts2.Add(SPt);
                        for (uint i = 0; i < pts0.Count; i++)
                        {
                            pts2.Add(pts0[(int)i]);
                        }
                        SVId = e1.EVId;
                        SPt = e1.EPt;
                    }
                }

                Coords = new List<Vector2>(pts2);
            }
            return true;
        }

    }
}
