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
        public void PCWaveguidePMLTDTriangleLatticeProblem1(MainWindow mainWindow)
        {
            // 直線導波路を解く
            double[] freqs;
            System.Numerics.Complex[] freqDomainAmpsInc;
            SolvePCWaveguidePMLTDTriangleLatticeProblem1_0(
                mainWindow, out freqs, out freqDomainAmpsInc);

            WPFUtils.DoEvents(10 * 1000);

            // 対象導波路を解く(直線導波路と同じ導波路、同じ計算条件である必要がある)
            SolvePCWaveguidePMLTDTriangleLatticeProblem1(mainWindow, freqDomainAmpsInc);
        }

        public void SolvePCWaveguidePMLTDTriangleLatticeProblem1(
            MainWindow mainWindow, System.Numerics.Complex[] freqDomainAmpsInc)
        {
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
            double timeDelta = courantNumber * eLen / (Constants.C0 * Math.Sqrt(2.0));
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

            System.Diagnostics.Debug.Assert(Math.Abs(latticeTheta - 60.0) < Constants.PrecisionLowerLimit);
            // 形状設定で使用する単位長さ
            double unitLen = periodicDistance;
            // PML層の厚さ
            int pmlRodCnt = 6;
            double pmlThickness = pmlRodCnt * unitLen;
            //int pmlWgRodCnt = 0;
            int pmlWgRodCnt = 0;
            double pmlWgRodThickness = pmlWgRodCnt * unitLen;
            // 導波路２の三角形格子角度
            double latticeThetaPort2 = latticeTheta;
            // 導波路２のロッドの数（半分）
            int rodCntHalfPort2 = rodCntHalf;
            // 導波路２の欠陥ロッド数
            int defectRodCntPort2 = defectRodCnt;
            // 導波路２の格子数
            int latticeCntPort2 = rodCntHalfPort2 * 2 + defectRodCntPort2;
            // 導波路２の幅
            double waveguideWidth2 = rodDistanceY * (latticeCntPort2 - 1) + 2.0 * (rodRadius + electricWallDistance);
            System.Diagnostics.Debug.Assert(Math.Abs(waveguideWidth2 - waveguideWidth) < 1.0e-12);
            // 導波路２の１格子当たりの分割数
            int divCntForOneLatticePort2 = divCntForOneLattice;

            // 観測ポート数
            int refPortCnt = 2;
            double port1PMLXB1 = 0;
            double port1PMLXB2 = pmlThickness;
            // 励振位置
            double srcXB1 = port1PMLXB2 + 1 * unitLen;
            double srcXB2 = srcXB1 + periodicDistance;
            // 観測点
            int port1OfsX = 1;
            int port2OfsX = 1;
            // ポート1
            double refport1XB1 = srcXB2 + port1OfsX * unitLen;
            double refport1XB2 = refport1XB1 + periodicDistance;
            // 入出力導入部の距離
            //int rodCntIntroPort1 = 5;
            //int rodCntIntroPort2 = 3;
            int rodCntIntroPort1 = 5;
            int rodCntIntroPort2 = 3;
            double introLength1 = rodDistanceX * rodCntIntroPort1;
            double introLength2 = rodDistanceX * rodCntIntroPort2;
            // 周期構造をポート1とポート2で一致させるための調整
            introLength1 += 0.5 * rodDistanceX;
            introLength2 += 0.5 * rodDistanceX;
            double barintroLength1 = introLength1 - electricWallDelta / Math.Sqrt(3.0);
            double barintroLength2 = introLength2 - electricWallDelta / Math.Sqrt(3.0);

            // ベンド下側角
            double bendX10 = pmlThickness + introLength1 +
                (waveguideWidth - 2.0 * electricWallDelta) / Math.Sqrt(3.0);
            double bendY10 = 0.0 + electricWallDelta;
            double bendX1 = bendX10 + electricWallDelta / Math.Sqrt(3.0);
            double bendY1 = 0.0;
            // ベンド部と出力部の境界
            double bendX20 = bendX10 + (waveguideWidth2 - 2.0 * electricWallDelta) / (2.0 * Math.Sqrt(3.0));
            double bendY20 = bendY10 + (waveguideWidth2 - 2.0 * electricWallDelta) / 2.0;
            // 出力部内部境界の下側（終点）
            double port2PMLX2B20 = bendX20 + introLength2 / 2.0;
            double port2PMLY2B20 = bendY20 + introLength2 * Math.Sqrt(3.0) / 2.0;
            double port2PMLX2B2 = port2PMLX2B20 + electricWallDelta * Math.Sqrt(3.0) / 2.0;
            double port2PMLY2B2 = port2PMLY2B20 - electricWallDelta / 2.0;
            // 出力部境界の終点
            double port2PMLX2B1 = port2PMLX2B2 + pmlThickness / 2.0;
            double port2PMLY2B1 = port2PMLY2B2 + pmlThickness * Math.Sqrt(3.0) / 2.0;
            // ベンド部上側角
            double bendX30 = pmlThickness + introLength1;
            double bendY30 = waveguideWidth - electricWallDelta;
            double bendX3 = bendX30 - electricWallDelta / Math.Sqrt(3.0);
            double bendY3 = waveguideWidth;
            // 出力部内部境界の上側（始点）
            double port2PMLX1B20 = bendX30 + introLength2 / 2.0;
            double port2PMLY1B20 = bendY30 + introLength2 * Math.Sqrt(3.0) / 2.0;
            double port2PMLX1B2 = port2PMLX1B20 - electricWallDelta * Math.Sqrt(3.0) / 2.0;
            double port2PMLY1B2 = port2PMLY1B20 + electricWallDelta / 2.0;
            // 出力部境界の始点
            double port2PMLX1B1 = port2PMLX1B2 + pmlThickness / 2.0;
            double port2PMLY1B1 = port2PMLY1B2 + pmlThickness * Math.Sqrt(3.0) / 2.0;
            // ポート2の下側
            double refport2X2B1 = port2PMLX2B2 - port2OfsX * unitLen / 2.0;
            double refport2Y2B1 = port2PMLY2B2 - port2OfsX * unitLen * Math.Sqrt(3.0) / 2.0;
            double refport2X2B2 = refport2X2B1 - periodicDistance / 2.0;
            double refport2Y2B2 = refport2Y2B1 - periodicDistance * Math.Sqrt(3.0) / 2.0;
            // ポート2の上側
            double refport2X1B1 = port2PMLX1B2 - port2OfsX * unitLen / 2.0;
            double refport2Y1B1 = port2PMLY1B2 - port2OfsX * unitLen * Math.Sqrt(3.0) / 2.0;
            double refport2X1B2 = refport2X1B1 - periodicDistance / 2.0;
            double refport2Y1B2 = refport2Y1B1 - periodicDistance * Math.Sqrt(3.0) / 2.0;

            // check
            {
                OpenTK.Vector2d port2PMLB1Pt1 = new OpenTK.Vector2d(port2PMLX1B1, port2PMLY1B1);
                OpenTK.Vector2d port2PMLB1Pt2 = new OpenTK.Vector2d(port2PMLX2B1, port2PMLY2B1);
                double distancePort2PMLB1 = OpenTK.Vector2d.Distance(port2PMLB1Pt1, port2PMLB1Pt2);
                System.Diagnostics.Debug.Assert(
                    Math.Abs(distancePort2PMLB1 - waveguideWidth2) < 1.0e-12);
                OpenTK.Vector2d port2PMLB2Pt1 = new OpenTK.Vector2d(port2PMLX1B2, port2PMLY1B2);
                OpenTK.Vector2d port2PMLB2Pt2 = new OpenTK.Vector2d(port2PMLX2B2, port2PMLY2B2);
                double distancePort2PMLB2 = OpenTK.Vector2d.Distance(port2PMLB2Pt1, port2PMLB2Pt2);
                System.Diagnostics.Debug.Assert(
                    Math.Abs(distancePort2PMLB2 - waveguideWidth2) < 1.0e-12);
                OpenTK.Vector2d refport2B1Pt1 = new OpenTK.Vector2d(refport2X1B1, refport2Y1B1);
                OpenTK.Vector2d refport2B1Pt2 = new OpenTK.Vector2d(refport2X2B1, refport2Y2B1);
                double distanceRefport2B1 = OpenTK.Vector2d.Distance(refport2B1Pt1, refport2B1Pt2);
                System.Diagnostics.Debug.Assert(
                    Math.Abs(distanceRefport2B1 - waveguideWidth2) < 1.0e-12);
                OpenTK.Vector2d refport2B2Pt1 = new OpenTK.Vector2d(refport2X1B2, refport2Y1B2);
                OpenTK.Vector2d refport2B2Pt2 = new OpenTK.Vector2d(refport2X2B2, refport2Y2B2);
                double distanceRefport2B2 = OpenTK.Vector2d.Distance(refport2B2Pt1, refport2B2Pt2);
                System.Diagnostics.Debug.Assert(
                    Math.Abs(distanceRefport2B2 - waveguideWidth2) < 1.0e-12);
            }

            IList<uint> rodLoopIds = new List<uint>();
            IList<uint> pmlRodLoopIdsPort1 = new List<uint>();
            IList<uint> inputWgRodLoopIdsSrc = new List<uint>();
            IList<uint> inputWgRodLoopIdsPort1 = new List<uint>();
            IList<uint> inputWgRodLoopIdsPort2 = new List<uint>();
            IList<uint> pmlRodLoopIdsPort2 = new List<uint>();
            IList<uint>[] inputWgRodLoopIdss = {
                pmlRodLoopIdsPort1, pmlRodLoopIdsPort2,
                inputWgRodLoopIdsPort1, inputWgRodLoopIdsPort2,
                inputWgRodLoopIdsSrc
            };
            uint[] inputWgBaseLoopIds = { 1, 9, 5, 7, 3 };
            IList<uint> pmlLIds1 = new List<uint>();
            IList<uint> pmlLIds2 = new List<uint>();
            uint[] pmlBaseLoopIds = { 1, 9 };
            IList<uint>[] pmlLIdss = { pmlLIds1, pmlLIds2 };
            IList<uint>[] pmlRodLIdss = { pmlRodLoopIdsPort1, pmlRodLoopIdsPort2 };
            uint[] portInputWgBaseLoopIds = {
                inputWgBaseLoopIds[2], inputWgBaseLoopIds[3], inputWgBaseLoopIds[4] };
            IList<uint>[] portInputWgRodLoopIdss = {
                inputWgRodLoopIdss[2], inputWgRodLoopIdss[3], inputWgRodLoopIdss[4] };
            IList<uint> rodEIdsPort1PMLB1 = new List<uint>();
            IList<uint> rodEIdsPort1PMLB2 = new List<uint>();
            IList<uint> rodEIdsPort2PMLB1 = new List<uint>();
            IList<uint> rodEIdsPort2PMLB2 = new List<uint>();
            IList<uint> rodEIdsPort1B1 = new List<uint>();
            IList<uint> rodEIdsPort1B2 = new List<uint>();
            IList<uint> rodEIdsPort2B1 = new List<uint>();
            IList<uint> rodEIdsPort2B2 = new List<uint>();
            IList<uint> rodEIdsSrcB1 = new List<uint>();
            IList<uint> rodEIdsSrcB2 = new List<uint>();
            IList<uint> rodVIdsPort1PMLB1 = new List<uint>();
            IList<uint> rodVIdsPort1PMLB2 = new List<uint>();
            IList<uint> rodVIdsPort2PMLB1 = new List<uint>();
            IList<uint> rodVIdsPort2PMLB2 = new List<uint>();
            IList<uint> rodVIdsPort1B1 = new List<uint>();
            IList<uint> rodVIdsPort1B2 = new List<uint>();
            IList<uint> rodVIdsPort2B1 = new List<uint>();
            IList<uint> rodVIdsPort2B2 = new List<uint>();
            IList<uint> rodVIdsSrcB1 = new List<uint>();
            IList<uint> rodVIdsSrcB2 = new List<uint>();

            CadObject2D cad = new CadObject2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PMLXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(srcXB1, 0.0));
                pts.Add(new OpenTK.Vector2d(srcXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(refport1XB1, 0.0));
                pts.Add(new OpenTK.Vector2d(refport1XB2, 0.0));
                pts.Add(new OpenTK.Vector2d(bendX1, bendY1));
                pts.Add(new OpenTK.Vector2d(refport2X2B2, refport2Y2B2));
                pts.Add(new OpenTK.Vector2d(refport2X2B1, refport2Y2B1));
                pts.Add(new OpenTK.Vector2d(port2PMLX2B2, port2PMLY2B2));
                pts.Add(new OpenTK.Vector2d(port2PMLX2B1, port2PMLY2B1));
                pts.Add(new OpenTK.Vector2d(port2PMLX1B1, port2PMLY1B1));
                pts.Add(new OpenTK.Vector2d(port2PMLX1B2, port2PMLY1B2));
                pts.Add(new OpenTK.Vector2d(refport2X1B1, refport2Y1B1));
                pts.Add(new OpenTK.Vector2d(refport2X1B2, refport2Y1B2));
                pts.Add(new OpenTK.Vector2d(bendX3, bendY3));
                pts.Add(new OpenTK.Vector2d(refport1XB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(refport1XB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(srcXB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(srcXB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1PMLXB2, waveguideWidth));
                uint _lId1 = cad.AddPolygon(pts).AddLId;
                uint _lId2 = cad.ConnectVertexLine(3, 22).AddLId;
                uint _lId3 = cad.ConnectVertexLine(4, 21).AddLId;
                uint _lId4 = cad.ConnectVertexLine(5, 20).AddLId;
                uint _lId5 = cad.ConnectVertexLine(6, 19).AddLId;
                uint _lId6 = cad.ConnectVertexLine(7, 18).AddLId;
                uint _lId7 = cad.ConnectVertexLine(9, 16).AddLId;
                uint _lId8 = cad.ConnectVertexLine(10, 15).AddLId;
                uint _lId9 = cad.ConnectVertexLine(11, 14).AddLId;
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
                1, 23, 12, 30,
                26, 27, 29, 28,
                24, 25
            };
            IList<uint>[] rodVIdsB = {
                rodVIdsPort1PMLB1, rodVIdsPort1PMLB2, rodVIdsPort2PMLB1, rodVIdsPort2PMLB2,
                rodVIdsPort1B1, rodVIdsPort1B2, rodVIdsPort2B1, rodVIdsPort2B2,
                rodVIdsSrcB1, rodVIdsSrcB2
            };
            IList<uint>[] rodEIdsB = {
                rodEIdsPort1PMLB1, rodEIdsPort1PMLB2, rodEIdsPort2PMLB1, rodEIdsPort2PMLB2,
                rodEIdsPort1B1, rodEIdsPort1B2, rodEIdsPort2B1, rodEIdsPort2B2,
                rodEIdsSrcB1, rodEIdsSrcB2
            };
            double[] abovePtXsB = {
                port1PMLXB1, port1PMLXB2,
                port2PMLX1B1, port2PMLX1B2,
                refport1XB1, refport1XB2,
                refport2X1B1, refport2X1B2,
                srcXB1, srcXB2
            };
            double[] abovePtYsB = {
                waveguideWidth, waveguideWidth,
                port2PMLY1B1, port2PMLY1B2,
                waveguideWidth, waveguideWidth,
                refport2Y1B1, refport2Y1B2,
                waveguideWidth, waveguideWidth
            };
            double[] belowPtXsB = {
                port1PMLXB1, port1PMLXB2,
                port2PMLX2B1, port2PMLX2B2,
                refport1XB1, refport1XB2,
                refport2X2B1, refport2X2B2,
                srcXB1, srcXB2
            };
            double[] belowPtYsB = {
                0.0, 0.0,
                port2PMLY2B1, port2PMLY2B2,
                0.0, 0.0,
                refport2Y2B1, refport2Y2B2,
                0.0, 0.0
            };
            for (int boundaryIndex = 0; boundaryIndex < eIdsB.Length; boundaryIndex++)
            {
                uint eId = eIdsB[boundaryIndex];
                double aPtXB = abovePtXsB[boundaryIndex];
                double aPtYB = abovePtYsB[boundaryIndex];
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
                    else if (boundaryIndex == 1 ||
                        boundaryIndex == 4 || boundaryIndex == 5 ||
                        boundaryIndex == 8 || boundaryIndex == 9)
                    {
                        // 上から順に追加
                        ptY0 = rodY0sX1[i];
                        ptY1 = ptY0 + rodRadius;
                        ptY2 = ptY0 - rodRadius;
                        ptX0 = bPtXB;
                        ptX1 = bPtXB;
                        ptX2 = bPtXB;
                    }
                    else if (boundaryIndex == 2 || boundaryIndex == 3 ||
                        boundaryIndex == 6 || boundaryIndex == 7)
                    {
                        // 上から順に追加
                        double basePtY0 = rodY0sX1[i];
                        double basePtY1 = basePtY0 + rodRadius;
                        double basePtY2 = basePtY0 - rodRadius;

                        ptX0 = bPtXB + (aPtXB - bPtXB) * (basePtY0 - 0.0) / waveguideWidth2;
                        ptY0 = bPtYB + (aPtYB - bPtYB) * (basePtY0 - 0.0) / waveguideWidth2;
                        ptX1 = bPtXB + (aPtXB - bPtXB) * (basePtY1 - 0.0) / waveguideWidth2;
                        ptY1 = bPtYB + (aPtYB - bPtYB) * (basePtY1 - 0.0) / waveguideWidth2;
                        ptX2 = bPtXB + (aPtXB - bPtXB) * (basePtY2 - 0.0) / waveguideWidth2;
                        ptY2 = bPtYB + (aPtYB - bPtYB) * (basePtY2 - 0.0) / waveguideWidth2;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    double[] ptXs = { ptX1, ptX0, ptX2 };
                    double[] ptYs = { ptY1, ptY0, ptY2 };
                    for (int iY = 0; iY < ptYs.Length; iY++)
                    {
                        var res = cad.AddVertex(
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
                if (index == 0)
                {
                    // 左端は追加しない
                    continue;
                }
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
                    uint lId = 0;
                    if (index == 2 || index == 5 || index == 6)
                    {
                        double startAngle = (180.0 - 30.0);
                        lId = PCWaveguideUtils.AddExactlyHalfRod(
                            cad,
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
                if (index == 1)
                {
                    // 右端は追加しない
                    continue;
                }
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
                    uint lId;
                    if (index == 1 || index == 2 || index == 5 || index == 6)
                    {
                        double startAngle = (0.0 - 30.0);
                        lId = PCWaveguideUtils.AddExactlyHalfRod(
                            cad,
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
            int periodicCntPML1 = pmlRodCnt;
            int periodicCntX1 = periodicCntPML1 + rodCntIntroPort1;
            double introPlusBend1 = bendX1;
            int periodicCntXBend1 =
                (int)((introPlusBend1 - periodicDistance * periodicCntX1) / periodicDistance) + 1;
            for (int iX = 0; iX < (periodicCntX1 + periodicCntXBend1); iX++)
            {
                double centerX = periodicDistance * 0.5 + iX * periodicDistance;
                uint baseLoopId = 0;
                int inputWgNo = 0;
                bool isBend = false;
                if (centerX >= 0 && centerX < port1PMLXB2)
                {
                    baseLoopId = 1;
                    if (centerX < (port1PMLXB2 - pmlWgRodThickness))
                    {
                        continue;
                    }
                }
                else if (centerX >= port1PMLXB2 && centerX < srcXB1)
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
                else if (centerX >= refport1XB2 && centerX < (pmlThickness + barintroLength1))
                {
                    baseLoopId = 6;
                }
                else if (centerX >= (pmlThickness + barintroLength1) &&
                    centerX < (periodicCntX1 + periodicCntXBend1) * periodicDistance)
                {
                    baseLoopId = 6;
                    isBend = true;
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
                        if (Math.Abs(x0 - port1PMLXB1) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - port1PMLXB2) < th)
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
                            if (x0 - (pmlThickness + barintroLength1) < waveguideWidth / Math.Sqrt(3.0))
                            {
                                double bendCenterY = bendY3 + (bendY1 - bendY3) *
                                    (x0 - (pmlThickness + barintroLength1)) /
                                    (waveguideWidth / Math.Sqrt(3.0));
                                if (y0 >= (bendCenterY + 0.1 * rodRadius))
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

            // ポート2側の内部領域
            int periodicCntPML2 = pmlRodCnt;
            int periodicCntX2 = periodicCntPML2 + rodCntIntroPort2;
            double introPlusBend2 =
                Math.Sqrt((port2PMLX2B1 - bendX1) * (port2PMLX2B1 - bendX1) +
                (port2PMLY2B1 - bendY1) * (port2PMLY2B1 - bendY1));
            int periodicCntXBend2 =
                (int)((introPlusBend2 - periodicDistance * periodicCntX2) / periodicDistance) + 1;
            double port2AboveX1 = port2PMLX1B1;
            double port2AboveY1 = port2PMLY1B1;
            double port2BelowX1 = port2PMLX2B1;
            double port2BelowY1 = port2PMLY2B1;
            double port2AboveX2 = port2AboveX1 - (pmlThickness + introLength2) / 2.0;
            double port2AboveY2 = port2AboveY1 - (pmlThickness + introLength2) * Math.Sqrt(3.0) / 2.0;
            double port2BelowX2 = port2BelowX1 - (pmlThickness + introLength2) / 2.0;
            double port2BelowY2 = port2BelowY1 - (pmlThickness + introLength2) * Math.Sqrt(3.0) / 2.0;
            double port2BaseXB1 = 0.0;
            double port2BaseXB2 = OpenTK.Vector2d.Distance(
                new OpenTK.Vector2d(port2PMLX2B1, port2PMLY2B1),
                new OpenTK.Vector2d(port2PMLX2B2, port2PMLY2B2));
            double refport2BaseXB1 = OpenTK.Vector2d.Distance(
                new OpenTK.Vector2d(port2PMLX2B1, port2PMLY2B1),
                new OpenTK.Vector2d(refport2X2B1, refport2Y2B1));
            double refport2BaseXB2 = OpenTK.Vector2d.Distance(
                new OpenTK.Vector2d(port2PMLX2B1, port2PMLY2B1),
                new OpenTK.Vector2d(refport2X2B2, refport2Y2B2));
            for (int iX = 0; iX < (periodicCntX2 + periodicCntXBend2); iX++)
            {
                double baseCenterX = periodicDistance * 0.5 + iX * periodicDistance;
                uint baseLoopId = 0;
                int inputWgNo = 0;
                bool isBend = false;
                if (baseCenterX >= 0 && baseCenterX < port2BaseXB2)
                {
                    baseLoopId = 9;
                    if (baseCenterX < (pmlThickness - pmlWgRodThickness))
                    {
                        continue;
                    }
                }
                else if (baseCenterX >= port2BaseXB2 && baseCenterX < refport2BaseXB1)
                {
                    baseLoopId = 8;
                }
                else if (baseCenterX >= refport2BaseXB1 && baseCenterX < refport2BaseXB2)
                {
                    baseLoopId = 7;
                }
                else if (baseCenterX >= refport2BaseXB2 && baseCenterX < (pmlThickness + barintroLength2))
                {
                    baseLoopId = 6;
                }
                else if (baseCenterX >= (pmlThickness + barintroLength2) &&
                    baseCenterX < (periodicCntX2 + periodicCntXBend2) * periodicDistance)
                {
                    baseLoopId = 6;
                    isBend = true;
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
                    double baseX0 = 0.0;
                    if (iSubX % 2 == 0)
                    {
                        workRodY0s = rodY0sX1;
                        baseX0 = iX * periodicDistance;

                        // 追加済みチェック
                        double th = 1.0e-12;
                        if (Math.Abs(baseX0 - port2BaseXB1) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(baseX0 - port2BaseXB2) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(baseX0 - refport2BaseXB1) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(baseX0 - refport2BaseXB2) < th)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        workRodY0s = rodY0sX2;
                        baseX0 = iX * periodicDistance + 0.5 * periodicDistance;
                    }
                    double aX = port2AboveX1 +
                        (port2AboveX2 - port2AboveX1) * (baseX0 - 0.0) / (pmlThickness + introLength2);
                    double aY = port2AboveY1 +
                        (port2AboveY2 - port2AboveY1) * (baseX0 - 0.0) / (pmlThickness + introLength2);
                    double bX = port2BelowX1 +
                        (port2BelowX2 - port2BelowX1) * (baseX0 - 0.0) / (pmlThickness + introLength2);
                    double bY = port2BelowY1 +
                        (port2BelowY2 - port2BelowY1) * (baseX0 - 0.0) / (pmlThickness + introLength2);
                    {
                        // check
                        OpenTK.Vector2d a1 = new OpenTK.Vector2d(port2AboveX1, port2AboveY1);
                        OpenTK.Vector2d a2 = new OpenTK.Vector2d(port2AboveX2, port2AboveY2);
                        OpenTK.Vector2d b1 = new OpenTK.Vector2d(port2BelowX1, port2BelowY1);
                        OpenTK.Vector2d b2 = new OpenTK.Vector2d(port2BelowX2, port2BelowY2);
                        double uDistance = OpenTK.Vector2d.Distance(a1, a2);
                        double bDistance = OpenTK.Vector2d.Distance(b1, b2);
                        System.Diagnostics.Debug.Assert(
                            Math.Abs(uDistance - (pmlThickness + introLength2)) < 1.0e-12);
                        System.Diagnostics.Debug.Assert(
                            Math.Abs(bDistance - (pmlThickness + introLength2)) < 1.0e-12);
                    }
                    int rodCnt = workRodY0s.Count;
                    for (int i = 0; i < rodCnt; i++)
                    {
                        double baseY0 = workRodY0s[i];

                        if (isBend)
                        {
                            if (baseX0 - (pmlThickness + barintroLength2) < waveguideWidth2 / Math.Sqrt(3.0))
                            {
                                double baseBendCenterY = bendY3 + (bendY1 - bendY3) *
                                    (baseX0 - (pmlThickness + barintroLength2)) /
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

                        double x0 = bX + (aX - bX) * (baseY0 - 0.0) / waveguideWidth2;
                        double y0 = bY + (aY - bY) * (baseY0 - 0.0) / waveguideWidth2;
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

            // pml
            for (int pmlIndex = 0; pmlIndex < pmlLIdss.Length; pmlIndex++)
            {
                IList<uint> pmlLIds = pmlLIdss[pmlIndex];
                uint lId0 = pmlBaseLoopIds[pmlIndex];
                pmlLIds.Add(lId0);

                IList<uint> workrodLIds = pmlRodLIdss[pmlIndex];
                foreach (uint lId in workrodLIds)
                {
                    pmlLIds.Add(lId);
                }
            }

            // check
            {
                double[] rodColor = { 1.0, 0.6, 0.6 };
                double[] pmlColor = { 0.5, 0.5, 0.5 };
                double[] pmlCoreColor = { 0.6, 0.3, 0.3 };
                for (int iL = 0; iL < (9 + rodLoopIds.Count); iL++)
                {
                    uint lId = (uint)(iL + 1);
                    int hitPMLIndex = -1;
                    for (int pmlIndex = 0; pmlIndex < pmlLIdss.Length; pmlIndex++)
                    {
                        IList<uint> pmlLIds = pmlLIdss[pmlIndex];
                        IList<uint> pmlRodLIds = pmlRodLIdss[pmlIndex];
                        if (pmlLIds.Contains(lId))
                        {
                            hitPMLIndex = pmlIndex;
                            if (pmlRodLIds.Contains(lId))
                            {
                                cad.SetLoopColor(lId, pmlCoreColor);
                            }
                            else
                            {
                                cad.SetLoopColor(lId, pmlColor);
                            }
                            break;
                        }
                    }
                    if (hitPMLIndex == -1)
                    {
                        if (rodLoopIds.Contains(lId))
                        {
                            cad.SetLoopColor(lId, rodColor);
                        }
                    }
                }
                foreach (var rodEIds in rodEIdsB)
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
            IDrawer drawer = new CadObject2DDrawer(cad);
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
            IDrawer meshDrawer = new Mesher2DDrawer(mesher);
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

            uint claddingMaId = 0;
            uint coreMaId = 0;
            IList<uint> pmlCoreMaIds = new List<uint>();
            IList<uint> pmlCladdingMaIds = new List<uint>();
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
                DielectricPMLMaterial pmlCladdingMa1 = new DielectricPMLMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = claddingMu,
                    Muyy = claddingMu,
                    Muzz = claddingMu,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLXB2, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0
                };
                DielectricPMLMaterial pmlCoreMa1 = new DielectricPMLMaterial
                {
                    Epxx = coreEp,
                    Epyy = coreEp,
                    Epzz = coreEp,
                    Muxx = coreMu,
                    Muyy = coreMu,
                    Muzz = coreMu,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLXB2, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0
                };
                DielectricPMLMaterial pmlCladdingMa2 = new DielectricPMLMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = claddingMu,
                    Muyy = claddingMu,
                    Muzz = claddingMu,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port2PMLX2B2, port2PMLY2B2),
                    XThickness = pmlThickness,
                    YThickness = 0.0,
                    RotOriginPoint = new OpenTK.Vector2d(port2PMLX2B2, port2PMLY2B2),
                    RotAngle = 60.0 * Math.PI / 180.0
                };
                DielectricPMLMaterial pmlCoreMa2 = new DielectricPMLMaterial
                {
                    Epxx = coreEp,
                    Epyy = coreEp,
                    Epzz = coreEp,
                    Muxx = coreMu,
                    Muyy = coreMu,
                    Muzz = coreMu,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port2PMLX2B2, port2PMLY2B2),
                    XThickness = pmlThickness,
                    YThickness = 0.0,
                    RotOriginPoint = new OpenTK.Vector2d(port2PMLX2B2, port2PMLY2B2),
                    RotAngle = 60.0 * Math.PI / 180.0
                };

                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);

                DielectricPMLMaterial[] pmlCladdingMas = { pmlCladdingMa1, pmlCladdingMa2 };
                DielectricPMLMaterial[] pmlCoreMas = { pmlCoreMa1, pmlCoreMa2 };
                for (int pmlIndex = 0; pmlIndex < pmlCladdingMas.Length; pmlIndex++)
                {
                    var pmlCladdingMa = pmlCladdingMas[pmlIndex];
                    var pmlCoreMa = pmlCoreMas[pmlIndex];
                    uint pmlCladdingMaId = world.AddMaterial(pmlCladdingMa);
                    pmlCladdingMaIds.Add(pmlCladdingMaId);
                    uint pmlCoreMaId = world.AddMaterial(pmlCoreMa);
                    pmlCoreMaIds.Add(pmlCoreMaId);
                }

                System.Diagnostics.Debug.Assert(pmlLIdss.Length == pmlCladdingMaIds.Count);
                System.Diagnostics.Debug.Assert(pmlLIdss.Length == pmlCoreMaIds.Count);

                uint[] lIds = new uint[9 + rodLoopIds.Count];
                for (int i = 0; i < 9; i++)
                {
                    lIds[i] = (uint)(i + 1);
                }
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 9] = rodLoopIds[i];
                }
                uint[] maIds = new uint[lIds.Length];
                for (int i = 0; i < lIds.Length; i++)
                {
                    uint lId = lIds[i];
                    uint maId = claddingMaId;
                    int hitPMLIndex = -1;
                    for (int pmlIndex = 0; pmlIndex < pmlLIdss.Length; pmlIndex++)
                    {
                        IList<uint> pmlLIds = pmlLIdss[pmlIndex];
                        IList<uint> pmlRodLIds = pmlRodLIdss[pmlIndex];
                        if (pmlLIds.Contains(lId))
                        {
                            hitPMLIndex = pmlIndex;
                            if (pmlRodLIds.Contains(lId))
                            {
                                maId = pmlCoreMaIds[hitPMLIndex];
                            }
                            else
                            {
                                maId = pmlCladdingMaIds[hitPMLIndex];
                            }
                            break;
                        }
                    }
                    if (hitPMLIndex == -1)
                    {
                        if (rodLoopIds.Contains(lId))
                        {
                            maId = coreMaId;
                        }
                        else
                        {
                            maId = claddingMaId;
                        }
                    }

                    maIds[i] = maId;
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
            bool[] isPortBc2Reverse = { false, false, false };
            System.Diagnostics.Debug.Assert(isPortBc2Reverse.Length == (refPortCnt + 1));
            for (int portId = 0; portId < (refPortCnt + 1); portId++)
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
            for (int portId = 0; portId < (refPortCnt + 1); portId++)
            {
                uint lId0 = portInputWgBaseLoopIds[portId];
                var inputWgRodLoopIds = portInputWgRodLoopIdss[portId];
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
            int rodCntPort1PMLB1 = rodEIdsPort1PMLB1.Count / 2;
            int divCntPort1PMLB1 = 3 * rodCntPort1PMLB1 + 1;
            int rodCntPort2PMLB1 = rodEIdsPort2PMLB1.Count / 2;
            int divCntPort2PMLB1 = 3 * rodCntPort2PMLB1 + 1;
            int rodCntPort1B1 = rodEIdsPort1B1.Count / 2;
            int divCntPort1B1 = 3 * rodCntPort1B1 + 1;
            int rodCntPort2B1 = rodEIdsPort2B1.Count / 2;
            int divCntPort2B1 = 3 * rodCntPort2B1 + 1;
            int rodCntSrcB1 = rodEIdsSrcB1.Count / 2;
            int divCntSrcB1 = 3 * rodCntSrcB1 + 1;
            int[] divCntsB1 = { divCntPort1B1, divCntPort2B1, divCntSrcB1 };
            IList<uint>[] rodEIdssB1 = {
                rodEIdsPort1B1, rodEIdsPort2B1, rodEIdsSrcB1
            };
            uint[] eIds0B1 = { 26, 29, 24 };
            for (int portId = 0; portId < (refPortCnt + 1); portId++)
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
                    eId = (uint)(30 + (divCntPort1PMLB1 - 1) * 2 + (divCntPort2PMLB1 - 1) * 2);
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
                rodEIdsPort1B2, rodEIdsPort2B2, rodEIdsSrcB2
            };
            uint[] eIds0B2 = { 27, 28, 25 };
            for (int portId = 0; portId < (refPortCnt + 1); portId++)
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
                    eId = (uint)(30 + (divCntPort1PMLB1 - 1) * 2 + (divCntPort2PMLB1 - 1) * 2);
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
                for (int portId = 0; portId < (refPortCnt + 1); portId++)
                {
                    PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                    IList<uint> lIds = wgPortInfo.LoopIds;
                    IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                    IList<uint> bcEIds2 = wgPortInfo.BcEdgeIds2;
                    PortCondition portCondition = new PortCondition(
                        lIds, bcEIds1, bcEIds2, FieldValueType.Scalar, new List<uint> { 0 }, 0);
                    portConditions.Add(portCondition);
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
            for (int portId = 0; portId < (refPortCnt + 1); portId++)
            {
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.SetupAfterMakeElements(world, quantityId, (uint)portId);
            }
            // フォトニック結晶導波路チャンネル上節点を取得する
            for (int portId = 0; portId < (refPortCnt + 1); portId++)
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

                // 後片付け
                world.RotAngle = 0;
                world.RotOrigin = null;
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

            var FEM = new PCWaveguide2DPMLTDFEM(world);
            FEM.TimeLoopCnt = timeLoopCnt;
            FEM.TimeIndex = 0;
            FEM.TimeDelta = timeDelta;
            FEM.GaussianType = gaussianType;
            FEM.GaussianT0 = gaussianT0;
            FEM.GaussianTp = gaussianTp;
            FEM.SrcFrequency = srcFreq;
            FEM.StartFrequencyForSMatrix = freq1;
            FEM.EndFrequencyForSMatrix = freq2;
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
            System.Numerics.Complex[] _freqDomainAmpsInc = freqDomainAmpsInc;
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
        }
    }
}
