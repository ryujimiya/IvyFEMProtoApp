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
        public void PCWaveguidePMLTriangleLatticeProblem1_0(MainWindow mainWindow)
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

            // 計算する周波数領域
            double sFreq = 0.267;
            double eFreq = 0.287;
            int freqDiv = 25;

            // 形状設定で使用する単位長さ
            double unitLen = periodicDistance;
            // PML層の厚さ
            //int pmlRodCnt = 6;
            int pmlRodCnt = 10;
            double pmlThickness = pmlRodCnt * unitLen;
            //int pmlWgRodCnt = 0;
            int pmlWgRodCnt = pmlRodCnt;
            double pmlWgRodThickness = pmlWgRodCnt * unitLen;
            // 導波路不連続領域の長さ
            //const int disconRodCnt = 5; // 最低7必要
            const int disconRodCnt = 5;
            double disconLength = periodicDistance * disconRodCnt;
            double disconPlusPMLLength = disconLength + 2.0 * pmlThickness;
            double port1PMLXB1 = 0;
            double port1PMLXB2 = pmlThickness;
            double port2PMLXB1 = disconPlusPMLLength;
            double port2PMLXB2 = disconPlusPMLLength - pmlThickness;
            // 観測点
            int port1OfsX = 1;
            int port2OfsX = 1;
            double refport1XB1 = port1PMLXB2 + port1OfsX * unitLen;
            double refport1XB2 = refport1XB1 + periodicDistance;
            double refport2XB1 = port2PMLXB2 - port2OfsX * unitLen;
            double refport2XB2 = refport2XB1 - periodicDistance;
            // 観測ポート数
            int refPortCnt = 2;
            IList<uint> rodLoopIds = new List<uint>();
            IList<uint> pmlRodLoopIdsPort1 = new List<uint>();
            IList<uint> inputWgRodLoopIdsPort1 = new List<uint>();
            IList<uint> inputWgRodLoopIdsPort2 = new List<uint>();
            IList<uint> pmlRodLoopIdsPort2 = new List<uint>();
            IList<uint>[] inputWgRodLoopIdss = {
                pmlRodLoopIdsPort1, pmlRodLoopIdsPort2,
                inputWgRodLoopIdsPort1, inputWgRodLoopIdsPort2
            };
            uint[] inputWgBaseLoopIds = { 1, 7, 3, 5 };
            IList<uint> pmlLIds1 = new List<uint>();
            IList<uint> pmlLIds2 = new List<uint>();
            uint[] pmlBaseLoopIds = { 1, 7 };
            IList<uint>[] pmlLIdss = { pmlLIds1, pmlLIds2 };
            IList<uint>[] pmlRodLIdss = { pmlRodLoopIdsPort1, pmlRodLoopIdsPort2 };
            uint[] portInputWgBaseLoopIds = {
                inputWgBaseLoopIds[2], inputWgBaseLoopIds[3] };
            IList<uint>[] portInputWgRodLoopIdss = {
                inputWgRodLoopIdss[2], inputWgRodLoopIdss[3] };
            IList<uint> rodEIdsPort1PMLB1 = new List<uint>();
            IList<uint> rodEIdsPort1PMLB2 = new List<uint>();
            IList<uint> rodEIdsPort2PMLB1 = new List<uint>();
            IList<uint> rodEIdsPort2PMLB2 = new List<uint>();
            IList<uint> rodEIdsPort1B1 = new List<uint>();
            IList<uint> rodEIdsPort1B2 = new List<uint>();
            IList<uint> rodEIdsPort2B1 = new List<uint>();
            IList<uint> rodEIdsPort2B2 = new List<uint>();
            IList<uint> rodVIdsPort1PMLB1 = new List<uint>();
            IList<uint> rodVIdsPort1PMLB2 = new List<uint>();
            IList<uint> rodVIdsPort2PMLB1 = new List<uint>();
            IList<uint> rodVIdsPort2PMLB2 = new List<uint>();
            IList<uint> rodVIdsPort1B1 = new List<uint>();
            IList<uint> rodVIdsPort1B2 = new List<uint>();
            IList<uint> rodVIdsPort2B1 = new List<uint>();
            IList<uint> rodVIdsPort2B2 = new List<uint>();

            Cad2D cad = new Cad2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PMLXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(refport1XB1, 0.0));
                pts.Add(new OpenTK.Vector2d(refport1XB2, 0.0));
                pts.Add(new OpenTK.Vector2d(refport2XB2, 0.0));
                pts.Add(new OpenTK.Vector2d(refport2XB1, 0.0));
                pts.Add(new OpenTK.Vector2d(port2PMLXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(disconPlusPMLLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconPlusPMLLength, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port2PMLXB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(refport2XB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(refport2XB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(refport1XB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(refport1XB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1PMLXB2, waveguideWidth));
                uint _lId1 = cad.AddPolygon(pts).AddLId;
                uint _lId2 = cad.ConnectVertexLine(3, 16).AddLId;
                uint _lId3 = cad.ConnectVertexLine(4, 15).AddLId;
                uint _lId4 = cad.ConnectVertexLine(5, 14).AddLId;
                uint _lId5 = cad.ConnectVertexLine(6, 13).AddLId;
                uint _lId6 = cad.ConnectVertexLine(7, 12).AddLId;
                uint _lId7 = cad.ConnectVertexLine(8, 11).AddLId;
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
                1, 17, 9, 22,
                18, 19, 21, 20
            };
            IList<uint>[] rodVIdsB = { 
                rodVIdsPort1PMLB1, rodVIdsPort1PMLB2, rodVIdsPort2PMLB1, rodVIdsPort2PMLB2,
                rodVIdsPort1B1, rodVIdsPort1B2, rodVIdsPort2B1, rodVIdsPort2B2
            };
            IList<uint>[] rodEIdsB = {
                rodEIdsPort1PMLB1, rodEIdsPort1PMLB2, rodEIdsPort2PMLB1, rodEIdsPort2PMLB2,
                rodEIdsPort1B1, rodEIdsPort1B2, rodEIdsPort2B1, rodEIdsPort2B2
            };
            double[] ptXsB = {
                port1PMLXB1, port1PMLXB2,
                port2PMLXB1, port2PMLXB2,
                refport1XB1, refport1XB2,
                refport2XB1, refport2XB2
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
                1, 2, 7,
                3, 4, 6, 5
            };
            IList<uint>[] leftRodVIdsB = { 
                rodVIdsB[0], rodVIdsB[1], rodVIdsB[3],
                rodVIdsB[4], rodVIdsB[5], rodVIdsB[6], rodVIdsB[7] 
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
                1, 7, 6,
                2, 3, 5, 4 
            };
            IList<uint>[] rightRodVIdsB = {
                rodVIdsB[1], rodVIdsB[2], rodVIdsB[3],
                rodVIdsB[4], rodVIdsB[5], rodVIdsB[6], rodVIdsB[7]
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
            int periodicCntPML1 = pmlRodCnt;
            int periodicCntPML2 = pmlRodCnt;
            int periodicCntX1 = periodicCntPML1 + disconRodCnt;

            // PML1と不連続領域
            for (int iX = 0; iX < periodicCntX1; iX++)
            {
                double centerX = periodicDistance * 0.5 + iX * periodicDistance;
                uint baseLoopId = 0;
                int inputWgNo = 0;
                if (centerX >= 0 && centerX < port1PMLXB2)
                {
                    baseLoopId = 1;
                    if (centerX < (port1PMLXB2 - pmlWgRodThickness))
                    {
                        continue;
                    }
                }
                else if (centerX >= port1PMLXB2 && centerX < refport1XB1)
                {
                    baseLoopId = 2;
                }
                else if (centerX >= refport1XB1 && centerX < refport1XB2)
                {
                    baseLoopId = 3;
                }
                else if (centerX >= refport1XB2 && centerX < refport2XB2)
                {
                    baseLoopId = 4;
                }
                else if (centerX >= refport2XB2 && centerX < refport2XB1)
                {
                    baseLoopId = 5;
                }
                else if (centerX >= refport2XB1 && centerX < port2PMLXB2)
                {
                    baseLoopId = 6;
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
                        if (Math.Abs(x0 - refport1XB1) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - refport1XB2) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - refport2XB2) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - refport2XB1) < th)
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

            // PML2
            int periodicCntX2 = periodicCntPML2;
            for (int iX = 0; iX < periodicCntX2; iX++)
            {
                double baseCenterX = periodicDistance * 0.5 + iX * periodicDistance;
                uint baseLoopId = 0;
                int inputWgNo = 0;
                {
                    baseLoopId = 7;
                    if (baseCenterX < (pmlThickness - pmlWgRodThickness))
                    {
                        continue;
                    }
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
                        x0 = port2PMLXB1 - iX * periodicDistance;

                        // 追加済みチェック
                        double th = 1.0e-12;
                        if (Math.Abs(x0 - port2PMLXB1) < th)
                        {
                            continue;
                        }
                        if (Math.Abs(x0 - port2PMLXB2) < th)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        workRodY0s = rodY0sX2;
                        x0 = port2PMLXB1 - iX * periodicDistance - 0.5 * periodicDistance;
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
                for (int iL = 0; iL < (7 + rodLoopIds.Count); iL++)
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
                uint dof = 1; // 複素数
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
                    OriginPoint = new OpenTK.Vector2d(port2PMLXB2, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0
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
                    OriginPoint = new OpenTK.Vector2d(port2PMLXB2, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0
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

                uint[] lIds = new uint[7 + rodLoopIds.Count];
                for (int i = 0; i < 7; i++)
                {
                    lIds[i] = (uint)(i + 1);
                }
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 7] = rodLoopIds[i];
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
            for (int portId = 0; portId < refPortCnt; portId++)
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
            {
                int srcPortId = refPortCnt;
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[srcPortId];
                int port1PortId = 0;
                PCWaveguidePortInfo wgPortInfoPort1 = wgPortInfos[port1PortId];
                wgPortInfo.LoopIds = new List<uint>(wgPortInfoPort1.LoopIds);
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
            int[] divCntsB1 = { divCntPort1B1, divCntPort2B1 };
            IList<uint>[] rodEIdssB1 = {
                rodEIdsPort1B1, rodEIdsPort2B1
            };
            uint[] eIds0B1 = { 18, 21 };
            for (int portId = 0; portId < refPortCnt; portId++)
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
                    eId = 22;
                    if (portId == 0)
                    {
                        eId += (uint)((divCntPort1PMLB1 - 1) * 2 + (divCntPort2PMLB1 - 1) * 2);
                    }
                    else if (portId == 1)
                    {
                        eId += (uint)((divCntPort1PMLB1 - 1) * 2 + (divCntPort2PMLB1 - 1) * 2);
                        eId += (uint)((divCntPort1B1 - 1) * 2);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
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
            {
                int srcPortId = refPortCnt;
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[srcPortId];
                int port1PortId = 0;
                PCWaveguidePortInfo wgPortInfoPort1 = wgPortInfos[port1PortId];
                wgPortInfo.BcEdgeIds1 = new List<uint>(wgPortInfoPort1.BcEdgeIds1);
            }
            // 周期構造境界2
            IList<uint>[] rodEIdssB2 = { 
                rodEIdsPort1B2, rodEIdsPort2B2
            };
            uint[] eIds0B2 = { 19, 20 };
            for (int portId = 0; portId < refPortCnt; portId++)
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
                    eId = 22;
                    if (portId == 0)
                    {
                        eId += (uint)((divCntPort1PMLB1 - 1) * 2 + (divCntPort2PMLB1 - 1) * 2);
                    }
                    else if (portId == 1)
                    {
                        eId += (uint)((divCntPort1PMLB1 - 1) * 2 + (divCntPort2PMLB1 - 1) * 2);
                        eId += (uint)((divCntPort1B1 - 1) * 2);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
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
                int srcPortId = refPortCnt;
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[srcPortId];
                int port1PortId = 0;
                PCWaveguidePortInfo wgPortInfoPort1 = wgPortInfos[port1PortId];
                wgPortInfo.BcEdgeIds2 = new List<uint>(wgPortInfoPort1.BcEdgeIds2);
            }

            {
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                for (int portId = 0; portId < (refPortCnt + 1); portId++)
                {
                    PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                    IList<uint> lIds = wgPortInfo.LoopIds;
                    IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                    IList<uint> bcEIds2 = wgPortInfo.BcEdgeIds2;
                    // 複素数
                    PortCondition portCondition = new PortCondition(
                        CadElementType.Edge,
                        lIds, bcEIds1, bcEIds2, FieldValueType.ZScalar, new List<uint> { 0 }, 0);
                    portConditions.Add(portCondition);
                }
            }

            /*
            // 強制境界
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

            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closing += ChartWindow1_Closing;
            }
            ChartWindow chartWin = ChartWindow1;
            chartWin.Owner = mainWindow;
            chartWin.Left = mainWindow.Left + mainWindow.Width;
            chartWin.Top = mainWindow.Top;
            chartWin.Show();
            chartWin.TextBox1.Text = "";
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

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.ZAbs);
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

            for (int iFreq = 0; iFreq < (freqDiv + 1); iFreq++)
            {
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波長
                double waveLength = latticeA / normalizedFreq;
                // 周波数
                double freq = Constants.C0 / waveLength;
                // 角周波数
                double omega = 2.0 * Math.PI * freq;
                // 波数
                double k0 = omega / Constants.C0;
                System.Diagnostics.Debug.WriteLine("a / λ: " + normalizedFreq);

                var FEM = new PCWaveguide2DPMLFEM(world);
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
                FEM.IsTMMode = isTMMode;
                FEM.WgPortInfos = wgPortInfos;
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] Ez = FEM.Ez;
                System.Numerics.Complex[][] S = FEM.S;

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
                //AlertWindow.ShowDialog(ret, "");
                series1.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                series2.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, Ez);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
