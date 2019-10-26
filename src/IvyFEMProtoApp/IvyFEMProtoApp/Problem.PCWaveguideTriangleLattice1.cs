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
            double waveguideWidth = 1.0;

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
            //bool isShift180 = false; // for latticeTheta = 60 r = 0.30a air hole
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

            // 格子の数
            int latticeCnt = rodCntHalf * 2 + defectRodCnt;
            // ロッドから上下の電気壁までの距離
            double electricWallDistance = 0.1 * waveguideWidth / (rodCntHalf * 2 + defectRodCnt);
            // ロッド間の距離(Y方向)
            double rodDistanceY =
                (waveguideWidth - 2.0 * electricWallDistance) /
                (latticeCnt - 1 + 2.0 * rodRadiusRatio / Math.Sin(latticeTheta * Math.PI / 180.0));
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
            double eLen = 1.05 * waveguideWidth / (latticeCnt * divCntForOneLattice);

            // フォトニック結晶導波路の場合、a/λを規格化周波数とする
            double sFreq = 0.267;
            double eFreq = 0.287;
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
            int latticeCntPort2 = (rodCntHalfPort2 * 2 + defectRodCntPort2);
            // 導波路２の幅
            double waveguideWidth2 = rodDistanceY * (latticeCntPort2 - 1) + 2.0 * (rodRadius + electricWallDistance);
            System.Diagnostics.Debug.Assert(Math.Abs(waveguideWidth2 - waveguideWidth) < 1.0e-12);
            // 導波路２の１格子当たりの分割数
            int divCntForOneLatticePort2 = divCntForOneLattice;
            // 入出力不連続部の距離
            int rodCntDisconPort1 = rodCntDiscon;
            int rodCntDisconPort2 = rodCntDiscon;
            double disconLength1 = rodDistanceX * rodCntDisconPort1;
            double disconLength2 = rodDistanceX * rodCntDisconPort2;
            // 周期構造をポート1とポート2で一致させるための調整
            disconLength1 += 0.5 * rodDistanceX;
            disconLength2 += 0.5 * rodDistanceX;

            // ロッド中心と電気壁間の距離
            double electricWallDelta = electricWallDistance + rodRadius;
            // ベンド下側角
            double bendX10 = inputWgLength1 + disconLength1 +
                (waveguideWidth - 2.0 * electricWallDelta) / Math.Sqrt(3.0);
            double bendY10 = 0.0 + electricWallDelta;
            double bendX1 = bendX10 + electricWallDelta / Math.Sqrt(3.0);
            double bendY1 = 0.0;
            // ベンド部と出力部の境界
            double bendX20 = bendX10 + (waveguideWidth2 - 2.0 * electricWallDelta) / (2.0 * Math.Sqrt(3.0));
            double bendY20= bendY10 + (waveguideWidth2 - 2.0 * electricWallDelta) / 2.0;
            // 出力部内部境界の下側（終点）
            double port2X2B40 = bendX20 + disconLength2 / 2.0;
            double port2Y2B40 = bendY20 + disconLength2 * Math.Sqrt(3.0) / 2.0;
            double port2X2B4 = port2X2B40 + electricWallDelta * Math.Sqrt(3.0) / 2.0;
            double port2Y2B4 = port2Y2B40 - electricWallDelta / 2.0;
            // 出力部境界の終点
            double port2X2B3 = port2X2B4 + inputWgLength2 / 2.0;
            double port2Y2B3 = port2Y2B4 + inputWgLength2 * Math.Sqrt(3.0) / 2.0;
            // ベンド部上側角
            double bendX30 = inputWgLength1 + disconLength1;
            double bendY30 = waveguideWidth - electricWallDelta;
            double bendX3 = bendX30 - electricWallDelta / Math.Sqrt(3.0);
            double bendY3 = waveguideWidth;
            // 出力部内部境界の上側（始点）
            double port2X1B40 = bendX30 + disconLength2 / 2.0;
            double port2Y1B40 = bendY30 + disconLength2 * Math.Sqrt(3.0) / 2.0;
            double port2X1B4 = port2X1B40 - electricWallDelta * Math.Sqrt(3.0) / 2.0;
            double port2Y1B4 = port2Y1B40 + electricWallDelta / 2.0;
            // 出力部境界の始点
            double port2X1B3 = port2X1B4 + inputWgLength2 / 2.0;
            double port2Y1B3 = port2Y1B4 + inputWgLength2 * Math.Sqrt(3.0) / 2.0;

            // check
            {
                OpenTK.Vector2d port2B3Pt1 = new OpenTK.Vector2d(port2X1B3, port2Y1B3);
                OpenTK.Vector2d port2B3Pt2 = new OpenTK.Vector2d(port2X2B3, port2Y2B3);
                double distanceB3 = OpenTK.Vector2d.Distance(port2B3Pt1, port2B3Pt2);
                System.Diagnostics.Debug.Assert(
                    Math.Abs(distanceB3 - waveguideWidth2) < 1.0e-12);
                OpenTK.Vector2d port2B4Pt1 = new OpenTK.Vector2d(port2X1B4, port2Y1B4);
                OpenTK.Vector2d port2B4Pt2 = new OpenTK.Vector2d(port2X2B4, port2Y2B4);
                double distanceB4 = OpenTK.Vector2d.Distance(port2B4Pt1, port2B4Pt2);
                System.Diagnostics.Debug.Assert(
                    Math.Abs(distanceB4 - waveguideWidth2) < 1.0e-12);
            }

            IList<uint> rodLoopIds = new List<uint>();
            IList<uint> inputWgRodLoopIds1 = new List<uint>();
            IList<uint> inputWgRodLoopIds2 = new List<uint>();
            IList<uint> rodEIdsB1 = new List<uint>();
            IList<uint> rodEIdsB2 = new List<uint>();
            IList<uint> rodEIdsB3 = new List<uint>();
            IList<uint> rodEIdsB4 = new List<uint>();
            IList<uint> rodVIdsB1 = new List<uint>();
            IList<uint> rodVIdsB2 = new List<uint>();
            IList<uint> rodVIdsB3 = new List<uint>();
            IList<uint> rodVIdsB4 = new List<uint>();
            {
                //------------------------------------------------------------------
                // 図面作成
                //------------------------------------------------------------------
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    // 領域追加
                    pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(inputWgLength1, 0.0));
                    pts.Add(new OpenTK.Vector2d(bendX1, bendY1));
                    pts.Add(new OpenTK.Vector2d(port2X2B4, port2Y2B4));
                    pts.Add(new OpenTK.Vector2d(port2X2B3, port2Y2B3));
                    pts.Add(new OpenTK.Vector2d(port2X1B3, port2Y1B3));
                    pts.Add(new OpenTK.Vector2d(port2X1B4, port2Y1B4));
                    pts.Add(new OpenTK.Vector2d(bendX3, bendY3));
                    pts.Add(new OpenTK.Vector2d(inputWgLength1, waveguideWidth));
                    uint lId1 = cad2D.AddPolygon(pts).AddLId;
                }
                // 入出力領域を分離
                uint eIdAdd1 = cad2D.ConnectVertexLine(3, 10).AddEId;
                uint eIdAdd2 = cad2D.ConnectVertexLine(5, 8).AddEId;

                //////////////////////////////////////////
                // ロッドの中心Y座標(偶数/奇数カラム)
                IList<double> rodY0sX1 = new List<double>();
                IList<double> rodY0sX2 = new List<double>();
                for (int i = 0; i < latticeCnt; i++)
                {
                    if (i >= rodCntHalf && i < (rodCntHalf + defectRodCnt))
                    {
                        // 欠陥部
                        continue;
                    }
                    double upperRodPosY = waveguideWidth - electricWallDistance - rodRadius;
                    if (i % 2 == 0)
                    {
                        double y0 = upperRodPosY - i * rodDistanceY;
                        if ((y0 + rodRadius) > waveguideWidth || (y0 - rodRadius) < 0.0)
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        rodY0sX2.Add(y0);
                    }
                    else
                    {
                        double y0 = upperRodPosY - rodDistanceY - (i - 1) * rodDistanceY;
                        if ((y0 + rodRadius) > waveguideWidth || (y0 - rodRadius) < 0.0)
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        rodY0sX1.Add(y0);
                    }
                }

                ///////////////////////////////////////////
                // 境界上のロッドの頂点を追加
                // 入力導波路 外側境界
                // 入力導波路 内部側境界
                // 出力導波路 外側境界
                // 出力導波路 内部側境界
                uint[] eIdsB = { 1, 11, 6, 12 };
                IList<uint>[] rodVIdsB = { rodVIdsB1, rodVIdsB2, rodVIdsB3, rodVIdsB4 };
                IList<uint>[] rodEIdsB = { rodEIdsB1, rodEIdsB2, rodEIdsB3, rodEIdsB4 };
                double[] upperPtXsB = { 0.0, inputWgLength1, port2X1B3,  port2X1B4 };
                double[] upperPtYsB = { waveguideWidth, waveguideWidth, port2Y1B3, port2Y1B4 };
                double[] belowPtXsB = { 0.0, inputWgLength1, port2X2B3, port2X2B4 };
                double[] belowPtYsB = { 0.0, 0.0, port2Y2B3, port2Y2B4 };
                for (int boundaryIndex = 0; boundaryIndex < eIdsB.Length; boundaryIndex++)
                {
                    uint eId = eIdsB[boundaryIndex];
                    double uPtXB = upperPtXsB[boundaryIndex];
                    double uPtYB = upperPtYsB[boundaryIndex];
                    double bPtXB = belowPtXsB[boundaryIndex];
                    double bPtYB = belowPtYsB[boundaryIndex];
                    IList<uint> workrodEIdsB = rodEIdsB[boundaryIndex];
                    IList<uint> workrodVIdsB = rodVIdsB[boundaryIndex];
                    int rodCntX1 = rodY0sX1.Count;
                    for (int i = 0; i < rodCntX1; i++)
                    {
                        double ptX0 = 0.0;
                        double ptY0 = 0.0;
                        double ptX1 = 0.0;
                        double ptY1 = 0.0;
                        double ptX2 = 0.0;
                        double ptY2 = 0.0;
                        if (boundaryIndex == 0)
                        {
                            // 入力導波路 外側境界
                            // 下から順に追加
                            ptY0 = rodY0sX1[rodCntX1 - 1 - i];
                            ptY1 = ptY0 - rodRadius;
                            ptY2 = ptY0 + rodRadius;
                            ptX0 = bPtXB;
                            ptX1 = bPtXB;
                            ptX2 = bPtXB;
                        }
                        else if (boundaryIndex == 1)
                        {
                            // 上から順に追加
                            ptY0 = rodY0sX1[i];
                            ptY1 = ptY0 + rodRadius;
                            ptY2 = ptY0 - rodRadius;
                            ptX0 = bPtXB;
                            ptX1 = bPtXB;
                            ptX2 = bPtXB;
                        }
                        else if (boundaryIndex == 2 || boundaryIndex == 3)
                        {
                            // 上から順に追加
                            double basePtY0 = rodY0sX1[i];
                            double basePtY1 = basePtY0 + rodRadius;
                            double basePtY2 = basePtY0 - rodRadius;

                            ptX0 = bPtXB + (uPtXB - bPtXB) * (basePtY0 - 0.0) / waveguideWidth2;
                            ptY0 = bPtYB + (uPtYB - bPtYB) * (basePtY0 - 0.0) / waveguideWidth2;
                            ptX1 = bPtXB + (uPtXB - bPtXB) * (basePtY1 - 0.0) / waveguideWidth2;
                            ptY1 = bPtYB + (uPtYB - bPtYB) * (basePtY1 - 0.0) / waveguideWidth2;
                            ptX2 = bPtXB + (uPtXB - bPtXB) * (basePtY2 - 0.0) / waveguideWidth2;
                            ptY2 = bPtYB + (uPtYB - bPtYB) * (basePtY2 - 0.0) / waveguideWidth2;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }

                        double[] ptXs = { ptX1, ptX0, ptX2 };
                        double[] ptYs = { ptY1, ptY0, ptY2 };
                        for (int iY = 0; iY < ptYs.Length; iY++)
                        {
                            var res = cad2D.AddVertex(
                                CadElementType.Edge, eId, new OpenTK.Vector2d(ptXs[iY], ptYs[iY]));
                            uint addVId = res.AddVId;
                            uint addEId = res.AddEId;
                            System.Diagnostics.Debug.Assert(addVId != 0);
                            System.Diagnostics.Debug.Assert(addEId != 0);
                            workrodVIdsB.Add(addVId);
                            if (iY != 0) // iY == 0のときはロッドの外の辺が生成される
                            {
                                workrodEIdsB.Add(addEId);
                            }
                        }
                    }
                    System.Diagnostics.Debug.Assert(workrodVIdsB.Count % 3 == 0);
                    System.Diagnostics.Debug.Assert(workrodEIdsB.Count % 2 == 0);
                }

                /////////////////////////////////////////////////////////////////////////////
                // 左のロッドを追加
                uint[] leftRodContainsLIds = { 1, 2, 3 };
                IList<uint>[] leftRodVIdsB = { rodVIdsB[0], rodVIdsB[1], rodVIdsB[3] };
                IList<uint>[] leftRodContainsInputWgRodLoopIdss = { inputWgRodLoopIds1, null, inputWgRodLoopIds2 };
                for (int index = 0; index < leftRodContainsLIds.Length; index++)
                {
                    IList<uint> workrodVIdsB = leftRodVIdsB[index];
                    uint baseLoopId = leftRodContainsLIds[index];
                    IList<uint> workInputWgRodLoopIds = leftRodContainsInputWgRodLoopIdss[index];

                    // 始点、終点が逆？
                    bool isReverse = false;
                    if (index == 0)
                    {
                        // 入力境界 外側
                        isReverse = false;
                    }
                    else
                    {
                        isReverse = true;
                    }

                    int rodCnt = workrodVIdsB.Count / 3;

                    for (int i = 0; i < rodCnt; i++)
                    {
                        uint vId0 = workrodVIdsB[i * 3];
                        uint vId1 = workrodVIdsB[i * 3 + 1]; // 中心はこれ
                        uint vId2 = workrodVIdsB[i * 3 + 2];
                        // 中心
                        OpenTK.Vector2d cPt = cad2D.GetVertexCoord(vId1);
                        double x0 = cPt.X;
                        double y0 = cPt.Y;

                        uint workVId0 = 0;
                        uint workVId2 = 0;
                        if (isReverse)
                        {
                            workVId0 = vId2;
                            workVId2 = vId0;
                        }
                        else
                        {
                            workVId0 = vId0;
                            workVId2 = vId2;
                        }
                        // 左のロッド
                        uint lId = 0;
                        if (index == 2)
                        {
                            double startAngle = (180.0 - 30.0);
                            lId = PCWaveguideUtils.AddExactlyHalfRod(
                                cad2D,
                                baseLoopId,
                                workVId0,
                                vId1,
                                workVId2,
                                x0,
                                y0,
                                rodRadius,
                                rodCircleDiv,
                                rodRadiusDiv,
                                startAngle,
                                true);
                        }
                        else
                        {
                            lId = PCWaveguideUtils.AddLeftRod(
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

                        }
                        rodLoopIds.Add(lId);

                        if (workInputWgRodLoopIds != null)
                        {
                            workInputWgRodLoopIds.Add(lId);
                        }
                    }
                }

                /////////////////////////////////////////////////////////////////////////////
                // 右のロッドを追加
                uint[] rightRodContainsLIds = { 1, 3, 2 };
                IList<uint>[] rightRodVIdsB = { rodVIdsB[1], rodVIdsB[2], rodVIdsB[3] };
                IList<uint>[] rightRodContainsInputWgRodLoopIdss = { inputWgRodLoopIds1, inputWgRodLoopIds2, null };
                for (int index = 0; index < rightRodContainsLIds.Length; index++)
                {
                    IList<uint> workrodVIdsB = rightRodVIdsB[index];
                    uint baseLoopId = rightRodContainsLIds[index];
                    IList<uint> workInputWgRodLoopIds = rightRodContainsInputWgRodLoopIdss[index];

                    // 始点、終点が逆？
                    bool isReverse = false;

                    int rodCnt = workrodVIdsB.Count / 3;

                    for (int i = 0; i < rodCnt; i++)
                    {
                        uint vId0 = workrodVIdsB[i * 3];
                        uint vId1 = workrodVIdsB[i * 3 + 1]; // 中心はこれ
                        uint vId2 = workrodVIdsB[i * 3 + 2];
                        // 中心
                        OpenTK.Vector2d cPt = cad2D.GetVertexCoord(vId1);
                        double x0 = cPt.X;
                        double y0 = cPt.Y;

                        uint workVId0 = 0;
                        uint workVId2 = 0;
                        if (isReverse)
                        {
                            workVId0 = vId2;
                            workVId2 = vId0;
                        }
                        else
                        {
                            workVId0 = vId0;
                            workVId2 = vId2;
                        }
                        // 右のロッド
                        uint lId;
                        if (index == 1 || index == 2)
                        {
                            double startAngle = (0.0 - 30.0);
                            lId = PCWaveguideUtils.AddExactlyHalfRod(
                                cad2D,
                                baseLoopId,
                                workVId0,
                                vId1,
                                workVId2,
                                x0,
                                y0,
                                rodRadius,
                                rodCircleDiv,
                                rodRadiusDiv,
                                startAngle,
                                true);
                        }
                        else
                        {
                            lId = PCWaveguideUtils.AddRightRod(
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
                        }
                        rodLoopIds.Add(lId);

                        if (workInputWgRodLoopIds != null)
                        {
                            workInputWgRodLoopIds.Add(lId);
                        }
                    }
                }

                /////////////////////////////////////////////////////////////////////////
                // 領域のロッド
                // ポート1側の内部領域
                int periodicCntInputWg1 = 1;
                int periodicCntX1 = periodicCntInputWg1 + rodCntDiscon;
                int periodicCntXBend1 = 4;
                for (int iX = 0; iX < (periodicCntX1 + periodicCntXBend1); iX++)
                {
                    uint baseLoopId = 0;
                    int inputWgNo = 0;
                    bool isBend = false;
                    if (iX >= 0 && iX < periodicCntInputWg1)
                    {
                        baseLoopId = 1;
                        inputWgNo = 1;
                    }
                    else if (iX >= periodicCntInputWg1 &&
                        iX < (periodicCntInputWg1 + rodCntDiscon))
                    {
                        baseLoopId = 2;
                        inputWgNo = 0;
                    }
                    else if (iX >= (periodicCntInputWg1 + rodCntDiscon) &&
                        iX < (periodicCntInputWg1 + rodCntDiscon + periodicCntXBend1))
                    {
                        baseLoopId = 2;
                        inputWgNo = 0;
                        isBend = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    for (int iSubX = 0; iSubX < 2; iSubX++)
                    {
                        IList<double> workRodY0s = null;
                        double x0 = 0.0;
                        if (iSubX % 2 == 0)
                        {
                            if (iX == 0 || iX == periodicCntInputWg1)
                            {
                                // 追加済み
                                continue;
                            }
                            if (iX == (periodicCntX1 - 1))
                            {
                                // 追加済み
                                continue;
                            }
                            workRodY0s = rodY0sX1;
                            x0 = iX * periodicDistance;
                        }
                        else
                        {
                            workRodY0s = rodY0sX2;
                            x0 = iX * periodicDistance + 0.5 * periodicDistance;
                        }
                        int rodCnt = workRodY0s.Count;
                        for (int i = 0; i < rodCnt; i++)
                        {
                            double y0 = workRodY0s[i];

                            if (isBend)
                            {
                                if (x0 - (inputWgLength1 + disconLength1) < waveguideWidth / Math.Sqrt(3.0))
                                {
                                    double bendCenterY = bendY3 + (bendY1 - bendY3) *
                                        (x0 - (inputWgLength1 + disconLength1)) / (waveguideWidth / Math.Sqrt(3.0));
                                    if (y0 >= bendCenterY)
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            uint lId = PCWaveguideUtils.AddRod(
                                cad2D, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                            rodLoopIds.Add(lId);
                            if (inputWgNo == 1)
                            {
                                inputWgRodLoopIds1.Add(lId);
                            }
                            else
                            {
                                // なにもしない
                            }
                        }
                    }
                }

                // ポート2側の内部領域
                int periodicCntInputWg2 = 1;
                int periodicCntX2 = periodicCntInputWg2 + rodCntDiscon;
                int periodicCntXBend2 = 3;
                double port2UpperX1 = port2X1B3;
                double port2UpperY1 = port2Y1B3;
                double port2BelowX1 = port2X2B3;
                double port2BelowY1 = port2Y2B3;
                double port2UpperX2 = port2UpperX1 - (inputWgLength2 + disconLength2) / 2.0;
                double port2UpperY2 = port2UpperY1 - (inputWgLength2 + disconLength2) * Math.Sqrt(3.0) / 2.0;
                double port2BelowX2 = port2BelowX1 - (inputWgLength2 + disconLength2) / 2.0;
                double port2BelowY2 = port2BelowY1 - (inputWgLength2 + disconLength2) * Math.Sqrt(3.0) / 2.0;
                for (int iX = 0; iX < (periodicCntX2 + periodicCntXBend2); iX++)
                {
                    bool isBend = false;
                    uint baseLoopId = 0;
                    int inputWgNo = 0;
                    if (iX >= 0 && iX < periodicCntInputWg2)
                    {
                        baseLoopId = 3;
                        inputWgNo = 2;
                    }
                    else if (iX >= periodicCntInputWg2 &&
                        iX < (periodicCntInputWg2 + rodCntDiscon))
                    {
                        baseLoopId = 2;
                        inputWgNo = 0;
                    }
                    else if (iX >= (periodicCntInputWg2 + rodCntDiscon) &&
                        iX < (periodicCntInputWg2 + rodCntDiscon + periodicCntXBend2))
                    {
                        baseLoopId = 2;
                        inputWgNo = 0;
                        isBend = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    for (int iSubX = 0; iSubX < 2; iSubX++)
                    {
                        IList<double> workRodY0s = null;
                        double baseX0 = 0.0;
                        if (iSubX % 2 == 0)
                        {
                            if (iX == 0 || iX == periodicCntInputWg2)
                            {
                                // 追加済み
                                continue;
                            }
                            if (iX == (periodicCntX2 - 1))
                            {
                                // 追加済み
                                continue;
                            }
                            workRodY0s = rodY0sX1;
                            baseX0 = iX * periodicDistance;
                        }
                        else
                        {
                            workRodY0s = rodY0sX2;
                            baseX0 = iX * periodicDistance + 0.5 * periodicDistance;
                        }
                        double uX = port2UpperX1 + 
                            (port2UpperX2 - port2UpperX1) * (baseX0 - 0.0) / (inputWgLength2 + disconLength2);
                        double uY = port2UpperY1 + 
                            (port2UpperY2 - port2UpperY1) * (baseX0 - 0.0) / (inputWgLength2 + disconLength2);
                        double bX = port2BelowX1 + 
                            (port2BelowX2 - port2BelowX1) * (baseX0 - 0.0) / (inputWgLength2 + disconLength2);
                        double bY = port2BelowY1 +
                            (port2BelowY2 - port2BelowY1) * (baseX0 - 0.0) / (inputWgLength2 + disconLength2);
                        {
                            // check
                            OpenTK.Vector2d u1 = new OpenTK.Vector2d(port2UpperX1, port2UpperY1);
                            OpenTK.Vector2d u2 = new OpenTK.Vector2d(port2UpperX2, port2UpperY2);
                            OpenTK.Vector2d b1 = new OpenTK.Vector2d(port2BelowX1, port2BelowY1);
                            OpenTK.Vector2d b2 = new OpenTK.Vector2d(port2BelowX2, port2BelowY2);
                            double uDistance = OpenTK.Vector2d.Distance(u1, u2);
                            double bDistance = OpenTK.Vector2d.Distance(b1, b2);
                            System.Diagnostics.Debug.Assert(
                                Math.Abs(uDistance - (inputWgLength2 + disconLength2)) < 1.0e-12);
                            System.Diagnostics.Debug.Assert(
                                Math.Abs(bDistance - (inputWgLength2 + disconLength2)) < 1.0e-12);
                        }
                        int rodCnt = workRodY0s.Count;
                        for (int i = 0; i < rodCnt; i++)
                        {
                            double baseY0 = workRodY0s[i];

                            if (isBend)
                            {
                                if (baseX0 - (inputWgLength2 + disconLength2) < waveguideWidth2 / Math.Sqrt(3.0))
                                {
                                    double baseBendCenterY = bendY3 + (bendY1 - bendY3) *
                                        (baseX0 - (inputWgLength2 + disconLength2)) /
                                        (waveguideWidth2 / Math.Sqrt(3.0));
                                    if (baseY0 >= (baseBendCenterY - rodDistanceY))
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            double x0 = bX + (uX - bX) * (baseY0 - 0.0) / waveguideWidth2;
                            double y0 = bY + (uY - bY) * (baseY0 - 0.0) / waveguideWidth2;
                            uint lId = PCWaveguideUtils.AddRod(
                                cad2D, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                            rodLoopIds.Add(lId);
                            if (inputWgNo == 2)
                            {
                                inputWgRodLoopIds2.Add(lId);
                            }
                            else
                            {
                                // なにもしない
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
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            IDrawer meshDrawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
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
            int rodCnt1 = rodEIdsB1.Count / 2;
            int divCntPort1 = 3 * rodCnt1 + 1;
            int rodCnt2 = rodEIdsB3.Count / 2;
            int divCntPort2 = 3 * rodCnt2 + 1;
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
            world.SetIncidentPortId(quantityId, 0);
            world.SetIncidentModeId(quantityId, 0);
            IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
            {
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
                        //if (y >= (waveguideWidth - rodDistanceY * (rodCntHalf + defectRodCnt) -
                        //    (0.5 * rodDistanceY - rodRadius)) && y <=
                        //    (waveguideWidth - rodDistanceY * rodCntHalf +
                        //    (0.5 * rodDistanceY - rodRadius))) // dielectric rod
                        if (y >= (waveguideWidth - rodDistanceY * (rodCntHalf + defectRodCnt) -
                            1.0 * rodDistanceY) &&
                            y <= (waveguideWidth - rodDistanceY * rodCntHalf +
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
            chartWin.Plot.Model = model;
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
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
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
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
