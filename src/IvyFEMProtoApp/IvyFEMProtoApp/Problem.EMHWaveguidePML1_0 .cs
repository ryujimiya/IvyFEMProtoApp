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
        public void HWaveguidePMLProblem1_0(MainWindow mainWindow)
        {
            double waveguideWidth = 1;
            //double eLen = waveguideWidth * 0.09; // 10分割(0.1)
            double eLen = waveguideWidth * 0.9 * (1.0 / 20.0);

            // 計算する周波数領域
            double sFreq = 1.0;
            double eFreq = 2.0;
            int freqDiv = 50;

            // 形状設定で使用する単位長さ
            double unitLen = waveguideWidth / 20.0;
            // PML層の厚さ
            double pmlThickness = 10 * unitLen;
            // 導波管不連続領域の長さ
            double disconLength = 1.0 * waveguideWidth + 2.0 * pmlThickness;
            // PML位置
            double port1PMLPosX = pmlThickness;
            double port2PMLPosX = disconLength - pmlThickness;
            uint loopCnt = 3;
            uint[] pmlLIds1 = { 1 };
            uint[] pmlLIds2 = { 3 };
            uint[][] pmlLIdss = { pmlLIds1, pmlLIds2 };
            uint eIdRef1 = 9;
            uint eIdRef2 = 10;
            uint eIdSrc = eIdRef1;
            CadObject2D cad = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PMLPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port2PMLPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port2PMLPosX, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1PMLPosX, waveguideWidth));
                uint _lId1 = cad.AddPolygon(pts).AddLId;
                uint _lId2 = cad.ConnectVertexLine(3, 8).AddLId;
                uint _lId3 = cad.ConnectVertexLine(4, 7).AddLId;
            }

            // check
            {
                double[] pmlColor = { 0.5, 0.5, 0.5 };
                foreach (uint[] lIds in pmlLIdss)
                {
                    foreach (uint lId in lIds)
                    {
                        cad.SetLoopColor(lId, pmlColor);
                    }
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

            Mesher2D mesher = new Mesher2D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            IDrawer meshDrawer = new Mesher2DDrawer(mesher);
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

            uint vacuumMaId = 0;
            IList<uint> pmlMaIds = new List<uint>();
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
                DielectricPMLMaterial pmlMa1 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0
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
                    OriginPoint = new OpenTK.Vector2d(port2PMLPosX, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0
                };

                vacuumMaId = world.AddMaterial(vacuumMa);
                uint pmlMaId1 = world.AddMaterial(pmlMa1);
                pmlMaIds.Add(pmlMaId1);
                uint pmlMaId2 = world.AddMaterial(pmlMa2);
                pmlMaIds.Add(pmlMaId2);

                System.Diagnostics.Debug.Assert(pmlLIdss.Length == pmlMaIds.Count);

                uint[] eIds = { eIdRef1, eIdRef2, eIdSrc };
                foreach (uint eId in eIds)
                {
                    uint maId = vacuumMaId;
                    world.SetCadEdgeMaterial(eId, maId);
                }
                for (int i = 0; i < loopCnt; i++)
                {
                    uint lId = (uint)(i + 1);
                    uint maId = vacuumMaId;
                    int hitPMLIndex = -1;
                    for (int pmlIndex = 0; pmlIndex < pmlLIdss.Length; pmlIndex++)
                    {
                        uint[] lIds = pmlLIdss[pmlIndex];
                        if (lIds.Contains(lId))
                        {
                            hitPMLIndex = pmlIndex;
                            break;
                        }
                    }
                    if (hitPMLIndex != -1)
                    {
                        maId = pmlMaIds[hitPMLIndex];
                    }
                    else
                    {
                        maId = vacuumMaId;
                    }

                    world.SetCadLoopMaterial(lId, maId);
                }
            }
            {
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                IList<IList<uint>> portEIdss = new List<IList<uint>>();
                uint[] eIds = { eIdRef1, eIdRef2, eIdSrc };
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
                    // 複素数
                    PortCondition portCondition = new PortCondition(portEIds, FieldValueType.ZScalar);
                    portConditions.Add(portCondition);
                }

                // 入射ポート、モード
                int incidentPortId = 0;
                world.SetIncidentPortId(quantityId, incidentPortId);
                world.SetIncidentModeId(quantityId, 0);
            }
            uint[] zeroEIds = { 2, 3, 4, 6, 7, 8 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

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

                var FEM = new EMWaveguide2DHPlanePMLFEM(world);
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
