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
        public void HWaveguideProblem2(MainWindow mainWindow)
        {
            double waveguideWidth = 1.0;
            //double eLen = waveguideWidth * (0.95 * 1.0 / 30.0);
            double eLen = waveguideWidth * (0.95 * 1.0 / 30.0);
            // 誘電体スラブ導波路幅
            double coreWidth = waveguideWidth * (4.0 / 30.0);
            // 誘電体スラブ比誘電率
            double coreEp = 3.6 * 3.6;
            double claddingEp = 3.24 * 3.24;
            bool isTMMode = true; // TMモード

            double sFreq = 0.0;
            double eFreq = 1.0;
            int freqDiv = 50;

            const int portCnt = 2;
            // 導波管不連続領域の長さ
            double disconLength = 2.0 * waveguideWidth;
            // 誘電体終端
            double terminalPosX = 0.5 * disconLength;
            double coreY1 = (waveguideWidth - coreWidth) * 0.5;
            double coreY2 = coreY1 + coreWidth;
            uint loopCnt = 4;
            uint[] coreLIds = { 4 };
            uint[] claddingLIds = { 1, 3 };
            uint[] port1EIds = { 1, 9, 8 };
            uint[] port1CoreEIds = { 9 };
            uint[] port1CladdingEIds = { 1, 8 };
            uint[] port2EIds = { 4 };
            uint[] port2CoreEIds = { };
            uint[] port2CladdingEIds = { };
            // メッシュの長さ
            double[] eLens = new double[loopCnt];
            for (int i = 0; i < loopCnt; i++)
            {
                uint lId = (uint)(i + 1);
                //double workeLen = eLen;
                double workeLen = coreLIds.Contains(lId) ? (eLen * 0.5) : eLen;
                eLens[i] = workeLen;
            }

            CadObject2D cad = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(terminalPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(terminalPosX, waveguideWidth));
                uint _lId1 = cad.AddPolygon(pts).AddLId;
                uint _lId2 = cad.ConnectVertexLine(3, 6).AddLId;
                System.Diagnostics.Debug.Assert(_lId1 == 1);
                System.Diagnostics.Debug.Assert(_lId2 == 2);
            }
            // スラブ導波路と境界の交点
            {
                // 入力面と終端
                double[] portXs = { 0.0, terminalPosX };
                uint[] parentEIds = { 1, 7 };
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
                    uint vId1 = cad.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY1)).AddVId;
                    uint vId2 = cad.AddVertex(
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
                    {
                        uint workVId1 = coreVIds[0][0];
                        uint workVId2 = coreVIds[1][0];
                        uint workEId = cad.ConnectVertexLine(workVId1, workVId2).AddEId;
                    }
                    {
                        uint workVId1 = coreVIds[0][1];
                        uint workVId2 = coreVIds[1][1];
                        uint workEId = cad.ConnectVertexLine(workVId1, workVId2).AddEId;
                    }
                }
            }

            // check
            {
                double[] coreColor = { 0.6, 0.0, 0.0 };
                foreach (uint lId in coreLIds)
                {
                    cad.SetLoopColor(lId, coreColor);
                }
                double[] edgeCoreColor = { 0.4, 0.0, 0.0 };
                uint[][] portCoreEIdss = { port1CoreEIds, port2CoreEIds };
                foreach (uint[] eIds in portCoreEIdss)
                {
                    foreach (uint eId in eIds)
                    {
                        cad.SetEdgeColor(eId, edgeCoreColor);
                    }
                }
                double[] claddingColor = { 0.5, 1.0, 0.5 };
                foreach (uint lId in claddingLIds)
                {
                    cad.SetLoopColor(lId, claddingColor);
                }
                double[] edgeCladdingColor = { 0.3, 0.8, 0.3 };
                uint[][] portCladdingEIdss = { port1CladdingEIds, port2CladdingEIds };
                foreach (uint[] eIds in portCladdingEIdss)
                {
                    foreach (uint eId in eIds)
                    {
                        cad.SetEdgeColor(eId, edgeCladdingColor);
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

            //Mesher2D mesher = new Mesher2D(cad, eLen);
            Mesher2D mesher = new Mesher2D();
            mesher.SetMeshingModeElemLength();
            for (int i = 0; i < loopCnt; i++)
            {
                uint lId = (uint)(i + 1);
                double workeLen = eLens[i];
                mesher.AddCutMeshLoopCadId(lId, workeLen);
            }
            mesher.Meshing(cad);

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
                DielectricMaterial claddingMa = new DielectricMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricMaterial coreMa = new DielectricMaterial
                {
                    Epxx = coreEp,
                    Epyy = coreEp,
                    Epzz = coreEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
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
                // 入出力面
                uint[][] portEIdss = { port1EIds, port2EIds };
                uint[][] portCoreEIdss = { port1CoreEIds, port2CoreEIds };
                uint[][] portCladdingEIdss = { port1CladdingEIds, port2CladdingEIds };
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
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                uint[][] _portEIdss = { port1EIds, port2EIds };
                IList<IList<uint>> portEIdss = new List<IList<uint>>();
                foreach (uint[] _portEIds in _portEIdss)
                {
                    IList<uint> __portEIds = _portEIds.ToList();
                    portEIdss.Add(__portEIds);

                }
                foreach (IList<uint> portEIds in portEIdss)
                {
                    PortCondition portCondition = new PortCondition(portEIds, FieldValueType.ZScalar);
                    portConditions.Add(portCondition);
                }
            }
            /*
            uint[] zeroEIds = { };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }
            */

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
                Title = "2Wc√(n1^2 - n2^2) /λ",
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

            for (int iFreq = 0; iFreq < (freqDiv + 1); iFreq++)
            {
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波長
                double waveLength = 2.0 * coreWidth * Math.Sqrt(coreEp - claddingEp) / normalizedFreq;
                // 周波数
                double freq = Constants.C0 / waveLength;
                // 角周波数
                double omega = 2.0 * Math.PI * freq;
                // 波数
                double k0 = omega / Constants.C0;
                System.Diagnostics.Debug.WriteLine("2Wc √(n1^2 - n2^2) / λ: " + normalizedFreq);

                var FEM = new EMWaveguide2DHPlaneFEM(world);
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
                FEM.IsTMMode = isTMMode;
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
                ret = "2Wc √(n1^2 - n2^2) / λ: " + normalizedFreq + CRLF;
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
