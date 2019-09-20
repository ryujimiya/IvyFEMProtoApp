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
        public void HPlaneWaveguideHigherOrderABCTDProblem0(MainWindow mainWindow)
        {
            IList<double> times;
            IList<double> timeEzsInc;
            SolveHPlaneWaveguideHigherOrderABCTDStraightWaveguide(
                mainWindow, out times, out timeEzsInc);
        }

        public void SolveHPlaneWaveguideHigherOrderABCTDStraightWaveguide(
            MainWindow mainWindow,
            out IList<double> retTimes,
            out IList<double> retTimeEzsInc)
        {
            // FDTDのモデルに合わせる (FDTDのモデルは幅を3.0e-3 * 20 * 4としていた)
            double WaveguideWidth = 3.0e-3 * 20 * 4; 
            double eLen = WaveguideWidth * 0.09; // 10分割(0.1)

            // 導波管不連続領域の長さ
            //double disconLength = 1.0 * WaveguideWidth;
            double disconLength = 1.0 * WaveguideWidth;
            // 形状設定で使用する単位長さ
            double unitLen = WaveguideWidth / 20.0;
            // 励振位置
            double srcPosX = 5 * unitLen;
            // 観測点
            int port1OfsX = 5;
            int port2OfsX = 5;
            double port1PosX = srcPosX + port1OfsX * unitLen;
            double port1PosY = WaveguideWidth * 0.5;
            double port2PosX = disconLength - port2OfsX * unitLen;
            double port2PosY = WaveguideWidth * 0.5;

            // 時間刻み幅の算出
            double courantNumber = 0.5;
            // Note: timeLoopCnt は 2^mでなければならない
            //int timeLoopCnt = 1024;
            int timeLoopCnt = 4096;
            double timeDelta = courantNumber * eLen / (Constants.C0 * Math.Sqrt(2.0));
            // 励振源
            // 規格化周波数
            double srcNormalizedFreq = 2.0;
            // 波長
            double srcWaveLength = 2.0 * WaveguideWidth / srcNormalizedFreq;
            // 周波数
            double srcFreq = Constants.C0 / srcWaveLength;
            // 計算する周波数領域
            double normalizedFreq1 = 1.0;
            double normalizedFreq2 = 2.0;
            // 吸収境界条件の次数
            //int ABCOrder = 5;
            int ABCOrder = 5;

            // ガウシアンパルス
            //GaussianType gaussianType = GaussianType.Normal; // 素のガウシアンパルス
            //GaussianType gaussianType = GaussianType.SinModulation; // 正弦波変調
            GaussianType gaussianType = GaussianType.SinModulation;
            double gaussianT0 = 0;
            double gaussianTp = 0;
            if (gaussianType == GaussianType.Normal)
            {
                // ガウシアンパルス
                gaussianT0 = 20 * timeDelta;
                gaussianTp = gaussianT0 / (Math.Sqrt(2.0) * 4.0);
            }
            else if (gaussianType == GaussianType.SinModulation)
            {
                // 正弦波変調ガウシアンパルス
                // 搬送波の振動回数
                int nCycle = 5;
                gaussianT0 = 0.67 * (1.0 / srcFreq) * nCycle / 2;
                gaussianTp = gaussianT0 / (2.0 * Math.Sqrt(2.0 * Math.Log(2.0)));
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }

            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, WaveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(srcPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, WaveguideWidth));
                pts.Add(new OpenTK.Vector2d(srcPosX, WaveguideWidth));
                uint _lId1 = cad2D.AddPolygon(pts).AddLId;
                uint _lId2 = cad2D.ConnectVertexLine(3, 6).AddLId;
                // 観測点
                cad2D.AddVertex(CadElementType.Loop, _lId2, new OpenTK.Vector2d(port1PosX, port1PosY));
                cad2D.AddVertex(CadElementType.Loop, _lId2, new OpenTK.Vector2d(port2PosX, port2PosY));
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
            WPFUtils.DoEvents();

            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            IDrawer meshDrawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
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

            uint eId1 = 1;
            uint eId2 = 4;
            uint eId3 = 7;
            uint lId1 = 1;
            uint lId2 = 2;
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
                uint maId = world.AddMaterial(vacuumMa);

                world.SetCadEdgeMaterial(eId1, maId);
                world.SetCadEdgeMaterial(eId2, maId);
                world.SetCadEdgeMaterial(eId3, maId);
                world.SetCadLoopMaterial(lId1, maId);
                world.SetCadLoopMaterial(lId2, maId);
            }
            {
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                portConditions.Clear();
                IList<uint> port1EIds = new List<uint>();
                IList<uint> port2EIds = new List<uint>();
                IList<uint> port3EIds = new List<uint>();
                port1EIds.Add(eId1);
                port2EIds.Add(eId2);
                port3EIds.Add(eId3);
                IList<IList<uint>> portEIdss = new List<IList<uint>>();
                portEIdss.Add(port1EIds);
                portEIdss.Add(port2EIds);
                portEIdss.Add(port3EIds);
                foreach (IList<uint> portEIds in portEIdss)
                {
                    // スカラー
                    PortCondition portCondition = new PortCondition(portEIds, FieldValueType.Scalar);
                    portConditions.Add(portCondition);
                }
            }
            uint[] zeroEIds = { 2, 3, 5, 6 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                zeroFixedCads.Add(fixedCad);
            }

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
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            var FEM = new EMWaveguide2DHPlaneHigherOrderABCTDFEM(world);
            FEM.ABCOrder = ABCOrder;
            FEM.TimeLoopCnt = timeLoopCnt;
            FEM.TimeIndex = 0;
            FEM.TimeDelta = timeDelta;
            FEM.GaussianType = gaussianType;
            FEM.GaussianT0 = gaussianT0;
            FEM.GaussianTp = gaussianTp;
            FEM.SrcFrequency = srcFreq;
            {
                // 観測点
                uint[] vIds = { 7, 8 };
                IList<uint> refVIds = FEM.RefVIds;
                foreach (uint vId in vIds)
                {
                    refVIds.Add(vId);
                }
            }
            /*
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
            FEM.Solver = null;

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
                chartWin.plot.Model = model;
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
                var datas = new List<DataPoint>();
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

                int timeIndex = FEM.TimeIndex;
                double ezPort1 = FEM.TimeEzsPort1[timeIndex];
                double ezPort2 = FEM.TimeEzsPort2[timeIndex];
                var chartWin = ChartWindow2;
                var model = chartWin.plot.Model;
                var series = model.Series;
                var series1 = series[0] as LineSeries;
                var series2 = series[1] as LineSeries;
                series1.Points.Add(new DataPoint(timeIndex, ezPort1));
                series2.Points.Add(new DataPoint(timeIndex, ezPort2));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
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
                chartWin.plot.Model = model;
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
                var datas = new List<DataPoint>();
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();
            }

            // S11、S21の周波数特性
            IList<double> timeEzsPort1 = FEM.TimeEzsPort1;
            IList<double> timeEzsPort2 = FEM.TimeEzsPort2;
            IList<double> timeEzsInc = timeEzsPort1; // 直線導波路の場合
            IList<double> freqs;
            IList<System.Numerics.Complex[]> Sss;
            FEM.CalcSParameter(timeEzsInc, out freqs, out Sss);
            int freqCnt = freqs.Count;
            for (int iFreq = 0; iFreq < freqCnt; iFreq++)
            {
                // 周波数
                double freq = freqs[iFreq];
                // 波長
                double waveLength = Constants.C0 / freq;
                // 規格化周波数
                double normalizedFreq = 2.0 * WaveguideWidth / waveLength;
                if (normalizedFreq < normalizedFreq1)
                {
                    continue;
                }
                if (normalizedFreq > normalizedFreq2)
                {
                    break;
                }
                // S
                System.Numerics.Complex[] Ss = Sss[iFreq];
                System.Numerics.Complex S11 = Ss[0];
                System.Numerics.Complex S21 = Ss[1];
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
                var model = chartWin.plot.Model;
                var series = model.Series;
                var series1 = series[0] as LineSeries;
                var series2 = series[1] as LineSeries;
                series1.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                series2.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();
            }

            // 他の導波路解析のSマトリクス計算時の入射波に利用
            retTimes = new List<double>();
            for (int iTime = 0; iTime < timeLoopCnt; iTime++)
            {
                double time = iTime * timeDelta;
                retTimes.Add(time);
            }
            retTimeEzsInc = new List<double>(timeEzsPort1);
        }
    }
}
