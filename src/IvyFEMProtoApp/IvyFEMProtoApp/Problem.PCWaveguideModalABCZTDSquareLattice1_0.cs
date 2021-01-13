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
        public void PCWaveguideModalABCZTDSquareLatticeProblem1_0(MainWindow mainWindow)
        {
            double[] freqs;
            System.Numerics.Complex[] freqDomainAmpsInc;
            SolvePCWaveguideModalABCZTDSquareLatticeProblem1_0(
                mainWindow, out freqs, out freqDomainAmpsInc);
        }

        public void SolvePCWaveguideModalABCZTDSquareLatticeProblem1_0(
            MainWindow mainWindow,
            out double[] retFreqs,
            out System.Numerics.Complex[] retFreqDomainAmpsInc)
        {
            retFreqs = null;
            retFreqDomainAmpsInc = null;

            double waveguideWidth = 1.0;

            // フォトニック導波路
            // ロッドの数（半分）
            //const int rodCntHalf = 5;
            const int rodCntHalf = 5;
            // 欠陥ロッド数
            const int defectRodCnt = 1;
            // 格子数
            const int latticeCnt = rodCntHalf * 2 + defectRodCnt;
            // 格子定数
            double latticeA = waveguideWidth / (double)latticeCnt;
            // 周期構造距離
            double periodicDistance = latticeA;
            // ロッドの半径
            double rodRadius = 0.18 * latticeA;
            // ロッドの比誘電率
            double rodEp = 3.4 * 3.4;
            // 格子１辺の分割数
            //const int divForOneLatticeCnt = 6;
            const int divForOneLatticeCnt = 6;
            // 境界の総分割数
            const int divCnt = latticeCnt * divForOneLatticeCnt;
            // ロッドの円周分割数
            const int rodCircleDiv = 8;
            // ロッドの半径の分割数
            const int rodRadiusDiv = 1;
            // メッシュサイズ
            double eLen = 1.05 * waveguideWidth / divCnt;
            // 最小屈折率
            double minEffN = 0.0;
            // 最大屈折率
            double maxEffN = 1.0;

            // 時間刻み幅の算出
            double courantNumber = 0.5;
            // Note: timeLoopCnt は 2^mでなければならない
            //int timeLoopCnt = 2048;
            int timeLoopCnt = 2048;
            double timeStep = courantNumber * eLen / (Constants.C0 * Math.Sqrt(2.0));
            // 励振源
            // 規格化周波数
            //double srcNormalizedFreq = 0.385;
            double srcNormalizedFreq = 0.385;
            // 波長
            double srcWaveLength = latticeA / srcNormalizedFreq;
            // 周波数
            double srcFreq = Constants.C0 / srcWaveLength;
            // 計算する周波数領域
            double normalizedFreq1 = 0.330;
            double normalizedFreq2 = 0.440;
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
            double gaussianTp = 0.25 * 6.0 / (freq2 - freq1);
            double gaussianT0 = gaussianTp * (2.0 * Math.Sqrt(2.0 * Math.Log(2.0)));

            const uint portCnt = 2;
            // 入出力導波路の周期構造部分の長さ
            double inputWgLength = latticeA;
            // 導波路不連続領域の長さ
            //const int disconRodCnt = 7; // 最低7必要
            const int disconRodCnt = 7;
            double disconLength = latticeA * disconRodCnt;
            double disconPlusInputWgLength = disconLength + 2.0 * inputWgLength;
            // 形状設定で使用する単位長さ
            double unitLen = periodicDistance;
            double port10PosXB1 = 0;
            double port10PosXB2 = periodicDistance;
            double port20PosXB1 = disconPlusInputWgLength;
            double port20PosXB2 = disconPlusInputWgLength - periodicDistance;
            // 励振位置
            double srcPosXB1 = port10PosXB2 + 1 * unitLen;
            double srcPosXB2 = srcPosXB1 + periodicDistance;
            // 観測点
            int port1OfsX = 1;
            int port2OfsX = 1;
            double port1PosXB1 = srcPosXB2 + port1OfsX * unitLen;
            double port1PosXB2 = port1PosXB1 + periodicDistance;
            double port2PosXB1 = port20PosXB2 - port2OfsX * unitLen;
            double port2PosXB2 = port2PosXB1 - periodicDistance;
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

            // Cad
            Cad2D cad = new Cad2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            {
                System.Diagnostics.Debug.Assert(Math.Abs(port10PosXB1 - 0.0) < 1.0e-12);
                System.Diagnostics.Debug.Assert(Math.Abs(port20PosXB1 - disconPlusInputWgLength) < 1.0e-12);
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(port10PosXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(srcPosXB1, 0.0));
                pts.Add(new OpenTK.Vector2d(srcPosXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PosXB1, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PosXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(port2PosXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(port2PosXB1, 0.0));
                pts.Add(new OpenTK.Vector2d(port20PosXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(disconPlusInputWgLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconPlusInputWgLength, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port20PosXB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port2PosXB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port2PosXB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1PosXB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1PosXB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(srcPosXB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(srcPosXB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port10PosXB2, waveguideWidth));
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
            uint port10B1EId = 1;
            uint port10B2EId = 21;
            uint srcEIdB1 = 22;
            uint srcEIdB2 = 23;
            uint port1B1EId = 24;
            uint port1B2EId = 25;
            uint port2B1EId = 27;
            uint port2B2EId = 26;
            uint port20B1EId = 11;
            uint port20B2EId = 28;
            uint[] port10EIds = { port10B1EId, port10B2EId };
            uint[] srcEIds = { srcEIdB1, srcEIdB2 };
            uint[] port1EIds = { port1B1EId, port1B2EId };
            uint[] port2EIds = { port2B1EId, port2B2EId };
            uint[] port20EIds = { port20B1EId, port20B2EId };
            uint[][] portEIdss = { port10EIds, port20EIds, port1EIds, port2EIds, srcEIds };
            double[] port10PosXs = { port10PosXB1, port10PosXB2 };
            double[] srcPosXs = { srcPosXB1, srcPosXB2 };
            double[] port1PosXs = { port1PosXB1, port1PosXB2 };
            double[] port2PosXs = { port2PosXB1, port2PosXB2 };
            double[] port20PosXs = { port20PosXB1, port20PosXB2 };
            double[][] portPosXss = { port10PosXs, port20PosXs, port1PosXs, port2PosXs, srcPosXs };
            System.Diagnostics.Debug.Assert(portEIdss.Length == portPosXss.Length);
            // 入出力導波路の周期構造境界上の頂点を追加
            //  逆から追加しているのは、頂点によって新たに生成される辺に頂点を追加しないようにするため
            for (int portId = 0; portId < portEIdss.Length; portId++)
            {
                uint[] _portEIds = portEIdss[portId];
                double[] _portPosXs = portPosXss[portId];
                System.Diagnostics.Debug.Assert(_portEIds.Length == 2);
                System.Diagnostics.Debug.Assert(_portPosXs.Length == 2);
                for (int bcIndex = 0; bcIndex < _portEIds.Length; bcIndex++)
                {
                    uint eId = _portEIds[bcIndex];
                    double x1 = _portPosXs[bcIndex];
                    double y1 = 0.0;
                    double y2 = 0.0;
                    if (portId == 0 && bcIndex == 0)
                    {
                        y1 = waveguideWidth;
                        y2 = 0.0; 
                    }
                    else
                    {
                        y1 = 0.0;
                        y2 = waveguideWidth;
                    }
                    PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x1, y2);
                }
            }

            // ロッドを追加
            int rodCntInputWgPort10 = 1;
            int rodCntInputWgPort20 = 1;
            int rodCntAll = rodCntInputWgPort10 + rodCntInputWgPort20 + disconRodCnt;
            System.Diagnostics.Debug.Assert(inputWgRodLoopIdss.Length == inputWgBaseLoopIds.Length);
            for (int col = 0; col < rodCntAll; col++)
            {
                double centerX = latticeA * 0.5 + col * latticeA;
                uint baseLoopId = 0;
                int inputWgNo = 0;
                if (centerX >= 0 && centerX < port10PosXB2)
                {
                    baseLoopId = 1;
                }
                else if (centerX >= port10PosXB2 && centerX < srcPosXB1)
                {
                    baseLoopId = 2;
                }
                else if (centerX >= srcPosXB1 && centerX < srcPosXB2)
                {
                    baseLoopId = 3;
                }
                else if (centerX >= srcPosXB2 && centerX < port1PosXB1)
                {
                    baseLoopId = 4;
                }
                else if (centerX >= port1PosXB1 && centerX < port1PosXB2)
                {
                    baseLoopId = 5;
                }
                else if (centerX >= port1PosXB2 && centerX < port2PosXB2)
                {
                    baseLoopId = 6;
                }
                else if (centerX >= port2PosXB2 && centerX < port2PosXB1)
                {
                    baseLoopId = 7;
                }
                else if (centerX >= port2PosXB1 && centerX < port20PosXB2)
                {
                    baseLoopId = 8;
                }
                else if (centerX >= port20PosXB2 && centerX < port20PosXB1)
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

                for (int row = 0; row < rodCntHalf; row++)
                {
                    double x0 = centerX;
                    double y0 = waveguideWidth - row * latticeA - latticeA * 0.5;
                    uint lId = PCWaveguideUtils.AddRod(
                        cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                    rodLoopIds.Add(lId);
                    if (inputWgNo != 0)
                    {
                        inputWgRodLoopIdss[inputWgNo - 1].Add(lId);
                    }
                }
                for (int row = 0; row < rodCntHalf; row++)
                {
                    double x0 = centerX;
                    double y0 = latticeA * rodCntHalf - row * latticeA - latticeA * 0.5;
                    uint lId = PCWaveguideUtils.AddRod(
                        cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                    rodLoopIds.Add(lId);
                    if (inputWgNo != 0)
                    {
                        inputWgRodLoopIdss[inputWgNo - 1].Add(lId);
                    }
                }
            }

            // check
            {
                foreach (uint lId in rodLoopIds)
                {
                    cad.SetLoopColor(lId, new double[] { 1.0, 0.6, 0.6 });
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
                DielectricMaterial claddingMa = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricMaterial coreMa = new DielectricMaterial
                {
                    Epxx = rodEp,
                    Epyy = rodEp,
                    Epzz = rodEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
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
                wgPortInfo.IsSVEA = true; // 緩慢変化包絡線近似
                wgPortInfo.IsPortBc2Reverse = isPortBc2Reverse[portId];
                wgPortInfo.LatticeA = latticeA;
                wgPortInfo.PeriodicDistanceX = periodicDistance;
                wgPortInfo.MinEffN = minEffN;
                wgPortInfo.MaxEffN = maxEffN;
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
            for (int portId = 0; portId < (portCnt + refPortCnt + 1); portId++)
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];

                int bcIndex = 0; // B1
                uint eId0 = portEIdss[portId][bcIndex];
                eIds[0] = eId0;
                maIds[0] = claddingMaId;

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    eIds[i] = (uint)(28 + (divCnt - 1) * (1 + 2 * portId) - (i - 1));
                    maIds[i] = claddingMaId;
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
            for (int portId = 0; portId < (portCnt + refPortCnt + 1); portId++)
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];

                int bcIndex = 1; // B2
                uint eId0 = portEIdss[portId][bcIndex];
                eIds[0] = eId0;
                maIds[0] = claddingMaId;

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    eIds[i] = (uint)(28 + (divCnt - 1) * (2 * (portId + 1)) - (i - 1));
                    maIds[i] = claddingMaId;
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

            uint[] zeroEIds = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                zeroFixedCads.Add(fixedCad);
            }

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
                        if (y >= (waveguideWidth - latticeA * (rodCntHalf + defectRodCnt)) &&
                            y <= (waveguideWidth - latticeA * (rodCntHalf)))
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
