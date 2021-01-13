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
        public void PCWaveguideModalABCZTDTriangleLatticeProblem1_0(MainWindow mainWindow)
        {
            double[] freqs;
            System.Numerics.Complex[] freqDomainAmpsInc;
            SolvePCWaveguideModalABCZTDTriangleLatticeProblem1_0(
                mainWindow, out freqs, out freqDomainAmpsInc);
        }

        public void SolvePCWaveguideModalABCZTDTriangleLatticeProblem1_0(
            MainWindow mainWindow,
            out double[] retFreqs,
            out System.Numerics.Complex[] retFreqDomainAmpsInc)
        {
            retFreqs = null;
            retFreqDomainAmpsInc = null;

            double waveguideWidth = 1.0;

            // フォトニック導波路
            // 考慮する波数ベクトルの最小値
            double minWaveNum = 0.0;
            // 考慮する波数ベクトルの最大値
            double maxWaveNum = 0.5;

            // 空孔？
            //bool isAirHole = false; // 誘電体ロッド
            //bool isAirHole = true; //エアホール
            bool isAirHole = true;
            //bool isTMMode = true;  // TMモード
            //bool isTMMode = false;  // TEモード
            bool isTMMode = true;
            // ロッドの数(半分)
            //const int rodCntHalf = 5;
            const int rodCntHalf = 3;
            // 欠陥ロッド数
            const int defectRodCnt = 1;
            // 三角形格子の内角
            double latticeTheta = 60.0;
            // ロッドの半径
            double rodRadiusRatio = 0.30;
            // ロッドの比誘電率
            double rodEp = 2.76 * 2.76;
            // 1格子当たりの分割点の数
            //const int divCntForOneLattice = 9;
            //const int divCntForOneLattice = 8;
            //const int divCntForOneLattice = 6;
            const int divCntForOneLattice = 8;
            // ロッド円周の分割数
            const int rodCircleDiv = 12;
            // ロッドの半径の分割数
            const int rodRadiusDiv = 4;

            // 格子の数
            int latticeCnt = rodCntHalf * 2 + defectRodCnt;
            // ロッドから上下の電気壁までの距離
            double electricWallDistance = 0.1 * waveguideWidth / (rodCntHalf * 2 + defectRodCnt);
            // ロッド間の距離(Y方向)
            double rodDistanceY =
                (waveguideWidth - 2.0 * electricWallDistance) /
                (latticeCnt - 1 + 2.0 * rodRadiusRatio / Math.Sin(latticeTheta * Math.PI / 180.0));
            // 格子定数
            double latticeA = rodDistanceY / Math.Sin(latticeTheta * Math.PI / 180.0);
            // ロッド間の距離(X方向)
            double rodDistanceX = rodDistanceY * 2.0 / Math.Tan(latticeTheta * Math.PI / 180.0);
            // 周期構造距離
            double periodicDistance = rodDistanceX;
            // ロッドの半径
            double rodRadius = rodRadiusRatio * latticeA;
            // ロッド中心と電気壁間の距離
            double electricWallDelta = electricWallDistance + rodRadius;
            // メッシュのサイズ
            double eLen = 1.05 * waveguideWidth / (latticeCnt * divCntForOneLattice);
            // 最小屈折率
            double minEffN = 0.0;
            // 最大屈折率
            double maxEffN = 1.0;
            if (isAirHole)
            {
                // air hole
                minEffN = 0.0;
                maxEffN = Math.Sqrt(rodEp);
            }

            // 時間刻み幅の算出
            //double courantNumber = 0.5;
            double courantNumber = 0.5;
            // Note: timeLoopCnt は 2^mでなければならない
            //int timeLoopCnt = 2048;
            int timeLoopCnt = 2048 * 8; // 測定帯域が短いので全観測時間を長くする
            double timeStep = courantNumber * eLen / (Constants.C0 * Math.Sqrt(2.0));
            // 励振源
            // 規格化周波数
            //double srcNormalizedFreq = 0.277;
            double srcNormalizedFreq = 0.277;
            // 波長
            double srcWaveLength = latticeA / srcNormalizedFreq;
            // 周波数
            double srcFreq = Constants.C0 / srcWaveLength;
            // 計算する周波数領域
            double normalizedFreq1 = 0.267;
            double normalizedFreq2 = 0.287;
            double waveLength1 = latticeA / normalizedFreq1;
            double waveLength2 = latticeA / normalizedFreq2;
            double freq1 = Constants.C0 / waveLength1;
            double freq2 = Constants.C0 / waveLength2;

            // 規格化周波数変換
            Func<double, double> toNormalizedFreq =
                waveLength => latticeA / waveLength;

            // ガウシアンパルス
            GaussianType gaussianType = GaussianType.SinModulation; // 正弦波変調
            /*
            // 搬送波の振動回数
            int nCycle = 5;
            double gaussianT0 = 1.00 * (1.0 / srcFreq) * nCycle / 2.0;
            double gaussianTp = gaussianT0 / (2.0 * Math.Sqrt(2.0 * Math.Log(2.0)));
            */
            double gaussianTp = (1.0 / 4.0) * 6.0 / (freq2 - freq1);
            double gaussianT0 = gaussianTp * (2.0 * Math.Sqrt(2.0 * Math.Log(2.0)));

            const uint portCnt = 2;
            // 入出力導波路の周期構造部分の長さ
            double inputWgLength = periodicDistance;
            // 導波路不連続領域の長さ
            //const int disconRodCnt = 7; // 最低7必要
            const int disconRodCnt = 7;
            double disconLength = periodicDistance * disconRodCnt;
            double disconPlusInputWgLength = disconLength + 2.0 * inputWgLength;
            // 形状設定で使用する単位長さ
            double unitLen = periodicDistance;
            double port1XB1 = 0;
            double port1XB2 = periodicDistance;
            double port2XB1 = disconPlusInputWgLength;
            double port2XB2 = disconPlusInputWgLength - periodicDistance;
            // 励振位置
            double srcXB1 = port1XB2 + 1 * unitLen;
            double srcXB2 = srcXB1 + periodicDistance;
            // 観測点
            int port1OfsX = 1;
            int port2OfsX = 1;
            double refport1XB1 = srcXB2 + port1OfsX * unitLen;
            double refport1XB2 = refport1XB1 + periodicDistance;
            double refport2XB1 = port2XB2 - port2OfsX * unitLen;
            double refport2XB2 = refport2XB1 - periodicDistance;
            // 観測ポート数
            int refPortCnt = 2;
            IList<uint> rodLoopIds = new List<uint>();
            IList<uint> inputWgRodLoopIdsPort10 = new List<uint>();
            IList<uint> inputWgRodLoopIdsSrc = new List<uint>();
            IList<uint> inputWgRodLoopIdsPort1 = new List<uint>();
            IList<uint> inputWgRodLoopIdsPort2 = new List<uint>();
            IList<uint> inputWgRodLoopIdsPort20 = new List<uint>();
            IList<uint>[] inputWgRodLoopIdss = {
                inputWgRodLoopIdsPort10, inputWgRodLoopIdsPort20,
                inputWgRodLoopIdsPort1, inputWgRodLoopIdsPort2,
                inputWgRodLoopIdsSrc
            };
            uint[] inputWgBaseLoopIds = { 1, 9, 5, 7, 3 };
            IList<uint> rodEIdsPort10B1 = new List<uint>();
            IList<uint> rodEIdsPort10B2 = new List<uint>();
            IList<uint> rodEIdsPort20B1 = new List<uint>();
            IList<uint> rodEIdsPort20B2 = new List<uint>();
            IList<uint> rodEIdsPort1B1 = new List<uint>();
            IList<uint> rodEIdsPort1B2 = new List<uint>();
            IList<uint> rodEIdsPort2B1 = new List<uint>();
            IList<uint> rodEIdsPort2B2 = new List<uint>();
            IList<uint> rodEIdsSrcB1 = new List<uint>();
            IList<uint> rodEIdsSrcB2 = new List<uint>();
            IList<uint> rodVIdsPort10B1 = new List<uint>();
            IList<uint> rodVIdsPort10B2 = new List<uint>();
            IList<uint> rodVIdsPort20B1 = new List<uint>();
            IList<uint> rodVIdsPort20B2 = new List<uint>();
            IList<uint> rodVIdsPort1B1 = new List<uint>();
            IList<uint> rodVIdsPort1B2 = new List<uint>();
            IList<uint> rodVIdsPort2B1 = new List<uint>();
            IList<uint> rodVIdsPort2B2 = new List<uint>();
            IList<uint> rodVIdsSrcB1 = new List<uint>();
            IList<uint> rodVIdsSrcB2 = new List<uint>();

            Cad2D cad = new Cad2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            {
                System.Diagnostics.Debug.Assert(Math.Abs(port1XB1 - 0.0) < 1.0e-12);
                System.Diagnostics.Debug.Assert(Math.Abs(port2XB1 - disconPlusInputWgLength) < 1.0e-12);
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(port1XB2, 0.0));
                pts.Add(new OpenTK.Vector2d(srcXB1, 0.0));
                pts.Add(new OpenTK.Vector2d(srcXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(refport1XB1, 0.0));
                pts.Add(new OpenTK.Vector2d(refport1XB2, 0.0));
                pts.Add(new OpenTK.Vector2d(refport2XB2, 0.0));
                pts.Add(new OpenTK.Vector2d(refport2XB1, 0.0));
                pts.Add(new OpenTK.Vector2d(port2XB2, 0.0));
                pts.Add(new OpenTK.Vector2d(disconPlusInputWgLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconPlusInputWgLength, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port2XB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(refport2XB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(refport2XB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(refport1XB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(refport1XB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(srcXB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(srcXB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1XB2, waveguideWidth));
                uint _lId1 = cad.AddPolygon(pts).AddLId;
                uint _lId2 = cad.ConnectVertexLine(3, 20).AddLId;
                uint _lId3 = cad.ConnectVertexLine(4, 19).AddLId;
                uint _lId4 = cad.ConnectVertexLine(5, 18).AddLId;
                uint _lId5 = cad.ConnectVertexLine(6, 17).AddLId;
                uint _lId6 = cad.ConnectVertexLine(7, 16).AddLId;
                uint _lId7 = cad.ConnectVertexLine(8, 15).AddLId;
                uint _lId8 = cad.ConnectVertexLine(9, 14).AddLId;
                uint _lId9 = cad.ConnectVertexLine(10, 13).AddLId;
            }

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
                double topRodPosY = waveguideWidth - electricWallDistance - rodRadius;
                if (i % 2 == 0)
                {
                    double y0 = topRodPosY - i * rodDistanceY;
                    if ((y0 + rodRadius) > waveguideWidth || (y0 - rodRadius) < 0.0)
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                    rodY0sX2.Add(y0);
                }
                else
                {
                    double y0 = topRodPosY - rodDistanceY - (i - 1) * rodDistanceY;
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
            uint[] eIdsB = { 
                1, 21, 11, 28,
                24, 25, 27, 26,
                22, 23
            };
            IList<uint>[] rodVIdsB = { 
                rodVIdsPort10B1, rodVIdsPort10B2, rodVIdsPort20B1, rodVIdsPort20B2,
                rodVIdsPort1B1, rodVIdsPort1B2, rodVIdsPort2B1, rodVIdsPort2B2,
                rodVIdsSrcB1, rodVIdsSrcB2
            };
            IList<uint>[] rodEIdsB = {
                rodEIdsPort10B1, rodEIdsPort10B2, rodEIdsPort20B1, rodEIdsPort20B2,
                rodEIdsPort1B1, rodEIdsPort1B2, rodEIdsPort2B1, rodEIdsPort2B2,
                rodEIdsSrcB1, rodEIdsSrcB2
            };
            double[] ptXsB = {
                port1XB1, port1XB2,
                port2XB1, port2XB2,
                refport1XB1, refport1XB2,
                refport2XB1, refport2XB2,
                srcXB1, srcXB2
            };
            for (int boundaryIndex = 0; boundaryIndex < eIdsB.Length; boundaryIndex++)
            {
                uint eId = eIdsB[boundaryIndex];
                double ptXB = ptXsB[boundaryIndex];
                IList<uint> workrodEIdsB = rodEIdsB[boundaryIndex];
                IList<uint> workrodVIdsB = rodVIdsB[boundaryIndex];
                int rodCntX1 = rodY0sX1.Count;
                for (int i = 0; i < rodCntX1; i++)
                {
                    double ptY0 = 0.0;
                    double ptY1 = 0.0;
                    double ptY2 = 0.0;
                    if (boundaryIndex == 0)
                    {
                        // 入力導波路 外側境界
                        // 下から順に追加
                        ptY0 = rodY0sX1[rodCntX1 - 1 - i];
                        ptY1 = ptY0 - rodRadius;
                        ptY2 = ptY0 + rodRadius;
                    }
                    else
                    {
                        // 上から順に追加
                        ptY0 = rodY0sX1[i];
                        ptY1 = ptY0 + rodRadius;
                        ptY2 = ptY0 - rodRadius;
                    }

                    double[] ptYs = { ptY1, ptY0, ptY2 };
                    for (int iY = 0; iY < ptYs.Length; iY++)
                    {
                        var res = cad.AddVertex(
                            CadElementType.Edge, eId, new OpenTK.Vector2d(ptXB, ptYs[iY]));
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
            uint[] leftRodContainsLIds = {
                1, 2, 9,
                5, 6, 8, 7,
                3, 4
            };
            IList<uint>[] leftRodVIdsB = { 
                rodVIdsB[0], rodVIdsB[1], rodVIdsB[3],
                rodVIdsB[4], rodVIdsB[5], rodVIdsB[6], rodVIdsB[7],
                rodVIdsB[8], rodVIdsB[9]
            };
            for (int index = 0; index < leftRodContainsLIds.Length; index++)
            {
                IList<uint> workrodVIdsB = leftRodVIdsB[index];
                uint baseLoopId = leftRodContainsLIds[index];
                IList<uint> workInputWgRodLoopIds = null;
                {
                    int inputWgIndex = Array.IndexOf(inputWgBaseLoopIds, baseLoopId);
                    if (inputWgIndex != -1)
                    {
                        workInputWgRodLoopIds = inputWgRodLoopIdss[inputWgIndex];
                    }
                }

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
                    OpenTK.Vector2d cPt = cad.GetVertexCoord(vId1);
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
                    uint lId = PCWaveguideUtils.AddLeftRod(
                        cad,
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

                    if (workInputWgRodLoopIds != null)
                    {
                        workInputWgRodLoopIds.Add(lId);
                    }
                }
            }

            /////////////////////////////////////////////////////////////////////////////
            // 右のロッドを追加
            uint[] rightRodContainsLIds = {
                1, 9, 8,
                4, 5, 7, 6,
                2, 3
            };
            IList<uint>[] rightRodVIdsB = {
                rodVIdsB[1], rodVIdsB[2], rodVIdsB[3],
                rodVIdsB[4], rodVIdsB[5], rodVIdsB[6], rodVIdsB[7],
                rodVIdsB[8], rodVIdsB[9]
            };
            for (int index = 0; index < rightRodContainsLIds.Length; index++)
            {
                IList<uint> workrodVIdsB = rightRodVIdsB[index];
                uint baseLoopId = rightRodContainsLIds[index];
                IList<uint> workInputWgRodLoopIds = null;
                {
                    int inputWgIndex = Array.IndexOf(inputWgBaseLoopIds, baseLoopId);
                    if (inputWgIndex != -1)
                    {
                        workInputWgRodLoopIds = inputWgRodLoopIdss[inputWgIndex];
                    }
                }

                // 始点、終点が逆？
                bool isReverse = false;

                int rodCnt = workrodVIdsB.Count / 3;

                for (int i = 0; i < rodCnt; i++)
                {
                    uint vId0 = workrodVIdsB[i * 3];
                    uint vId1 = workrodVIdsB[i * 3 + 1]; // 中心はこれ
                    uint vId2 = workrodVIdsB[i * 3 + 2];
                    // 中心
                    OpenTK.Vector2d cPt = cad.GetVertexCoord(vId1);
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
                    uint lId = PCWaveguideUtils.AddRightRod(
                        cad,
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

                    if (workInputWgRodLoopIds != null)
                    {
                        workInputWgRodLoopIds.Add(lId);
                    }
                }
            }

            /////////////////////////////////////////////////////////////////////////
            // 領域のロッド
            int periodicCntInputWg1 = 1;
            int periodicCntInputWg2 = 1;
            int periodicCntX = periodicCntInputWg1 + disconRodCnt + periodicCntInputWg2;

            for (int iX = 0; iX < periodicCntX; iX++)
            {
                double centerX = periodicDistance * 0.5 + iX * periodicDistance;
                uint baseLoopId = 0;
                int inputWgNo = 0;
                if (centerX >= 0 && centerX < port1XB2)
                {
                    baseLoopId = 1;
                }
                else if (centerX >= port1XB2 && centerX < srcXB1)
                {
                    baseLoopId = 2;
                }
                else if (centerX >= srcXB1 && centerX < srcXB2)
                {
                    baseLoopId = 3;
                }
                else if (centerX >= srcXB2 && centerX < refport1XB1)
                {
                    baseLoopId = 4;
                }
                else if (centerX >= refport1XB1 && centerX < refport1XB2)
                {
                    baseLoopId = 5;
                }
                else if (centerX >= refport1XB2 && centerX < refport2XB2)
                {
                    baseLoopId = 6;
                }
                else if (centerX >= refport2XB2 && centerX < refport2XB1)
                {
                    baseLoopId = 7;
                }
                else if (centerX >= refport2XB1 && centerX < port2XB2)
                {
                    baseLoopId = 8;
                }
                else if (centerX >= port2XB2 && centerX < port2XB1)
                {
                    baseLoopId = 9;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                {
                    int index = Array.IndexOf(inputWgBaseLoopIds, baseLoopId);
                    if (index != -1)
                    {
                        inputWgNo = index + 1;
                    }
                    else
                    {
                        inputWgNo = 0;
                    }
                }

                for (int iSubX = 0; iSubX < 2; iSubX++)
                {
                    IList<double> workRodY0s = null;
                    double x0 = 0.0;
                    if (iSubX % 2 == 0)
                    {
                        workRodY0s = rodY0sX1;
                        x0 = iX * periodicDistance;

                        // 追加済みチェック
                        double th = 1.0e-12;
                        if (Math.Abs(x0 - port1XB1) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - port1XB2) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - srcXB1) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - srcXB2) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - refport1XB1) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - refport1XB2) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - refport2XB1) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - refport2XB2) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - port2XB1) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - port2XB2) < th)
                        {
                            continue;
                        }
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
                        uint lId = PCWaveguideUtils.AddRod(
                            cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                        rodLoopIds.Add(lId);
                        if (inputWgNo != 0)
                        {
                            inputWgRodLoopIdss[inputWgNo - 1].Add(lId);
                        }
                        else
                        {
                            // なにもしない
                        }
                    }
                }
            }

            // check
            {
                foreach (uint lId in rodLoopIds)
                {
                    cad.SetLoopColor(lId, new double[] { 1.0, 0.6, 0.6 });
                }
                IList<uint>[] rodEIdss = { 
                    rodEIdsPort10B1, rodEIdsPort10B2, rodEIdsPort20B1, rodEIdsPort20B2,
                    rodEIdsPort1B1, rodEIdsPort1B2, rodEIdsPort2B1, rodEIdsPort2B2,
                    rodEIdsSrcB1, rodEIdsSrcB2
                };
                foreach (var rodEIds in rodEIdss)
                {
                    foreach (uint eId in rodEIds)
                    {
                        cad.SetEdgeColor(eId, new double[] { 1.0, 0.4, 0.4 });
                    }
                }
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad2DDrawer(cad);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            Mesher2D mesher = new Mesher2D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            var meshDrawer = new Mesher2DDrawer(mesher);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 1; // スカラー
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint claddingMaId = uint.MaxValue;
            uint coreMaId = uint.MaxValue;
            {
                world.ClearMaterial();

                // 媒質リスト作成
                double claddingMu = 1.0;
                double claddingEp = 1.0;
                double coreMu = 1.0;
                double coreEp = 1.0;
                if (isAirHole)
                {
                    // 誘電体基盤 + 空孔(air hole)
                    claddingMu = 1.0;
                    claddingEp = rodEp;
                    coreMu = 1.0;
                    coreEp = 1.0;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                DielectricMaterial claddingMa = new DielectricMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = claddingMu,
                    Muyy = claddingMu,
                    Muzz = claddingMu
                };
                DielectricMaterial coreMa = new DielectricMaterial
                {
                    Epxx = coreEp,
                    Epyy = coreEp,
                    Epzz = coreEp,
                    Muxx = coreMu,
                    Muyy = coreMu,
                    Muzz = coreMu
                };

                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);

                uint[] lIds = new uint[9 + rodLoopIds.Count];
                for (int i = 0; i < 9; i++)
                {
                    lIds[i] = (uint)(i + 1);
                }
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 9] = rodLoopIds[i];
                }
                uint[] maIds = new uint[9 + rodLoopIds.Count];
                for (int i = 0; i < 9; i++)
                {
                    maIds[i] = claddingMaId;
                }
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    maIds[i + 9] = coreMaId;
                }

                for (int i = 0; i < lIds.Length; i++)
                {
                    uint lId = lIds[i];
                    uint maId = maIds[i];
                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            IList<PCWaveguidePortInfo> wgPortInfos = new List<PCWaveguidePortInfo>();
            // ポート情報リスト作成
            bool[] isPortBc2Reverse = { true, false, false, false, false };
            System.Diagnostics.Debug.Assert(isPortBc2Reverse.Length == (portCnt + refPortCnt + 1));
            for (int portId = 0; portId < (portCnt + refPortCnt + 1); portId++)
            {
                var wgPortInfo = new PCWaveguidePortInfo();
                wgPortInfos.Add(wgPortInfo);
                System.Diagnostics.Debug.Assert(wgPortInfos.Count == (portId + 1));
                //wgPortInfo.IsSVEA = true; // 緩慢変化包絡線近似
                wgPortInfo.IsSVEA = false; // Φを直接解く
                wgPortInfo.IsPortBc2Reverse = isPortBc2Reverse[portId];
                wgPortInfo.LatticeA = latticeA;
                wgPortInfo.PeriodicDistanceX = periodicDistance;
                wgPortInfo.MinEffN = minEffN;
                wgPortInfo.MaxEffN = maxEffN;
                wgPortInfo.MinWaveNum = minWaveNum;
                wgPortInfo.MaxWaveNum = maxWaveNum;
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // 周期構造入出力導波路
            for (int portId = 0; portId < (portCnt + refPortCnt + 1); portId++)
            {
                uint lId0 = inputWgBaseLoopIds[portId];
                var inputWgRodLoopIds = inputWgRodLoopIdss[portId];
                // ワールド座標系のループIDを取得
                uint[] lIds = null;

                lIds = new uint[1 + inputWgRodLoopIds.Count];
                lIds[0] = lId0;
                for (int i = 0; i < inputWgRodLoopIds.Count; i++)
                {
                    lIds[i + 1] = inputWgRodLoopIds[i];
                }

                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.LoopIds = new List<uint>(lIds);
            }

            // 周期構造境界1
            int rodCntPort10B1 = rodEIdsPort10B1.Count / 2;
            int divCntPort10B1 = 3 * rodCntPort10B1 + 1;
            int rodCntPort20B1 = rodEIdsPort20B1.Count / 2;
            int divCntPort20B1 = 3 * rodCntPort20B1 + 1;
            int rodCntPort1B1 = rodEIdsPort1B1.Count / 2;
            int divCntPort1B1 = 3 * rodCntPort1B1 + 1;
            int rodCntPort2B1 = rodEIdsPort2B1.Count / 2;
            int divCntPort2B1 = 3 * rodCntPort2B1 + 1;
            int rodCntSrcB1 = rodEIdsSrcB1.Count / 2;
            int divCntSrcB1 = 3 * rodCntSrcB1 + 1;
            int[] divCntsB1 = { divCntPort10B1, divCntPort20B1, divCntPort1B1, divCntPort2B1, divCntSrcB1 };
            IList<uint>[] rodEIdssB1 = {
                rodEIdsPort10B1, rodEIdsPort20B1, rodEIdsPort1B1, rodEIdsPort2B1, rodEIdsSrcB1
            };
            uint[] eIds0B1 = { 1, 11, 24, 27, 22 };
            for (int portId = 0; portId < (portCnt + refPortCnt + 1); portId++)
            {
                int divCnt = divCntsB1[portId];
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                IList<uint> workrodEIdsB = rodEIdssB1[portId];
                uint eId0 = eIds0B1[portId];

                eIds[0] = eId0;

                if (workrodEIdsB.Contains(eIds[0]))
                {
                    maIds[0] = coreMaId;
                }
                else
                {
                    maIds[0] = claddingMaId;
                }

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    uint eId = 0;
                    eId = 28;
                    for (int p = 0; p <= (portId - 1); p++)
                    {
                        eId += (uint)((divCntsB1[p] - 1) * 2);
                    }
                    eId += (uint)((divCntsB1[portId] - 1) - (i - 1));
                    eIds[i] = eId;

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
                wgPortInfo.BcEdgeIds1 = new List<uint>(eIds);
            }
            // 周期構造境界2
            IList<uint>[] rodEIdssB2 = { 
                rodEIdsPort10B2, rodEIdsPort20B2, rodEIdsPort1B2, rodEIdsPort2B2, rodEIdsSrcB2
            };
            uint[] eIds0B2 = { 21, 28, 25, 26, 23 };
            for (int portId = 0; portId < (portCnt + refPortCnt + 1); portId++)
            {
                int divCnt = divCntsB1[portId];
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                IList<uint> workrodEIdsB = rodEIdssB2[portId];
                uint eId0 = eIds0B2[portId];

                eIds[0] = eId0;

                if (workrodEIdsB.Contains(eIds[0]))
                {
                    maIds[0] = coreMaId;
                }
                else
                {
                    maIds[0] = claddingMaId;
                }

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    uint eId = 0;
                    eId = 28;
                    for (int p = 0; p <= (portId - 1); p++)
                    {
                        eId += (uint)((divCntsB1[p] - 1) * 2);
                    }
                    eId += (uint)((divCntsB1[portId] - 1) * 2 - (i - 1));
                    eIds[i] = eId;

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

            {
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                for (int portId = 0; portId < (portCnt + refPortCnt + 1); portId++)
                {
                    /*
                    if (portId < portCnt)
                    {
                        // ABCは通常の導波路として扱う
                        PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                        IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                        PortCondition portCondition = new PortCondition(bcEIds1, CadElementType.Edge, FieldValueType.Scalar);
                        portConditions.Add(portCondition);
                        wgPortInfos[portId] = null; // クリアする
                    }
                    else
                    */
                    {
                        // 周期構造導波路
                        PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                        IList<uint> lIds = wgPortInfo.LoopIds;
                        IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                        IList<uint> bcEIds2 = wgPortInfo.BcEdgeIds2;
                        PortCondition portCondition = new PortCondition(
                            CadElementType.Edge,
                            lIds, bcEIds1, bcEIds2, FieldValueType.Scalar, new List<uint> { 0 }, 0);
                        portConditions.Add(portCondition);
                    }
                }

            }

            /*
            // 強制境界
            // TMモードの場合磁気壁
            uint[] zeroEIds = {};
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }
            */

            world.MakeElements();

            // ポートの境界上の節点を準備する
            for (int portId = 0; portId < (portCnt + refPortCnt + 1); portId++)
            {
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                if (wgPortInfo == null)
                {
                    continue;
                }
                wgPortInfo.SetupAfterMakeElements(world, quantityId, (uint)portId);
            }
            // フォトニック結晶導波路チャンネル上節点を取得する
            for (int portId = 0; portId < (portCnt + refPortCnt + 1); portId++)
            {
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                if (wgPortInfo == null)
                {
                    continue;
                }
                wgPortInfo.PCChannelCoIds = new List<IList<int>>();

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
                        double topRodY = waveguideWidth - electricWallDelta;
                        double defectY1 =
                            topRodY - rodDistanceY * (rodCntHalf - 1) - rodDistanceY * (defectRodCnt + 1) - rodRadius;
                        double defectY2 = topRodY - rodDistanceY * (rodCntHalf - 1) + rodRadius;
                        // air hole
                        if (y >= defectY1 && y <= defectY2)
                        {
                            channelCoIds.Add(coId);
                        }
                    }
                }
            }

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // スカラー
                valueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            var FEM = new PCWaveguide2DModalABCZTDFEM(world);
            FEM.TimeLoopCnt = timeLoopCnt;
            FEM.TimeIndex = 0;
            FEM.TimeStep = timeStep;
            FEM.GaussianType = gaussianType;
            FEM.GaussianT0 = gaussianT0;
            FEM.GaussianTp = gaussianTp;
            FEM.SrcFrequency = srcFreq;
            FEM.StartFrequencyForSMatrix = freq1;
            FEM.EndFrequencyForSMatrix = freq2;
            // 観測点（参照ポート数）
            FEM.RefPortCount = refPortCnt;
            FEM.IsTMMode = isTMMode;
            FEM.WgPortInfos = wgPortInfos;

            {
                var solver = new IvyFEM.Linear.LapackEquationSolver();
                //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                solver.IsOrderingToBandMatrix = true;
                solver.IsRepeatSolve = true;
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
                //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                //FEM.Solver = solver;
            }


            if (ChartWindow2 == null)
            {
                ChartWindow2 = new ChartWindow();
                ChartWindow2.Closing += ChartWindow2_Closing;
            }
            {
                ChartWindow chartWin = ChartWindow2;
                chartWin.Owner = mainWindow;
                chartWin.Left = mainWindow.Left + mainWindow.Width;
                chartWin.Top = mainWindow.Top;
                chartWin.Show();
                chartWin.TextBox1.Text = "";
                var model = new PlotModel();
                chartWin.Plot.Model = model;
                model.Title = "ez(t): Time Domain";
                var axis1 = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "t",
                    //Minimum = 0,
                    //Maximum = timeLoopCnt
                };
                var axis2 = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "ez(t)"
                };
                model.Axes.Add(axis1);
                model.Axes.Add(axis2);
                var series1 = new LineSeries
                {
                    Title = "Port1"
                };
                var series2 = new LineSeries
                {
                    Title = "Port2"
                };
                model.Series.Add(series1);
                model.Series.Add(series2);
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();
            }

            for (int iTime = 0; iTime < timeLoopCnt; iTime++)
            {
                // 解く
                FEM.Solve();
                // 時間領域のEz
                double[] Ez = FEM.Ez;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, Ez);

                {
                    int timeIndex = FEM.TimeIndex;
                    int nodeCntB1 = FEM.RefTimeEzsss[0][timeIndex].Length;
                    int refNodeIdB1 = nodeCntB1 / 2;
                    int nodeCntB2 = FEM.RefTimeEzsss[1][timeIndex].Length;
                    int refNodeIdB2 = nodeCntB2 / 2;
                    double ezPort1 = FEM.RefTimeEzsss[0][timeIndex][refNodeIdB1];
                    double ezPort2 = FEM.RefTimeEzsss[1][timeIndex][refNodeIdB2];
                    var chartWin = ChartWindow2;
                    var model = chartWin.Plot.Model;
                    var series = model.Series;
                    var series1 = series[0] as LineSeries;
                    var series2 = series[1] as LineSeries;
                    series1.Points.Add(new DataPoint(timeIndex, ezPort1));
                    series2.Points.Add(new DataPoint(timeIndex, ezPort2));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                FEM.TimeIndex++;
            }

            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closing += ChartWindow1_Closing;
            }
            {
                ChartWindow chartWin = ChartWindow1;
                chartWin.Owner = mainWindow;
                chartWin.Left = mainWindow.Left + mainWindow.Width;
                chartWin.Top = mainWindow.Top + ChartWindow2.Height;
                chartWin.Show();
                chartWin.TextBox1.Text = "";
                var model = new PlotModel();
                chartWin.Plot.Model = model;
                model.Title = "Waveguide Example";
                var axis1 = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "a/λ",
                    Minimum = normalizedFreq1,
                    Maximum = normalizedFreq2
                };
                var axis2 = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "|S|",
                    Minimum = 0.0,
                    //Maximum = 1.0
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
            }

            // S11、S21の周波数特性
            double[] freqs;
            System.Numerics.Complex[] _freqDomainAmpsInc = null; // 直線導波路の場合
            IList<System.Numerics.Complex[]> freqDomainAmpss;
            IList<System.Numerics.Complex[]> Sss;
            FEM.CalcSParameter(_freqDomainAmpsInc, out freqs, out freqDomainAmpss, out Sss);
            int freqCnt = freqs.Length;
            for (int iFreq = 0; iFreq < freqCnt; iFreq++)
            {
                // 周波数
                double freq = freqs[iFreq];
                // 波長
                double waveLength = Constants.C0 / freq;
                // 規格化周波数
                double normalizedFreq = toNormalizedFreq(waveLength);
                if (normalizedFreq < normalizedFreq1)
                {
                    continue;
                }
                if (normalizedFreq > normalizedFreq2)
                {
                    break;
                }
                // S
                System.Numerics.Complex S11 = Sss[0][iFreq];
                System.Numerics.Complex S21 = Sss[1][iFreq];
                double S11Abs = S11.Magnitude;
                double S21Abs = S21.Magnitude;
                double total = S11Abs * S11Abs + S21Abs * S21Abs;

                string ret;
                string CRLF = System.Environment.NewLine;
                ret = "a/λ: " + normalizedFreq + CRLF;
                ret += "|S11| = " + S11Abs + CRLF +
                      "|S21| = " + S21Abs + CRLF +
                      "|S11|^2 + |S21|^2 = " + total + CRLF;
                System.Diagnostics.Debug.WriteLine(ret);
                //AlertWindow.ShowDialog(ret, "");
                var chartWin = ChartWindow1;
                var model = chartWin.Plot.Model;
                var series = model.Series;
                var series1 = series[0] as LineSeries;
                var series2 = series[1] as LineSeries;
                series1.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                series2.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();
            }

            // 他の導波路解析のSマトリクス計算時の入射波に利用
            retFreqs = freqs.ToArray();
            retFreqDomainAmpsInc = freqDomainAmpss[0].ToArray();
        }
    }
}
