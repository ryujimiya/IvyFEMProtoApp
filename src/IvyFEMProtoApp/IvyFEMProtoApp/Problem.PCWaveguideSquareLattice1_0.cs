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
        public void PCWaveguideSquareLatticeProblem1_0(MainWindow mainWindow)
        {
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

            // フォトニック結晶導波路の場合、a/λを規格化周波数とする
            double sFreq = 0.330;
            double eFreq = 0.440;
            int freqDiv = 20;

            const uint portCnt = 2;
            // 導波路不連続領域の長さ
            //const int disconRodCnt = 5;
            const int disconRodCnt = 1;
            double disconLength = latticeA * disconRodCnt;
            // 入出力導波路の周期構造部分の長さ
            double inputWgLength = latticeA;
            IList<uint> rodLoopIds = new List<uint>();
            IList<uint> inputWgRodLoopIds1 = new List<uint>();
            IList<uint> inputWgRodLoopIds2 = new List<uint>();

            Cad2D cad = new Cad2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                // 領域追加
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(inputWgLength, 0.0));
                pts.Add(new OpenTK.Vector2d(inputWgLength + disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(inputWgLength * 2 + disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(inputWgLength * 2 + disconLength, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(inputWgLength + disconLength, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(inputWgLength, waveguideWidth));
                uint lId = cad.AddPolygon(pts).AddLId;
                // 入出力領域を分離
                uint eIdAdd1 = cad.ConnectVertexLine(3, 8).AddEId;
                uint eIdAdd2 = cad.ConnectVertexLine(4, 7).AddEId;
            }

            // 入出力導波路の周期構造境界上の頂点を追加
            //  逆から追加しているのは、頂点によって新たに生成される辺に頂点を追加しないようにするため
            // 入力導波路
            {
                uint eId = 1;
                double x1 = 0.0;
                double y1 = waveguideWidth;
                double y2 = 0.0;
                PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x1, y2);
            }
            {
                uint eId = 9;
                double x1 = inputWgLength;
                double y1 = 0.0;
                double y2 = waveguideWidth;
                PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x1, y2);
            }
            // 出力導波路
            {
                uint eId = 5;
                double x1 = inputWgLength * 2 + disconLength;
                double y1 = 0.0;
                double y2 = waveguideWidth;
                PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x1, y2);
            }
            {
                uint eId = 10;
                double x1 = inputWgLength + disconLength;
                double y1 = 0.0;
                double y2 = waveguideWidth;
                PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x1, y2);
            }
            // ロッドを追加
            int rodCntInputWg1 = 1;
            int rodCntInputWg2 = 1;
            int rodCntAll = rodCntInputWg1 + rodCntInputWg2 + disconRodCnt;
            for (int col = 0; col < rodCntAll; col++)
            {
                uint baseLoopId = 0;
                int inputWgNo = 0;
                if (col >= 0 && col < rodCntInputWg1)
                {
                    baseLoopId = 1;
                    inputWgNo = 1;
                }
                else if (col >= rodCntInputWg1 && col < rodCntInputWg1 + disconRodCnt)
                {
                    baseLoopId = 2;
                    inputWgNo = 0;
                }
                else if (col >= rodCntInputWg1 + disconRodCnt && col < rodCntInputWg1 + rodCntInputWg2 + disconRodCnt)
                {
                    baseLoopId = 3;
                    inputWgNo = 2;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                for (int row = 0; row < rodCntHalf; row++)
                {
                    double x0 = latticeA * 0.5 + col * latticeA;
                    double y0 = waveguideWidth - row * latticeA - latticeA * 0.5;
                    uint lId = PCWaveguideUtils.AddRod(
                        cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
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
                for (int row = 0; row < rodCntHalf; row++)
                {
                    double x0 = latticeA * 0.5 + col * latticeA;
                    double y0 = latticeA * rodCntHalf - row * latticeA - latticeA * 0.5;
                    uint lId = PCWaveguideUtils.AddRod(
                        cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
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
                uint dof = 1; // 複素数
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint claddingMaId = 0;
            uint coreMaId = 0;
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
            // ポート情報リスト作成
            bool[] isPortBc2Reverse = { true, false };
            for (int portId = 0; portId < portCnt; portId++)
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
            // 周期構造境界1
            for (int portIndex = 0; portIndex < portCnt; portIndex++)
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];

                if (portIndex == 0)
                {
                    eIds[0] = 1;
                }
                else if (portIndex == 1)
                {
                    eIds[0] = 5;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                maIds[0] = claddingMaId;

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    if (portIndex == 0)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) - (i - 1));
                    }
                    else if (portIndex == 1)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) * 3 - (i - 1));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                    maIds[i] = claddingMaId;
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
            // 周期構造境界2
            for (int portId = 0; portId < portCnt; portId++)
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];

                if (portId == 0)
                {
                    eIds[0] = 9;
                }
                else if (portId == 1)
                {
                    eIds[0] = 10;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                maIds[0] = claddingMaId;

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    if (portId == 0)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) * 2 - (i - 1));
                    }
                    else if (portId == 1)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) * 4 - (i - 1));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
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
                        CadElementType.Edge,
                        lIds, bcEIds1, bcEIds2, FieldValueType.ZScalar, new List<uint> { 0 }, 0);
                    portConditions.Add(portCondition);
                }
            }
            // 強制境界
            uint[] zeroEIds = { 2, 3, 4, 6, 7, 8 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
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
