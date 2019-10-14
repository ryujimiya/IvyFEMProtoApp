﻿using System;
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
        public void HWaveguideHigherOrderABCTDProblemb1_0(MainWindow mainWindow)
        {
            double[] freqs;
            System.Numerics.Complex[] freqDomainAmpsInc;
            SolveHWaveguideHigherOrderABCTDProblemb1_0(
                mainWindow, out freqs, out freqDomainAmpsInc);
        }

        public void SolveHWaveguideHigherOrderABCTDProblemb1_0(
            MainWindow mainWindow,
            out double[] retFreqs,
            out System.Numerics.Complex[] retFreqDomainAmpsInc)
        {
            retFreqs = null;
            retFreqDomainAmpsInc = null;

            double waveguideWidth = 1; 
            double eLen = waveguideWidth * 0.09; // 10分割(0.1)

            // 導波管不連続領域の長さ
            double disconLength = 1.0 * waveguideWidth;
            // 形状設定で使用する単位長さ
            double unitLen = waveguideWidth / 20.0;
            // 励振位置
            double srcPosX = 5 * unitLen;
            // 観測点
            int port1OfsX = 5;
            int port2OfsX = 5;
            double port1PosX = srcPosX + port1OfsX * unitLen;
            double port2PosX = disconLength - port2OfsX * unitLen;

            // 時間刻み幅の算出
            double courantNumber = 0.5;
            // Note: timeLoopCnt は 2^mでなければならない
            //int timeLoopCnt = 2048;
            int timeLoopCnt = 2048;
            double timeDelta = courantNumber * eLen / (Constants.C0 * Math.Sqrt(2.0));
            // 励振源
            // 規格化周波数
            double srcNormalizedFreq = 2.0;
            // 波長
            double srcWaveLength = 2.0 * waveguideWidth / srcNormalizedFreq;
            // 周波数
            double srcFreq = Constants.C0 / srcWaveLength;
            // 計算する周波数領域
            double normalizedFreq1 = 1.0;
            double normalizedFreq2 = 2.0;
            // 規格化周波数変換
            Func<double, double> toNormalizedFreq =
                waveLength => 2.0 * waveguideWidth / waveLength;
            // 吸収境界条件の次数
            int[] abcOrders = { 5, 5 };

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
                gaussianT0 = 0.67 * (1.0 / srcFreq) * nCycle / 2.0;
                gaussianTp = gaussianT0 / (2.0 * Math.Sqrt(2.0 * Math.Log(2.0)));
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }

            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(srcPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port2PosX, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port2PosX, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1PosX, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(srcPosX, waveguideWidth));
                uint _lId1 = cad2D.AddPolygon(pts).AddLId;
                uint _lId2 = cad2D.ConnectVertexLine(3, 10).AddLId;
                uint _lId3 = cad2D.ConnectVertexLine(4, 9).AddLId;
                uint _lId4 = cad2D.ConnectVertexLine(5, 8).AddLId;
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

            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

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

            uint loopCnt = 4;
            uint eId1 = 1;
            uint eId2 = 6;
            uint eIdRef1 = 12;
            uint eIdRef2 = 13;
            uint eIdSrc = 11;
            // 観測ポート数
            int refPortCnt = 2;
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

                uint[] eIds = { eId1, eId2, eIdRef1, eIdRef2, eIdSrc };
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, maId);
                }
                for (int i = 0; i <  loopCnt; i++)
                {
                    uint lId = (uint)(i + 1);
                    world.SetCadLoopMaterial(lId, maId);
                }
            }
            {
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                IList<IList<uint>> portEIdss = new List<IList<uint>>();
                uint[] eIds = { eId1, eId2, eIdRef1, eIdRef2, eIdSrc };
                foreach (uint eId in eIds)
                {
                    IList<uint> portEIds = new List<uint>();
                    {
                        portEIds.Add(eId);
                    }
                    portEIdss.Add(portEIds);
                }
                foreach (IList<uint> portEIds in portEIdss)
                {
                    // スカラー
                    PortCondition portCondition = new PortCondition(portEIds, FieldValueType.Scalar);
                    portConditions.Add(portCondition);
                }
            }
            uint[] zeroEIds = { 2, 3, 4, 5, 7, 8, 9, 10 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
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
            FEM.GaussianType = gaussianType;
            FEM.GaussianT0 = gaussianT0;
            FEM.GaussianTp = gaussianTp;
            FEM.SrcFrequency = srcFreq;
            // 観測点（参照ポート数）
            FEM.RefPortCount = refPortCnt;

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
