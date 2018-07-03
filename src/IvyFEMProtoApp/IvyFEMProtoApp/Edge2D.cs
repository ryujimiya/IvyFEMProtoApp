using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    class Edge2D
    {
        public CurveType Type { get; private set; } = CurveType.CURVE_LINE;
        private double[] Color = new double[3];

        private bool IsLeftSide;
        private double Dist;
        private IList<double> RelCoMeshs = new List<double>();
        private double[] RelCoBeziers = new double[4];
        private uint SVId = 0;
        private uint EVId = 0;
        private Vector2 SPt;
        private Vector2 EPt;
        private BoundingBox2D BB = new BoundingBox2D();

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

        public void Copy(Edge2D src)
        {
            Type = src.Type;
            IsLeftSide = src.IsLeftSide;
            Dist = src.Dist;
            RelCoMeshs.Clear();
            foreach (var relcoMesh in src.RelCoMeshs)
            {
                RelCoMeshs.Add(relcoMesh);
            }
            for (int i = 0; i < 4; i++)
            {
                RelCoBeziers[i] = src.RelCoBeziers[i];
            }
            SVId = src.SVId;
            EVId = src.EVId;
            for (int i = 0; i < 3; i++)
            {
                Color[i] = src.Color[i];
            }
            SPt = new Vector2(src.SPt.X, src.SPt.Y);
            EPt = new Vector2(src.EPt.X, src.EPt.Y);
            BB.Copy(src.BB);
        }

        public string Dump()
        {
            string ret = "";
            string CRLF = System.Environment.NewLine;

            ret += "■Edge2D" + CRLF;
            ret += "Type = " + Type + CRLF;
            ret += "IsLeftSide = " + IsLeftSide + CRLF;
            ret += "Dist = " + Dist + CRLF;
            ret += "RelCoMeshs" + CRLF;
            for (int i = 0; i < RelCoMeshs.Count; i++)
            {
                var relcoMesh = RelCoMeshs[i];
                ret += "RelCoMeshs[" + i + "] = " + relcoMesh + CRLF;
            }
            ret += "RelCoBeziers" + CRLF;
            for (int i = 0; i < 4; i++)
            {
                ret += "RelCoBeziers[" + i + "] = " + RelCoBeziers[i] + CRLF;
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
            ret += BB.Dump();

            return ret;
        }

        public void SetVertexs(Vector2 sPt, Vector2 ePt)
        {
            SPt = sPt;
            EPt = ePt;
            BB.IsntEmpty = false;
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
            if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_LINE)
            {
                return CadUtils.GetDistanceLineSegLineSeg(SPt, EPt, sPt1, ePt1);
            }
            else if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_ARC)
            {
                Vector2 cPt1;
                double radius1 = 0;
                e1.GetCenterRadius(out cPt1, out radius1);
                return CadUtils.GetDistanceLineSegArc(SPt, EPt, e1.SPt, e1.EPt, cPt1, radius1, e1.IsLeftSide);
            }
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_LINE)
            {
                return e1.Distance(this);
            }
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_ARC)
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
            else if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_POLYLINE)
            {
                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = ((uint)relcomsh1.Count) / 2 + 1;
                Vector2 h1 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-h1.Y, h1.X);
                double minDist = -1;

                for (int idiv = 0; idiv < ndiv1; idiv++)
                {
                    Vector2 pt0;
                    if (idiv == 0)
                    {
                        pt0 = e1.SPt;
                    }
                    else
                    {
                        pt0 = e1.SPt + h1 * (float)relcomsh1[(idiv - 1) * 2 + 0] +
                            v1 * (float)relcomsh1[(idiv - 1) * 2 + 1];
                    }
                    Vector2 pt1;
                    if (idiv == ndiv1 - 1)
                    {
                        pt1 = e1.EPt;
                    }
                    else
                    {
                        pt1 = e1.SPt + h1 * (float)relcomsh1[idiv * 2 + 0] +
                            v1 * (float)relcomsh1[idiv * 2 + 1];
                    }

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
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_LINE)
            {
                return e1.Distance(this);
            }
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_ARC)
            {
                return e1.Distance(this);
            }
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_POLYLINE)
            {
                Vector2 cPt0 = new Vector2(); // dummy
                double radius0 = 0;
                GetCenterRadius(out cPt0, out radius0);

                Vector2 h1 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-h1.Y, h1.X);

                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = ((uint)relcomsh1.Count) / 2 + 1;
                double minDist = -1;
                for (int idiv = 0; idiv < ndiv1; idiv++)
                {
                    Vector2 pt0;
                    if (idiv == 0)
                    {
                        pt0 = e1.SPt;
                    }
                    else
                    {
                        pt0 = e1.SPt + h1 * (float)relcomsh1[(idiv - 1) * 2 + 0] +
                            v1 * (float)relcomsh1[(idiv - 1) * 2 + 1];
                    }
                    Vector2 pt1;
                    if (idiv == ndiv1 - 1)
                    {
                        pt1 = e1.EPt;
                    }
                    else
                    {
                        pt1 = e1.SPt + h1 * (float)relcomsh1[idiv * 2 + 0] +
                            v1 * (float)relcomsh1[idiv * 2 + 1];
                    }

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
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_POLYLINE)
            {
                Vector2 h0 = EPt - SPt;
                Vector2 v0 = new Vector2(-h0.Y, h0.X);
                Vector2 h1 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-h1.Y, h1.X);

                IList<double> relcomsh0 = RelCoMeshs;
                uint ndiv0 = ((uint)relcomsh0.Count) / 2 + 1;
                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = ((uint)relcomsh1.Count) / 2 + 1;

                double minDist = -1;

                for (int idiv = 0; idiv < ndiv0; idiv++)
                {
                    Vector2 iPt0;
                    if (idiv == 0)
                    {
                        iPt0 = SPt;
                    }
                    else
                    {
                        iPt0 = SPt + h0 * (float)RelCoMeshs[(idiv - 1) * 2 + 0] +
                            v0 * (float)RelCoMeshs[(idiv - 1) * 2 + 1];
                    }
                    Vector2 iPt1;
                    if (idiv == ndiv0 - 1)
                    {
                        iPt1 = EPt;
                    }
                    else
                    {
                        iPt1 = SPt + h0 * (float)RelCoMeshs[idiv * 2 + 0] +
                            v0 * (float)RelCoMeshs[idiv * 2 + 1];
                    }
                    for (int jdiv = 0; jdiv < ndiv1; jdiv++)
                    {
                        Vector2 jPt0;
                        if (jdiv == 0)
                        {
                            jPt0 = e1.SPt;
                        }
                        else
                        {
                            jPt0 = e1.SPt + h1 * (float)relcomsh1[(jdiv - 1) * 2 + 0] +
                                v1 * (float)relcomsh1[(jdiv - 1) * 2 + 1];
                        }
                        Vector2 jPt1;
                        if (jdiv == ndiv1 - 1)
                        {
                            jPt1 = e1.EPt;
                        }
                        else
                        {
                            jPt1 = e1.SPt + h1 * (float)relcomsh1[jdiv * 2 + 0] +
                                v1 * (float)relcomsh1[jdiv * 2 + 1];
                        }
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

            if (Type ==CurveType.CURVE_LINE)
            {
                return;
            }
            else if (Type == CurveType.CURVE_ARC)
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
            else if (Type == CurveType.CURVE_POLYLINE)
            {
                uint n = (uint)(RelCoMeshs.Count / 2);
                Vector2 v0 = EPt - SPt;
                Vector2 v1 = new Vector2(-v0.Y, v0.X);
                for (int i = 0; i < n; i++)
                {
                    Vector2 pt0 = SPt + v0 * (float)RelCoMeshs[i * 2 + 0] +
                        v1 * (float)RelCoMeshs[i * 2 + 1];
                    minX = (pt0.X < minX) ? pt0.X : minX;
                    maxX = (pt0.X > maxX) ? pt0.X : maxX;
                    minY = (pt0.Y < minY) ? pt0.Y : minY;
                    maxY = (pt0.Y > maxY) ? pt0.Y : maxY;
                }
            }
            else if (Type == CurveType.CURVE_BEZIER)
            {
                Vector2 v0 = EPt - SPt;
                Vector2 v1 = new Vector2(-v0.Y, v0.X);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pt0 = SPt + v0 * (float)RelCoBeziers[i * 2 + 0] +
                        v1 * (float)RelCoBeziers[i * 2 + 1];
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
            if (Type == CurveType.CURVE_LINE || Type == CurveType.CURVE_ARC)
            {
                return false;
            }
            if (Type == CurveType.CURVE_POLYLINE)
            {
                IList<double> relcomsh = RelCoMeshs;
                uint ndiv = (uint)(relcomsh.Count / 2 + 1);
                Vector2 h0 = EPt - SPt;
                Vector2 v0 = new Vector2(-h0.Y, h0.X);
                for (uint idiv = 0; idiv < ndiv; idiv++)
                {
                    Vector2 iPt0;
                    if (idiv == 0)
                    {
                        iPt0 = SPt;
                    }
                    else
                    {
                        iPt0 = SPt + h0 * (float)relcomsh[(int)((idiv - 1) * 2 + 0)] +
                            v0 * (float)relcomsh[(int)((idiv - 1) * 2 + 1)];
                    }
                    Vector2 iPt1;
                    if (idiv == ndiv - 1)
                    {
                        iPt1 = EPt;
                    }
                    else
                    {
                        iPt1 = SPt + h0 * (float)relcomsh[(int)(idiv * 2 + 0)] +
                            v0 * (float)relcomsh[(int)(idiv * 2 + 1)];
                    }
                    for (uint jdiv = idiv + 2; jdiv < ndiv; jdiv++)
                    {
                        Vector2 jPt0;
                        if (jdiv == 0)
                        {
                            jPt0 = SPt;
                        }
                        else
                        {
                            jPt0 = SPt + h0 * (float)relcomsh[(int)((jdiv - 1) * 2 + 0)] +
                                v0 * (float)relcomsh[(int)((jdiv - 1) * 2 + 1)];
                        }
                        Vector2 jPt1;
                        if (jdiv == ndiv - 1)
                        {
                            jPt1 = EPt;
                        }
                        else
                        {
                            jPt1 = SPt + h0 * (float)relcomsh[(int)(jdiv * 2 + 0)] +
                                v0 * (float)relcomsh[(int)(jdiv * 2 + 1)];
                        }
                        if (CadUtils.IsCrossLineSegLineSeg(iPt0, iPt1, jPt0, jPt1))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else if (Type == CurveType.CURVE_BEZIER)
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
            if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_LINE)
            {
                return CadUtils.IsCrossLineSegLineSeg(SPt, EPt, sPt1, ePt1);
            }
            else if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_ARC)
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
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdge(this);
            }
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_ARC)
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
            else if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_POLYLINE)
            {
                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = (uint)(relcomsh1.Count / 2 + 1);
                Vector2 h1 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-h1.Y, h1.X);
                for (uint idiv = 0; idiv < ndiv1; idiv++)
                {
                    Vector2 pt0;
                    if (idiv == 0)
                    {
                        pt0 = e1.SPt;
                    }
                    else
                    {
                        pt0 = e1.SPt + h1 * (float)relcomsh1[(int)((idiv - 1) * 2 + 0)] +
                            v1 * (float)relcomsh1[(int)((idiv - 1) * 2 + 1)]; }
                    Vector2 pt1;
                    if (idiv == ndiv1 - 1)
                    {
                        pt1 = e1.EPt;
                    }
                    else
                    {
                        pt1 = e1.SPt + h1 * (float)relcomsh1[(int)(idiv * 2 + 0)] +
                            v1 * (float)relcomsh1[(int)(idiv * 2 + 1)];
                    }
                    if (CadUtils.IsCrossLineSegLineSeg(SPt, EPt, pt0, pt1))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdge(this);
            }
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_ARC)
            {
                return e1.IsCrossEdge(this);
            }
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_POLYLINE)
            {
                Vector2 cPt0;
                double radius0;
                GetCenterRadius(out cPt0, out radius0);

                Vector2 h1 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-h1.Y, h1.X);
                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = (uint)(relcomsh1.Count / 2 + 1);
                for (uint idiv = 0; idiv < ndiv1; idiv++)
                {
                    Vector2 pt0;
                    if (idiv == 0)
                    {
                        pt0 = e1.SPt;
                    }
                    else
                    {
                        pt0 = e1.SPt + h1 * (float)relcomsh1[(int)((idiv - 1) * 2 + 0)] +
                            v1 * (float)relcomsh1[(int)((idiv - 1) * 2 + 1)];
                    }
                    Vector2 pt1;
                    if (idiv == ndiv1 - 1)
                    {
                        pt1 = e1.EPt;
                    }
                    else
                    {
                        pt1 = e1.SPt + h1 * (float)relcomsh1[(int)(idiv * 2 + 0)] +
                            v1 * (float)relcomsh1[(int)(idiv * 2 + 1)];
                    }
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
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_POLYLINE)
            {
                Vector2 h0 = EPt - SPt;
                Vector2 v0 = new Vector2(-h0.Y, h0.X);
                Vector2 h1 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-h1.Y, h1.X);

                IList<double> relcomsh0 = RelCoMeshs;
                uint ndiv0 = (uint)(relcomsh0.Count / 2 + 1);
                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = (uint)(relcomsh1.Count / 2 + 1);
                for (uint idiv = 0; idiv < ndiv0; idiv++)
                {
                    Vector2 iPt0;
                    if (idiv == 0)
                    {
                        iPt0 = SPt;
                    }
                    else
                    {
                        iPt0 = SPt + h0 * (float)relcomsh0[(int)((idiv - 1) * 2 + 0)] +
                            v0 * (float)relcomsh0[(int)((idiv - 1) * 2 + 1)];
                    }
                    Vector2 iPt1;
                    if (idiv == ndiv0 - 1)
                    {
                        iPt1 = EPt;
                    }
                    else
                    {
                        iPt1 = SPt + h0 * (float)relcomsh0[(int)(idiv * 2 + 0)] +
                            v0 * (float)relcomsh0[(int)(idiv * 2 + 1)];
                    }
                    for (uint jdiv = 0; jdiv < ndiv1; jdiv++)
                    {
                        Vector2 jPt0;
                        if (jdiv == 0)
                        {
                            jPt0 = e1.SPt;
                        }
                        else
                        {
                            jPt0 = e1.SPt + h1 * (float)relcomsh1[(int)((jdiv - 1) * 2 + 0)] +
                                v1 * (float)relcomsh1[(int)((jdiv - 1) * 2 + 1)];
                        }
                        Vector2 jPt1;
                        if (jdiv == ndiv1 - 1)
                        {
                            jPt1 = e1.EPt;
                        }
                        else
                        {
                            jPt1 = e1.SPt + h1 * (float)relcomsh1[(int)(jdiv * 2 + 0)] +
                                v1 * (float)relcomsh1[(int)(jdiv * 2 + 1)];
                        }
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
            if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_LINE)
            {
                return false;
            }
            else if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_ARC)
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
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdgeShareOnePoint(this, isShareS1, isShareS0);
            }
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_ARC)
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
            else if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_POLYLINE)
            { 
                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = (uint)(relcomsh1.Count / 2 + 1);
                Vector2 v0 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-v0.Y, v0.X);
                uint iSDiv = (isShareS1) ? 1u : 0;
                uint iEDiv = (isShareS1) ? ndiv1 : ndiv1 - 1;
                for (uint idiv = iSDiv; idiv < iEDiv; idiv++)
                {
                    Vector2 pt0;
                    if (idiv == 0)
                    {
                        pt0 = e1.SPt;
                    }
                    else
                    {
                        pt0 = e1.SPt + v0 * (float)relcomsh1[(int)((idiv - 1) * 2 + 0)] +
                            v1 * (float)relcomsh1[(int)((idiv - 1) * 2 + 1)];
                    }
                    Vector2 pt1;
                    if (idiv == ndiv1 - 1)
                    {
                        pt1 = e1.EPt;
                    }
                    else
                    {
                        pt1 = e1.SPt + v0 * (float)relcomsh1[(int)(idiv * 2 + 0)] + 
                            v1 * (float)relcomsh1[(int)(idiv * 2 + 1)];
                    }
                    if (CadUtils.IsCrossLineSegLineSeg(SPt, EPt, pt0, pt1))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdgeShareOnePoint(this, isShareS1, isShareS0);
            }
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_POLYLINE)
            {
                Vector2 cPt0;
                double radius0;
                GetCenterRadius(out cPt0, out radius0);

                Vector2 h1 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-h1.Y, h1.X);

                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = (uint)(relcomsh1.Count / 2 + 1);
                uint iSDiv = (isShareS1) ? 1u : 0;
                uint iEDiv = (isShareS1) ? ndiv1 : ndiv1 - 1;
                for (uint idiv = iSDiv; idiv < iEDiv; idiv++)
                {
                    Vector2 pt0;
                    if (idiv == 0)
                    {
                        pt0 = e1.SPt;
                    }
                    else
                    {
                        pt0 = e1.SPt + h1 * (float)relcomsh1[(int)((idiv - 1) * 2 + 0)] +
                            v1 * (float)relcomsh1[(int)((idiv - 1) * 2 + 1)];
                    }
                    Vector2 pt1;
                    if (idiv == ndiv1 - 1)
                    {
                        pt1 = e1.EPt;
                    }
                    else
                    {
                        pt1 = e1.SPt + h1 * (float)relcomsh1[(int)(idiv * 2 + 0)] +
                            v1 * (float)relcomsh1[(int)(idiv * 2 + 1)];
                    }
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
                        pt1 = e1.SPt + h1 * (float)relcomsh1[0] + v1 * (float)relcomsh1[1];
                    }
                    else
                    {
                        pt1 = e1.EPt;
                        pt0 = e1.SPt + h1 * (float)relcomsh1[(int)(ndiv1 * 2 - 4)] +
                            v1 * (float)relcomsh1[(int)(ndiv1 * 2 - 3)];
                    }

                    double t0;
                    double t1;
                    if (!CadUtils.IsCrossLineCircle(cPt0, radius0, pt0, pt1, out t0, out t1))
                    {
                        return false;
                    }
                    Vector2 r0 = pt0 + (pt1 - pt0) * (float)t0;
                    Vector2 r1 = pt0 + (pt1 - pt0) * (float)t1;
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
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_ARC)
            {
                return e1.IsCrossEdgeShareOnePoint(this, isShareS1, isShareS0);
            }
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_POLYLINE)
            {
                Vector2 h0 = EPt - SPt;
                Vector2 v0 = new Vector2(-h0.Y, h0.X);
                Vector2 h1 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-h1.Y, h1.X);

                IList<double> relcomsh0 = RelCoMeshs;
                uint ndiv0 = (uint)(relcomsh0.Count / 2 + 1);
                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = (uint)(relcomsh1.Count / 2 + 1);
                uint iExcDiv0 = (isShareS0) ? 0 : ndiv0 - 1;
                uint iExcDiv1 = (isShareS1) ? 0 : ndiv1 - 1;
                for (uint idiv = 0; idiv < ndiv0; idiv++)
                {
                    Vector2 iPt0;
                    if (idiv == 0)
                    {
                        iPt0 = SPt;
                    }
                    else
                    {
                        iPt0 = SPt + h0 * (float)relcomsh0[(int)((idiv - 1) * 2 + 0)] +
                            v0 * (float)relcomsh0[(int)((idiv - 1) * 2 + 1)]; }
                    Vector2 iPt1;
                    if (idiv == ndiv0 - 1)
                    {
                        iPt1 = EPt;
                    }
                    else
                    {
                        iPt1 = SPt + h0 * (float)relcomsh0[(int)(idiv * 2 + 0)] +
                            v0 * (float)relcomsh0[(int)(idiv * 2 + 1)];
                    }
                    for (uint jdiv = 0; jdiv < ndiv1; jdiv++)
                    {
                        if (idiv == iExcDiv0 && jdiv == iExcDiv1)
                        {
                            continue;
                        }
                        Vector2 jPt0;
                        if (jdiv == 0)
                        {
                            jPt0 = e1.SPt;
                        }
                        else
                        {
                            jPt0 = e1.SPt + h1 * (float)relcomsh1[(int)((jdiv - 1) * 2 + 0)] +
                                v1 * (float)relcomsh1[(int)((jdiv - 1) * 2 + 1)];
                        }
                        Vector2 jPt1;
                        if (jdiv == ndiv1 - 1)
                        {
                            jPt1 = e1.EPt;
                        }
                        else
                        {
                            jPt1 = e1.SPt + h1 * (float)relcomsh1[(int)(jdiv * 2 + 0)] +
                                v1 * (float)relcomsh1[(int)(jdiv * 2 + 1)];
                        }
                        if (CadUtils.IsCrossLineSegLineSeg(iPt0, iPt1, jPt0, jPt1))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else if (Type == CurveType.CURVE_BEZIER || e1.Type == CurveType.CURVE_BEZIER)
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
            Vector2 sPt1 = e1.SPt;
            Vector2 ePt1 = e1.EPt;
            if (isShareS0S1)
            {
                System.Diagnostics.Debug.Assert(CadUtils.SquareLength(SPt, sPt1) < 1.0e-20 &&
                    CadUtils.SquareLength(EPt, ePt1) < 1.0e-20);
            }
            else
            {
                System.Diagnostics.Debug.Assert(CadUtils.SquareLength(SPt, ePt1) < 1.0e-20 &&
                    CadUtils.SquareLength(EPt, sPt1) < 1.0e-20);
            }

            if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_LINE)
            {
                return true;
            }
            else if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_ARC)
            {
                return false;
            }
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdgeShareBothPoints(this, isShareS0S1);
            }
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_ARC)
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
            else if (Type == CurveType.CURVE_LINE && e1.Type == CurveType.CURVE_POLYLINE)
            {
                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = (uint)(relcomsh1.Count / 2 + 1);
                Vector2 v0 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-v0.Y, v0.X);
                for (uint idiv = 1; idiv < ndiv1 - 1; idiv++)
                {
                    Vector2 pt0;
                    if (idiv == 0)
                    {
                        pt0 = e1.SPt;
                    }
                    else
                    {
                        pt0 = e1.SPt + v0 * (float)relcomsh1[(int)((idiv - 1) * 2 + 0)] +
                            v1 * (float)relcomsh1[(int)((idiv - 1) * 2 + 1)];
                    }
                    Vector2 pt1;
                    if (idiv == ndiv1 - 1)
                    {
                        pt1 = e1.EPt;
                    }
                    else
                    {
                        pt1 = e1.SPt + v0 * (float)relcomsh1[(int)(idiv * 2 + 0)] + 
                            v1 * (float)relcomsh1[(int)(idiv * 2 + 1)];
                    }
                    if (CadUtils.IsCrossLineSegLineSeg(SPt, EPt, pt0, pt1))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_LINE)
            {
                return e1.IsCrossEdgeShareBothPoints(this, isShareS0S1);
            }
            else if (Type == CurveType.CURVE_ARC && e1.Type == CurveType.CURVE_POLYLINE)
            {
                Vector2 cPt0;
                double radius0;
                GetCenterRadius(out cPt0, out radius0);

                Vector2 h1 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-h1.Y, h1.X);

                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = (uint)(relcomsh1.Count / 2 + 1);
                for (uint idiv = 1; idiv < ndiv1 - 1; idiv++)
                {
                    Vector2 pt0;
                    if (idiv == 0)
                    {
                        pt0 = e1.SPt;
                    }
                    else
                    {
                        pt0 = e1.SPt + h1 * (float)relcomsh1[(int)((idiv - 1) * 2 + 0)] +
                            v1 * (float)relcomsh1[(int)((idiv - 1) * 2 + 1)];
                    }
                    Vector2 pt1;
                    if (idiv == ndiv1 - 1)
                    {
                        pt1 = e1.EPt;
                    }
                    else
                    {
                        pt1 = e1.SPt + h1 * (float)relcomsh1[(int)(idiv * 2 + 0)] +
                            v1 * (float)relcomsh1[(int)(idiv * 2 + 1)];
                    }

                    double t0, t1;
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
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_ARC)
            {
                return e1.IsCrossEdgeShareBothPoints(this, isShareS0S1);
            }
            else if (Type == CurveType.CURVE_POLYLINE && e1.Type == CurveType.CURVE_POLYLINE)
            {
                Vector2 h0 = EPt - SPt;
                Vector2 v0 = new Vector2(-h0.Y, h0.X);
                Vector2 h1 = e1.EPt - e1.SPt;
                Vector2 v1 = new Vector2(-h1.Y, h1.X);

                IList<double> relcomsh0 = RelCoMeshs;
                uint ndiv0 = (uint)(relcomsh0.Count / 2 + 1);
                IList<double> relcomsh1 = e1.RelCoMeshs;
                uint ndiv1 = (uint)(relcomsh1.Count / 2 + 1);
                for (uint idiv = 1; idiv < ndiv0 - 1; idiv++)
                {
                    Vector2 iPt0;
                    if (idiv == 0)
                    {
                        iPt0 = SPt;
                    }
                    else
                    {
                        iPt0 = SPt + h0 * (float)relcomsh0[(int)((idiv - 1) * 2 + 0)] +
                            v0 * (float)relcomsh0[(int)((idiv - 1) * 2 + 1)];
                    }
                    Vector2 iPt1;
                    if (idiv == ndiv0 - 1)
                    {
                        iPt1 = EPt;
                    }
                    else
                    {
                        iPt1 = SPt + h0 * (float)relcomsh0[(int)(idiv * 2 + 0)] +
                            v0 * (float)relcomsh0[(int)(idiv * 2 + 1)];
                    }
                    for (uint jdiv = 1; jdiv < ndiv1 - 1; jdiv++)
                    {
                        Vector2 jPt0;
                        if (jdiv == 0)
                        {
                            jPt0 = e1.SPt;
                        }
                        else
                        {
                            jPt0 = e1.SPt + h1 * (float)relcomsh1[(int)((jdiv - 1) * 2 + 0)] +
                                v1 * (float)relcomsh1[(int)((jdiv - 1) * 2 + 1)];
                        }
                        Vector2 jPt1;
                        if (jdiv == ndiv1 - 1)
                        {
                            jPt1 = e1.EPt;
                        }
                        else
                        {
                            jPt1 = e1.SPt + h1 * (float)relcomsh1[(int)(jdiv * 2 + 0)] +
                                v1 * (float)relcomsh1[(int)(jdiv * 2 + 1)];
                        }
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
            if (Type != CurveType.CURVE_ARC)
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
            if (Type == CurveType.CURVE_LINE)
            {
                Vector2 h0 = SPt - EPt;
                return h0.Length();
            }
            else if (Type == CurveType.CURVE_ARC)
            {
                double radius;
                double theta;
                Vector2 cPt;
                Vector2 lx;
                Vector2 ly;
                GetCenterRadiusThetaLXY(out cPt, out radius, out theta, out lx, out ly);
                return radius * theta;
            }
            else if (Type == CurveType.CURVE_POLYLINE)
            {
                uint nPt = (uint)(RelCoMeshs.Count / 2);
                double relTotLen = 0;
                for (uint idiv = 0; idiv < nPt + 1; idiv++)
                {
                    double x0;
                    double y0;
                    if (idiv == 0)
                    {
                        x0 = 0;
                        y0 = 0;
                    }
                    else
                    {
                        x0 = RelCoMeshs[(int)(idiv * 2 - 2)];
                        y0 = RelCoMeshs[(int)(idiv * 2 - 1)];
                    }
                    double x1;
                    double y1;
                    if (idiv == nPt)
                    {
                        x1 = 1;
                        y1 = 0;
                    }
                    else
                    {
                        x1 = RelCoMeshs[(int)(idiv * 2 + 0)];
                        y1 = RelCoMeshs[(int)(idiv * 2 + 1)];
                    }
                    Vector2 pt0 = new Vector2((float)x0, (float)y0);
                    Vector2 pt1 = new Vector2((float)x1, (float)y1);
                    Vector2 tmpH0 = pt0 - pt1;
                    relTotLen += tmpH0.Length();
                }
                Vector2 h0 = SPt - SPt;
                double edgeLen = h0.Length();
                return relTotLen * edgeLen;
            }
            else if (Type == CurveType.CURVE_BEZIER)
            {
                Vector2 gh = EPt - SPt;
                Vector2 gv = new Vector2(-gh.Y, gh.X);
                Vector2 scPt = SPt + gh * (float)RelCoBeziers[0] + gv * (float)RelCoBeziers[1];
                Vector2 ecPt = SPt + gh * (float)RelCoBeziers[2] + gv * (float)RelCoBeziers[3];
                uint ndiv = 16;
                double tDiv = 1.0 / (ndiv);
                double edge_len = 0;
                for (uint i = 0; i < ndiv; i++){
                    double t0 = (i + 0) * tDiv;
                    double t1 = (i + 1) * tDiv;
                    Vector2 vecT0 = (float)(-3 * (1 - t0) * (1 - t0)) * SPt +
                        (float)(3 * (3 * t0 - 1) * (t0 - 1)) * scPt -
                        (float)(3 * t0 * (3 * t0 - 2)) * ecPt +
                        (float)(3 * t0 * t0) * EPt;
                    Vector2 vecT1 = (float)(-3 * (1 - t1) * (1 - t1)) * SPt +
                        (float)(3 * (3 * t1 - 1) * (t1 - 1)) * scPt -
                        (float)(3 * t1 * (3 * t1 - 2)) * ecPt +
                        (float)(3 * t1 * t1) * EPt;
                    double aveLen = (vecT0.Length() + vecT1.Length()) * 0.5;
                    edge_len += aveLen * tDiv;
                }
                return edge_len;
            }
            return 0;
        }

        public bool GetCurveAsPolyline(out IList<Vector2> pts, int ndiv)
        {
            pts = new List<Vector2>();
            if (ndiv <= 0)
            {
                if (Type == CurveType.CURVE_LINE)
                {
                    return true;
                }
                else if (Type == CurveType.CURVE_ARC)
                {
                    double radius;
                    double theta;
                    Vector2 cPt;
                    Vector2 lx;
                    Vector2 ly;
                    GetCenterRadiusThetaLXY(out cPt, out radius, out theta, out lx, out ly);
                    uint ndiv1 = (uint)(theta * 360 / (5.0 * 6.28) + 1);
                    return GetCurveAsPolyline(out pts, (int)ndiv1);
                }
                else if (Type == CurveType.CURVE_POLYLINE)
                {
                    Vector2 v0 = EPt - SPt;
                    Vector2 v1 = new Vector2(-v0.Y, v0.X);
                    uint nPt = (uint)(RelCoMeshs.Count / 2);
                    for (uint i = 0; i < nPt; i++)
                    {
                        Vector2 vec0 = SPt + v0 * (float)RelCoMeshs[(int)(i * 2 + 0)] +
                            v1 * (float)RelCoMeshs[(int)(i * 2 + 1)];
                        pts.Add(vec0);
                    }
                    return true;
                }
                else if (Type == CurveType.CURVE_BEZIER)
                {
                    return GetCurveAsPolyline(out pts, 16);
                }
            }
            else
            { 
                if (Type == CurveType.CURVE_LINE)
                {
                    Vector2 tDiv = (EPt - SPt) * (float)(1.0 / ndiv);
                    for (uint idiv = 1; idiv < ndiv; idiv++){
                        Vector2 vec0 = SPt + tDiv * idiv;
                        pts.Add(vec0);
                    }
                    System.Diagnostics.Debug.Assert(pts.Count <= ndiv);
                    return true;
                }
                else if (Type == CurveType.CURVE_ARC)
                {
                    double radius;
                    double theta;
                    Vector2 cPt;
                    Vector2 lx;
                    Vector2 ly;
                    GetCenterRadiusThetaLXY(out cPt, out radius, out theta, out lx, out ly);
                    double thetaDiv = theta / ndiv;
                    for (uint i = 1; i < ndiv; i++)
                    {
                        double curTheta = i * thetaDiv;
                        Vector2 vec0 = ((float)Math.Cos(curTheta) * lx +
                            (float)Math.Sin(curTheta) * ly) * (float)radius + cPt;
                        pts.Add(vec0);
                    }
                    System.Diagnostics.Debug.Assert(pts.Count <= ndiv);
                    return true;
                }
                else if (Type == CurveType.CURVE_POLYLINE)
                {
                    uint nPt = (uint)(RelCoMeshs.Count / 2);
                    double relTotLen = 0;
                    for (uint idiv = 0; idiv < nPt + 1; idiv++)
                    {
                        double x0;
                        double y0;
                        if (idiv == 0)
                        {
                            x0 = 0;
                            y0 = 0;
                        }
                        else
                        {
                            x0 = RelCoMeshs[(int)(idiv * 2 - 2)];
                            y0 = RelCoMeshs[(int)(idiv * 2 - 1)];
                        }
                        double x1;
                        double y1;
                        if (idiv == nPt)
                        {
                            x1 = 1;
                            y1 = 0;
                        }
                        else
                        {
                            x1 = RelCoMeshs[(int)(idiv * 2 + 0)];
                            y1 = RelCoMeshs[(int)(idiv * 2 + 1)];
                        }
                        Vector2 pt0 = new Vector2((float)x0, (float)y0);
                        Vector2 pt1 = new Vector2((float)x1, (float)y1);
                        Vector2 h = pt0 - pt1;
                        relTotLen += h.Length();
                    }
                    uint ndiv1 = (uint)ndiv;
                    uint nPt1 = (uint)(ndiv - 1);
                    double relDiv = relTotLen / ndiv1;
                    Vector2 gh = EPt - SPt;
                    Vector2 gv = new Vector2(-gh.Y, gh.X);
                    uint curIDiv0 = 0;
                    double curRatio0 = 0;
                    for (uint iPt1 = 0; iPt1 < nPt1; iPt1++)
                    {
                        double curRestLen0 = relDiv;
                        for (;;)
                        {
                            System.Diagnostics.Debug.Assert(curIDiv0 < nPt + 1);
                            System.Diagnostics.Debug.Assert(curRatio0 >= 0 && curRatio0 <= 1);
                            double x0, y0;
                            if (curIDiv0 == 0)
                            {
                                x0 = 0;
                                y0 = 0;
                            }
                            else
                            {
                                x0 = RelCoMeshs[(int)(curIDiv0 * 2 - 2)];
                                y0 = RelCoMeshs[(int)(curIDiv0 * 2 - 1)];
                            }
                            double x1;
                            double y1;
                            if (curIDiv0 == nPt)
                            {
                                x1 = 1;
                                y1 = 0;
                            }
                            else
                            {
                                x1 = RelCoMeshs[(int)(curIDiv0 * 2 + 0)];
                                y1 = RelCoMeshs[(int)(curIDiv0 * 2 + 1)];
                            }
                            Vector2 pt0 = new Vector2((float)x0, (float)y0);
                            Vector2 pt1 = new Vector2((float)x1, (float)y1);
                            Vector2 h = pt0 - pt1;
                            double iDivLen0 = h.Length();
                            if (iDivLen0 * (1 - curRatio0) > curRestLen0)
                            {
                                curRatio0 += curRestLen0 / iDivLen0;
                                double xintp = x0 * (1.0 - curRatio0) + x1 * curRatio0;
                                double yintp = y0 * (1.0 - curRatio0) + y1 * curRatio0;
                                pts.Add(SPt + gh * (float)xintp + gv * (float)yintp);
                                break;
                            }
                            curRestLen0 -= iDivLen0 * (1 - curRatio0);
                            curIDiv0 += 1;
                            curRatio0 = 0;
                        }
                    }
                    System.Diagnostics.Debug.Assert(pts.Count <= nPt1);
                    return true;
                }
                else if (Type == CurveType.CURVE_BEZIER)
                {
                    Vector2 gh = EPt - SPt;
                    Vector2 gv = new Vector2(-gh.Y, gh.X);
                    Vector2 scPt = SPt + gh * (float)RelCoBeziers[0] + gv * (float)RelCoBeziers[1];
                    Vector2 ecPt = SPt + gh * (float)RelCoBeziers[2] + gv * (float)RelCoBeziers[3];
                    double tDiv = 1.0 / (ndiv);
                    for (uint i = 1; i < ndiv; i++)
                    {
                        double t = i * tDiv;
                        Vector2 vec0 = (float)((1 - t) * (1 - t) * (1 - t)) * SPt +
                            (float)(3 * (1 - t) * (1 - t) * t) * scPt +
                            (float)(3 * (1 - t) * t * t) * ecPt +
                            (float)(t * t * t) * EPt;
                        pts.Add(vec0);
                    }
                    System.Diagnostics.Debug.Assert(pts.Count == ndiv);
                    return true;
                }
            }
            return false;
        }

        public bool GetCenterRadius(out Vector2 cPt, out double radius)
        {
            if (Type != CurveType.CURVE_ARC)
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
            if (Type != CurveType.CURVE_ARC)
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

            if (Type != CurveType.CURVE_ARC)
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
            if (Type == CurveType.CURVE_LINE)
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
            else if (Type == CurveType.CURVE_ARC)
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
            else if (Type == CurveType.CURVE_POLYLINE)
            {
                IList<double> relcomsh = RelCoMeshs;
                uint ndiv = (uint)(relcomsh.Count / 2 + 1);
                Vector2 h0 = EPt - SPt;
                Vector2 v0 = new Vector2(-h0.Y, h0.X);
                double minDist = Vector2.Distance(SPt, pt);
                Vector2 cand = SPt;
                for (uint idiv = 0; idiv < ndiv; idiv++)
                {
                    Vector2 iPt0;
                    if (idiv == 0)
                    {
                        iPt0 = SPt;
                    }
                    else
                    {
                        iPt0 = SPt + h0 * (float)relcomsh[(int)(idiv - 1) * 2 + 0] +
                            v0 * (float)relcomsh[(int)(idiv - 1) * 2 + 1];
                    }
                    Vector2 iPt1;
                    if (idiv == ndiv - 1)
                    {
                        iPt1 = EPt;
                    }
                    else
                    {
                        iPt1 = SPt + h0 * (float)relcomsh[(int)idiv * 2 + 0] +
                            v0 * (float)relcomsh[(int)idiv * 2 + 1];
                    }
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
            if (Type == CurveType.CURVE_LINE)
            {
                Vector2 d = (isS) ? EPt - SPt : SPt - EPt;
                d = CadUtils.Normalize(d);
                return d;
            }
            else if (Type == CurveType.CURVE_ARC)
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
            else if (Type == CurveType.CURVE_POLYLINE)
            {
                IList<double> relcomsh = RelCoMeshs;
                uint ndiv = (uint)(relcomsh.Count / 2 + 1);
                Vector2 h0 = EPt - SPt;
                Vector2 v0 = new Vector2(-h0.Y, h0.X);
                if (isS)
                {
                    if (ndiv == 1)
                    {
                        return h0;
                    }
                    Vector2 d = h0 * (float)relcomsh[0] + v0 * (float)relcomsh[1];
                    d = CadUtils.Normalize(d);
                    return d;
                }
                else
                {
                    if (ndiv == 1)
                    {
                        return h0 * -1;
                    }
                    Vector2 d = SPt + h0 * (float)relcomsh[(int)((ndiv - 2) * 2 + 0)] +
                        v0 * (float)relcomsh[(int)((ndiv - 2) * 2 + 1)] - EPt;
                    d = CadUtils.Normalize(d);
                    return d;
                }
            }
            else if (Type == CurveType.CURVE_BEZIER)
            {
                Vector2 gh = EPt - SPt;
                Vector2 gv = new Vector2(-gh.Y, gh.X);
                Vector2 scPt = SPt + gh * (float)RelCoBeziers[0] + gv * (float)RelCoBeziers[1];
                Vector2 ecPt = SPt + gh * (float)RelCoBeziers[2] + gv * (float)RelCoBeziers[3];
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
            if (Type == CurveType.CURVE_LINE)
            {

            }
            else if (Type == CurveType.CURVE_ARC)
            {
                Vector2 sPt = SPt;
                Vector2 ePt = EPt;
                Vector2 cPt;
                double r;
                GetCenterRadius(out cPt, out r);
                Dist = CadUtils.TriHeight(cPt, sPt, addPt) / Vector2.Distance(sPt, addPt);
                addEdge.Type = CurveType.CURVE_ARC;
                addEdge.IsLeftSide = IsLeftSide;
                addEdge.Dist = CadUtils.TriHeight(cPt, addPt, ePt) / Vector2.Distance(addPt, ePt);
            }
            else if (Type == CurveType.CURVE_POLYLINE)
            {
                Vector2 sPt = SPt;
                Vector2 ePt = EPt;
                IList<Vector2> addPts = new List<Vector2>();
                {
                    Vector2 h0 = ePt - sPt;
                    Vector2 v0 = new Vector2(-h0.Y, h0.X);
                    IList<double> relcomsh = RelCoMeshs;
                    uint nPt = (uint)(relcomsh.Count / 2);
                    addPts.Clear();
                    for (uint i = 0; i < nPt; i++)
                    {
                        Vector2 tmpPt = sPt + h0 * (float)relcomsh[(int)(i * 2 + 0)] +
                            v0 * (float)relcomsh[(int)(i * 2 + 1)];
                        addPts.Add(tmpPt);
                    }
                }
                int ePtIndex0;
                int sPtIndex1;
                {
                    bool isSegment = false;
                    int ind = -1;
                    double minDist = Vector2.Distance(sPt, addPt);
                    for (uint idiv = 0; idiv < addPts.Count + 1; idiv++)
                    {
                        Vector2 pt0 = (idiv == 0) ? sPt : addPts[(int)(idiv - 1)];
                        Vector2 pt1 = (idiv == addPts.Count) ? ePt : addPts[(int)idiv];
                        if (Vector2.Distance(addPt, pt1) < minDist)
                        {
                            isSegment = false;
                            ind = (int)idiv;
                            minDist = Vector2.Distance(addPt, pt1);
                        }
                        double t = CadUtils.FindNearestPointParameterLinePoint(addPt, pt0, pt1);
                        if (t < 0.01 || t > 0.99)
                        {
                            continue;
                        }
                        Vector2 midPt = pt0 + (pt1 - pt0) * (float)t;
                        if (Vector2.Distance(addPt, midPt) < minDist)
                        {
                            isSegment = true;
                            minDist = Vector2.Distance(addPt, midPt);
                            ind = (int)idiv;
                        }
                    }

                    if (isSegment)
                    {
                        ePtIndex0 = ind - 2;
                        sPtIndex1 = ind + 1;
                    }
                    else
                    {
                        ePtIndex0 = ind - 1;
                        sPtIndex1 = ind + 1;
                    }
                }

                if (ePtIndex0 > 0)
                {
                    Type = CurveType.CURVE_POLYLINE;
                    uint nPt = (uint)(ePtIndex0 + 1);
                    double sqLen = CadUtils.SquareLength(addPt - sPt);
                    Vector2 eh = (addPt - sPt) * (float)(1 / sqLen);
                    Vector2 ev = new Vector2(-eh.Y, eh.X);
                    RelCoMeshs.Clear();
                    for (int i = 0; i < nPt; i++)
                    {
                        double x1 = Vector2.Dot(addPts[i] - sPt, eh);
                        double y1 = Vector2.Dot(addPts[i] - sPt, ev);
                        RelCoMeshs.Add(x1);
                        System.Diagnostics.Debug.Assert(RelCoMeshs.Count == i * 2 + 0 + 1);
                        RelCoMeshs.Add(y1);
                        System.Diagnostics.Debug.Assert(RelCoMeshs.Count == i * 2 + 1 + 1);
                    }
                }
                else
                {
                    Type = CurveType.CURVE_LINE;
                }
                if (sPtIndex1 < (int)addPts.Count)
                {
                    addEdge.Type = CurveType.CURVE_POLYLINE;
                    uint nPt = (uint)(addPts.Count - sPtIndex1);
                    double sqLen = CadUtils.SquareLength(ePt - addPt);
                    Vector2 eh = (ePt - addPt) * (float)(1 / sqLen);
                    Vector2 ev = new Vector2(-eh.Y, eh.X);
                    addEdge.RelCoMeshs.Clear();
                    for (int i = 0; i < nPt; i++)
                    {
                        double x1 = Vector2.Dot(addPts[i + sPtIndex1] - addPt, eh);
                        double y1 = Vector2.Dot(addPts[i + sPtIndex1] - addPt, ev);
                        RelCoMeshs.Add(x1);
                        System.Diagnostics.Debug.Assert(RelCoMeshs.Count == i * 2 + 0 + 1);
                        RelCoMeshs.Add(y1);
                        System.Diagnostics.Debug.Assert(RelCoMeshs.Count == i * 2 + 1 + 1);
                    }
                }
                else
                {
                    addEdge.Type = CurveType.CURVE_LINE;
                }
            }
            return true;
        }

        public double EdgeArea()
        {
            if (Type == CurveType.CURVE_LINE)
            {
                return 0;
            }
            else if (Type == CurveType.CURVE_ARC)
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
            else if (Type == CurveType.CURVE_POLYLINE)
            {
                uint n = (uint)(RelCoMeshs.Count / 2);
                if (n == 0)
                {
                    return 0;
                }
                double areaRatio = 0;
                for (uint i = 0; i < n + 1; i++)
                {
                    double xDiv;
                    double h0;
                    double h1;
                    if (i == 0)
                    {
                        xDiv = RelCoMeshs[0];
                        h0 = 0;
                        h1 = RelCoMeshs[1];
                    }
                    else if (i == n)
                    {
                        xDiv = 1 - RelCoMeshs[(int)(n * 2 - 2)];
                        h0 = RelCoMeshs[(int)(n * 2 - 1)];
                        h1 = 0;
                    }
                    else
                    {
                        xDiv = RelCoMeshs[(int)(i * 2)] - RelCoMeshs[(int)(i * 2 - 2)];
                        h0 = RelCoMeshs[(int)(i * 2 - 1)];
                        h1 = RelCoMeshs[(int)(i * 2 + 1)];
                    }
                    areaRatio += 0.5 * xDiv * (h0 + h1);
                }
                double sqLen = CadUtils.SquareLength(EPt - SPt);
                double segmentArea = -sqLen * areaRatio;
                return segmentArea;
            }
            else if (Type == CurveType.CURVE_BEZIER)
            {
                Vector2 gh = EPt - SPt;
                Vector2 gv = new Vector2(-gh.Y, gh.X);
                Vector2 scPt = SPt + gh * (float)RelCoBeziers[0] + gv * (float)RelCoBeziers[1];
                Vector2 ecPt = SPt + gh * (float)RelCoBeziers[2] + gv * (float)RelCoBeziers[3];
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
            if (Type == CurveType.CURVE_LINE)
            {
                bool ret = CadUtils.IsCrossLineSegLineSeg(SPt, EPt, bPt, dPt);
                return ret ? 1 : 0;
            }
            else if (Type == CurveType.CURVE_ARC)
            {
                return NumCrossArcLineSeg(bPt, dPt);

            }
            else if (Type == CurveType.CURVE_POLYLINE)
            {
                IList<double> relcomsh = RelCoMeshs;
                uint ndiv = (uint)(relcomsh.Count / 2 + 1);
                Vector2 h0 = EPt - SPt;
                Vector2 v0 = new Vector2(-h0.Y, h0.X);
                uint icnt = 0;
                for (uint idiv = 0; idiv < ndiv; idiv++)
                {
                    Vector2 iPt0;
                    if (idiv == 0)
                    {
                        iPt0 = SPt;
                    }
                    else
                    {
                        iPt0 = SPt + h0 * (float)relcomsh[(int)((idiv - 1) * 2 + 0)] +
                            v0 * (float)relcomsh[(int)((idiv - 1) * 2 + 1)];
                    }
                    Vector2 iPt1;
                    if (idiv == ndiv - 1)
                    {
                        iPt1 = EPt;
                    }
                    else
                    {
                        iPt1 = SPt + h0 * (float)relcomsh[(int)(idiv * 2 + 0)] +
                            v0 * (float)relcomsh[(int)(idiv * 2 + 1)];
                    }
                    bool ret = CadUtils.IsCrossLineSegLineSeg(bPt, dPt, iPt0, iPt1);
                    uint res = ret? 1u: 0;
                    //if (res == 0)
                    //{
                    //    return -1;
                    //}
                    icnt += res;
                }
                return (int)icnt;
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
            if (Type == CurveType.CURVE_POLYLINE)
            {
                IList<Vector2> pts0 = new List<Vector2>();
                {
                    Vector2 sPt = SPt;
                    Vector2 ePt = EPt;
                    Vector2 h0 = ePt - sPt;
                    Vector2 v0 = new Vector2(-h0.Y, h0.X);
                    IList<double> relcomsh = RelCoMeshs;
                    uint nPt = (uint)(relcomsh.Count / 2);
                    pts0.Clear();
                    for (uint i = 0; i < nPt; i++)
                    {
                        pts0[(int)i] = sPt + h0 * (float)relcomsh[(int)(i * 2 + 0)] +
                            v0 * (float)relcomsh[(int)(i * 2 + 1)];
                    }
                }
                double aveELen = GetCurveLength() / (pts0.Count + 1.0);
                IList<Vector2> pts1;
                {
                    uint ndiv1 = (uint)(e1.GetCurveLength() / aveELen);
                    e1.GetCurveAsPolyline(out pts1, (int)ndiv1);
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
                {
                    Vector2 sPt = SPt;
                    Vector2 ePt = EPt;
                    uint nPt = (uint)pts2.Count;
                    double sqLen = CadUtils.SquareLength(ePt - sPt);
                    Vector2 eh = (ePt - sPt) * (float)(1 / sqLen);
                    Vector2 ev = new Vector2(-eh.Y, eh.X);
                    for (uint ipo = 0; ipo < nPt; ipo++)
                    {
                        double x1 = Vector2.Dot(pts2[(int)ipo] - sPt, eh);
                        double y1 = Vector2.Dot(pts2[(int)ipo] - sPt, ev);
                        RelCoMeshs.Add(x1); // ipo * 2 + 0
                        RelCoMeshs.Add(y1); // ipo * 2 + 1
                    }
                    System.Diagnostics.Debug.Assert(RelCoMeshs.Count == nPt * 2);
                }
            }
            return true;
        }

    }
}
