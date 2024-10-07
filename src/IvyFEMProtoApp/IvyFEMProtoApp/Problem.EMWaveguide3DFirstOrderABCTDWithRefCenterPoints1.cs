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
        public void EMWaveguide3DFirstOrderABCTDWithRefCenterPointsProblem1(MainWindow mainWindow, uint feOrder)
        {
            // 直線導波路を解く
            double[] freqs;
            System.Numerics.Complex[][] freqDomainAmpsInc;
            SolveEMWaveguide3DFirstOrderABCTDWithRefCenterPointsProblem1_0(
                mainWindow, feOrder, out freqs, out freqDomainAmpsInc);

            WPFUtils.DoEvents(10 * 1000);

            // 対象導波路を解く(直線導波路と同じ導波路、同じ計算条件である必要がある)
            SolveEMWaveguide3DFirstOrderABCTDWithRefCenterPointsProblem1(mainWindow, feOrder, freqDomainAmpsInc);
        }

        public void SolveEMWaveguide3DFirstOrderABCTDWithRefCenterPointsProblem1(
            MainWindow mainWindow, uint feOrder, System.Numerics.Complex[][] freqDomainAmpsInc)
        {
            /////////////////////
            Dimension = 3; // 3次元
            var camera3D = mainWindow.Camera as Camera3D;
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 70.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            /////////////////////

            double waveguideWidth = 1.0;
            double waveguideHeight = 0.5;
            //double disconLength0 = 1.0;
            //double disconLength0 = 2.0;
            double disconLength0 = 1.0;
            //double deltaDistance = 0.1;
            //double deltaDistance = 0.2;
            //double deltaDistance = 1.0;
            double deltaDistance = 0.2;
            double disconLength = disconLength0 + deltaDistance * 3.0;
            // 励振位置
            double srcPosX = deltaDistance;
            double eLen = 0.0;
            // ポート1参照面
            double refPort1PosX = srcPosX + deltaDistance;
            // ポート2参照面
            double refPort2PosX = disconLength - deltaDistance;
            //---------------------------
            // 誘電体
            double wa1 = 0.556 / 2.0;
            double wa2 = 0.888 / 2.0;
            double wb1 = 0.399 / 2.0;
            double wc1 = 0.8 / 2.0;
            double pos1X = (disconLength - refPort1PosX - (disconLength - refPort2PosX) - wc1) / 2.0;
            double pos1Y = wa1;
            double pos2X = pos1X + wc1;
            double pos2Y = pos1Y + wa2;

            double dielectricEp = 6.0;
            //---------------------------
            // eLen
            if (feOrder == 1)
            {
                //eLen = 0.05;
                //eLen = 0.20;
                //eLen = 0.10;
                //eLen = 0.15;
                eLen = 0.15;
            }
            else if (feOrder == 2)
            {
                eLen = 0.20;//0.25;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }

            // 時間刻み幅の算出
            double courantNumber = 0.5;
            // Note: timeLoopCnt は 2^mでなければならない
            //int timeLoopCnt = 2048;
            int timeLoopCnt = 2048;
            double timeStep = courantNumber * eLen / (Constants.C0 * Math.Sqrt(3.0));
            // 励振源
            // 規格化周波数
            //double srcNormalizedFreq = 2.0;
            //double srcNormalizedFreq = 1.6;
            double srcNormalizedFreq = 1.05;//2.0;
            // 波長
            double srcWaveLength = 2.0 * waveguideWidth / srcNormalizedFreq;
            // 周波数
            double srcFreq = Constants.C0 / srcWaveLength;
            // 計算する周波数領域
            double normalizedFreq1 = 1.0;
            double normalizedFreq2 = 1.6;//2.0;
            double waveLength1 = 2.0 * waveguideWidth / normalizedFreq1;
            double waveLength2 = 2.0 * waveguideWidth / normalizedFreq2;
            double freq1 = Constants.C0 / waveLength1;
            double freq2 = Constants.C0 / waveLength2;
            // 規格化周波数変換
            Func<double, double> toNormalizedFreq =
                waveLength => 2.0 * waveguideWidth / waveLength;

            // ガウシアンパルス
            GaussianType gaussianType = GaussianType.SinModulation; // 正弦波変調
            // 搬送波の振動回数
            //int nCycle = 5;
            //int nCycle = 1; // 素のガウシアン
            int nCycle = 5;
            double gaussianAmp = 1.0;
            double gaussianT0 = 1.00 * (1.0 / srcFreq) * nCycle / 2.0;
            double gaussianTp = gaussianT0 / (2.0 * Math.Sqrt(2.0 * Math.Log(2.0)));

            IList<uint> shellLIds1;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(srcPosX, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(srcPosX, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(srcPosX, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(srcPosX, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(refPort1PosX, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(refPort1PosX, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(refPort1PosX, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(refPort1PosX, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(refPort2PosX, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(refPort2PosX, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(refPort2PosX, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(refPort2PosX, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(disconLength, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(disconLength, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(disconLength, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(disconLength, 0.0, waveguideHeight));

                uint layerCnt = 4;
                cad.AddCubeWithMultiLayers(pts, layerCnt);
            }
            {
                Loop3D tmpLoop = cad.GetLoop(10);
                //cad.AddRectLoop(10, new OpenTK.Vector2d(pos1X, pos1Y), new OpenTK.Vector2d(pos2X, pos2Y));
                //座標の与え方はXDirに依存する!!!!!
                System.Diagnostics.Debug.Assert(Math.Abs(tmpLoop.XDir.Y - 1.0) < 1.0e-12); // Y方向がローカルXDir
                cad.AddRectLoop(10, new OpenTK.Vector2d(pos1Y, pos1X), new OpenTK.Vector2d(pos2Y, pos2X));
                cad.LiftLoop(22, cad.GetLoop(22).Normal * (+wb1));
            }
            {
                var eIds = new List<uint> { 37, 38, 39, 40 };
                cad.MakeRadialLoop(eIds);
            }
            System.Diagnostics.Debug.Assert(cad.IsElementId(CadElementType.Loop, 27)); // 27までのはず
            System.Diagnostics.Debug.Assert(!cad.IsElementId(CadElementType.Loop, 28)); // 27までのはず
            {
                IList<uint> lIds1 = new List<uint> {
                    1, 2, 3, 4, 5, 19
                };
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(lIds1, holes1, insideVIds1);

                IList<uint> lIds2 = new List<uint> {
                    19, 6, 7, 8, 9, 20
                };
                IList<OpenTK.Vector3d> holes2 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds2 = new List<uint>();
                uint sId2 = cad.AddSolid(lIds2, holes2, insideVIds2);

                IList<uint> lIds3 = new List<uint> {
                    20, 10,
                    22, 23, 24, 25, 26,
                    11, 12, 13, 21
                };
                IList<OpenTK.Vector3d> holes3 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds3 = new List<uint>();
                uint sId3 = cad.AddSolid(lIds3, holes3, insideVIds3);

                IList<uint> lIds4 = new List<uint> {
                    21, 14, 15, 16, 17, 18
                };
                IList<OpenTK.Vector3d> holes4 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds4 = new List<uint>();
                uint sId4 = cad.AddSolid(lIds4, holes4, insideVIds4);

                IList<uint> lIds5 = new List<uint> {
                    22, 23, 24, 25, 26, 27
                };
                IList<OpenTK.Vector3d> holes5 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds5 = new List<uint>();
                uint sId5 = cad.AddSolid(lIds5, holes5, insideVIds5);
            }

            /*
            // 電気壁
            cad.SetLoopColor(2, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(3, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(4, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(5, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(6, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(7, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(8, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(9, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(10, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(11, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(12, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(13, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(14, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(15, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(16, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(17, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(27, new double[] { 0.0, 0.0, 0.0 });
            // ポート
            cad.SetLoopColor(1, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(1, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(2, new double[] { 0.0, 0.0, 1.0 });

            cad.SetLoopColor(18, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(33, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(34, new double[] { 0.0, 0.0, 1.0 });
            // 励振面
            cad.SetLoopColor(19, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(9, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(10, new double[] { 0.0, 0.0, 1.0 });
            // ポート1参照面
            cad.SetLoopColor(20, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(17, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(18, new double[] { 0.0, 0.0, 1.0 });
            // ポート2参照面
            cad.SetLoopColor(21, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(25, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(26, new double[] { 0.0, 0.0, 1.0 });
            */

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad3DDrawer(cad);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            Mesher3D mesher = new Mesher3D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            var drawerArray1 = mainWindow.DrawerArray;
            drawerArray1.Clear();
            var drawer1 = new Mesher3DDrawer(mesher);
            drawer1.IsMask = true;
            mainWindow.DrawerArray.Add(drawer1);
            mainWindow.Camera.Fit(drawerArray1.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            uint scalarQuantityId; // ポートの固有値問題にスカラー節点が必要
            {
                uint dof1 = 1; // スカラー
                uint dof2 = 1;
                uint feOrder1 = feOrder;
                uint feOrder2 = feOrder;
                quantityId = world.AddQuantity(dof1, feOrder1, FiniteElementType.Edge);
                scalarQuantityId = world.AddQuantity(dof2, feOrder2, FiniteElementType.ScalarLagrange);
            }

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
                DielectricMaterial dielectricMa = new DielectricMaterial
                {
                    Epxx = dielectricEp,
                    Epyy = dielectricEp,
                    Epzz = dielectricEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                uint maId1 = world.AddMaterial(vacuumMa);
                uint maId2 = world.AddMaterial(dielectricMa);

                uint[] sIds = { 1, 2, 3, 4 };
                foreach (uint sId1 in sIds)
                {
                    world.SetCadSolidMaterial(sId1, maId1);
                }
                uint dielectricSId = 5;
                world.SetCadSolidMaterial(dielectricSId, maId2);

                uint[] lIds = { 1, 18, 20, 21, 19 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId1);
                }
            }

            int refPortCnt = 2;
            {
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                // ABCポート1, ABCポート2, 参照面1, 参照面2, 励振面の順
                uint[] lIds = { 1, 18, 20, 21, 19 };
                uint[][] dirEIdss =
                {
                    new uint[] { 1, 2 }, new uint[] { 33, 34 },
                    new uint[] { 17, 18 }, new uint[] { 25, 26 },
                    new uint[] { 9, 10 } 
                };
                IList<IList<uint>> portLIdss = new List<IList<uint>>();
                foreach (uint lId in lIds)
                {
                    IList<uint> portLIds = new List<uint>();
                    {
                        portLIds.Add(lId);
                    }
                    portLIdss.Add(portLIds);
                }
                for (int portId = 0; portId < portLIdss.Count; portId++)
                {
                    IList<uint> portLIds = portLIdss[portId];
                    uint[] dirEIds = dirEIdss[portId];
                    PortCondition portCondition = new PortCondition(portLIds, CadElementType.Loop, FieldValueType.ZScalar);
                    portCondition.IntAdditionalParameters = new int[] { (int)dirEIds[0], (int)dirEIds[1] };
                    portConditions.Add(portCondition);
                }
            }
            {
                IList<PortCondition> portConditions = world.GetPortConditions(scalarQuantityId);
                uint[] lIds = { 1, 18, 20, 21, 19 };
                IList<IList<uint>> portLIdss = new List<IList<uint>>();
                foreach (uint lId in lIds)
                {
                    IList<uint> portLIds = new List<uint>();
                    {
                        portLIds.Add(lId);
                    }
                    portLIdss.Add(portLIds);
                }
                foreach (IList<uint> portLIds in portLIdss)
                {
                    PortCondition portCondition = new PortCondition(portLIds, CadElementType.Loop, FieldValueType.ZScalar);
                    portConditions.Add(portCondition);
                }
            }
            //------------------------------------------------------------
            double[] refCenterPoint1 = { refPort1PosX, 0.5 * waveguideWidth, 0.5 * waveguideHeight };
            double[] refCenterPoint2 = { refPort2PosX, 0.5 * waveguideWidth, 0.5 * waveguideHeight };
            double[][] refCenterPoints = { refCenterPoint1, refCenterPoint2 };
            //-----------------------------------------------------------

            uint[] zeroLIds = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 27 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint lId in zeroLIds)
            {
                // スカラー(辺方向成分)
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Scalar);
                zeroFixedCads.Add(fixedCad);
            }
            uint[] scalarZeroLIds = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 27 };
            var scalarZeroFixedCads = world.GetZeroFieldFixedCads(scalarQuantityId);
            foreach (uint lId in scalarZeroLIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Scalar);
                scalarZeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            //-------------------------------------------------------------------
            uint valueId = 0;
            uint vecValueId = 0;
            VectorFieldDrawer vectorDrawer;
            EdgeFieldDrawer edgeDrawer;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // スカラー(辺方向成分)
                // ElementEdgeを指定
                valueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    quantityId, FieldValueNodeType.ElementEdge, FieldShowType.Real);

                // Vector3
                vecValueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    quantityId, true, FieldShowType.Real);
                vectorDrawer = new VectorFieldDrawer(
                    vecValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
            }
            //------------------------------------------
            // 全体領域
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(vectorDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }
            //------------------------------------------

            double dt = timeStep;
            var FEM = new EMWaveguide3DFirstOrderABCTDFEMWithRefPoints(world);
            FEM.TimeLoopCnt = timeLoopCnt;
            FEM.TimeIndex = 0;
            FEM.TimeStep = timeStep;
            FEM.GaussianType = gaussianType;
            FEM.GaussianT0 = gaussianT0;
            FEM.GaussianTp = gaussianTp;
            FEM.SrcFrequency = srcFreq;
            FEM.GaussianAmp = gaussianAmp;
            FEM.StartFrequencyForSMatrix = freq1;
            FEM.EndFrequencyForSMatrix = freq2;
            // 観測点(ポート数)
            FEM.RefPortCount = refPortCnt;
            // 参照面中心点
            FEM.RefCenterPoints = refCenterPoints;

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
                model.Title = "et(t): Time Domain";
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
                    Title = "et(t)"
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
                // 時間領域のE
                double[] E = FEM.E;
                double[] coordExyz = FEM.CoordExyz;

                {
                    int timeIndex = FEM.TimeIndex;
                    double[] exyzPort1 = FEM.RefTimeExyzsss[0][timeIndex];
                    double[] exyzPort2 = FEM.RefTimeExyzsss[1][timeIndex];
                    var chartWin = ChartWindow2;
                    var model = chartWin.Plot.Model;
                    var series = model.Series;
                    var series1 = series[0] as LineSeries;
                    var series2 = series[1] as LineSeries;
                    //!!!!! FIXME: Z方向固定になっている
                    int dimIndex0 = 2;
                    series1.Points.Add(new DataPoint(timeIndex, exyzPort1[dimIndex0]));
                    series2.Points.Add(new DataPoint(timeIndex, exyzPort2[dimIndex0]));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }

                //--------------------------------------------------
                // Exyzを表示用にスケーリングする
                {
                    double maxValue = 0;
                    int cnt = coordExyz.Length;
                    foreach (System.Numerics.Complex value in coordExyz)
                    {
                        double abs = value.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.4;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            coordExyz[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                world.UpdateBubbleFieldValueValuesFromCoordValues(vecValueId, FieldDerivativeType.Value, coordExyz);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                //--------------------------------------------------

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
                    Title = "2W/λ",
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
            IList<System.Numerics.Complex[][]> freqDomainAmpss;
            IList<System.Numerics.Complex[]> Sss;
            FEM.CalcSParameter(freqDomainAmpsInc, out freqs, out freqDomainAmpss, out Sss);
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
