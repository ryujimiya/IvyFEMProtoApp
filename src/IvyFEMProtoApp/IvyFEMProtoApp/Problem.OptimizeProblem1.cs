﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;
using IvyFEMProtoApp;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Microsoft.SolverFoundation.Solvers;
using Microsoft.SolverFoundation.Services;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void Optimize1Problem(MainWindow mainWindow)
        {
            int paramCnt = 5;
            double[] ds = new double[paramCnt];
            double[] minds = new double[paramCnt];
            double[] maxds = new double[paramCnt]; 
            for (int i = 0; i < paramCnt; i++)
            {
                // 直角コーナー
                double theta = (i + 1) / (double)paramCnt * Math.PI / 4.0;
                double x = 1.0;
                double y = x * Math.Tan(theta);
                ds[i] = Math.Sqrt(x * x + y * y);
                // 円弧
                //ds[i] = 1.0;
                minds[i] = 0.1;
                maxds[i] = 2.0;
            }
            double totalS11;
            WaveguideProblemToBeOptimized(mainWindow, paramCnt, ds, out totalS11);

            int maxItr = 50;
            NonlinerOptimizingProblem.Solve(paramCnt, ds, minds, maxds, maxItr, ValueFunctionToBeOptimized, mainWindow);
        }

        private double ValueFunctionToBeOptimized(INonlinearModel model, int rowVId,
                ValuesByIndex values, bool newValues, object arg)
        {
            MainWindow mainWindow = arg as MainWindow;

            System.Diagnostics.Debug.Assert(model.GetKeyFromIndex(rowVId) as string == "obj");
            int paramCnt = model.VariableCount;
            double[] ds = new double[paramCnt];
            for (int i = 0; i < paramCnt; i++)
            {
                string vKey = "v" + i;
                int vId = model.GetIndexFromKey(vKey);
                ds[i] = values[vId];
            }

            double totalS11;
            WaveguideProblemToBeOptimized(mainWindow, paramCnt, ds, out totalS11);
            return totalS11;
        }

        private void WaveguideProblemToBeOptimized(
            MainWindow mainWindow, int paramCnt, double[] ds, out double totalS11)
        {
            totalS11 = 0;

            double waveguideWidth = 1.0;
            double inputWGLength = 1.0 * waveguideWidth;

            IList<uint> zeroEIds = new List<uint>();
            uint eId1 = 0;
            uint eId2 = 0;
            CadObject2D cad = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(inputWGLength, 0.0));
                pts.Add(new OpenTK.Vector2d(inputWGLength, (-inputWGLength)));
                pts.Add(new OpenTK.Vector2d((inputWGLength + waveguideWidth), (-inputWGLength)));
                pts.Add(new OpenTK.Vector2d((inputWGLength + waveguideWidth), 0.0));
                for (int i = 0; i < (paramCnt - 1); i++)
                {
                    double theta = (i + 1) / (double)paramCnt * Math.PI / 4.0;
                    double x = inputWGLength + waveguideWidth * ds[i] * Math.Cos(theta);
                    double y = 0.0 + waveguideWidth * ds[i] * Math.Sin(theta);
                    pts.Add(new OpenTK.Vector2d(x, y));
                }
                {
                    double theta = Math.PI / 4.0;
                    double x = inputWGLength + waveguideWidth * ds[paramCnt - 1] * Math.Cos(theta);
                    double y = 0.0 + waveguideWidth * ds[paramCnt - 1] * Math.Sin(theta);
                    pts.Add(new OpenTK.Vector2d(x, y));
                }
                for (int i = 0; i < (paramCnt - 1); i++)
                {
                    double theta = (i + 1) / (double)paramCnt * Math.PI / 4.0 + Math.PI / 4.0;
                    double x = inputWGLength + waveguideWidth * ds[paramCnt - 2 - i] * Math.Cos(theta);
                    double y = 0.0 + waveguideWidth * ds[paramCnt - 2 - i] * Math.Sin(theta);
                    pts.Add(new OpenTK.Vector2d(x, y));
                }
                pts.Add(new OpenTK.Vector2d(inputWGLength, waveguideWidth));
                var res = cad.AddPolygon(pts);
                IList<uint> eIds = res.EIds;
                eId1 = eIds[0];
                eId2 = eIds[3];
                foreach (uint eId in eIds)
                {
                    if (eId == eId1 || eId ==  eId2)
                    {
                        continue;
                    }
                    zeroEIds.Add(eId);
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

            double eLen = waveguideWidth * 0.05;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 1; // 複素数
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            int loopCnt = 1;
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

                uint[] eIds = { eId1, eId2 };
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, maId);
                }
                for (int i = 0; i < loopCnt; i++)
                {
                    uint lId = (uint)(i + 1);
                    world.SetCadLoopMaterial(lId, maId);
                }
            }
            {
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                IList<IList<uint>> portEIdss = new List<IList<uint>>();
                uint[] eIds = { eId1, eId2 };
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
                    PortCondition portCondition = new PortCondition(portEIds, FieldValueType.ZScalar);
                    portConditions.Add(portCondition);
                }
            }
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            double sFreq = 1.0;
            double eFreq = 2.0;
            int freqDiv = 20;//50;

            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closing += ChartWindow1_Closing;
            }
            ChartWindow chartWin = ChartWindow1;
            chartWin.Owner = mainWindow;
            chartWin.Show();
            chartWin.TextBox1.Text = "";
            var model = new PlotModel();
            chartWin.Plot.Model = model;
            model.Title = "Waveguide Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "2W/λ",
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

            totalS11 = 0;
            for (int iFreq = 0; iFreq < (freqDiv + 1); iFreq++)
            {
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波数
                double k0 = normalizedFreq * Math.PI / waveguideWidth;
                // 角周波数
                double omega = k0 * Constants.C0;
                // 周波数
                double freq = omega / (2.0 * Math.PI);
                System.Diagnostics.Debug.WriteLine("2W/λ: " + normalizedFreq);

                var FEM = new EMWaveguide2DHPlaneFEM(world);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.IsOrderingToBandMatrix = true;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    //FEM.Solver = solver;
                }
                {
                    //var solver = new IvyFEM.Linear.LisEquationSolver();
                    //solver.Method = IvyFEM.Linear.LisEquationSolverMethod.Default;
                    //FEM.Solver = solver;
                }
                {
                    var solver = new IvyFEM.Linear.IvyFEMEquationSolver();
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.COCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] Ez = FEM.Ez;
                System.Numerics.Complex[][] S = FEM.S;

                System.Numerics.Complex S11 = S[0][0];
                System.Numerics.Complex S21 = S[1][0];
                double S11Abs = S11.Magnitude;
                double S21Abs = S21.Magnitude;
                double total = S11Abs * S11Abs + S21Abs * S21Abs;
                totalS11 += S11Abs * S11Abs;

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
