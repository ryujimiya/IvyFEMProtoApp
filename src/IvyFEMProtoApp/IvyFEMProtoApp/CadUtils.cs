using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    class CadUtils
    {
        /// <summary>
        /// 三角形最小面積
        /// </summary>
        public const double MinTriArea = 1.0e-10;

        public static string Dump(Vector2 v)
        {
            string ret = "";
            string CRLF = System.Environment.NewLine;

            ret += "Vector2" + CRLF;
            ret += "(" + v.X + ", " + v.Y + ")" + CRLF;
            return ret;
        }

        public static Vector2 Normalize(Vector2 v)
        {
            float len = v.Length();
            return v / len;
        }

        public static Vector2 GetProjectedPointOnCircle(Vector2 c, double r, Vector2 v)
        {
            Vector2 cv = v - c;
            double k = (r / cv.Length());
            return cv * (float)k + c;
        }

        public static double SquareLength(Vector2 iPt0, Vector2 iPt1)
        {
            Vector2 v = iPt1 - iPt0;
            float len = v.Length();
            return len * len;
        }

        public static double SquareLength(Vector2 point)
        {
            float len = point.Length();
            return len * len;
        }

        public static double TriHeight(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            double area = TriArea(v1, v2, v3);
            double len = Math.Sqrt(SquareLength(v2, v3));
            return area * 2.0 / len;
        }

        public static double TriArea(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            return 0.5 * ((v2.X - v1.X) * (v3.Y - v1.Y) - (v3.X - v1.X) * (v2.Y - v1.Y));
        }

        public static double TriArea(int iv1, int iv2, int iv3, IList<Vector2> points)
        {
            return TriArea(points[iv1], points[iv2], points[iv3]);
        }

        public static void UnitNormalAreaTri3D(out double[] n, out double a, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            n = new double[3];
            n[0] = (v2.Y - v1.Y) * (v3.Z - v1.Z) - (v3.Y - v1.Y) * (v2.Z - v1.Z);
            n[1] = (v2.Z - v1.Z) * (v3.X - v1.X) - (v3.Z - v1.Z) * (v2.X - v1.X);
            n[2] = (v2.X - v1.X) * (v3.Y - v1.Y) - (v3.X - v1.X) * (v2.Y - v1.Y);
            a = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]) * 0.5;
            double invA = 0.5 / a;
            n[0] *= invA;
            n[1] *= invA;
            n[2] *= invA;
        }

        public static bool IsCrossLineSegLineSeg(Vector2 sPt0, Vector2 ePt0, Vector2 sPt1, Vector2 ePt1)
        {
            double minX0 = (sPt0.X < ePt0.X) ? sPt0.X : ePt0.X;
            double maxX0 = (sPt0.X > ePt0.X) ? sPt0.X : ePt0.X;
            double maxX1 = (sPt1.X > ePt1.X) ? sPt1.X : ePt1.X;
            double minX1 = (sPt1.X < ePt1.X) ? sPt1.X : ePt1.X;
            double minY0 = (sPt0.Y < ePt0.Y) ? sPt0.Y : ePt0.Y;
            double maxY0 = (sPt0.Y > ePt0.Y) ? sPt0.Y : ePt0.Y;
            double maxY1 = (sPt1.Y > ePt1.Y) ? sPt1.Y : ePt1.Y;
            double minY1 = (sPt1.Y < ePt1.Y) ? sPt1.Y : ePt1.Y;
            double len = ((maxX0 - minX0) + (maxY0 - minY0) + (maxX1 - minX1) + (maxY1 - minY1)) * 0.0001;
            if (maxX1 + len < minX0)
            {
                return false;
            }
            if (maxX0 + len < minX1)
            {
                return false;
            }
            if (maxY1 + len < minY0)
            {
                return false;
            }
            if (maxY0 + len < minY1)
            {
                return false;
            }

            double area1 = TriArea(sPt0, ePt0, sPt1);
            double area2 = TriArea(sPt0, ePt0, ePt1);
            double area3 = TriArea(sPt1, ePt1, sPt0);
            double area4 = TriArea(sPt1, ePt1, ePt0);
            double a12 = area1 * area2;
            if (a12 > 0)
            {
                return false;
            }
            double a34 = area3 * area4;
            if (a34 > 0)
            {
                return false;
            }
            return true;
        }

        public static bool IsCrossCircleCircle(Vector2 cPt0, double radius0, Vector2 cPt1, double radius1,
            out Vector2 pt0, out Vector2 pt1)
        {
            pt0 = new Vector2();
            pt1 = new Vector2();

            double sqDist = SquareLength(cPt0, cPt1);
            double dist = Math.Sqrt(sqDist);
            if (radius0 + radius1 < dist)
            {
                return false;
            }
            if (Math.Abs(radius0 - radius1) > dist)
            {
                return false;
            }
            if (dist < 1.0e-30)
            {
                return false;
            }
            double ct = 0.5 * (sqDist + radius0 * radius0 - radius1 * radius1) / (radius0 * dist);
            System.Diagnostics.Debug.Assert(ct >= -1 && ct <= 1);
            double st = Math.Sqrt(1 - ct * ct);
            Vector2 e0 = (cPt1 - cPt0) * (float)(1 / dist);
            Vector2 e1 = new Vector2(e0.Y, -e0.X);
            pt0 = cPt0 + e0 * (float)(radius0 * ct) + e1 * (float)(radius0 * st);
            pt1 = cPt0 + e0 * (float)(radius0 * ct) - e1 * (float)(radius0 * st);
            return true;
        }

        public static bool IsCrossLineCircle(Vector2 cPt, double radius, Vector2 sPt, Vector2 ePt,
            out double t0, out double t1)
        {
            t0 = 0.0f;
            t1 = 0.0f;

            double minX = (sPt.X < ePt.X) ? sPt.X : ePt.X;
            if (cPt.X + radius < minX)
            {
                return false;
            }
            double maxX = (sPt.X > ePt.X) ? sPt.X : ePt.X;
            if (cPt.X - radius > maxX)
            {
                return false;
            }
            double minY = (sPt.Y < ePt.Y) ? sPt.Y : ePt.Y;
            if (cPt.Y + radius < minY)
            {
                return false;
            }
            double maxY = (sPt.Y > ePt.Y) ? sPt.Y : ePt.Y;
            if (cPt.Y - radius > maxY)
            {
                return false;
            }

            Vector2 es = ePt - sPt;
            Vector2 cs = cPt - sPt;
            double a = SquareLength(es);
            double b = Vector2.Dot(es, cs);
            double c = SquareLength(cs) - radius * radius;
            double det = b * b - a * c;
            if (det < 0)
            {
                return false;
            }
            t0 = (b - Math.Sqrt(det)) / a;
            t1 = (b + Math.Sqrt(det)) / a;
            return true;
        }

        public static double FindNearestPointParameterLinePoint(Vector2 cPt, Vector2 sPt, Vector2 ePt)
        {
            Vector2 es = ePt - sPt;
            Vector2 sc = sPt - cPt;
            double a = SquareLength(es);
            double b = Vector2.Dot(es, sc);
            return -b / a;
        }

        public static bool IsDirectionArc(Vector2 pt, Vector2 sPt, Vector2 ePt, Vector2 cPt, bool isLeftSide)
        {
            if (isLeftSide)
            {
                if (TriArea(sPt, cPt, ePt) > 0.0)
                {
                    if (TriArea(sPt, cPt, pt) > 0.0 && TriArea(pt, cPt, ePt) > 0.0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (TriArea(sPt, cPt, pt) > 0.0 || TriArea(pt, cPt, ePt) > 0.0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (TriArea(ePt, cPt, sPt) > 0.0)
                {
                    if (TriArea(ePt, cPt, pt) > 0.0 && TriArea(pt, cPt, sPt) > 0.0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (TriArea(ePt, cPt, pt) > 0.0 || TriArea(pt, cPt, sPt) > 0.0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }


        public static double GetDistancePointArc(Vector2 pt, Vector2 sPt1, Vector2 ePt1,
            Vector2 cPt1, double radius1, bool isLeftSide1)
        {
            double minDist = Vector2.Distance(pt, sPt1);
            double d0 = Vector2.Distance(pt, ePt1);
            minDist = (minDist < d0) ? minDist : d0;
            if (IsDirectionArc(GetProjectedPointOnCircle(cPt1, radius1, pt), sPt1, ePt1, cPt1, isLeftSide1))
            {
                d0 = Math.Abs(Vector2.Distance(pt, cPt1) - radius1);
                minDist = (d0 < minDist) ? d0 : minDist;
            }
            return minDist;
        }

        public static double GetDistanceLineSegLineSeg(Vector2 sPt0, Vector2 ePt0, Vector2 sPt1, Vector2 ePt1)
        {
            if (IsCrossLineSegLineSeg(sPt0, ePt0, sPt1, ePt1))
            {
                return -1;
            }
            double sD1 = GetDistanceLineSegPoint(sPt0, sPt1, ePt1);
            double eD1 = GetDistanceLineSegPoint(ePt0, sPt1, ePt1);
            double sD0 = GetDistanceLineSegPoint(sPt1, sPt0, ePt0);
            double eD0 = GetDistanceLineSegPoint(ePt1, sPt0, ePt0);
            double minDist = sD1;
            minDist = (eD1 < minDist) ? eD1 : minDist;
            minDist = (sD0 < minDist) ? sD0 : minDist;
            minDist = (eD0 < minDist) ? eD0 : minDist;
            return minDist;
        }

        public static double GetDistanceLineSegPoint(Vector2 cPt, Vector2 sPt, Vector2 ePt)
        {
            Vector2 es = ePt - sPt;
            Vector2 sc = sPt - cPt;
            double a = SquareLength(es);
            double b = Vector2.Dot(es, sc);
            double t = -b / a;
            if (t < 0)
            {
                return Vector2.Distance(sPt, cPt);
            }
            if (t > 1)
            {
                return Vector2.Distance(ePt, cPt);
            }
            Vector2 p = sPt + (float)t * (ePt - sPt);
            return Vector2.Distance(p, cPt);
        }

        public static double GetDistanceLineSegArc(
            Vector2 sPt0, Vector2 ePt0,
            Vector2 sPt1, Vector2 ePt1,
            Vector2 cPt1, double radius1, bool isLeftSide1)
        {
            double t0 = 0;
            double t1 = 0;
            if (IsCrossLineCircle(cPt1, radius1, sPt0, ePt0, out t0, out t1))
            {
                if (0 < t0 && t0 < 1 &&
                    IsDirectionArc(sPt0 + (ePt0 - sPt0) * (float)t0, sPt1, ePt1, cPt1, isLeftSide1))
                {
                    return -1;
                }
                if (0 < t1 && t1 < 1 &&
                    IsDirectionArc(sPt0 + (ePt0 - sPt0) * (float)t1, sPt1, ePt1, cPt1, isLeftSide1))
                {
                    return -1;
                }
            }
            double sminDist0 = GetDistancePointArc(sPt0, sPt1, ePt1, cPt1, radius1, isLeftSide1);
            double eminDist0 = GetDistancePointArc(ePt0, sPt1, ePt1, cPt1, radius1, isLeftSide1);
            double minDist = (sminDist0 < eminDist0) ? sminDist0 : eminDist0;
            double t = FindNearestPointParameterLinePoint(cPt1, sPt0, ePt0);
            if (t > 0 && t < 1)
            {
                Vector2 v = sPt0 + (ePt0 - sPt0) * (float)t;
                double d0 = Vector2.Distance(v, cPt1) - radius1;
                if (d0 > 0)
                {
                    if (IsDirectionArc(GetProjectedPointOnCircle(cPt1, radius1, v), sPt1, ePt1, cPt1, isLeftSide1))
                    {
                        minDist = (d0 < minDist) ? d0 : minDist;
                    }
                }
            }
            return minDist;
        }

        public static int CheckEdgeIntersection(IList<Edge2D> edges)
        {
            uint edgeCnt = (uint)edges.Count;
            for (int iedge = 0; iedge < edgeCnt; iedge++)
            {
                Edge2D iE = edges[iedge];
                if (iE.IsCrossEdgeSelf())
                {
                    return 1;
                }
                uint iPt0 = iE.GetVertexId(true);
                uint iPt1 = iE.GetVertexId(false);
                BoundingBox2D iBB = iE.GetBoundingBox();
                for (int jedge = iedge + 1; jedge < edgeCnt; jedge++)
                {
                    Edge2D jE = edges[jedge];
                    uint jPt0 = jE.GetVertexId(true);
                    uint jPt1 = jE.GetVertexId(false);
                    if ((iPt0 - jPt0) * (iPt0 - jPt1) * (iPt1 - jPt0) * (iPt1 - jPt1) != 0)
                    {
                        BoundingBox2D jBB = jE.GetBoundingBox();
                        if (jBB.MinX > iBB.MaxX || jBB.MaxX < iBB.MinX)
                        {
                            continue;
                        }
                        if (jBB.MinY > iBB.MaxY || jBB.MaxY < iBB.MinY) continue;
                        if (!iE.IsCrossEdge(jE))
                        {
                            continue;
                        }
                        return 1;
                    }
                    if (iPt0 == jPt0 && iPt1 == jPt1)
                    {
                        if (iE.IsCrossEdgeShareBothPoints(jE, true))
                        {
                            return 1;
                        }
                    }
                    else if (iPt0 == jPt1 && iPt1 == jPt0)
                    {
                        if (iE.IsCrossEdgeShareBothPoints(jE, false))
                        {
                            return 1;
                        }
                    }
                    else if (iPt0 == jPt0)
                    {
                        if (iE.IsCrossEdgeShareOnePoint(jE, true, true))
                        {
                            return 1;
                        }
                    }
                    else if (iPt0 == jPt1)
                    {
                        if (iE.IsCrossEdgeShareOnePoint(jE, true, false))
                        {
                            return 1;
                        }
                    }
                    else if (iPt1 == jPt0)
                    {
                        if (iE.IsCrossEdgeShareOnePoint(jE, false, true))
                        {
                            return 1;
                        }
                    }
                    else if (iPt1 == jPt1)
                    {
                        if (iE.IsCrossEdgeShareOnePoint(jE, false, false))
                        {
                            return 1;
                        }
                    }
                    continue;
                }
            }
            return 0;
        }

        public static void RotMatrix33(Quaternion quat, double[] m)
        {
            double real = quat.W;
            double x = quat.X;
            double y = quat.Y;
            double z = quat.Z;

            m[0] = 1.0 - 2.0 * (y * y + z * z);
            m[1] = 2.0 * (x * y - z * real);
            m[2] = 2.0 * (z * x + y * real);

            m[3] = 2.0 * (x * y + z * real);
            m[4] = 1.0 - 2.0 * (z * z + x * x);
            m[5] = 2.0 * (y * z - x * real);

            m[6] = 2.0 * (z * x - y * real);
            m[7] = 2.0 * (y * z + x * real);
            m[8] = 1.0 - 2.0 * (y * y + x * x);
        }

    }
}
