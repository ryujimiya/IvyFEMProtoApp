using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void PCWaveguideTriangleLatticeProblem1(MainWindow mainWindow)
        {
            double WaveguideWidth = 1.0;

            // 格子定数
            double latticeA = 0;
            // 周期構造距離
            double periodicDistance = 0;

            // フォトニック導波路
            // 考慮する波数ベクトルの最小値
            //double minWaveNum = 0.0;
            double minWaveNum = 0.5; // for latticeTheta = 60 r = 0.30a
            // 考慮する波数ベクトルの最大値
            //double maxWaveNum = 0.5;
            double maxWaveNum = 1.0; // for latticeTheta = 60 r = 0.30a

            // 空孔？
            //bool isAirHole = false; // dielectric rod
            bool isAirHole = true; // air hole
            // 周期を180°ずらす
            bool isShift180 = false; // for latticeTheta = 60 r = 0.30a air hole
            //bool isShift180 = true; // for latticeTheta = 45 r = 0.18a dielectric rod
            // ロッドの数(半分)
            //const int rodCntHalf = 5;
            const int rodCntHalf = 3;
            System.Diagnostics.Debug.Assert(rodCntHalf % 2 == 1); // 奇数を指定（60°ベンド図面の都合上)
            // 欠陥ロッド数
            const int defectRodCnt = 1;
            // 三角形格子の内角
            double latticeTheta = 60.0; // for latticeTheta = 60 r = 0.30a air hole
            // ロッドの半径
            double rodRadiusRatio = 0.30;
            // ロッドの比誘電率
            double rodEps = 2.76 * 2.76;
            // 1格子当たりの分割点の数
            //const int divCntForOneLattice = 9;
            const int divCntForOneLattice = 9;
            // ロッド円周の分割数
            const int rodCircleDiv = 12;
            // ロッドの半径の分割数
            const int rodRadiusDiv = 4;
            // 導波路不連続領域の長さ
            //const int rodCntDiscon = 1;
            const int rodCntDiscon = 1;

            // ロッドが1格子を超える？
            //bool isLargeRod = (rodRadiusRatio >= 0.25);
            bool isLargeRod = (rodRadiusRatio >= 0.5 * Math.Sin(latticeTheta * Math.PI / 180.0));
            // 格子の数
            int latticeCnt = rodCntHalf * 2 + defectRodCnt;
            // ロッド間の距離(Y方向)
            double rodDistanceY = WaveguideWidth / (double)latticeCnt;
            if (isLargeRod)
            {
                rodDistanceY = WaveguideWidth / (double)(latticeCnt - 1);
            }
            // 格子定数
            latticeA = rodDistanceY / Math.Sin(latticeTheta * Math.PI / 180.0);
            // ロッド間の距離(X方向)
            double rodDistanceX = rodDistanceY * 2.0 / Math.Tan(latticeTheta * Math.PI / 180.0);
            // 周期構造距離
            periodicDistance = rodDistanceX;
            // ロッドの半径
            double rodRadius = rodRadiusRatio * latticeA;
            // 導波路不連続領域の長さ
            double disconLength = rodDistanceX * rodCntDiscon;
            // 入出力導波路の周期構造部分の長さ
            double inputWgLength1 = rodDistanceX;
            double inputWgLength2 = rodDistanceX;
            // メッシュのサイズ
            double eLen = 1.05 * WaveguideWidth / (latticeCnt * divCntForOneLattice);

            // フォトニック結晶導波路の場合、a/λを規格化周波数とする
            double sFreq = 0.268;
            double eFreq = 0.282;
            int freqDiv = 20;

            // 最小屈折率
            double minEffN = 0.0;
            // 最大屈折率
            double maxEffN = 1.0;
            // air hole
            {
                minEffN = 0.0;
                maxEffN = Math.Sqrt(rodEps);
            }

            // μ0
            //double replacedMu0 = Constants.Mu0; // TEモード
            double replacedMu0 = Constants.Ep0; // TMモード

            // 媒質リスト作成
            double claddingP = 1.0;
            double claddingQ = 1.0;
            double coreP = 1.0;
            double coreQ = 1.0;
            if (isAirHole)
            {
                // 誘電体基盤 + 空孔(air hole)
                ///////////////////////////////////////
                // Note: IvyFEMではTEモードで実装されているが、係数を読み替えればTMモードにも適用できる
                ///////////////////////////////////////
                {
                    // TMモード
                    claddingP = 1.0 / rodEps;
                    claddingQ = 1.0;
                    coreP = 1.0 / 1.0;
                    coreQ = 1.0;
                }
                /*
                {
                    // TEモード
                    claddingP = 1.0;
                    claddingQ = rodEps;
                    coreP = 1.0;
                    coreQ = 1.0;
                }
                */
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }

            // 媒質リスト作成
            DielectricMaterial claddingMa = new DielectricMaterial
            {
                Epxx = claddingQ,
                Epyy = claddingQ,
                Epzz = claddingQ,
                Muxx = 1.0 / claddingP,
                Muyy = 1.0 / claddingP,
                Muzz = 1.0 / claddingP
            };
            DielectricMaterial coreMa = new DielectricMaterial
            {
                Epxx = coreQ,
                Epyy = coreQ,
                Epzz = coreQ,
                Muxx = 1.0 / coreP,
                Muyy = 1.0 / coreP,
                Muzz = 1.0 / coreP
            };

            // Cad
            CadObject2D cad2D = new CadObject2D();
            cad2D.IsSkipAssertValid = true; // AssertValidを無視する

            int portCnt = 2;
            // 導波路２の三角形格子角度
            double latticeThetaPort2 = latticeTheta;
            // 導波路２のロッドの数（半分）
            int rodCntHalfPort2 = rodCntHalf;
            // 導波路２の欠陥ロッド数
            int defectRodCntPort2 = defectRodCnt;
            // 導波路２の格子数
            int latticeCnt_port2 = (rodCntHalfPort2 * 2 + defectRodCntPort2);
            // 導波路２の幅
            double waveguideWidth2 = rodDistanceY * latticeCnt_port2;
            System.Diagnostics.Debug.Assert(
                Math.Abs(waveguideWidth2 - WaveguideWidth) < Constants.PrecisionLowerLimit);
            // 導波路２の１格子当たりの分割数
            int divCntForOneLatticePort2 = divCntForOneLattice;
            // 入出力不連続部の距離
            int rodCntDisconPort1 = rodCntDiscon;
            int rodCntDisconPort2 = rodCntDiscon;
            if (!isShift180)
            {
                rodCntDisconPort2 = rodCntDiscon + 1;
            }
            double disconLength1 = rodDistanceX * rodCntDisconPort1;
            double disconLength2 = rodDistanceX * rodCntDisconPort2;

            IList<uint> rodLoopIds = new List<uint>();
            IList<uint> inputWgRodLoopIds1 = new List<uint>();
            IList<uint> inputWgRodLoopIds2 = new List<uint>();
            int divCntPort1 = 0;
            int divCntPort2 = 0;
            IList<uint> rodEIdsB1 = new List<uint>();
            IList<uint> rodEIdsB2 = new List<uint>();
            IList<uint> rodEIdsB3 = new List<uint>();
            IList<uint> rodEIdsB4 = new List<uint>();
            IList<uint> eIdsF1 = new List<uint>();
            IList<uint> eIdsF2 = new List<uint>();
            IList<uint> eIdsF2Bend = new List<uint>();

            // ベンド下側角
            double bendX10 = inputWgLength1 + disconLength1 + WaveguideWidth / Math.Sqrt(3.0);
            double bendY10 = 0.0;
            bendX10 -= latticeA * Math.Sqrt(3.0) / 4.0;
            bendY10 -= rodDistanceY / 4.0;
            if (!isShift180)
            {
                bendX10 += rodDistanceX / 4.0;
                bendY10 -= rodDistanceY / 2.0;
            }
            double bendX1 = bendX10 + (0.0 - bendY10) / Math.Sqrt(3.0);
            double bendY1 = 0.0;
            // ベンド部と出力部の境界
            double bendX2 = bendX10 + waveguideWidth2 / (2.0 * Math.Sqrt(3.0));
            double bendY2 = bendY10 + waveguideWidth2 / 2.0;
            // 出力部内部境界の終点
            double port2X2B4 = bendX2 + disconLength2 / 2.0;
            double port2Y2B4 = bendY2 + disconLength2 * Math.Sqrt(3.0) / 2.0;
            // 出力部境界の終点
            double port2X2B3 = port2X2B4 + inputWgLength2 / 2.0;
            double port2Y2B3 = port2Y2B4 + inputWgLength2 * Math.Sqrt(3.0) / 2.0;
            // ベンド部上側角
            double bendX30 = inputWgLength1 + disconLength1;
            double bendY30 = WaveguideWidth;
            bendX30 -= latticeA * Math.Sqrt(3.0) / 4.0;
            bendY30 -= rodDistanceY / 4.0;
            if (!isShift180)
            {
                bendX30 += rodDistanceX / 4.0;
                bendY30 -= rodDistanceY / 2.0;
            }
            double bendX3 = bendX30 + (WaveguideWidth - bendY30) / Math.Sqrt(3.0);
            double bendY3 = WaveguideWidth;
            // 出力部内部境界の始点
            double port2X1B4 = bendX30 + disconLength2 / 2.0;
            double port2Y1B4 = bendY30 + disconLength2 * Math.Sqrt(3.0) / 2.0;
            // 出力部境界の始点
            double port2X1B3 = port2X1B4 + inputWgLength2 / 2.0;
            double port2Y1B3 = port2Y1B4 + inputWgLength2 * Math.Sqrt(3.0) / 2.0;

            // check
            {
                double th = 1.0e-12;
                double[] port2B3Pt1 = new double[] { port2X1B3, port2Y1B3 };
                double[] port2B3Pt2 = new double[] { port2X2B3, port2Y2B3 };
                double distanceB3 = CadUtils.GetDistance2D(port2B3Pt1, port2B3Pt2);
                System.Diagnostics.Debug.Assert(
                    Math.Abs(distanceB3 - waveguideWidth2) < th);
                double[] port2B4Pt1 = new double[] { port2X1B4, port2Y1B4 };
                double[] port2B4Pt2 = new double[] { port2X2B4, port2Y2B4 };
                double distanceB4 = CadUtils.GetDistance2D(port2B4Pt1, port2B4Pt2);
                System.Diagnostics.Debug.Assert(
                    Math.Abs(distanceB4 - waveguideWidth2) < th);
            }

            {
                //------------------------------------------------------------------
                // 図面作成
                //------------------------------------------------------------------
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    // 領域追加
                    pts.Add(new OpenTK.Vector2d(0.0, WaveguideWidth));
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(inputWgLength1, 0.0));
                    pts.Add(new OpenTK.Vector2d(bendX1, bendY1));
                    pts.Add(new OpenTK.Vector2d(port2X2B4, port2Y2B4));
                    pts.Add(new OpenTK.Vector2d(port2X2B3, port2Y2B3));
                    pts.Add(new OpenTK.Vector2d(port2X1B3, port2Y1B3));
                    pts.Add(new OpenTK.Vector2d(port2X1B4, port2Y1B4));
                    pts.Add(new OpenTK.Vector2d(bendX3, bendY3));
                    pts.Add(new OpenTK.Vector2d(inputWgLength1, WaveguideWidth));
                    uint lId1 = cad2D.AddPolygon(pts).AddLId;
                }
                // 入出力領域を分離
                uint eIdAdd1 = cad2D.ConnectVertexLine(3, 10).AddEId;
                uint eIdAdd2 = cad2D.ConnectVertexLine(5, 8).AddEId;

                // 入出力導波路の周期構造境界上の頂点を追加
                IList<double> port1Ys = new List<double>();
                IList<double> rodPort1Ys = new List<double>();
                IList<double> port2Xs = new List<double>();
                IList<double> rodPort2Xs = new List<double>();
                IList<uint> rodB1VIds = new List<uint>();
                IList<uint> rodB2VIds = new List<uint>();
                IList<uint> rod_B3VIds = new List<uint>();
                IList<uint> rod_B4VIds = new List<uint>();

                for (int portIndex = 0; portIndex < portCnt; portIndex++)
                {
                    int currodCntHalf = 0;
                    int curdefectRodCnt = 0;
                    int curdivCntForOneLattice = 0;
                    double curWaveguideWidth = 0.0;
                    double currodDistanceY = 0.0;
                    IList<double> ys = null;
                    IList<double> ys_rod = null;
                    if (portIndex == 0)
                    {
                        currodCntHalf = rodCntHalf;
                        curdefectRodCnt = defectRodCnt;
                        curdivCntForOneLattice = divCntForOneLattice;
                        curWaveguideWidth = WaveguideWidth;
                        currodDistanceY = rodDistanceY;
                        ys = port1Ys;
                        ys_rod = rodPort1Ys;
                        System.Diagnostics.Debug.Assert(ys.Count == 0);
                        System.Diagnostics.Debug.Assert(ys_rod.Count == 0);
                    }
                    else if (portIndex == 1)
                    {
                        currodCntHalf = rodCntHalfPort2;
                        curdefectRodCnt = defectRodCntPort2;
                        curdivCntForOneLattice = divCntForOneLatticePort2;
                        curWaveguideWidth = waveguideWidth2;
                        currodDistanceY = rodDistanceY;
                        ys = port2Xs;
                        ys_rod = rodPort2Xs;
                        System.Diagnostics.Debug.Assert(ys.Count == 0);
                        System.Diagnostics.Debug.Assert(ys_rod.Count == 0);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    // 境界上にロッドのある格子
                    // 境界上のロッドの頂点
                    for (int i = 0; i < currodCntHalf; i++)
                    {
                        if ((currodCntHalf - 1 - i) % 2 == (isShift180 ? 1 : 0)) continue;
                        double y0 = curWaveguideWidth - i * currodDistanceY - 0.5 * currodDistanceY;
                        ys_rod.Add(y0);
                        for (int k = 1; k <= rodRadiusDiv; k++)
                        {
                            ys_rod.Add(y0 - k * rodRadius / rodRadiusDiv);
                            ys_rod.Add(y0 + k * rodRadius / rodRadiusDiv);
                        }
                    }
                    for (int i = 0; i < currodCntHalf; i++)
                    {
                        if (i % 2 == (isShift180 ? 1 : 0)) continue;
                        double y0 = currodDistanceY * currodCntHalf - i * currodDistanceY - 0.5 * currodDistanceY;
                        ys_rod.Add(y0);
                        for (int k = 1; k <= rodRadiusDiv; k++)
                        {
                            ys_rod.Add(y0 - k * rodRadius / rodRadiusDiv);
                            ys_rod.Add(y0 + k * rodRadius / rodRadiusDiv);
                        }
                    }
                    foreach (double y_rod in ys_rod)
                    {
                        ys.Add(y_rod);
                    }

                    // 境界上のロッドの外の頂点はロッドから少し離さないとロッドの追加で失敗するのでマージンをとる
                    double radiusMargin = currodDistanceY * 0.01;
                    // 境界上にロッドのある格子
                    // ロッドの外
                    for (int i = 0; i < currodCntHalf; i++)
                    {
                        if ((currodCntHalf - 1 - i) % 2 == (isShift180 ? 1 : 0)) continue;
                        for (int k = 1; k <= (curdivCntForOneLattice - 1); k++)
                        {
                            double y_divpt = curWaveguideWidth - i * currodDistanceY - k * (currodDistanceY / curdivCntForOneLattice);
                            double y_min_rod = curWaveguideWidth - i * currodDistanceY - 0.5 * currodDistanceY - rodRadius - radiusMargin;
                            double y_max_rod = curWaveguideWidth - i * currodDistanceY - 0.5 * currodDistanceY + rodRadius + radiusMargin;
                            if (y_divpt < y_min_rod || y_divpt > y_max_rod)
                            {
                                ys.Add(y_divpt);
                            }
                        }
                    }
                    for (int i = 0; i < currodCntHalf; i++)
                    {
                        if (i % 2 == (isShift180 ? 1 : 0)) continue;
                        for (int k = 1; k <= (curdivCntForOneLattice - 1); k++)
                        {
                            double y_divpt = currodDistanceY * currodCntHalf - i * currodDistanceY - k * (currodDistanceY / curdivCntForOneLattice);
                            double y_min_rod = currodDistanceY * currodCntHalf - i * currodDistanceY - 0.5 * currodDistanceY - rodRadius - radiusMargin;
                            double y_max_rod = currodDistanceY * currodCntHalf - i * currodDistanceY - 0.5 * currodDistanceY + rodRadius + radiusMargin;
                            if (y_divpt < y_min_rod || y_divpt > y_max_rod)
                            {
                                ys.Add(y_divpt);
                            }
                        }
                    }

                    // 境界上にロッドのない格子
                    for (int i = 0; i < currodCntHalf; i++)
                    {
                        if ((currodCntHalf - 1 - i) % 2 == (isShift180 ? 0 : 1)) continue;
                        for (int k = 0; k <= curdivCntForOneLattice; k++)
                        {
                            if (i == 0 && k == 0) continue;
                            double y_divpt = curWaveguideWidth - i * currodDistanceY - k * (currodDistanceY / curdivCntForOneLattice);
                            ys.Add(y_divpt);
                        }
                    }
                    for (int i = 0; i < currodCntHalf; i++)
                    {
                        if (i % 2 == (isShift180 ? 0 : 1)) continue;
                        for (int k = 0; k <= curdivCntForOneLattice; k++)
                        {
                            if (i == (currodCntHalf - 1) && k == curdivCntForOneLattice) continue;
                            double y_divpt = currodDistanceY * currodCntHalf - i * currodDistanceY - k * (currodDistanceY / curdivCntForOneLattice);
                            ys.Add(y_divpt);
                        }
                    }
                    // 欠陥部
                    for (int i = 0; i <= (curdefectRodCnt * curdivCntForOneLattice); i++)
                    {
                        if (!isShift180 && (i == 0 || i == (curdefectRodCnt * curdivCntForOneLattice))) continue;
                        double y_divpt = currodDistanceY * (currodCntHalf + curdefectRodCnt) - i * (currodDistanceY / curdivCntForOneLattice);
                        ys.Add(y_divpt);
                    }

                    // 昇順でソート
                    double[] yAry = ys.ToArray();
                    Array.Sort(yAry);
                    int curdivCnt = 0;
                    curdivCnt = yAry.Length + 1;
                    if (portIndex == 0)
                    {
                        divCntPort1 = curdivCnt;
                    }
                    else if (portIndex == 1)
                    {
                        divCntPort2 = curdivCnt;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    // yAryは昇順なので、yAryの並びの順に追加すると境界1上を逆方向に移動することになる
                    //  逆から追加しているのは、頂点によって新たに生成される辺に頂点を追加しないようにするため
                    // 入力導波路 外側境界
                    // 入力導波路 内部側境界
                    // 出力導波路 外側境界
                    // 出力導波路 内部側境界
                    for (int boundaryIndex = 0; boundaryIndex < 2; boundaryIndex++)
                    {
                        bool isInRod = false;
                        for (int i = 0; i < yAry.Length; i++)
                        {
                            uint eId = 0;
                            double ptX = 0.0;
                            double ptY = 0.0;

                            IList<uint> workrodBEIds = null;
                            IList<uint> workrodBVIds = null;
                            int yAryIndex = 0;
                            if (portIndex == 0 && boundaryIndex == 0)
                            {
                                // 入力導波路 外側境界
                                eId = 1;
                                ptX = 0.0;
                                ptY = yAry[i];
                                yAryIndex = i;
                                workrodBEIds = rodEIdsB1;
                                workrodBVIds = rodB1VIds;
                            }
                            else if (portIndex == 0 && boundaryIndex == 1)
                            {
                                // 入力導波路 内側境界
                                eId = 11;
                                ptX = inputWgLength1;
                                ptY = yAry[yAry.Length - 1 - i];
                                yAryIndex = yAry.Length - 1 - i;
                                workrodBEIds = rodEIdsB2;
                                workrodBVIds = rodB2VIds;
                            }
                            else if (portIndex == 1 && boundaryIndex == 0)
                            {
                                // 出力導波路 外側境界
                                eId = 6;
                                ptX = port2X1B3 + yAry[i] * Math.Sqrt(3.0) / 2.0;
                                ptY = port2Y1B3 - yAry[i] / 2.0;
                                yAryIndex = i;
                                workrodBEIds = rodEIdsB3;
                                workrodBVIds = rod_B3VIds;
                            }
                            else if (portIndex == 1 && boundaryIndex == 1)
                            {
                                // 出力導波路 内側境界
                                eId = 12;
                                ptX = port2X1B4 + yAry[i] * Math.Sqrt(3.0) / 2.0;
                                ptY = port2Y1B4 - yAry[i] / 2.0;
                                yAryIndex = i;
                                workrodBEIds = rodEIdsB4;
                                workrodBVIds = rod_B4VIds;
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }

                            var resAddVertex = cad2D.AddVertex(
                                CadElementType.Edge, eId, new OpenTK.Vector2d(ptX, ptY));
                            uint addVId = resAddVertex.AddVId;
                            uint addEId = resAddVertex.AddEId;
                            System.Diagnostics.Debug.Assert(addVId != 0);
                            System.Diagnostics.Debug.Assert(addEId != 0);
                            if (isInRod)
                            {
                                workrodBEIds.Add(addEId);
                            }
                            bool contains = false;
                            foreach (double y_rod in ys_rod)
                            {
                                if (Math.Abs(y_rod - yAry[yAryIndex]) < Constants.PrecisionLowerLimit)
                                {
                                    contains = true;
                                    break;
                                }
                            }
                            if (contains)
                            {
                                workrodBVIds.Add(addVId);
                                if (workrodBVIds.Count % (rodRadiusDiv * 2 + 1) == 1)
                                {
                                    isInRod = true;
                                }
                                else if (workrodBVIds.Count % (rodRadiusDiv * 2 + 1) == 0)
                                {
                                    isInRod = false;
                                }
                            }
                        }
                    }
                }
                int bRodCntHalfPort1 = (isShift180 ? (int)((rodCntHalf + 1) / 2) : (int)((rodCntHalf) / 2));
                System.Diagnostics.Debug.Assert(rodB1VIds.Count == bRodCntHalfPort1 * 2 * (rodRadiusDiv * 2 + 1));
                System.Diagnostics.Debug.Assert(rodB2VIds.Count == bRodCntHalfPort1 * 2 * (rodRadiusDiv * 2 + 1));

                int bRodCntHalfPort2 = (isShift180 ? (int)((rodCntHalfPort2 + 1) / 2) : (int)((rodCntHalfPort2) / 2));
                System.Diagnostics.Debug.Assert(rod_B3VIds.Count == bRodCntHalfPort2 * 2 * (rodRadiusDiv * 2 + 1));
                System.Diagnostics.Debug.Assert(rod_B4VIds.Count == bRodCntHalfPort2 * 2 * (rodRadiusDiv * 2 + 1));

                // ロッドを追加
                const int innerAreaRodRadiusDiv = rodRadiusDiv;
                //const int innerAreaRodRadiusDiv = 3; // ロッド分割数を内部領域だけ緩和する
                /////////////////////////////////////////////////////////////
                // 入力導波路側ロッド
                // 左のロッドを追加
                for (int colIndex = 0; colIndex < 2; colIndex++) // このcolIndexは特に図面上のカラムを指すわけではない（ループ変数)
                {
                    // 左のロッド
                    IList<uint> workrodBVIds = null;
                    double xB = 0;
                    uint baseLoopId = 0;
                    int inputWgNo = 0;

                    // 始点、終点が逆？
                    bool isReverse = false;
                    if (colIndex == 0)
                    {
                        // 入力境界 外側
                        xB = 0.0;
                        workrodBVIds = rodB1VIds;
                        // 入力導波路領域
                        baseLoopId = 1;
                        inputWgNo = 1;
                        isReverse = false;
                    }
                    else if (colIndex == 1)
                    {
                        // 入力境界 内側
                        xB = inputWgLength1;
                        workrodBVIds = rodB2VIds;
                        // 不連続領域
                        baseLoopId = 2;
                        inputWgNo = 0;
                        isReverse = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    for (int i = 0; i < rodCntHalf; i++)
                    {
                        if ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = bRodCntHalfPort1 - 1 - (int)((rodCntHalf - 1 - i) / 2);
                            // 左のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                if (workrodBVIds == rodB1VIds)
                                {
                                    vId0 = workrodBVIds[workrodBVIds.Count - (rodRadiusDiv * 2 + 1) - i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrodBVIds[workrodBVIds.Count - (rodRadiusDiv + 1) - i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrodBVIds[workrodBVIds.Count - 1 - i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                else
                                {
                                    vId0 = workrodBVIds[0 + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrodBVIds[(rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrodBVIds[(rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                double x0 = xB;
                                double y0 = WaveguideWidth - i * rodDistanceY - rodDistanceY * 0.5;
                                uint workVId0 = vId0;
                                uint workVId2 = vId2;
                                if (isReverse)
                                {
                                    workVId0 = vId2;
                                    workVId2 = vId0;
                                }
                                uint lId = PCWaveguideUtils.AddLeftRod(
                                    cad2D,
                                    baseLoopId,
                                    workVId0,
                                    vId1,
                                    workVId2,
                                    x0,
                                    y0,
                                    rodRadius,
                                    rodCircleDiv,
                                    rodRadiusDiv);
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                    for (int i = 0; i < rodCntHalf; i++)
                    {
                        if (i % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = i / 2;
                            // 左のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                if (workrodBVIds == rodB1VIds)
                                {
                                    vId0 = workrodBVIds[
                                        workrodBVIds.Count / 2 - (rodRadiusDiv * 2 + 1) -
                                        i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrodBVIds[
                                        workrodBVIds.Count / 2 - (rodRadiusDiv + 1) - i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrodBVIds[
                                        workrodBVIds.Count / 2 - 1 - i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                else
                                {
                                    vId0 = workrodBVIds[
                                        workrodBVIds.Count / 2 + 0 + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrodBVIds[
                                        workrodBVIds.Count / 2 + (rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrodBVIds[
                                        workrodBVIds.Count / 2 + (rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                double x0 = xB;
                                double y0 = rodDistanceY * rodCntHalf - i * rodDistanceY - rodDistanceY * 0.5;
                                uint workVId0 = vId0;
                                uint workVId2 = vId2;
                                if (isReverse)
                                {
                                    workVId0 = vId2;
                                    workVId2 = vId0;
                                }
                                uint lId = PCWaveguideUtils.AddLeftRod(
                                    cad2D,
                                    baseLoopId,
                                    workVId0,
                                    vId1,
                                    workVId2,
                                    x0,
                                    y0,
                                    rodRadius,
                                    rodCircleDiv,
                                    rodRadiusDiv);
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                }
                // 右のロッドを追加
                for (int colIndex = 0; colIndex < 1; colIndex++) // このcolIndexは特に図面上のカラムを指すわけではない（ループ変数)
                {
                    // 右のロッド
                    IList<uint> workrodBVIds = null;
                    double xB = 0;
                    uint baseLoopId = 0;
                    int inputWgNo = 0;

                    if (colIndex == 0)
                    {
                        // 入力境界 内側
                        xB = inputWgLength1;
                        workrodBVIds = rodB2VIds;
                        // 入力導波路領域
                        baseLoopId = 1;
                        inputWgNo = 1;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    for (int i = 0; i < rodCntHalf; i++)
                    {
                        if ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = bRodCntHalfPort1 - 1 - (int)((rodCntHalf - 1 - i) / 2);
                            // 右のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                if (workrodBVIds == rodB1VIds)
                                {
                                    vId0 = workrodBVIds[
                                        workrodBVIds.Count - (rodRadiusDiv * 2 + 1) - i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrodBVIds[
                                        workrodBVIds.Count - (rodRadiusDiv + 1) - i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrodBVIds[
                                        workrodBVIds.Count - 1 - i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                else
                                {
                                    vId0 = workrodBVIds[0 + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrodBVIds[(rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrodBVIds[(rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                double x0 = xB;
                                double y0 = WaveguideWidth - i * rodDistanceY - rodDistanceY * 0.5;
                                OpenTK.Vector2d centerPt = cad2D.GetVertexCoord(vId1);
                                uint lId = PCWaveguideUtils.AddRightRod(
                                    cad2D,
                                    baseLoopId,
                                    vId0,
                                    vId1,
                                    vId2,
                                    x0,
                                    y0,
                                    rodRadius,
                                    rodCircleDiv,
                                    rodRadiusDiv);
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                    for (int i = 0; i < rodCntHalf; i++)
                    {
                        if (i % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = i / 2;
                            // 右のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                if (workrodBVIds == rodB1VIds)
                                {
                                    vId0 = workrodBVIds[
                                        workrodBVIds.Count / 2 - (rodRadiusDiv * 2 + 1) -
                                        i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrodBVIds[
                                        workrodBVIds.Count / 2 - (rodRadiusDiv + 1) - i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrodBVIds[
                                        workrodBVIds.Count / 2 - 1 - i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                else
                                {
                                    vId0 = workrodBVIds[
                                        workrodBVIds.Count / 2 + 0 + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrodBVIds[
                                        workrodBVIds.Count / 2 + (rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrodBVIds[
                                        workrodBVIds.Count / 2 + (rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                double x0 = xB;
                                double y0 = rodDistanceY * rodCntHalf - i * rodDistanceY - rodDistanceY * 0.5;
                                uint lId = PCWaveguideUtils.AddRightRod(
                                    cad2D,
                                    baseLoopId,
                                    vId0,
                                    vId1,
                                    vId2,
                                    x0,
                                    y0,
                                    rodRadius,
                                    rodCircleDiv,
                                    rodRadiusDiv);
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                }

                // 中央のロッド (入力導波路 + 不連続部)
                int periodCntInputWg1 = 1;
                int periodCntBendX = (rodCntHalf * 2 + defectRodCnt) / 2;
                int periodCntX = periodCntInputWg1 + rodCntDisconPort1 + periodCntBendX * 2;
                for (int col = 1; col <= (periodCntX * 2); col++)
                {
                    if (col == (periodCntInputWg1 * 2)) continue; // 入力導波路内部境界 (既にロッド追加済み)
                    uint baseLoopId = 0;
                    int inputWgNo = 0;
                    if (col >= 0 && col < (periodCntInputWg1 * 2))
                    {
                        baseLoopId = 1;
                        inputWgNo = 1;
                    }
                    else
                    {
                        baseLoopId = 2;
                        inputWgNo = 0;
                    }

                    // 中央のロッド
                    for (int i = 0; i < (rodCntHalf * 2 + defectRodCnt); i++)
                    {
                        // 出力ポートとベンド部の境界判定
                        if (col >= ((periodCntInputWg1 + rodCntDisconPort1) * 2 + 1))
                        {
                            int rowMin = (col - (periodCntInputWg1 + rodCntDisconPort1) * 2) / 3;
                            if (i < rowMin)
                            {
                                continue;
                            }
                        }
                        // 下部の境界判定
                        if (col >= ((periodCntInputWg1 + rodCntDisconPort1 + periodCntBendX) * 2 + 1))
                        {
                            int rowMax = (rodCntHalf * 2 + defectRodCnt) - 1 -
                                (col - (periodCntInputWg1 + rodCntDisconPort1 + periodCntBendX) * 2);
                            if (isShift180)
                            {
                                if (i > rowMax)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (i > (rowMax + 1))
                                {
                                    continue;
                                }
                            }
                        }
                        // ロッドの半径
                        double rr = rodRadius;
                        // ロッドの半径方向分割数
                        int nr = rodRadiusDiv;
                        // ロッドの周方向分割数
                        int nc = rodCircleDiv;
                        // ずらす距離
                        double rodOfsX = 0.0;
                        double rodOfsY = 0.0;
                        if (inputWgNo == 0)
                        {
                            nr = innerAreaRodRadiusDiv;
                        }

                        // 初期構造
                        // 欠陥（入力部）
                        if ((col <= ((periodCntInputWg1 + rodCntDisconPort1) * 2 - 1))
                            && (i >= rodCntHalf && i <= (rodCntHalf + defectRodCnt - 1)))
                        {
                            continue;
                        }
                        // 欠陥（ベンド部入力側）
                        int colBendPort1 = (col - (periodCntInputWg1 + rodCntDisconPort1) * 2);
                        int colBendCenterLine = (periodCntInputWg1 + rodCntDisconPort1) * 2 + periodCntBendX;
                        if (!isShift180)
                        {
                            colBendCenterLine += 1;
                        }
                        if ((col >= ((periodCntInputWg1 + rodCntDisconPort1) * 2))
                            && col <= (colBendCenterLine - 1)
                            && (i >= rodCntHalf && i <= (rodCntHalf + defectRodCnt - 1)))
                        {
                            continue;
                        }
                        // 欠陥（ベンド部出力側）
                        if ((col >= (colBendCenterLine))
                            )
                        {
                            int colBendPort2 = col - colBendCenterLine;
                            if (i >= (rodCntHalf - colBendPort2) && i <= (rodCntHalf + defectRodCnt - 1 - colBendPort2))
                            {
                                continue;
                            }
                        }

                        if ((col % 2 == 1 && ((rodCntHalf * 2 + defectRodCnt - 1 - i) % 2 == (isShift180 ? 1 : 0)))
                            || (col % 2 == 0 && ((rodCntHalf * 2 + defectRodCnt - 1 - i) % 2 == (isShift180 ? 0 : 1))))
                        {
                            // 中央ロッド
                            double x0 = rodDistanceX * 0.5 * col;
                            double y0 = WaveguideWidth - i * rodDistanceY - rodDistanceY * 0.5;
                            x0 += rodOfsX;
                            y0 += rodOfsY;
                            uint lId = PCWaveguideUtils.AddRod(cad2D, baseLoopId, x0, y0, rr, nc, nr);
                            rodLoopIds.Add(lId);
                            if (inputWgNo == 1)
                            {
                                inputWgRodLoopIds1.Add(lId);
                            }
                            else if (inputWgNo == 2)
                            {
                                inputWgRodLoopIds2.Add(lId);
                            }
                        }
                    }
                }

                /////////////////////////////////////////////////////////////
                // 出力導波路側ロッド
                // 上のロッドを追加
                for (int colIndex = 0; colIndex < 2; colIndex++) // このcolIndexは特に図面上のカラムを指すわけではない（ループ変数)
                {
                    // 上のロッド
                    IList<uint> workrodBVIds = null;
                    double x1B = 0;
                    double y1B = 0;
                    uint baseLoopId = 0;
                    int inputWgNo = 0;

                    if (colIndex == 0)
                    {
                        // 出力境界 外側
                        x1B = port2X1B3;
                        y1B = port2Y1B3;
                        workrodBVIds = rod_B3VIds;
                        // 出力導波路領域
                        baseLoopId = 3;
                        inputWgNo = 2;
                    }
                    else if (colIndex == 1)
                    {
                        // 出力境界内側
                        x1B = port2X1B4;
                        y1B = port2Y1B4;
                        workrodBVIds = rod_B4VIds;
                        // 不連続領域
                        baseLoopId = 2;
                        inputWgNo = 0;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    for (int i = 0; i < rodCntHalfPort2; i++)
                    {
                        if ((rodCntHalfPort2 - 1 - i) % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = bRodCntHalfPort2 - 1 - (int)((rodCntHalfPort2 - 1 - i) / 2);
                            // 上のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                {
                                    vId0 = workrodBVIds[0 + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrodBVIds[(rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrodBVIds[(rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                double yProj = i * rodDistanceY + rodDistanceY * 0.5;
                                double x0 = x1B + yProj * Math.Sqrt(3.0) / 2.0;
                                double y0 = y1B - yProj / 2.0;
                                OpenTK.Vector2d centerPt = cad2D.GetVertexCoord(vId1);
                                uint lId = PCWaveguideUtils.AddExactlyHalfRod(
                                    cad2D,
                                    baseLoopId,
                                    vId0,
                                    vId1,
                                    vId2,
                                    x0,
                                    y0,
                                    rodRadius,
                                    rodCircleDiv,
                                    rodRadiusDiv,
                                    (0.0 - 30.0),
                                    true);
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                    for (int i = 0; i < rodCntHalfPort2; i++)
                    {
                        if (i % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = i / 2;
                            // 上のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                {
                                    vId0 = workrodBVIds[workrodBVIds.Count / 2 + 0 + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrodBVIds[workrodBVIds.Count / 2 + (rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrodBVIds[workrodBVIds.Count / 2 + (rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                double yProj = waveguideWidth2 - rodDistanceY * rodCntHalfPort2 + i * rodDistanceY + rodDistanceY * 0.5;
                                double x0 = x1B + yProj * Math.Sqrt(3.0) / 2.0;
                                double y0 = y1B - yProj / 2.0;
                                uint lId = PCWaveguideUtils.AddExactlyHalfRod(
                                    cad2D,
                                    baseLoopId,
                                    vId0,
                                    vId1,
                                    vId2,
                                    x0,
                                    y0,
                                    rodRadius,
                                    rodCircleDiv,
                                    rodRadiusDiv,
                                    (0.0 - 30.0),
                                    true);
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                }

                // 下のロッドを追加
                for (int colIndex = 0; colIndex < 1; colIndex++) // このcolIndexは特に図面上のカラムを指すわけではない（ループ変数)
                {
                    // 下のロッド
                    IList<uint> workrod_BVIds = null;
                    double x1B = 0;
                    double y1B = 0;
                    uint baseLoopId = 0;
                    int inputWgNo = 0;

                    if (colIndex == 0)
                    {
                        // 出力境界 内側
                        x1B = port2X1B4;
                        y1B = port2Y1B4;
                        workrod_BVIds = rod_B4VIds;
                        // 出力導波路領域
                        baseLoopId = 3;
                        inputWgNo = 2;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    for (int i = 0; i < rodCntHalfPort2; i++)
                    {
                        if ((rodCntHalfPort2 - 1 - i) % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = bRodCntHalfPort2 - 1 - (int)((rodCntHalfPort2 - 1 - i) / 2);
                            // 下のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                {
                                    vId0 = workrod_BVIds[(rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrod_BVIds[(rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrod_BVIds[0 + i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                double yProj = i * rodDistanceY + rodDistanceY * 0.5;
                                double x0 = x1B + yProj * Math.Sqrt(3.0) / 2.0;
                                double y0 = y1B - yProj / 2.0;
                                OpenTK.Vector2d centerPt = cad2D.GetVertexCoord(vId1);
                                uint lId = PCWaveguideUtils.AddExactlyHalfRod(
                                    cad2D,
                                    baseLoopId,
                                    vId0,
                                    vId1,
                                    vId2,
                                    x0,
                                    y0,
                                    rodRadius,
                                    rodCircleDiv,
                                    rodRadiusDiv,
                                    (180.0 - 30.0),
                                    true);
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                    for (int i = 0; i < rodCntHalfPort2; i++)
                    {
                        if (i % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = i / 2;
                            // 下のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                {
                                    vId0 = workrod_BVIds[workrod_BVIds.Count / 2 + (rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId1 = workrod_BVIds[workrod_BVIds.Count / 2 + (rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1)];
                                    vId2 = workrod_BVIds[workrod_BVIds.Count / 2 + 0 + i2 * (rodRadiusDiv * 2 + 1)];
                                }
                                double y_proj = waveguideWidth2 - rodDistanceY * rodCntHalfPort2 +
                                    i * rodDistanceY + rodDistanceY * 0.5;
                                double x0 = x1B + y_proj * Math.Sqrt(3.0) / 2.0;
                                double y0 = y1B - y_proj / 2.0;
                                uint lId = PCWaveguideUtils.AddExactlyHalfRod(
                                    cad2D,
                                    baseLoopId,
                                    vId0,
                                    vId1,
                                    vId2,
                                    x0,
                                    y0,
                                    rodRadius,
                                    rodCircleDiv,
                                    rodRadiusDiv,
                                    (180.0 - 30.0),
                                    true);
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////
                // 中央のロッド (出力導波路)
                int periodCntInputWg2 = 1;
                int periodCntY = periodCntInputWg2 + rodCntDisconPort2;

                // 中央のロッド(出力導波路)(左右の強制境界と交差する円)と境界の交点
                IList<uint> vIdsF1 = new List<uint>();
                IList<uint> vIdsF2 = new List<uint>();

                // 中央のロッド (出力導波路)
                for (int col = 1; col <= (periodCntY * 2 - 2); col++)
                {
                    if (col == (periodCntInputWg2 * 2)) continue; // 入力導波路内部境界 (既にロッド追加済み)

                    uint baseLoopId = 0;
                    int inputWgNo = 0;
                    if (col >= 0 && col < (periodCntInputWg2 * 2))
                    {
                        baseLoopId = 3;
                        inputWgNo = 2;
                    }
                    else
                    {
                        baseLoopId = 2;
                        inputWgNo = 0;
                    }
                    double x1B = 0.0;
                    double y1B = 0.0;
                    x1B = port2X1B3 - col * ((rodDistanceX * 0.5) / 2.0);
                    y1B = port2Y1B3 - col * ((rodDistanceX * 0.5) * Math.Sqrt(3.0) / 2.0);

                    // 中央のロッド(出力導波路)
                    for (int i = 0; i < rodCntHalfPort2; i++)
                    {
                        int nr = rodRadiusDiv;
                        if (inputWgNo == 0)
                        {
                            nr = innerAreaRodRadiusDiv;
                        }
                        if ((col % 2 == 1 && ((rodCntHalfPort2 - 1 - i) % 2 == (isShift180 ? 1 : 0)))
                            || (col % 2 == 0 && ((rodCntHalfPort2 - 1 - i) % 2 == (isShift180 ? 0 : 1))))
                        {
                            // 中央ロッド
                            double yProj = i * rodDistanceY + rodDistanceY * 0.5;
                            double x0 = x1B + yProj * Math.Sqrt(3.0) / 2.0;
                            double y0 = y1B - yProj / 2.0;
                            uint lId = PCWaveguideUtils.AddRod(
                                cad2D, baseLoopId, x0, y0, rodRadius, rodCircleDiv, nr);
                            rodLoopIds.Add(lId);
                            if (inputWgNo == 1)
                            {
                                inputWgRodLoopIds1.Add(lId);
                            }
                            else if (inputWgNo == 2)
                            {
                                inputWgRodLoopIds2.Add(lId);
                            }
                        }
                    }
                    for (int i = 0; i < rodCntHalfPort2; i++)
                    {
                        int nr = rodRadiusDiv;
                        if (inputWgNo == 0)
                        {
                            nr = innerAreaRodRadiusDiv;
                        }
                        if ((col % 2 == 1 && (i % 2 == (isShift180 ? 1 : 0)))
                            || (col % 2 == 0 && (i % 2 == (isShift180 ? 0 : 1))))
                        {
                            // 中央ロッド
                            double yProj = waveguideWidth2 - rodDistanceY * rodCntHalfPort2 +
                                i * rodDistanceY + rodDistanceY * 0.5;
                            double x0 = x1B + yProj * Math.Sqrt(3.0) / 2.0;
                            double y0 = y1B - yProj / 2.0;
                            uint lId = PCWaveguideUtils.AddRod(
                                cad2D, baseLoopId, x0, y0, rodRadius, rodCircleDiv, nr);
                            rodLoopIds.Add(lId);
                            if (inputWgNo == 1)
                            {
                                inputWgRodLoopIds1.Add(lId);
                            }
                            else if (inputWgNo == 2)
                            {
                                inputWgRodLoopIds2.Add(lId);
                            }
                        }
                    }
                }
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
            WPFUtils.DoEvents();

            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            IDrawer meshDrawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 1; // 複素数
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint claddingMaId = uint.MaxValue;
            uint coreMaId = uint.MaxValue;
            {
                world.ClearMaterial();

                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);

                uint[] lIds = new uint[3 + rodLoopIds.Count];
                lIds[0] = 1;
                lIds[1] = 2;
                lIds[2] = 3;
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 3] = rodLoopIds[i];
                }
                uint[] maIds = new uint[3 + rodLoopIds.Count];
                for (int i = 0; i < 3; i++)
                {
                    maIds[i] = claddingMaId;
                }
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    maIds[i + 3] = coreMaId;
                }

                for (int i = 0; i < lIds.Length; i++)
                {
                    uint lId = lIds[i];
                    uint maId = maIds[i];
                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            IList<PCWaveguidePortInfo> wgPortInfos = new List<PCWaveguidePortInfo>();
            // ２ポート情報リスト作成
            //const uint portCnt = 2;
            bool[] isPortBc2Reverse = { true, false };
            for (int portId = 0; portId < portCnt; portId++)
            {
                var wgPortInfo = new PCWaveguidePortInfo();
                wgPortInfos.Add(wgPortInfo);
                System.Diagnostics.Debug.Assert(wgPortInfos.Count == (portId + 1));
                //wgPortInfo.IsSVEA = true; // 緩慢変化包絡線近似
                wgPortInfo.IsSVEA = false; // Φを直接解く
                wgPortInfo.IsPortBc2Reverse = isPortBc2Reverse[portId];
                wgPortInfo.LatticeA = latticeA;
                wgPortInfo.PeriodicDistance = periodicDistance;
                wgPortInfo.MinEffN = minEffN;
                wgPortInfo.MaxEffN = maxEffN;
                wgPortInfo.MinWaveNum = minWaveNum;
                wgPortInfo.MaxWaveNum = maxWaveNum;
                wgPortInfo.ReplacedMu0 = replacedMu0;
            }

            // 開口条件
            // 周期構造境界1
            for (int portIndex = 0; portIndex < portCnt; portIndex++)
            {
                int divCnt = 0;
                if (portIndex == 0)
                {
                    divCnt = divCntPort1;
                }
                else if (portIndex == 1)
                {
                    divCnt = divCntPort2;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                IList<uint> workrodEIdsB = null;

                if (portIndex == 0)
                {
                    eIds[0] = 1;
                    workrodEIdsB = rodEIdsB1;
                }
                else if (portIndex == 1)
                {
                    eIds[0] = 6;
                    workrodEIdsB = rodEIdsB3;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                if (workrodEIdsB.Contains(eIds[0]))
                {
                    maIds[0] = coreMaId;
                }
                else
                {
                    maIds[0] = claddingMaId;
                }

                for (int i = 1; i <= divCnt - 1; i++)
                {
                    if (portIndex == 0)
                    {
                        eIds[i] = (uint)(12 + (divCntPort1 - 1) - (i - 1));
                    }
                    else if (portIndex == 1)
                    {
                        eIds[i] = (uint)(12 + (divCntPort1 - 1) * 2 + (divCntPort2 - 1) - (i - 1));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    if (workrodEIdsB.Contains(eIds[i]))
                    {
                        maIds[i] = coreMaId;
                    }
                    else
                    {
                        maIds[i] = claddingMaId;
                    }
                }

                for (int i = 0; i < eIds.Length; i++)
                {
                    uint eId = eIds[i];
                    uint maId = maIds[i];
                    world.SetCadEdgeMaterial(eId, maId);
                }

                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portIndex];
                wgPortInfo.BcEdgeIds1 = new List<uint>(eIds);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // 周期構造入出力導波路
            for (int portId = 0; portId < portCnt; portId++)
            {
                // ワールド座標系のループIDを取得
                uint[] lIds = null;
                if (portId == 0)
                {
                    
                    lIds = new uint[1 + inputWgRodLoopIds1.Count];
                    lIds[0] = 1;
                    for (int i = 0; i < inputWgRodLoopIds1.Count; i++)
                    {
                        lIds[i + 1] = inputWgRodLoopIds1[i];
                    }
                }
                else if (portId == 1)
                {
                    lIds = new uint[1 + inputWgRodLoopIds2.Count];
                    lIds[0] = 3;
                    for (int i = 0; i < inputWgRodLoopIds2.Count; i++)
                    {
                        lIds[i + 1] = inputWgRodLoopIds2[i];
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.LoopIds = new List<uint>(lIds);
            }
            // 周期構造境界2
            for (int portId = 0; portId < portCnt; portId++)
            {
                int divCnt = 0;
                if (portId == 0)
                {
                    divCnt = divCntPort1;
                }
                else if (portId == 1)
                {
                    divCnt = divCntPort2;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                IList<uint> workrodEIdsB = null;

                if (portId == 0)
                {
                    eIds[0] = 11;
                    workrodEIdsB = rodEIdsB2;
                }
                else if (portId == 1)
                {
                    eIds[0] = 12;
                    workrodEIdsB = rodEIdsB4;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                if (workrodEIdsB.Contains(eIds[0]))
                {
                    maIds[0] = coreMaId;
                }
                else
                {
                   maIds[0] = claddingMaId;
                }

                for (int i = 1; i <= divCnt - 1; i++)
                {
                    if (portId == 0)
                    {
                        eIds[i] = (uint)(12 + (divCntPort1 - 1) * 2 - (i - 1));
                    }
                    else if (portId == 1)
                    {
                        eIds[i] = (uint)(12 + (divCntPort1 - 1) * 2 + (divCntPort2 - 1) * 2 - (i - 1));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    if (workrodEIdsB.Contains(eIds[i]))
                    {
                        maIds[i] = coreMaId;
                    }
                    else
                    {
                        maIds[i] = claddingMaId;
                    }
                }

                for (int i = 0; i < eIds.Length; i++)
                {
                    uint eId = eIds[i];
                    uint maId = maIds[i];
                    world.SetCadEdgeMaterial(eId, maId);
                }

                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.BcEdgeIds2 = new List<uint>(eIds);
            }

            // ポート条件
            IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
            {
                portConditions.Clear();
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                for (int portId = 0; portId < portCnt; portId++)
                {
                    PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                    IList<uint> lIds = wgPortInfo.LoopIds;
                    IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                    IList<uint> bcEIds2 = wgPortInfo.BcEdgeIds2;
                    PortCondition portCondition = new PortCondition(
                        lIds, bcEIds1, bcEIds2, FieldValueType.ZScalar, new List<uint> { 0 }, 0);
                    portConditions.Add(portCondition);
                }
            }

            world.MakeElements();

            // ポートの境界上の節点を準備する
            for (int portId = 0; portId < portCnt; portId++)
            {
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.SetupAfterMakeElements(world, quantityId, (uint)portId);
            }
            // フォトニック結晶導波路チャンネル上節点を取得する
            for (int portId = 0; portId < portCnt; portId++)
            {
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.PCChannelCoIds = new List<IList<int>>();

                // 回転移動
                double rotAngle;
                double[] rotOrigin;
                PCWaveguideUtils.GetRotOriginRotAngleFromY(
                    world, wgPortInfo.BcEdgeIds1.ToArray(), out rotAngle, out rotOrigin);
                world.RotAngle = rotAngle;
                world.RotOrigin = rotOrigin;

                // 全座標IDの取得
                IList<int> coIds = new List<int>();
                IList<uint> lIds = wgPortInfo.LoopIds;
                foreach (uint lId in lIds)
                {
                    IList<int> tmpCoIds = world.GetCoordIdsFromCadId(quantityId, lId, CadElementType.Loop);
                    foreach (int coId in tmpCoIds)
                    {
                        if (coIds.IndexOf(coId) == -1)
                        {
                            coIds.Add(coId);
                        }
                    }
                }
                // チャンネル
                {
                    IList<int> channelCoIds = new List<int>();
                    wgPortInfo.PCChannelCoIds.Add(channelCoIds);

                    int nodeId0 = wgPortInfo.BcNodess[0][0];
                    int coId0 = world.PortNode2Coord(quantityId, (uint)portId, nodeId0);
                    double[] coord0 = world.GetCoord(quantityId, coId0);
                    // 以下の処理はX方向周期構造の場合
                    System.Diagnostics.Debug.Assert(!wgPortInfo.IsYDirectionPeriodic);
                    foreach (int coId in coIds)
                    {
                        // 座標からチャンネル(欠陥部)を判定する
                        double[] coord = world.GetCoord(quantityId, coId);
                        // X方向周期構造
                        double y = Math.Abs(coord[1] - coord0[1]); // Y座標
                        //if (y >= (WaveguideWidth - rodDistanceY * (rodCntHalf + defectRodCnt) -
                        //    (0.5 * rodDistanceY - rodRadius)) && y <=
                        //    (WaveguideWidth - rodDistanceY * rodCntHalf +
                        //    (0.5 * rodDistanceY - rodRadius))) // dielectric rod
                        if (y >= (WaveguideWidth - rodDistanceY * (rodCntHalf + defectRodCnt) -
                            1.0 * rodDistanceY) &&
                            y <= (WaveguideWidth - rodDistanceY * rodCntHalf +
                            1.0 * rodDistanceY)) // air hole
                        {
                            channelCoIds.Add(coId);
                        }
                    }
                }

                // 後片付け
                world.RotAngle = 0;
                world.RotOrigin = null;
            }

            var chartWin = new ChartWindow();
            chartWin.Owner = mainWindow;
            chartWin.Left = mainWindow.Left + mainWindow.Width;
            chartWin.Top = mainWindow.Top;
            chartWin.Show();
            var model = new PlotModel();
            chartWin.plot.Model = model;
            model.Title = "PCWaveguide Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "a/λ",
                Minimum = sFreq,
                Maximum = eFreq
            };
            var axis2 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "|S|",
                Minimum = 0.0,
                Maximum = 1.0
            };
            model.Axes.Add(axis1);
            model.Axes.Add(axis2);
            var series1 = new LineSeries
            {
                Title = "|S11|"
            };
            var series2 = new LineSeries
            {
                Title = "|S21|"
            };
            model.Series.Add(series1);
            model.Series.Add(series2);
            var datas = new List<DataPoint>();
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.ZAbs);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            for (int iFreq = 0; iFreq < freqDiv + 1; iFreq++)
            {
                // a/λ
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波長
                double waveLength = latticeA / normalizedFreq;
                // 周波数
                double freq = Constants.C0 / waveLength;
                System.Diagnostics.Debug.WriteLine("a/λ: " + normalizedFreq);

                var FEM = new PCWaveguide2DFEM(world);
                {
                    var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    solver.IsOrderingToBandMatrix = true;
                    solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    FEM.Solver = solver;
                }
                {
                    //var solver = new IvyFEM.Linear.LisEquationSolver();
                    //solver.Method = IvyFEM.Linear.LisEquationSolverMethod.Default;
                    //FEM.Solver = solver;
                }
                {
                    //var solver = new IvyFEM.Linear.IvyFEMEquationSolver();
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.COCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    //FEM.Solver = solver;
                }
                FEM.WgPortInfos = wgPortInfos;
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] Ez = FEM.Ez;
                System.Numerics.Complex[][] S = FEM.S;

                // eigen
                System.Numerics.Complex[][] portBetas = new System.Numerics.Complex[portCnt][];
                System.Numerics.Complex[][][] portEVecs = new System.Numerics.Complex[portCnt][][];
                for (int portId = 0; portId < portCnt; portId++)
                {
                    var eigenFEM = FEM.EigenFEMs[portId];
                    portBetas[portId] = eigenFEM.Betas;
                    portEVecs[portId] = eigenFEM.EVecs;
                }

                // eigen
                int targetModeId = 0;
                int nodeCnt = (int)world.GetNodeCount(quantityId);
                System.Numerics.Complex[] eigenEz = new System.Numerics.Complex[nodeCnt];
                for (int portId = 0; portId < portCnt; portId++)
                {
                    int modeCnt = portBetas[portId].Length;
                    if (targetModeId >= modeCnt)
                    {
                        System.Diagnostics.Debug.WriteLine("No defect mode found at port: " + portId);
                        continue;
                    }
                    System.Numerics.Complex beta = portBetas[portId][targetModeId];
                    System.Numerics.Complex[] eVec = portEVecs[portId][targetModeId];
                    int portNodeCnt = (int)world.GetPortNodeCount(quantityId, (uint)portId);
                    System.Diagnostics.Debug.Assert(portNodeCnt == eVec.Length);
                    for (int portNodeId = 0; portNodeId < portNodeCnt; portNodeId++)
                    {
                        int coId = world.PortNode2Coord(quantityId, (uint)portId, portNodeId);
                        int nodeId = world.Coord2Node(quantityId, coId);
                        System.Numerics.Complex value = eVec[portNodeId];
                        eigenEz[nodeId] = value;
                    }
                }

                if (S[0].Length > 0 && S[1].Length > 0)
                {
                    System.Numerics.Complex S11 = S[0][0];
                    System.Numerics.Complex S21 = S[1][0];
                    double S11Abs = S11.Magnitude;
                    double S21Abs = S21.Magnitude;
                    double total = S11Abs * S11Abs + S21Abs * S21Abs;

                    string ret;
                    string CRLF = System.Environment.NewLine;
                    ret = "2W/λ: " + normalizedFreq + CRLF;
                    ret += "|S11| = " + S11Abs + CRLF +
                          "|S21| = " + S21Abs + CRLF +
                          "|S11|^2 + |S21|^2 = " + total + CRLF;
                    System.Diagnostics.Debug.WriteLine(ret);
                    series1.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                    series2.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }

                // eigen
                //world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, eigenEz);
                // Ez
                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, Ez);

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
