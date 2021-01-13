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
        // CROW(couple resonator optical waveguide)
        public void PCWaveguideEigenTriangleLatticeProblem2(MainWindow mainWindow)
        {
            // even mode
            SolvePCWaveguideEigenTriangleLatticeProblem2(mainWindow, true);

            WPFUtils.DoEvents(10 * 1000);

            // odd mode
            SolvePCWaveguideEigenTriangleLatticeProblem2(mainWindow, false);
        }

        private void SolvePCWaveguideEigenTriangleLatticeProblem2(MainWindow mainWindow, bool isEvenMode)
        {
            double waveguideWidth = 1.0;

            // 計算するモード
            /*
            //int targetModeIndex = 0; // 一番小さい周波数
            int targetModeIndex = 1; // 高次
            */
            int targetModeIndex = isEvenMode? 0 : 1; // 0: CROW even 1: CROW odd
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
            // ロッドのカラムをシフトする？
            bool isColumnShift180 = false;
            // ロッドの数(半分)
            //const int rodCntHalf = 4;
            const int rodCntHalf = 4;
            // 欠陥ロッド数
            const int defectRodCnt = 1;
            // 三角形格子の内角
            double latticeTheta = 30.0;
            // ロッドの半径
            double rodRadiusRatio = 0.30;
            // ロッドの比誘電率
            double rodEp = 3.40 * 3.40;
            // 1格子当たりの分割点の数
            //const int divCntForOneLattice = 9;
            const int divCntForOneLattice = 6;
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

            double sBeta = 0.0;
            double eBeta = maxWaveNum;
            int betaDiv = 50;

            // フォトニック結晶導波路の場合、a/λを規格化周波数とする
            double normalizedFreq1 = 0.200;
            double normalizedFreq2 = 0.290;

            // Cad
            Cad2D cad = new Cad2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            IList<uint> rodLoopIds = new List<uint>();
            IList<uint> rodEIdsB1 = new List<uint>();
            IList<uint> rodEIdsB2 = new List<uint>();
            IList<uint> rodVIdsB1 = new List<uint>();
            IList<uint> rodVIdsB2 = new List<uint>();
            {
                //------------------------------------------------------------------
                // 図面作成
                //------------------------------------------------------------------
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    // 領域追加
                    pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(periodicDistance, 0.0));
                    pts.Add(new OpenTK.Vector2d(periodicDistance, waveguideWidth));
                    uint lId1 = cad.AddPolygon(pts).AddLId;
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
                        var workrodY0s = isColumnShift180 ? rodY0sX1 : rodY0sX2;
                        workrodY0s.Add(y0);
                    }
                    else
                    {
                        double y0 = topRodPosY - rodDistanceY - (i - 1) * rodDistanceY;
                        if ((y0 + rodRadius) > waveguideWidth || (y0 - rodRadius) < 0.0)
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        var workrodY0s = isColumnShift180 ? rodY0sX2 : rodY0sX1;
                        workrodY0s.Add(y0);
                    }
                }

                ///////////////////////////////////////////
                // 境界上のロッドの頂点を追加
                // 入力導波路 外側境界
                // 入力導波路 内部側境界
                uint[] eIdsB = { 1, 3 };
                IList<uint>[] rodVIdsB = { rodVIdsB1, rodVIdsB2 };
                IList<uint>[] rodEIdsB = { rodEIdsB1, rodEIdsB2 };
                double[] ptXsB = { 0.0, periodicDistance };
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
                uint[] leftRodContainsLIds = { 1 };
                IList<uint>[] leftRodVIdsB = { rodVIdsB[0] };
                for (int index = 0; index < leftRodContainsLIds.Length; index++)
                {
                    IList<uint> workrodVIdsB = leftRodVIdsB[index];
                    uint baseLoopId = leftRodContainsLIds[index];

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
                    }
                }

                /////////////////////////////////////////////////////////////////////////////
                // 右のロッドを追加
                uint[] rightRodContainsLIds = { 1 };
                IList<uint>[] rightRodVIdsB = { rodVIdsB[1] };
                for (int index = 0; index < rightRodContainsLIds.Length; index++)
                {
                    IList<uint> workrodVIdsB = rightRodVIdsB[index];
                    uint baseLoopId = rightRodContainsLIds[index];

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
                    }
                }

                /////////////////////////////////////////////////////////////////////////
                // 領域のロッド
                {
                    uint baseLoopId = 1;
                    IList<double> workRodY0s = null;
                    double x0 = 0.0;

                    workRodY0s = rodY0sX2;
                    x0 = 0.5 * periodicDistance;
                    int rodCnt = workRodY0s.Count;
                    for (int i = 0; i < rodCnt; i++)
                    {
                        double y0 = workRodY0s[i];
                        uint lId = PCWaveguideUtils.AddRod(
                            cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                        rodLoopIds.Add(lId);
                    }
                }
            }

            // check
            {
                foreach (uint lId in rodLoopIds)
                {
                    cad.SetLoopColor(lId, new double[] { 1.0, 0.6, 0.6 });
                }
                IList<uint>[] rodEIdss = { rodEIdsB1, rodEIdsB2 };
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
                uint dof = 1; // 複素数
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint claddingMaId = 0;
            uint coreMaId = 0;
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

                uint[] lIds = new uint[1 + rodLoopIds.Count];
                lIds[0] = 1;
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 1] = rodLoopIds[i];
                }
                uint[] maIds = new uint[3 + rodLoopIds.Count];
                maIds[0] = claddingMaId;
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    maIds[i + 1] = coreMaId;
                }

                for (int i = 0; i < lIds.Length; i++)
                {
                    uint lId = lIds[i];
                    uint maId = maIds[i];
                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            double freq1 = normalizedFreq1 * Constants.C0 / latticeA;
            double freq2 = normalizedFreq2 * Constants.C0 / latticeA;
            // ポート情報リスト作成
            int portId = 0; // 固定
            bool[] isPortBc2Reverse = { true, false };
            var wgPortInfo = new PCWaveguidePortInfo();
            wgPortInfo.IsSVEA = false; // Φを直接解く
            wgPortInfo.IsPortBc2Reverse = isPortBc2Reverse[portId];
            wgPortInfo.LatticeA = latticeA;
            wgPortInfo.PeriodicDistanceX = periodicDistance;
            wgPortInfo.MinWaveNum = minWaveNum;
            wgPortInfo.MaxWaveNum = maxWaveNum;
            wgPortInfo.MinFrequency = freq1;
            wgPortInfo.MaxFrequency = freq2;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // 周期構造入出力導波路
            {
                // ワールド座標系のループIDを取得
                uint[] lIds = new uint[1 + rodLoopIds.Count];
                lIds[0] = 1;
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 1] = rodLoopIds[i];
                }
                wgPortInfo.LoopIds = new List<uint>(lIds);
            }
            // 周期構造境界1
            {
                uint eId0 = 1;
                IList<uint> workrodEIdsB = rodEIdsB1;
                int rodCnt = workrodEIdsB.Count / 2;
                int divCnt = 3 * rodCnt + 1;

                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
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
                    eIds[i] = (uint)(4 + (divCnt - 1) - (i - 1));

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

                wgPortInfo.BcEdgeIds1 = new List<uint>(eIds);
            }
            // 周期構造境界2
            {
                IList<uint> workrodEIdsB = rodEIdsB2;
                uint eId0 = 3;
                int rodCnt = workrodEIdsB.Count / 2;
                int divCnt = 3 * rodCnt + 1;

                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
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
                    eIds[i] = (uint)(4 + (divCnt - 1) * 2 - (i - 1));

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

                wgPortInfo.BcEdgeIds2 = new List<uint>(eIds);
            }

            // ポート条件
            IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
            {
                {
                    IList<uint> lIds = wgPortInfo.LoopIds;
                    IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                    IList<uint> bcEIds2 = wgPortInfo.BcEdgeIds2;
                    PortCondition portCondition = new PortCondition(
                        CadElementType.Edge,
                        lIds, bcEIds1, bcEIds2, FieldValueType.ZScalar, new List<uint> { 0 }, 0);
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
            {
                wgPortInfo.SetupAfterMakeElements(world, quantityId, (uint)portId);
            }
            // フォトニック結晶導波路チャンネル上節点を取得する
            {
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
            var chartWin = ChartWindow1;
            chartWin.Owner = mainWindow;
            chartWin.Left = mainWindow.Left + mainWindow.Width;
            chartWin.Top = mainWindow.Top;
            chartWin.Show();
            chartWin.TextBox1.Text = "";
            var model = new PlotModel();
            chartWin.Plot.Model = model;
            model.Title = "PCWaveguide Eigen Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "β(d/(2π))",
                Minimum = sBeta,
                Maximum = eBeta
            };
            var axis2 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "a/λ",
                Minimum = normalizedFreq1,
                Maximum = normalizedFreq2
            };
            model.Axes.Add(axis1);
            model.Axes.Add(axis2);
            // max light line
            double maxLightLine = eBeta * latticeA / periodicDistance;
            if (maxLightLine < normalizedFreq2)
            {
                maxLightLine = normalizedFreq2;
            }
            var series1 = new LineSeries
            {
                Title = "a/λ"
            };
            var series2 = new AreaSeries
            {
                Title = "light line",
                ConstantY2 = maxLightLine
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
                    quantityId, false, FieldShowType.ZReal);
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

            for (int iBeta = 0; iBeta < (betaDiv + 1); iBeta++)
            {
                // β
                double normalizedBeta = sBeta + (iBeta / (double)betaDiv) * (eBeta - sBeta);
                double beta = normalizedBeta * 2.0 * Math.PI / periodicDistance;
                System.Diagnostics.Debug.WriteLine("β(d/(2π)): " + normalizedBeta + " β:" + beta);

                var FEM = new PCWaveguide2DEigenFEMByBeta(world, quantityId, (uint)portId, wgPortInfo);
                FEM.IsTMMode = isTMMode;
                FEM.BetaX = beta;
                FEM.Solve();
                double[] freqs = FEM.Frequencys;
                System.Numerics.Complex[][] eVecs = FEM.EVecs;

                double normalizedFreq = double.MaxValue;
                System.Numerics.Complex[] eigenEz = null;
                int iMode = targetModeIndex;
                int nodeCnt = (int)world.GetNodeCount(quantityId);
                eigenEz = new System.Numerics.Complex[nodeCnt];
                int modeCnt = freqs.Length;
                if (iMode >= modeCnt)
                {
                    System.Diagnostics.Debug.WriteLine("No defect mode found for iMode = " + iMode);
                }
                else
                {
                    System.Numerics.Complex[] eVec = eVecs[iMode];
                    int portNodeCnt = (int)world.GetPortNodeCount(quantityId, (uint)portId);
                    System.Diagnostics.Debug.Assert(portNodeCnt == eVec.Length);
                    for (int portNodeId = 0; portNodeId < portNodeCnt; portNodeId++)
                    {
                        int coId = world.PortNode2Coord(quantityId, (uint)portId, portNodeId);
                        int nodeId = world.Coord2Node(quantityId, coId);
                        System.Numerics.Complex value = eVec[portNodeId];
                        eigenEz[nodeId] = value;
                    }
                    double freq = freqs[iMode];
                    double waveLength = Constants.C0 / freq;
                    normalizedFreq = latticeA / waveLength;
                }
                // light line
                double lightLine = normalizedBeta * latticeA / periodicDistance;

                string ret;
                string CRLF = System.Environment.NewLine;
                ret = "β(d/(2π))" + normalizedBeta + CRLF;
                ret += "a/λ = " + normalizedFreq + CRLF +
                      "light line = " + lightLine + CRLF;
                System.Diagnostics.Debug.WriteLine(ret);
                if (normalizedFreq < double.MaxValue)
                {
                    series1.Points.Add(new DataPoint(normalizedBeta, normalizedFreq));
                }
                series2.Points.Add(new DataPoint(normalizedBeta, lightLine));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();

                if (normalizedFreq < double.MaxValue)
                {
                    // eigenEz
                    world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, eigenEz);

                    fieldDrawerArray.Update(world);
                    mainWindow.GLControl.Invalidate();
                    mainWindow.GLControl.Update();
                    WPFUtils.DoEvents();
                }
            }
        }
    }
}
