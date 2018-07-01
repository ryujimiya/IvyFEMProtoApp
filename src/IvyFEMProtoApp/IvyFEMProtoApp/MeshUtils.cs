using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    class MeshUtils
    {
        /// <summary>
        /// 線分にいくら点があるか
        /// </summary>
        public const uint NoEd = 2;

        /// <summary>
        /// 三角形にいくら頂点があるか
        /// </summary>
        public const uint NoTri = 3;
        /// <summary>
        /// 三角形にいくら辺があるか
        /// </summary>
        public const uint NoEdTri = 3;
        /// <summary>
        /// 三角形の各辺の頂点番号
        /// </summary>
        public static uint[][] NoELTriEdge = new uint[(int)NoEdTri][]
        {
            new uint[(int)NoEd]{ 1, 2 },
            new uint[(int)NoEd]{ 2, 0 },
            new uint[(int)NoEd]{ 0, 1 }
        };
        /// <summary>
        /// 三角形の隣接関係
        /// </summary>
        public static uint[][] RelTriTri = new uint[3][]
        {
            new uint[3]{ 0, 2, 1 }, //  0
            new uint[3]{ 2, 1, 0 }, //  1 
	        new uint[3]{ 1, 0, 2 } //  2
        };

        /// <summary>
        /// 四角形にいくら頂点があるか
        /// </summary>
        public const uint NoQuad = 4;
        /// <summary>
        /// 四角形にいくら辺があるか
        /// </summary>
        public const uint NoEdQuad = 4;
        /// <summary>
        /// 四角形の各辺の頂点番号
        /// </summary>
        public static uint[][] NoELQuadEdge = new uint[(int)NoEdQuad][]
        {
            new uint[(int)NoEd]{ 0, 1 },
            new uint[(int)NoEd]{ 1, 2 },
            new uint[(int)NoEd]{ 2, 3 },
            new uint[(int)NoEd]{ 3, 0 }
        };
        /// <summary>
        /// 四角形の隣接関係
        /// </summary>
        public static uint[][] RelQuadQuad = new uint[(int)NoQuad][]
        {
            new uint[(int)NoQuad]{ 0, 3, 2, 1 }, //  
            new uint[(int)NoQuad]{ 1, 0, 3, 2 }, //  1
            new uint[(int)NoQuad]{ 2, 1, 0, 3 }, //  2
            new uint[(int)NoQuad]{ 3, 2, 1, 0 } //  3
        };


        private static uint[] InvRelTriTri = new uint[3]
        {
            0, 1, 2
        };

        /// <summary>
        /// (0に相当するノード番号)*3+(1に相当するノード番号)  →→　関係番号
        /// </summary>
        private static int[] NoEL2RelTriTri = new int[9]
        {
            -1, // 0 00
            -1, // 1 01
            0,  // 2 02
            2, // 3 10
            -1, // 4 11
            -1, // 5 12
            -1, // 6 20
            1,  // 7 21
            -1, // 8 22
        };

        /// <summary>
        /// (こちら側の辺番号)*3+(相手側の辺番号)　→→　関係番号
        /// </summary>
        private static uint[] Ed2RelTriTri = new uint[9]
        {
            0,  // 0 00
            2,  // 1 01
            1,  // 2 02
            2,  // 3 10
            1,  // 4 11
            0,  // 5 12
            1,  // 6 20
            0,  // 7 21
            2,  // 8 22
        };

        private static uint[][] IndexRot3 = new uint[3][]
        {
            new uint[3] { 0, 1, 2 },
            new uint[3] { 1, 2, 0 },
            new uint[3] { 2, 0, 1 },
        };

        /// <summary>
        /// ドロネー条件を満たすかどうか調べる
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        public static int DetDelaunay(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            double area = CadUtils.TriArea(p0, p1, p2);
            if (Math.Abs(area) < 1.0e-10)
            {
                return 3;
            }
            double tmpVal = 1.0 / (area * area * 16.0);

            double dtmp0 = CadUtils.SquareLength(p1, p2);
            double dtmp1 = CadUtils.SquareLength(p0, p2);
            double dtmp2 = CadUtils.SquareLength(p0, p1);

            double etmp0 = tmpVal * dtmp0 * (dtmp1 + dtmp2 - dtmp0);
            double etmp1 = tmpVal * dtmp1 * (dtmp0 + dtmp2 - dtmp1);
            double etmp2 = tmpVal * dtmp2 * (dtmp0 + dtmp1 - dtmp2);

            Vector2 outCenter = new Vector2(
                (float)etmp0 * p0.X + (float)etmp1 * p1.X + (float)etmp2 * p2.X,
                (float)etmp0 * p0.Y + (float)etmp1 * p1.Y + (float)etmp2 * p2.Y);

            double qradius = CadUtils.SquareLength(outCenter, p0);
            double qdistance = CadUtils.SquareLength(outCenter, p3);

            //System.Diagnostics.Debug.Assert(Math.Abs(qradius - CadUtils.SquareLength(outCenter, p1)) < 1.0e-10 * qradius);
            //System.Diagnostics.Debug.Assert(Math.Abs(qradius - CadUtils.SquareLength(outCenter, p2)) < 1.0e-10 * qradius);

            const double tol = 1.0e-20;
            if (qdistance > qradius * (1.0 + tol))
            {
                // 外接円の外
                return 2;
            }
            else
            {
                if (qdistance < qradius * (1.0 - tol))
                {
                    // 外接円の中
                    return 0;
                }
                else
                {
                    // 外接円上
                    return 1;
                }
            }
            return 0;
        }

        public static bool CheckTri(IList<Point2D> pts, IList<Tri2D> tris)
        {
            uint nPt = (uint)pts.Count;
            uint nTri = (uint)tris.Count;

            ////////////////////////////////
            // 要素Indexのチェック

            for (uint itri = 0; itri < nTri; itri++)
            {
               Tri2D tri = tris[(int)itri];
                for (uint inotri = 0; inotri < NoTri; inotri++)
                {
                    System.Diagnostics.Debug.Assert(tri.V[inotri] < nPt);
                }
                for (uint iedtri = 0; iedtri < NoEdTri; iedtri++)
                {
                    if (tri.G2[iedtri] == -2 || tri.G2[iedtri] == -3)
                    {
                        uint iSTri = tri.S2[iedtri];
                        uint iRel = tri.R2[iedtri];
                        System.Diagnostics.Debug.Assert(iSTri < nTri);
                        System.Diagnostics.Debug.Assert(iRel < 3);
                        // check sorounding
                        {
                            uint noELDia = RelTriTri[iRel][iedtri];
                            System.Diagnostics.Debug.Assert(noELDia < 3);
                            if (tris[(int)iSTri].S2[noELDia] != itri)
                            {
                                System.Diagnostics.Debug.WriteLine(itri + " " + iedtri);
                            }
                            System.Diagnostics.Debug.Assert(tris[(int)iSTri].S2[noELDia] == itri);
                        }
                        // check relation
                        for (uint inoed = 0; inoed < NoEd; inoed++)
                        {
                            uint inoel = NoELTriEdge[iedtri][inoed];
                            if (tri.V[inoel] != tris[(int)iSTri].V[(int)RelTriTri[iRel][inoel]])
                            {
                                System.Diagnostics.Debug.WriteLine(itri + " " + iedtri);
                            }
                            System.Diagnostics.Debug.Assert(tri.V[inoel] ==
                                tris[(int)iSTri].V[(int)RelTriTri[iRel][inoel]]);
                        }
                    }
                }
                {
                    if (tri.G2[0] == -1 && tri.G2[1] == -1 && tri.G2[2] == -1)
                    {
                        System.Diagnostics.Debug.WriteLine("Isolated Triangle " + itri);
                    }
                }
            }

            ////////////////////////////////
            // 頂点-要素間の一貫性のチェック

            for (uint ipt = 0; ipt < nPt; ipt++)
            {
                if (pts[(int)ipt].Elem >= 0)
                {
                    System.Diagnostics.Debug.Assert(pts[(int)ipt].Dir >= 0 && pts[(int)ipt].Dir < 3);
                    int itri0 = pts[(int)ipt].Elem;
                    uint inoel0 = pts[(int)ipt].Dir;
                    if (tris[itri0].V[inoel0] != ipt)
                    {
                        System.Diagnostics.Debug.WriteLine(itri0 + " " + inoel0 + "   " +
                            tris[itri0].V[inoel0] + " " + ipt);
                    }
                    System.Diagnostics.Debug.Assert(tris[itri0].V[inoel0] == ipt);
                }
            }

            ////////////////////////////////
            // Geometryのチェック

            for (uint itri = 0; itri < nTri; itri++)
            {
                Tri2D tri = tris[(int)itri];
                {
                    double area = CadUtils.TriArea(
                        pts[(int)tri.V[0]].Point,
                        pts[(int)tri.V[1]].Point,
                        pts[(int)tri.V[2]].Point);
                    if (area < 1.0e-10)
                    {
                        System.Diagnostics.Debug.WriteLine("Negative Volume : " + itri + " " + area);
                    }
                }

                // コメントアウトされてた部分　↓
                {
                    Vector2 v0 = pts[(int)tri.V[0]].Point;
                    Vector2 v1 = pts[(int)tri.V[1]].Point;
                    Vector2 v2 = pts[(int)tri.V[2]].Point;

                    double area = CadUtils.TriArea(v0, v1, v2);
                    double tmp1 = 0.5 / area;

                    double[] constTerm = new double[3];
                    constTerm[0] = tmp1 * (v1.X * v2.Y - v2.X * v1.Y);
                    constTerm[1] = tmp1 * (v2.X * v0.Y - v0.X * v2.Y);
                    constTerm[2] = tmp1 * (v0.X * v1.Y - v1.X * v0.Y);

                    double[][] dldx = new double[3][];
                    for (int i = 0; i < 3;  i++)
                    {
                        dldx[i] = new double[2];
                    }
                    dldx[0][0] = tmp1 * (v1.Y - v2.Y);
                    dldx[1][0] = tmp1 * (v2.Y - v0.Y);
                    dldx[2][0] = tmp1 * (v0.Y - v1.Y);

                    dldx[0][1] = tmp1 * (v2.X - v1.X);
                    dldx[1][1] = tmp1 * (v0.X - v2.X);
                    dldx[2][1] = tmp1 * (v1.X - v0.X);

                    System.Diagnostics.Debug.Assert(Math.Abs(dldx[0][0] + dldx[1][0] + dldx[2][0]) < 1.0e-15);
                    System.Diagnostics.Debug.Assert(Math.Abs(dldx[0][1] + dldx[1][1] + dldx[2][1]) < 1.0e-15);

                    System.Diagnostics.Debug.Assert(Math.Abs(constTerm[0] + dldx[0][0] * v0.X +
                        dldx[0][1] * v0.Y - 1.0) < 1.0e-10);
                    System.Diagnostics.Debug.Assert(Math.Abs(constTerm[0] + dldx[0][0] * v1.X +
                        dldx[0][1] * v1.Y) < 1.0e-10);
                    System.Diagnostics.Debug.Assert(Math.Abs(constTerm[0] + dldx[0][0] * v2.X +
                        dldx[0][1] * v2.Y) < 1.0e-10);

                    System.Diagnostics.Debug.Assert(Math.Abs(constTerm[1] + dldx[1][0] * v0.X +
                        dldx[1][1] * v0.Y) < 1.0e-10);
                    System.Diagnostics.Debug.Assert(Math.Abs(constTerm[1] + dldx[1][0] * v1.X +
                        dldx[1][1] * v1.Y - 1.0) < 1.0e-10);
                    System.Diagnostics.Debug.Assert(Math.Abs(constTerm[1] + dldx[1][0] * v2.X +
                        dldx[1][1] * v2.Y) < 1.0e-10);

                    System.Diagnostics.Debug.Assert(Math.Abs(constTerm[2] + dldx[2][0] * v0.X +
                        dldx[2][1] * v0.Y) < 1.0e-10);
                    System.Diagnostics.Debug.Assert(Math.Abs(constTerm[2] + dldx[2][0] * v1.X +
                        dldx[2][1] * v1.Y) < 1.0e-10);
                    System.Diagnostics.Debug.Assert(Math.Abs(constTerm[2] + dldx[2][0] * v2.X +
                        dldx[2][1] * v2.Y - 1.0) < 1.0e-10);
                }
            }

            return true;
        }

        public static bool InsertPointElem(uint iInsPt, uint iInsTri,
                              IList<Point2D> points, IList<Tri2D> tris)
        {
            System.Diagnostics.Debug.Assert(iInsTri < tris.Count);
            System.Diagnostics.Debug.Assert(iInsPt < points.Count);

            int itri0 = (int)iInsTri;
            int itri1 = tris.Count;
            int itri2 = tris.Count + 1;

            int trisCnt = tris.Count;
            for (int i = trisCnt; i < trisCnt + 2; i++)
            {
                tris.Add(new Tri2D());
            }

            Tri2D oldTri = new Tri2D(tris[(int)iInsTri]);

            points[(int)iInsPt].Elem = itri0;
            points[(int)iInsPt].Dir = 0;
            points[(int)oldTri.V[0]].Elem = itri1;
            points[(int)oldTri.V[0]].Dir = 2;
            points[(int)oldTri.V[1]].Elem = itri2;
            points[(int)oldTri.V[1]].Dir = 2;
            points[(int)oldTri.V[2]].Elem = itri0;
            points[(int)oldTri.V[2]].Dir = 2;

            {
                Tri2D tri = tris[itri0];

                tri.V[0] = iInsPt;
                tri.V[1] = oldTri.V[1];
                tri.V[2] = oldTri.V[2];
                tri.G2[0] = oldTri.G2[0];
                tri.G2[1] = -2;
                tri.G2[2] = -2;
                tri.S2[0] = oldTri.S2[0];
                tri.S2[1] = (uint)itri1;
                tri.S2[2] = (uint)itri2;

                if (oldTri.G2[0] == -2 || oldTri.G2[0] == -3)
                {
                    System.Diagnostics.Debug.Assert(oldTri.R2[0] < 3);
                    uint[] rel = RelTriTri[oldTri.R2[0]];
                    tri.R2[0] = (uint)NoEL2RelTriTri[rel[0] * 3 + rel[1]];
                    System.Diagnostics.Debug.Assert(tri.R2[0] >= 0 && tri.R2[0] < 3);
                    System.Diagnostics.Debug.Assert(oldTri.S2[0] < tris.Count);
                    tris[(int)oldTri.S2[0]].S2[rel[0]] = (uint)itri0;
                    tris[(int)oldTri.S2[0]].R2[rel[0]] = InvRelTriTri[tri.R2[0]];
                }
                tri.R2[1] = 0;
                tri.R2[2] = 0;
            }
            {
                Tri2D tri = tris[itri1];

                tri.V[0] = iInsPt;
                tri.V[1] = oldTri.V[2];
                tri.V[2] = oldTri.V[0];
                tri.G2[0] = oldTri.G2[1];
                tri.G2[1] = -2;
                tri.G2[2] = -2;
                tri.S2[0] = oldTri.S2[1];
                tri.S2[1] = (uint)itri2;
                tri.S2[2] = (uint)itri0;

                if (oldTri.G2[1] == -2 || oldTri.G2[1] == -3)
                {
                    System.Diagnostics.Debug.Assert(oldTri.R2[1] < 3);
                    uint[] rel = RelTriTri[oldTri.R2[1]];
                    tri.R2[0] = (uint)NoEL2RelTriTri[rel[1] * 3 + rel[2]];
                    System.Diagnostics.Debug.Assert(tri.R2[0] >= 0 && tri.R2[0] < 3);
                    System.Diagnostics.Debug.Assert(oldTri.S2[1] < tris.Count);
                    tris[(int)oldTri.S2[1]].S2[rel[1]] = (uint)itri1;
                    tris[(int)oldTri.S2[1]].R2[rel[1]] = InvRelTriTri[tri.R2[0]];
                }
                tri.R2[1] = 0;
                tri.R2[2] = 0;
            }
            {
                Tri2D tri = tris[itri2];

                tri.V[0] = iInsPt;
                tri.V[1] = oldTri.V[0];
                tri.V[2] = oldTri.V[1];
                tri.G2[0] = oldTri.G2[2];
                tri.G2[1] = -2;
                tri.G2[2] = -2;
                tri.S2[0] = oldTri.S2[2];
                tri.S2[1] = (uint)itri0;
                tri.S2[2] = (uint)itri1;

                if (oldTri.G2[2] == -2 || oldTri.G2[2] == -3)
                {
                    System.Diagnostics.Debug.Assert(oldTri.R2[2] < 3);
                    uint[] rel = RelTriTri[oldTri.R2[2]];
                    tri.R2[0] = (uint)NoEL2RelTriTri[rel[2] * 3 + rel[0]];
                    System.Diagnostics.Debug.Assert(tri.R2[0] >= 0 && tri.R2[0] < 3);
                    System.Diagnostics.Debug.Assert(oldTri.S2[2] < tris.Count);
                    tris[(int)oldTri.S2[2]].S2[rel[2]] = (uint)itri2;
                    tris[(int)oldTri.S2[2]].R2[rel[2]] = InvRelTriTri[tri.R2[0]];
                }
                tri.R2[1] = 0;
                tri.R2[2] = 0;
            }

            return true;
        }

        public static bool InsertPointElemEdge(uint iInsPt, uint iInsTri, uint iInsEd,
            IList<Point2D> points, IList<Tri2D> tris)
        {
            System.Diagnostics.Debug.Assert(iInsTri < tris.Count);
            System.Diagnostics.Debug.Assert(iInsPt < points.Count);

            if (tris[(int)iInsTri].G2[iInsEd] != -2)
            {
                // 未実装
                System.Diagnostics.Debug.Assert(false);
                new NotImplementedException();
            }

            uint iAdjTri = tris[(int)iInsTri].S2[iInsEd];
            uint iAdjEd = RelTriTri[(int)tris[(int)iInsTri].R2[iInsEd]][iInsEd];
            System.Diagnostics.Debug.Assert(iAdjTri < tris.Count);
            System.Diagnostics.Debug.Assert(iInsEd < 3);

            uint itri0 = iInsTri;
            uint itri1 = iAdjTri;
            uint itri2 = (uint)tris.Count;
            uint itri3 = (uint)(tris.Count + 1);

            int trisCnt = tris.Count; 
            for (int i = trisCnt; i < trisCnt + 2; i++)
            {
                tris.Add(new Tri2D());
            }

            Tri2D old0 = new Tri2D(tris[(int)iInsTri]);
            Tri2D old1 = new Tri2D(tris[(int)iAdjTri]);

            uint ino00 = iInsEd;
            uint ino10 = NoELTriEdge[iInsEd][0];
            uint ino20 = NoELTriEdge[iInsEd][1];

            uint ino01 = iAdjEd;
            uint ino11 = NoELTriEdge[iAdjEd][0];
            uint ino21 = NoELTriEdge[iAdjEd][1];

            System.Diagnostics.Debug.Assert(old0.V[ino10] == old1.V[ino21]);
            System.Diagnostics.Debug.Assert(old0.V[ino20] == old1.V[ino11]);
            System.Diagnostics.Debug.Assert(old0.S2[ino00] == itri1);
            System.Diagnostics.Debug.Assert(old1.S2[ino01] == itri0);

            points[(int)iInsPt].Elem = (int)itri0;
            points[(int)iInsPt].Dir = 0;
            points[(int)old0.V[ino20]].Elem = (int)itri0;
            points[(int)old0.V[ino20]].Dir = 1;
            points[(int)old0.V[ino00]].Elem = (int)itri1;
            points[(int)old0.V[ino00]].Dir = 1;
            points[(int)old1.V[ino21]].Elem = (int)itri2;
            points[(int)old1.V[ino21]].Dir = 1;
            points[(int)old1.V[ino01]].Elem = (int)itri3;
            points[(int)old1.V[ino01]].Dir = 1;

            {
                Tri2D  tri = tris[(int)itri0];
                tri.V[0] = iInsPt;
                tri.V[1] = old0.V[ino20];
                tri.V[2] = old0.V[ino00];
                tri.G2[0] = old0.G2[ino10];
                tri.G2[1] = -2;
                tri.G2[2] = -2;
                tri.S2[0] = old0.S2[ino10];
                tri.S2[1] = itri1;
                tri.S2[2] = itri3;
                if (old0.G2[ino10] == -2 || old0.G2[ino10] == -3)
                {
                    System.Diagnostics.Debug.Assert(old0.R2[ino10] < 3);
                    uint[] rel = RelTriTri[old0.R2[ino10]];
                    tri.R2[0] = (uint)NoEL2RelTriTri[rel[ino10] * 3 + rel[ino20]];
                    System.Diagnostics.Debug.Assert(tri.R2[0] >= 0 && tri.R2[0] < 3);
                    System.Diagnostics.Debug.Assert(old0.S2[ino10] < tris.Count);
                    tris[(int)old0.S2[ino10]].S2[rel[ino10]] = itri0;
                    tris[(int)old0.S2[ino10]].R2[rel[ino10]] = InvRelTriTri[tri.R2[0]];
                }
                tri.R2[1] = 0;
                tri.R2[2] = 0;
            }
            {
                Tri2D tri = tris[(int)itri1];
                tri.V[0] = iInsPt;
                tri.V[1] = old0.V[ino00];
                tri.V[2] = old0.V[ino10];
                tri.G2[0] = old0.G2[ino20];
                tri.G2[1] = -2;
                tri.G2[2] = -2;
                tri.S2[0] = old0.S2[ino20];
                tri.S2[1] = itri2;
                tri.S2[2] = itri0;
                if (old0.G2[ino20] == -2 || old0.G2[ino20] == -3)
                {
                    System.Diagnostics.Debug.Assert(old0.R2[ino20] < 3);
                    uint[] rel = RelTriTri[old0.R2[ino20]];
                    tri.R2[0] = (uint)NoEL2RelTriTri[rel[ino20] * 3 + rel[ino00]];
                    System.Diagnostics.Debug.Assert(tri.R2[0] >= 0 && tri.R2[0] < 3);
                    System.Diagnostics.Debug.Assert(old0.S2[ino20] < tris.Count);
                    tris[(int)old0.S2[ino20]].S2[rel[ino20]] = itri1;
                    tris[(int)old0.S2[ino20]].R2[rel[ino20]] = InvRelTriTri[tri.R2[0]];
                }
                tri.R2[1] = 0;
                tri.R2[2] = 0;
            }
            {
                Tri2D tri = tris[(int)itri2];
                tri.V[0] = iInsPt;
                tri.V[1] = old1.V[ino21];
                tri.V[2] = old1.V[ino01];
                tri.G2[0] = old1.G2[ino11];
                tri.G2[1] = -2;
                tri.G2[2] = -2;
                tri.S2[0] = old1.S2[ino11];
                tri.S2[1] = itri3;
                tri.S2[2] = itri1;
                if (old1.G2[ino11] == -2 || old0.G2[ino20] == -3)
                {
                    System.Diagnostics.Debug.Assert(old1.R2[ino11] < 3);
                    uint[] rel = RelTriTri[old1.R2[ino11]];
                    tri.R2[0] = (uint)NoEL2RelTriTri[rel[ino11] * 3 + rel[ino21]];
                    System.Diagnostics.Debug.Assert(tri.R2[0] >= 0 && tri.R2[0] < 3);
                    System.Diagnostics.Debug.Assert(old1.S2[ino11] < tris.Count);
                    tris[(int)old1.S2[ino11]].S2[rel[ino11]] = itri2;
                    tris[(int)old1.S2[ino11]].R2[rel[ino11]] = InvRelTriTri[tri.R2[0]];
                }
                tri.R2[1] = 0;
                tri.R2[2] = 0;
            }
            {
                Tri2D tri = tris[(int)itri3];
                tri.V[0] = iInsPt;
                tri.V[1] = old1.V[ino01];
                tri.V[2] = old1.V[ino11];
                tri.G2[0] = old1.G2[ino21];
                tri.G2[1] = -2;
                tri.G2[2] = -2;
                tri.S2[0] = old1.S2[ino21];
                tri.S2[1] = itri0;
                tri.S2[2] = itri2;
                if (old1.G2[ino21] == -2 || old1.G2[ino21] == -3)
                {
                    System.Diagnostics.Debug.Assert(old1.R2[ino21] < 3);
                    uint[] rel = RelTriTri[old1.R2[ino21]];
                    tri.R2[0] = (uint)NoEL2RelTriTri[rel[ino21] * 3 + rel[ino01]];
                    System.Diagnostics.Debug.Assert(tri.R2[0] >= 0 && tri.R2[0] < 3);
                    System.Diagnostics.Debug.Assert(old1.S2[ino21] < tris.Count);
                    tris[(int)old1.S2[ino21]].S2[rel[ino21]] = itri3;
                    tris[(int)old1.S2[ino21]].R2[rel[ino21]] = InvRelTriTri[tri.R2[0]];
                }
                tri.R2[1] = 0;
                tri.R2[2] = 0;
            }
            return true;
        }

        public static bool DelaunayAroundPoint(uint ipo0, IList<Point2D> points, IList<Tri2D> tris)
        {
            System.Diagnostics.Debug.Assert(ipo0 < points.Count);
            if (points[(int)ipo0].Elem == -1)
            {
                return true;
            }

            System.Diagnostics.Debug.Assert(points[(int)ipo0].Elem >= 0 &&
                points[(int)ipo0].Elem < tris.Count );
            System.Diagnostics.Debug.Assert(tris[points[(int)ipo0].Elem].V[points[(int)ipo0].Dir] == ipo0);

            uint itri0 = (uint)points[(int)ipo0].Elem;
            uint inotri0 = points[(int)ipo0].Dir;

            uint iCurTri = itri0;
            uint iNoCurTri = points[(int)ipo0].Dir;
            bool isWall = false;
            for (;;)
            {
                System.Diagnostics.Debug.Assert(tris[(int)iCurTri].V[iNoCurTri] == ipo0);

                if (tris[(int)iCurTri].G2[iNoCurTri] == -2)
                {
                    // 向かいの要素を調べる
                    uint iDiaTri = tris[(int)iCurTri].S2[iNoCurTri];
                    uint[] diaRel = RelTriTri[tris[(int)iCurTri].R2[iNoCurTri]];
                    uint iDiaNoTri = diaRel[iNoCurTri];
                    System.Diagnostics.Debug.Assert(tris[(int)iDiaTri].G2[iDiaNoTri] == -2);
                    System.Diagnostics.Debug.Assert(tris[(int)iDiaTri].S2[iDiaNoTri] == iCurTri);
                    uint iDiaPt = tris[(int)iDiaTri].V[iDiaNoTri];
                    if (DetDelaunay(
                        points[(int)tris[(int)iCurTri].V[0]].Point,
                        points[(int)tris[(int)iCurTri].V[1]].Point,
                        points[(int)tris[(int)iCurTri].V[2]].Point,
                        points[(int)iDiaPt].Point) == 0)
                    {
                        // Delaunay条件が満たされない場合

                        // 辺を切り替える
                        // FlipEdgeによってitri_curは時計回り側の３角形に切り替わる
                        FlipEdge(iCurTri, iNoCurTri, points, tris);

                        iNoCurTri = 2;
                        System.Diagnostics.Debug.Assert(tris[(int)iCurTri].V[iNoCurTri] == ipo0);
                        // Flipによってtris[itri0].V[inotri0] != ipo0 でなくなってしまうのを防ぐため
                        if (iCurTri == itri0)
                        {
                            inotri0 = iNoCurTri;
                        }
                        continue; // ループの始めに戻る
                    }
                }

                {
                    // 次の要素へ進める
                    uint inotri1 = IndexRot3[1][iNoCurTri];
                    if (tris[(int)iCurTri].G2[inotri1] != -2 && tris[(int)iCurTri].G2[inotri1] != -3)
                    {
                        isWall = true;
                        break;
                    }
                    uint iNexTri = tris[(int)iCurTri].S2[inotri1];
                    uint[] nexRel = RelTriTri[tris[(int)iCurTri].R2[inotri1]];
                    uint iNexNoTri = nexRel[iNoCurTri];
                    System.Diagnostics.Debug.Assert(tris[(int)iNexTri].V[iNexNoTri] == ipo0);
                    if (iNexTri == itri0)
                    {
                        break;   // 一周したら終わり
                    }
                    iCurTri = iNexTri;
                    iNoCurTri = iNexNoTri;
                }
            }
            if (!isWall)
            {
                return true;
            }

            ////////////////////////////////
            // 逆向きへの回転

            iCurTri = itri0;
            iNoCurTri = inotri0;
            for (;;)
            {
                System.Diagnostics.Debug.Assert(tris[(int)iCurTri].V[iNoCurTri] == ipo0);

                if (tris[(int)iCurTri].G2[iNoCurTri] == -2)
                {
                    // 向かいの要素を調べる
                    uint iDiaTri = tris[(int)iCurTri].S2[iNoCurTri];
                    uint[] diaRel = RelTriTri[tris[(int)iCurTri].R2[iNoCurTri]];
                    uint iDiaNoTri = diaRel[iNoCurTri];
                    System.Diagnostics.Debug.Assert(tris[(int)iDiaTri].G2[iDiaNoTri] == -2);
                    System.Diagnostics.Debug.Assert(tris[(int)iDiaTri].S2[iDiaNoTri] == iCurTri);
                    uint iDiaPt = tris[(int)iDiaTri].V[iDiaNoTri];
                    if (DetDelaunay(
                        points[(int)tris[(int)iCurTri].V[0]].Point,
                        points[(int)tris[(int)iCurTri].V[1]].Point,
                        points[(int)tris[(int)iCurTri].V[2]].Point,
                        points[(int)iDiaPt].Point) == 0)
                    {
                        // Delaunay条件が満たされない場合

                        // 辺を切り替える
                        FlipEdge(iCurTri, iNoCurTri, points, tris);

                        iCurTri = iDiaTri;
                        iNoCurTri = 1;
                        System.Diagnostics.Debug.Assert(tris[(int)iCurTri].V[iNoCurTri] == ipo0);
                        continue;   // ループの始めに戻る
                    }
                }

                { 
                    // 次の要素へ進める
                    uint inotri2 = IndexRot3[2][iNoCurTri];
                    if (tris[(int)iCurTri].G2[inotri2] != -2 && tris[(int)iCurTri].G2[inotri2] != -3)
                    {
                        return true;
                    }
                    uint iNexTri = tris[(int)iCurTri].S2[inotri2];
                    uint[] nexRel = RelTriTri[tris[(int)iCurTri].R2[inotri2]];
                    uint iNexNoTri = nexRel[iNoCurTri];
                    System.Diagnostics.Debug.Assert(tris[(int)iNexTri].V[iNexNoTri] == ipo0);
                    System.Diagnostics.Debug.Assert(iNexTri != itri0);  // 一周したら終わり
                    iCurTri = iNexTri;
                    iNoCurTri = iNexNoTri;
                }
            }
            return true;
        }

        public static bool FlipEdge(uint itri0, uint ied0, IList<Point2D> points, IList<Tri2D> tris)
        {
            System.Diagnostics.Debug.Assert(itri0 < tris.Count);
            System.Diagnostics.Debug.Assert(ied0 < 3);
            System.Diagnostics.Debug.Assert(tris[(int)itri0].G2[ied0] == -2);

            uint itri1 = tris[(int)itri0].S2[ied0];
            uint ied1 = RelTriTri[tris[(int)itri0].R2[ied0]][ied0];
            System.Diagnostics.Debug.Assert(itri1 < tris.Count);
            System.Diagnostics.Debug.Assert(ied1 < 3);
            System.Diagnostics.Debug.Assert(tris[(int)itri1].G2[ied1] == -2);

            //	std::cout << itri0 << "-" << ied0 << "    " << itri1 << "-" << ied1 << std::endl;

            Tri2D old0 = new Tri2D(tris[(int)itri0]);
            Tri2D old1 = new Tri2D(tris[(int)itri1]);

            uint no00 = ied0;
            uint no10 = NoELTriEdge[ied0][0];
            uint no20 = NoELTriEdge[ied0][1];

            uint no01 = ied1;
            uint no11 = NoELTriEdge[ied1][0];
            uint no21 = NoELTriEdge[ied1][1];

            System.Diagnostics.Debug.Assert(old0.V[no10] == old1.V[no21]);
            System.Diagnostics.Debug.Assert(old0.V[no20] == old1.V[no11]);

            points[(int)old0.V[no10]].Elem = (int)itri0;
            points[(int)old0.V[no10]].Dir = 0;
            points[(int)old0.V[no00]].Elem = (int)itri0;
            points[(int)old0.V[no00]].Dir = 2;
            points[(int)old1.V[no11]].Elem = (int)itri1;
            points[(int)old1.V[no11]].Dir = 0;
            points[(int)old1.V[no01]].Elem = (int)itri1;
            points[(int)old1.V[no01]].Dir = 2;

            {
                Tri2D tri = tris[(int)itri0];
                tri.V[0] = old0.V[no10];
                tri.V[1] = old1.V[no01];
                tri.V[2] = old0.V[no00];
                tri.G2[0] = -2;
                tri.G2[1] = old0.G2[no20];
                tri.G2[2] = old1.G2[no11];
                tri.S2[0] = itri1;
                tri.S2[1] = old0.S2[no20];
                tri.S2[2] = old1.S2[no11];

                tri.R2[0] = 0;
                if (old0.G2[no20] == -2 || old0.G2[no20] == -3)
                {
                    System.Diagnostics.Debug.Assert(old0.R2[no20] < 3);
                    uint[] rel = RelTriTri[old0.R2[no20]];
                    System.Diagnostics.Debug.Assert(old0.S2[no20] < tris.Count);
                    System.Diagnostics.Debug.Assert(old0.S2[no20] != itri0);
                    System.Diagnostics.Debug.Assert(old0.S2[no20] != itri1);
                    tri.R2[1] = (uint)NoEL2RelTriTri[rel[no10] * 3 + rel[no20]];
                    System.Diagnostics.Debug.Assert(tri.R2[1] >= 0 && tri.R2[1] < 3);
                    tris[(int)old0.S2[no20]].S2[rel[no20]] = itri0;
                    tris[(int)old0.S2[no20]].R2[rel[no20]] = InvRelTriTri[tri.R2[1]];
                }
                if (old1.G2[no11] == -2 || old1.G2[no11] == -3)
                {
                    System.Diagnostics.Debug.Assert(old1.R2[no11] < 3);
                    uint[] rel = RelTriTri[old1.R2[no11]];
                    System.Diagnostics.Debug.Assert(old1.S2[no11] < tris.Count);
                    tri.R2[2] = (uint)NoEL2RelTriTri[rel[no21] * 3 + rel[no01]];
                    System.Diagnostics.Debug.Assert(tri.R2[2] >= 0 && tri.R2[2] < 3);
                    tris[(int)old1.S2[no11]].S2[rel[no11]] = itri0;
                    tris[(int)old1.S2[no11]].R2[rel[no11]] = InvRelTriTri[tri.R2[2]];
                }
            }

            {
                Tri2D tri = tris[(int)itri1];
                tri.V[0] = old1.V[no11];
                tri.V[1] = old0.V[no00];
                tri.V[2] = old1.V[no01];
                tri.G2[0] = -2;
                tri.G2[1] = old1.G2[no21];
                tri.G2[2] = old0.G2[no10];
                tri.S2[0] = itri0; tri.S2[1] = old1.S2[no21];
                tri.S2[2] = old0.S2[no10];

                tri.R2[0] = 0;
                if (old1.G2[no21] == -2 || old1.G2[no21] == -3)
                {
                    System.Diagnostics.Debug.Assert(old1.R2[no21] < 3);
                    uint[] rel = RelTriTri[old1.R2[no21]];
                    System.Diagnostics.Debug.Assert(old1.S2[no21] < tris.Count);
                    tri.R2[1] = (uint)NoEL2RelTriTri[rel[no11] * 3 + rel[no21]];
                    System.Diagnostics.Debug.Assert(tri.R2[1] >= 0 && tri.R2[1] < 3);
                    tris[(int)old1.S2[no21]].S2[rel[no21]] = itri1;
                    tris[(int)old1.S2[no21]].R2[rel[no21]] = InvRelTriTri[tri.R2[1]];
                }
                if (old0.G2[no10] == -2 || old0.G2[no10] == -3)
                {
                    System.Diagnostics.Debug.Assert(old0.R2[no10] < 3);
                    uint[] rel = RelTriTri[old0.R2[no10]];
                    System.Diagnostics.Debug.Assert(old0.S2[no10] < tris.Count);
                    tri.R2[2] = (uint)NoEL2RelTriTri[rel[no20] * 3 + rel[no00]];
                    System.Diagnostics.Debug.Assert(tri.R2[2] >= 0 && tri.R2[2] < 3);
                    tris[(int)old0.S2[no10]].S2[rel[no10]] = itri1;
                    tris[(int)old0.S2[no10]].R2[rel[no10]] = InvRelTriTri[tri.R2[2]];
                }
            }
            return true;
        }

        // 辺[ipo0-ipo1]の左側の３角形itri0を探索する
        // 三角形がなければ->falseを返す。
        // 三角形があれば  ->true を返す。
        // 但し、その場合
        // tri[itri0].v[inotri0]==ipo0
        // tri[itri0].v[inotri1]==ipo1
        // を満たす
        public static bool FindEdge(uint ipo0, uint ipo1, out uint itri0, out uint inotri0, out uint inotri1,
            IList<Point2D> points, IList<Tri2D> tris)
        {
            itri0 = 0;
            inotri0 = 0;
            inotri1 = 0;

            uint iIniTri = (uint)points[(int)ipo0].Elem;
            uint iIniNoTri = points[(int)ipo0].Dir;
            uint iCurTri = iIniTri;
            uint iCurNoTri = iIniNoTri;
            for (;;)
            {
                //　時計周りに検索する。
                System.Diagnostics.Debug.Assert(tris[(int)iCurTri].V[iCurNoTri] == ipo0);
                { 
                    // この要素がOKか調べる
                    uint inotri2 = IndexRot3[1][iCurNoTri];
                    if (tris[(int)iCurTri].V[inotri2] == ipo1)
                    {
                        itri0 = iCurTri;
                        inotri0 = iCurNoTri;
                        inotri1 = inotri2;
                        System.Diagnostics.Debug.Assert(tris[(int)itri0].V[inotri0] == ipo0);
                        System.Diagnostics.Debug.Assert(tris[(int)itri0].V[inotri1] == ipo1);
                        return true;
                    }
                }
                {   // 次の要素へ進める
                    uint inotri2 = IndexRot3[2][iCurNoTri];
                    if (tris[(int)iCurTri].G2[inotri2] != -2 && tris[(int)iCurTri].G2[inotri2] != -3)
                    {
                        break;
                    }
                    uint iNexTri = tris[(int)iCurTri].S2[inotri2];
                    uint[] rel = RelTriTri[tris[(int)iCurTri].R2[inotri2]];
                    uint inotri3 = rel[iCurNoTri];
                    System.Diagnostics.Debug.Assert(tris[(int)iNexTri].V[inotri3] == ipo0);
                    if (iNexTri == iIniTri)
                    {
                        return false;
                    }
                    iCurTri = iNexTri;
                    iCurNoTri = inotri3;
                }
            }

            iCurNoTri = iIniNoTri;
            iCurTri = iIniTri;
            for (;;)
            {   //　反時計周りの検索
                System.Diagnostics.Debug.Assert(tris[(int)iCurTri].V[iCurNoTri] == ipo0);
                {  
                    // 次の要素へ進める
                    uint inotri2 = IndexRot3[1][iCurNoTri];
                    if (tris[(int)iCurTri].G2[inotri2] != -2 || tris[(int)iCurTri].G2[inotri2] != -3)
                    {
                        break;
                    }
                    uint iNexTri = tris[(int)iCurTri].S2[inotri2];
                    uint[] rel = RelTriTri[tris[(int)iCurTri].R2[inotri2]];
                    uint inotri3 = rel[iCurNoTri];
                    System.Diagnostics.Debug.Assert(tris[(int)iNexTri].V[inotri3] == ipo0);
                    if (iNexTri == iIniTri)
                    {   // 一周したら終わり
                        itri0 = 0;
                        inotri0 = 0; inotri1 = 0;
                        return false;
                    }
                    iCurTri = iNexTri;
                    iCurNoTri = inotri3;
                }
                {
                    // 要素の向きを調べる
                    uint inotri2 = IndexRot3[1][iCurNoTri];
                    if (tris[(int)iCurTri].V[inotri2] == ipo1)
                    {
                        itri0 = iCurTri;
                        inotri0 = iCurNoTri;
                        inotri1 = inotri2;
                        System.Diagnostics.Debug.Assert(tris[(int)itri0].V[inotri0] == ipo0);
                        System.Diagnostics.Debug.Assert(tris[(int)itri0].V[inotri1] == ipo1);
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool FindEdgePointAcrossEdge(uint ipo0, uint ipo1, 
            out uint itri0, out uint inotri0, out uint inotri1, out double ratio,
            IList<Point2D> points, IList<Tri2D> tris)
        {
            uint iIniTri = (uint)points[(int)ipo0].Elem;
            uint iIniNoTri = points[(int)ipo0].Dir;
            uint iCurTri = iIniTri;
            uint iCurNoTri = iIniNoTri;
            for (;;)
            {
                //　反時計周りの検索
                System.Diagnostics.Debug.Assert(tris[(int)iCurTri].V[iCurNoTri] == ipo0);
                {
                    uint inotri2 = IndexRot3[1][iCurNoTri];
                    uint inotri3 = IndexRot3[2][iCurNoTri];
                    double area0 = CadUtils.TriArea(
                        points[(int)ipo0].Point,
                        points[(int)tris[(int)iCurTri].V[inotri2]].Point,
                        points[(int)ipo1].Point);
                    if (area0 > -1.0e-20)
                    {
                        double area1 = CadUtils.TriArea(
                            points[(int)ipo0].Point,
                            points[(int)ipo1].Point,
                            points[(int)tris[(int)iCurTri].V[inotri3]].Point);
                        if (area1 > -1.0e-20)
                        {
                            System.Diagnostics.Debug.Assert(area0 + area1 > 1.0e-20);
                            ratio = area0 / (area0 + area1);
                            itri0 = iCurTri;
                            inotri0 = inotri2;
                            inotri1 = inotri3;
                            return true;
                        }
                    }
                }
                { 
                    // 次の要素へ進める
                    uint inotri2 = IndexRot3[1][iCurNoTri];
                    if (tris[(int)iCurTri].G2[inotri2] != -2 && tris[(int)iCurTri].G2[inotri2] != -3)
                    {
                        break;
                    }
                    uint iNexTri = tris[(int)iCurTri].S2[inotri2];
                    uint[] rel = RelTriTri[tris[(int)iCurTri].R2[inotri2]];
                    uint inotri3 = rel[iCurNoTri];
                    System.Diagnostics.Debug.Assert(tris[(int)iNexTri].V[inotri3] == ipo0);
                    if (iNexTri == iIniTri)
                    { 
                        // 一周したら終わり
                        itri0 = 0;
                        inotri0 = 0; inotri1 = 0;
                        ratio = 0.0;
                        return false;
                    }
                    iCurTri = iNexTri;
                    iCurNoTri = inotri3;
                }
            }

            iCurNoTri = iIniNoTri;
            iCurTri = iIniTri;
            for (;;)
            {  
                //　時計周りに検索する。
                System.Diagnostics.Debug.Assert(tris[(int)iCurTri].V[iCurNoTri] == ipo0);
                {
                    uint inotri2 = IndexRot3[1][iCurNoTri];
                    uint inotri3 = IndexRot3[2][iCurNoTri];
                    double area0 = CadUtils.TriArea(
                        points[(int)ipo0].Point,
                        points[(int)tris[(int)iCurTri].V[inotri2]].Point,
                        points[(int)ipo1].Point);
                    if (area0 > -1.0e-20)
                    {
                        double area1 = CadUtils.TriArea(
                            points[(int)ipo0].Point,
                            points[(int)ipo1].Point,
                            points[(int)tris[(int)iCurTri].V[inotri3]].Point);
                        if (area1 > -1.0e-20)
                        {
                            System.Diagnostics.Debug.Assert(area0 + area1 > 1.0e-20);
                            ratio = area0 / (area0 + area1);
                            itri0 = iCurTri;
                            inotri0 = inotri2;
                            inotri1 = inotri3;
                            return true;
                        }
                    }
                }
                {
                    // 次の要素へ進める
                    uint inotri2 = IndexRot3[2][iCurNoTri];
                    if (tris[(int)iCurTri].G2[inotri2] != -2 && tris[(int)iCurTri].G2[inotri2] != -3)
                    {
                        break;
                    }
                    uint iNexTri = tris[(int)iCurTri].S2[inotri2];
                    uint[] rel = RelTriTri[tris[(int)iCurTri].R2[inotri2]];
                    uint inotri3 = rel[iCurNoTri];
                    System.Diagnostics.Debug.Assert(tris[(int)iNexTri].V[inotri3] == ipo0);
                    if (iNexTri == iIniTri)
                    {
                        System.Diagnostics.Debug.Assert(false);  // 一周しないはず
                    }
                    iCurTri = iNexTri;
                    iCurNoTri = inotri3;
                }
            }

            // 失敗したときの値を入れる
            itri0 = 0;
            inotri0 = 0;
            inotri1 = 0;
            ratio = 0.0;

            return false;
        }

    }
}
