using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        public void EMWaveguide3DPMLTDProblem1_0(MainWindow mainWindow, uint feOrder)
        {
            double[] freqs;
            System.Numerics.Complex[] freqDomainAmpsInc;
            SolveEMWaveguide3DPMLTDProblem1_0(
                mainWindow, feOrder, out freqs, out freqDomainAmpsInc);
        }

        public void SolveEMWaveguide3DPMLTDProblem1_0(
            MainWindow mainWindow,
            uint feOrder,
            out double[] retFreqs,
            out System.Numerics.Complex[] retFreqDomainAmpsInc)
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

            retFreqs = null;
            retFreqDomainAmpsInc = null;

            double waveguideWidth = 1.0;
            double waveguideHeight = 0.5;
            double eLen = 0.0;

            // eLen
            if (feOrder == 1)
            {
                //eLen = 0.1;//0.15;
                eLen = 0.15;
            }
            else if (feOrder == 2)
            {
                eLen = 0.20;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }

            // 時間刻み幅の算出
            double courantNumber = 0.5;
            // Note: timeLoopCnt は 2^mでなければならない
            //int timeLoopCnt = 2048;
            int timeLoopCnt = 2048;//1024;
            //int execTimeLoopCnt = 0; // スキップしない
            //int execTimeLoopCnt = 512;//1024;
            int execTimeLoopCnt = 1024;
            double timeStep = courantNumber * eLen / (Constants.C0 * Math.Sqrt(3.0));
            // 励振源
            // 規格化周波数
            //double srcNormalizedFreq = 2.0;
            //double srcNormalizedFreq = 1.6;
            //double srcNormalizedFreq = 1.05;//2.0;
            double srcNormalizedFreq = 1.6;
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

            // 形状設定で使用する単位長さ
            double unitLen = waveguideWidth / 20.0;
            // PML層の厚さ
            //double pmlThickness = 10 * unitLen;
            double pmlThickness = 30 * unitLen;
            // 導波管不連続領域の長さ
            double disconLength = 1.0 * waveguideWidth + 2.0 * pmlThickness;
            // PML位置
            double port1PMLPosX = pmlThickness;
            double port2PMLPosX = disconLength - pmlThickness;
            // 励振位置
            double srcPosX = port1PMLPosX + 5 * unitLen;
            // 観測点
            int port1OfsX = 5;
            int port2OfsX = 5;
            double refPort1PosX = srcPosX + port1OfsX * unitLen;
            double refPort2PosX = port2PMLPosX - port2OfsX * unitLen;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(port1PMLPosX, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(port1PMLPosX, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(port1PMLPosX, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(port1PMLPosX, 0.0, waveguideHeight));

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

                pts.Add(new OpenTK.Vector3d(port2PMLPosX, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(port2PMLPosX, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(port2PMLPosX, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(port2PMLPosX, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(disconLength, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(disconLength, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(disconLength, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(disconLength, 0.0, waveguideHeight));

                uint layerCnt = 6;
                cad.AddCubeWithMultiLayers(pts, layerCnt);
            }
            System.Diagnostics.Debug.Assert(cad.IsElementId(CadElementType.Loop, 31)); // 31までのはず
            System.Diagnostics.Debug.Assert(!cad.IsElementId(CadElementType.Loop, 32)); // 31までのはず
            {
                IList<uint> lIds1 = new List<uint> {
                    1, 2, 3, 4, 5, 27
                };
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(lIds1, holes1, insideVIds1);

                IList<uint> lIds2 = new List<uint> {
                    27, 6, 7, 8, 9, 28
                };
                IList<OpenTK.Vector3d> holes2 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds2 = new List<uint>();
                uint sId2 = cad.AddSolid(lIds2, holes2, insideVIds2);

                IList<uint> lIds3 = new List<uint> {
                    28, 10, 11, 12, 13, 29
                };
                IList<OpenTK.Vector3d> holes3 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds3 = new List<uint>();
                uint sId3 = cad.AddSolid(lIds3, holes3, insideVIds3);

                IList<uint> lIds4 = new List<uint> {
                    29, 14, 15, 16, 17, 30
                };
                IList<OpenTK.Vector3d> holes4 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds4 = new List<uint>();
                uint sId4 = cad.AddSolid(lIds4, holes4, insideVIds4);

                IList<uint> lIds5 = new List<uint> {
                    30, 18, 19, 20, 21, 31
                };
                IList<OpenTK.Vector3d> holes5 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds5 = new List<uint>();
                uint sId5 = cad.AddSolid(lIds5, holes5, insideVIds5);

                IList<uint> lIds6 = new List<uint> {
                    31, 22, 23, 24, 25, 26
                };
                IList<OpenTK.Vector3d> holes6 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds6 = new List<uint>();
                uint sId6 = cad.AddSolid(lIds6, holes6, insideVIds6);
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
                uint vacuumMaId = 0;
                IList<uint> pmlMaIds = new List<uint>();

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
                DielectricPMLMaterial pmlMa1 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // X方向PML
                    Reflection0 = 1.0e-3,//1.0e-4,
                    OriginPoint3D = new OpenTK.Vector3d(port1PMLPosX, 0.0, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0,
                    ZThickness = 0.0
                };
                DielectricPMLMaterial pmlMa2 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // X方向PML
                    Reflection0 = 1.0e-3,//1.0e-4,
                    OriginPoint3D = new OpenTK.Vector3d(port2PMLPosX, 0.0, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0,
                    ZThickness = 0.0
                };

                vacuumMaId = world.AddMaterial(vacuumMa);
                uint pmlMaId1 = world.AddMaterial(pmlMa1);
                pmlMaIds.Add(pmlMaId1);
                uint pmlMaId2 = world.AddMaterial(pmlMa2);
                pmlMaIds.Add(pmlMaId2);

                uint[] sIds = { 2, 3, 4, 5 };
                foreach (uint sId1 in sIds)
                {
                    world.SetCadSolidMaterial(sId1, vacuumMaId);
                }

                uint pmlsId1 = 1;
                uint pmlsId2 = 6;
                world.SetCadSolidMaterial(pmlsId1, pmlMaId1);
                world.SetCadSolidMaterial(pmlsId2, pmlMaId2);

                uint[] lIds = { 29, 30, 28 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, vacuumMaId);
                }
            }

            int refPortCnt = 2;
            {
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                // 参照面1, 参照面2, 励振面の順
                uint[] lIds = { 29, 30, 28 };
                uint[][] dirEIdss =
                {
                    new uint[] { 25, 26 }, new uint[] { 33, 34 },
                    new uint[] { 17, 18 }
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
                uint[] lIds = { 29, 30, 28 };
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

            uint[] zeroLIds = {
                2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, 23, 24, 25 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint lId in zeroLIds)
            {
                // スカラー(辺方向成分)
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Scalar);
                zeroFixedCads.Add(fixedCad);
            }
            uint[] scalarZeroLIds = {
                2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, 23, 24, 25 };
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
            var FEM = new EMWaveguide3DPMLTDFEM(world);
            FEM.TimeLoopCnt = timeLoopCnt;
            FEM.ExecTimeLoopCnt = execTimeLoopCnt;
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
                    int edgeNodeCntB1 = FEM.RefTimeEsss[0][timeIndex].Length;
                    int refEdgeNodeIdB1 = edgeNodeCntB1 / 2;
                    int edgeNodeCntB2 = FEM.RefTimeEsss[1][timeIndex].Length;
                    int refEdgeNodeIdB2 = edgeNodeCntB2 / 2;
                    double etPort1 = FEM.RefTimeEsss[0][timeIndex][refEdgeNodeIdB1];
                    double etPort2 = FEM.RefTimeEsss[1][timeIndex][refEdgeNodeIdB2];
                    var chartWin = ChartWindow2;
                    var model = chartWin.Plot.Model;
                    var series = model.Series;
                    var series1 = series[0] as LineSeries;
                    var series2 = series[1] as LineSeries;
                    series1.Points.Add(new DataPoint(timeIndex, etPort1));
                    series2.Points.Add(new DataPoint(timeIndex, etPort2));
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
            System.Numerics.Complex[] freqDomainAmpsInc = null; // 直線導波路の場合
            IList<System.Numerics.Complex[]> freqDomainAmpss;
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

            // 他の導波路解析のSマトリクス計算時の入射波に利用
            retFreqs = freqs.ToArray();
            retFreqDomainAmpsInc = freqDomainAmpss[0].ToArray();
        }
    }
}
