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
        public void HWaveguideHigherOrderABCTDProblemb2(MainWindow mainWindow)
        {
            // 直線導波路を解く
            double[] freqs;
            System.Numerics.Complex[] freqDomainAmpsInc;
            SolveHWaveguideHigherOrderABCTDProblemb2_0(
                mainWindow, out freqs, out freqDomainAmpsInc);

            WPFUtils.DoEvents(10 * 1000);

            // 対象導波路を解く(直線導波路と同じ導波路、同じ計算条件である必要がある)
            SolveHWaveguideHigherOrderABCTDProblemb2(mainWindow, freqDomainAmpsInc);
        }

        public void SolveHWaveguideHigherOrderABCTDProblemb2(
            MainWindow mainWindow, System.Numerics.Complex[] freqDomainAmpsInc)
        {
            double waveguideWidth = 1.0;
            //double eLen = waveguideWidth * (0.95 * 1.0 / 30.0);
            double eLen = waveguideWidth * (0.95 * 1.0 / 30.0);

            // 導波管不連続領域の長さ
            double disconLength = 2.0 * waveguideWidth;
            // 誘電体スラブ導波路幅
            double coreWidth = waveguideWidth * (4.0 / 30.0);
            // 誘電体スラブ比誘電率
            double coreEps = 3.6 * 3.6;
            double claddingEps = 3.24 * 3.24;
            double replacedMu0 = Constants.Ep0; // TMモード

            // 形状設定で使用する単位長さ
            double unitLen = waveguideWidth / 20.0;
            // 励振位置
            double srcPosX = 5 * unitLen;
            // 誘電体終端
            double terminalPosX = 1.0 * waveguideWidth;
            System.Diagnostics.Debug.Assert(terminalPosX > srcPosX);
            // 観測点
            int port1OfsX = 5;
            int port2OfsX = 5;
            double port1PosX = srcPosX + port1OfsX * unitLen;
            double port2PosX = disconLength - port2OfsX * unitLen;

            // 時間刻み幅の算出
            double courantNumber = 0.5;
            // Note: timeLoopCnt は 2^mでなければならない
            //int timeLoopCnt = 4096;
            int timeLoopCnt = 2048;
            double timeDelta = courantNumber * eLen / (Constants.C0 * Math.Sqrt(2.0));
            // 励振源
            // 規格化周波数
            // 2Wc √(n1^2 - n2^2) / λ (Wc:コアの幅)
            //double srcNormalizedFreq = 0.10;
            double srcNormalizedFreq = 0.75;
            // 波長
            double srcWaveLength = 2.0 * coreWidth * Math.Sqrt(coreEps - claddingEps) / srcNormalizedFreq;
            // 周波数
            double srcFreq = Constants.C0 / srcWaveLength;
            // 角周波数
            double srcOmega = 2.0 * Math.PI * srcFreq;
            // 計算する周波数領域
            double normalizedFreq1 = 0.50;
            double normalizedFreq2 = 1.00;
            // 規格化周波数変換
            Func<double, double> toNormalizedFreq =
                waveLength => 2.0 * coreWidth * Math.Sqrt(coreEps - claddingEps) / waveLength;
            // 吸収境界条件の次数
            //int[] abcOrders = { 1, 1 };
            int[] abcOrders = { 1, 5 };

            // ガウシアンパルス？ (true: default ガウシアンパルス false: 正弦波)
            bool isGaussian = true;
            // ガウシアンパルス
            //GaussianType gaussianType = GaussianType.Normal; // 素のガウシアンパルス
            //GaussianType gaussianType = GaussianType.SinModulation; // 正弦波変調
            GaussianType gaussianType = GaussianType.SinModulation;
            double gaussianT0 = 0;
            double gaussianTp = 0;
            if (gaussianType == GaussianType.Normal)
            {
                // ガウシアンパルス
                gaussianT0 = 20.0 * timeDelta;
                gaussianTp = gaussianT0 / (2.0 * Math.Sqrt(2.0 * Math.Log(10.0)));
            }
            else if (gaussianType == GaussianType.SinModulation)
            {
                // 正弦波変調ガウシアンパルス
                // 搬送波の振動回数
                int nCycle = 5;
                gaussianT0 = 1.00 * (1.0 / srcFreq) * nCycle / 2.0;
                gaussianTp = gaussianT0 / (2.0 * Math.Sqrt(2.0 * Math.Log(2.0)));
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }

            const int portCnt = 2;
            double coreY1 = (waveguideWidth - coreWidth) * 0.5;
            double coreY2 = coreY1 + coreWidth;
            uint loopCnt = 11;
            uint[] coreLIds = { 7, 9, 11 };
            uint[] claddingLIds = { 1, 2, 3, 6, 8, 10 };
            uint[] port1EIds = { 1, 18, 17 };
            uint[] port1CoreEIds = { 18 };
            uint[] port1CladdingEIds = { 1, 17 };
            uint[] port2EIds = { 7 };
            uint[] port2CoreEIds = { };
            uint[] port2CladdingEIds = { };
            uint[] refport1EIds = { 14, 22, 21 };
            uint[] refport1CoreEIds = { 22 };
            uint[] refport1CladdingEIds = { 14, 21 };
            uint[] refport2EIds = { 16 };
            uint[] refport2CoreEIds = { };
            uint[] refport2CladdingEIds = { };
            uint[] portSrcEIds = { 13, 24, 23 };
            uint[] portSrcCoreEIds = { 24 };
            uint[] portSrcCladdingEIds = { 13, 23 };
            // 観測点ポート数
            int refPortCnt = 2;
            // メッシュの長さ
            double[] eLens = new double[loopCnt];
            for (int i = 0; i < loopCnt; i++)
            {
                uint lId = (uint)(i + 1);
                //double workeLen = eLen;
                double workeLen = coreLIds.Contains(lId) ? (eLen * 0.5) : eLen;
                eLens[i] = workeLen;
            }
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(srcPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PosX, 0.0));
                pts.Add(new OpenTK.Vector2d(terminalPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port2PosX, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port2PosX, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(terminalPosX, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1PosX, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(srcPosX, waveguideWidth));
                uint _lId1 = cad2D.AddPolygon(pts).AddLId;
                uint _lId2 = cad2D.ConnectVertexLine(3, 12).AddLId;
                uint _lId3 = cad2D.ConnectVertexLine(4, 11).AddLId;
                uint _lId4 = cad2D.ConnectVertexLine(5, 10).AddLId;
                uint _lId5 = cad2D.ConnectVertexLine(6, 9).AddLId;
                System.Diagnostics.Debug.Assert(_lId1 == 1);
                System.Diagnostics.Debug.Assert(_lId2 == 2);
                System.Diagnostics.Debug.Assert(_lId3 == 3);
                System.Diagnostics.Debug.Assert(_lId4 == 4);
                System.Diagnostics.Debug.Assert(_lId5 == 5);
            }
            // スラブ導波路と境界の交点
            {
                // 入力面と終端と参照面、励振面
                double[] portXs = { 0.0, terminalPosX, port1PosX, srcPosX };
                uint[] parentEIds = { 1, 15, 14, 13 };
                IList<uint[]> coreVIds = new List<uint[]>();
                for (int index = 0; index < portXs.Length; index++)
                {
                    double portX = portXs[index];
                    uint parentEId = parentEIds[index];

                    double workY1 = 0.0;
                    double workY2 = 0.0;
                    if (index == 0)
                    {
                        // 入力面
                        workY1 = coreY1;
                        workY2 = coreY2;
                    }
                    else
                    {
                        workY1 = coreY2;
                        workY2 = coreY1;
                    }
                    uint vId1 = cad2D.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY1)).AddVId;
                    uint vId2 = cad2D.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY2)).AddVId;
                    uint[] workVIds = new uint[2];
                    if (index == 0)
                    {
                        // 入力面
                        workVIds[0] = vId1;
                        workVIds[1] = vId2;
                    }
                    else
                    {
                        workVIds[0] = vId2;
                        workVIds[1] = vId1;
                    }
                    coreVIds.Add(workVIds);
                }
                // スラブ導波路
                {
                    // 励振面の左側領域
                    {
                        uint workVId1 = coreVIds[0][0];
                        uint workVId2 = coreVIds[3][0];
                        uint workEId = cad2D.ConnectVertexLine(workVId1, workVId2).AddEId;
                    }
                    {
                        uint workVId1 = coreVIds[0][1];
                        uint workVId2 = coreVIds[3][1];
                        uint workEId = cad2D.ConnectVertexLine(workVId1, workVId2).AddEId;
                    }
                    // 励振面の右側領域
                    {
                        uint workVId1 = coreVIds[3][0];
                        uint workVId2 = coreVIds[2][0];
                        uint workEId = cad2D.ConnectVertexLine(workVId1, workVId2).AddEId;
                    }
                    {
                        uint workVId1 = coreVIds[3][1];
                        uint workVId2 = coreVIds[2][1];
                        uint workEId = cad2D.ConnectVertexLine(workVId1, workVId2).AddEId;
                    }
                    //
                    {
                        uint workVId1 = coreVIds[2][0];
                        uint workVId2 = coreVIds[1][0];
                        uint workEId = cad2D.ConnectVertexLine(workVId1, workVId2).AddEId;
                    }
                    {
                        uint workVId1 = coreVIds[2][1];
                        uint workVId2 = coreVIds[1][1];
                        uint workEId = cad2D.ConnectVertexLine(workVId1, workVId2).AddEId;
                    }
                }
            }

            // check
            {
                double[] coreColor = { 0.6, 0.0, 0.0 };
                foreach (uint lId in coreLIds)
                {
                    cad2D.SetLoopColor(lId, coreColor);
                }
                double[] edgeCoreColor = { 0.4, 0.0, 0.0 };
                uint[][] portCoreEIdss = { 
                    port1CoreEIds, port2CoreEIds, refport1CoreEIds, refport2CoreEIds, portSrcCoreEIds };
                foreach (uint[] eIds in portCoreEIdss)
                {
                    foreach (uint eId in eIds)
                    {
                        cad2D.SetEdgeColor(eId, edgeCoreColor);
                    }
                }
                double[] claddingColor = { 0.5, 1.0, 0.5 };
                foreach (uint lId in claddingLIds)
                {
                    cad2D.SetLoopColor(lId, claddingColor);
                }
                double[] edgeCladdingColor = { 0.3, 0.8, 0.3 };
                uint[][] portCladdingEIdss = {
                    port1CladdingEIds, port2CladdingEIds,
                    refport1CladdingEIds, refport2CladdingEIds, portSrcCladdingEIds };
                foreach (uint[] eIds in portCladdingEIdss)
                {
                    foreach (uint eId in eIds)
                    {
                        cad2D.SetEdgeColor(eId, edgeCladdingColor);
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

            //Mesher2D mesher2D = new Mesher2D(cad2D, eLen);
            Mesher2D mesher2D = new Mesher2D();
            mesher2D.SetMeshingModeElemLength();
            for (int i = 0; i < loopCnt; i++)
            {
                uint lId = (uint)(i + 1);
                double workeLen = eLens[i];
                mesher2D.AddCutMeshLoopCadId(lId, workeLen);
            }
            mesher2D.Meshing(cad2D);
            
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
                uint dof = 1; // スカラー
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint vacuumMaId = 0;
            uint claddingMaId = 0;
            uint coreMaId = 0;
            {
                world.ClearMaterial();
                DielectricMaterial vacuumMa = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                // Note: TMモード(比誘電率は比透磁率のところにセットする)
                DielectricMaterial claddingMa = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = claddingEps,
                    Muyy = claddingEps,
                    Muzz = claddingEps
                };
                DielectricMaterial coreMa = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = coreEps,
                    Muyy = coreEps,
                    Muzz = coreEps
                };
                vacuumMaId = world.AddMaterial(vacuumMa);
                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);
            }
            {
                uint[] lIds = new uint[loopCnt];
                for (int i = 0; i < loopCnt; i++)
                {
                    lIds[i] = (uint)(i + 1);
                }
                uint[] maIds = new uint[lIds.Length];
                for (int i = 0; i < loopCnt; i++)
                {
                    uint lId = lIds[i];
                    uint maId = vacuumMaId;
                    if (coreLIds.Contains(lId))
                    {
                        maId = coreMaId;
                    }
                    else if (claddingLIds.Contains(lId))
                    {
                        maId = claddingMaId;
                    }
                    else
                    {
                        maId = vacuumMaId;
                    }
                    maIds[i] = maId;
                }
                for (int i = 0; i < loopCnt; i++)
                {
                    uint lId = lIds[i];
                    uint maId = maIds[i];
                    world.SetCadLoopMaterial(lId, maId);
                }
            }
            {
                // 入出力面、励振面
                uint[][] portEIdss = { port1EIds, port2EIds, refport1EIds, refport2EIds, portSrcEIds };
                uint[][] portCoreEIdss = {
                    port1CoreEIds, port2CoreEIds, refport1CoreEIds, refport2CoreEIds, portSrcCoreEIds };
                uint[][] portCladdingEIdss = {
                    port1CladdingEIds, port2CladdingEIds,
                    refport1CladdingEIds, refport2CladdingEIds, portSrcCladdingEIds };
                for (int eIdIndex = 0; eIdIndex < portEIdss.Length; eIdIndex++)
                {
                    uint[] eIds = portEIdss[eIdIndex];
                    IList<uint> portCoreEIds = portCoreEIdss[eIdIndex].ToList();
                    IList<uint> portCladdingEIds = portCladdingEIdss[eIdIndex].ToList();
                    int edgeCnt = eIds.Length;
                    uint[] maIds = new uint[edgeCnt];
                    for (int i = 0; i < edgeCnt; i++)
                    {
                        uint eId = eIds[i];
                        uint maId = vacuumMaId;
                        if (portCoreEIds.Contains(eId))
                        {
                            maId = coreMaId;
                        }
                        else if (portCladdingEIds.Contains(eId))
                        {
                            maId = claddingMaId;
                        }
                        else
                        {
                            maId = vacuumMaId;
                        }
                        maIds[i] = maId;
                    }
                    for (int i = 0; i < edgeCnt; i++)
                    {
                        uint eId = eIds[i];
                        uint maId = maIds[i];
                        world.SetCadEdgeMaterial(eId, maId);
                    }
                }
            }
            {
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                uint[][] _portEIdss = { port1EIds, port2EIds, refport1EIds, refport2EIds, portSrcEIds };
                IList<IList<uint>> portEIdss = new List<IList<uint>>();
                foreach (uint[] _portEIds in _portEIdss)
                {
                    IList<uint> __portEIds = _portEIds.ToList();
                    portEIdss.Add(__portEIds);

                }
                foreach (IList<uint> portEIds in portEIdss)
                {
                    // スカラー
                    PortCondition portCondition = new PortCondition(portEIds, FieldValueType.Scalar);
                    portConditions.Add(portCondition);
                }
            }
            /*
            uint[] zeroEIds = { };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                zeroFixedCads.Add(fixedCad);
            }
            */

            world.MakeElements();

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

            var FEM = new EMWaveguide2DHPlaneHigherOrderABCTDFEM(world);
            FEM.ABCOrdersToSet = abcOrders.ToList();
            FEM.TimeLoopCnt = timeLoopCnt;
            FEM.TimeIndex = 0;
            FEM.TimeDelta = timeDelta;
            FEM.IsGaussian = isGaussian;
            FEM.GaussianType = gaussianType;
            FEM.GaussianT0 = gaussianT0;
            FEM.GaussianTp = gaussianTp;
            FEM.SrcFrequency = srcFreq;
            FEM.ReplacedMu0 = replacedMu0;
            // 観測点ポート数
            FEM.RefPortCount = refPortCnt;

            /*
            // 逆行列を使わない
            FEM.IsUseInvMatrix = false;
            {
                {
                    var solver = new IvyFEM.Linear.LapackEquationSolver();
                    solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.IsOrderingToBandMatrix = true;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
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
            }
            */

            if (ChartWindow2 == null)
            {
                ChartWindow2 = new ChartWindow();
                ChartWindow2.Closed += ChartWindow2_Closed;
            }
            {
                ChartWindow chartWin = ChartWindow2;
                chartWin.Owner = mainWindow;
                chartWin.Left = mainWindow.Left + mainWindow.Width;
                chartWin.Top = mainWindow.Top;
                chartWin.Show();
                var model = new PlotModel();
                chartWin.Plot.Model = model;
                model.Title = "hz(t): Time Domain";
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
                    Title = "hz(t)"
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
                    int nodeCntB = FEM.RefTimeEzsss[0][timeIndex].Length;
                    int refNodeIdB = nodeCntB / 2;
                    double ezPort1 = FEM.RefTimeEzsss[0][timeIndex][refNodeIdB];
                    double ezPort2 = FEM.RefTimeEzsss[1][timeIndex][refNodeIdB];
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
                ChartWindow1.Closed += ChartWindow1_Closed;
            }
            {
                ChartWindow chartWin = ChartWindow1;
                chartWin.Owner = mainWindow;
                chartWin.Left = mainWindow.Left + mainWindow.Width;
                chartWin.Top = mainWindow.Top + ChartWindow2.Height;
                chartWin.Show();
                var model = new PlotModel();
                chartWin.Plot.Model = model;
                model.Title = "Waveguide Example";
                var axis1 = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "2Wc√(n1^2 - n2^2) /λ",
                    Minimum = normalizedFreq1,
                    Maximum = normalizedFreq2
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
                // 2Wc √(n1^2 - n2^2) / λ
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
                ret = "2W/λ: " + normalizedFreq + CRLF;
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
