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
        public void TimoshenkoTLFrameProblem3(MainWindow mainWindow)
        {
            double beamLen = 1.0;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            int divCnt = 10;
            double eLen = 0.95 * beamLen / (double)divCnt;
            Cad2D cad = new Cad2D();
            {
                uint lId0 = 0;
                uint vId1 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(0.0, 0.0)).AddVId;
                uint vId2 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(beamLen, 0.0)).AddVId;

                uint eId1 = cad.ConnectVertexLine(vId1, vId2).AddEId;
            }

            {
                double[] nullLoopColor = { 1.0, 1.0, 1.0 };
                uint[] lIds = cad.GetElementIds(CadElementType.Loop).ToArray();
                foreach (uint lId in lIds)
                {
                    cad.SetLoopColor(lId, nullLoopColor);
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

            Mesher2D mesher = new Mesher2D();
            mesher.SetMeshingModeElemLength();
            {
                IList<uint> eIds = cad.GetElementIds(CadElementType.Edge);
                foreach (uint eId in eIds)
                {
                    mesher.AddMeshingEdgeCadId(eId, eLen);
                }
            }
            mesher.MakeMesh(cad);

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
            uint d1QuantityId; // displacement1
            uint d2QuantityId; // displacement2
            uint rQuantityId; // rotation
            {
                uint d1Dof = 1; // Scalar (u)
                uint d2Dof = 1; // Scalar (v)
                uint rDof = 1; // Scalar (θ)
                uint d1FEOrder = 1;
                uint d2FEOrder = 1;
                uint rFEOrder = 1;
                d1QuantityId = world.AddQuantity(d1Dof, d1FEOrder, FiniteElementType.ScalarLagrange);
                d2QuantityId = world.AddQuantity(d2Dof, d2FEOrder, FiniteElementType.ScalarLagrange);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarLagrange);
            }
            uint[] dQuantityIds = { d1QuantityId, d2QuantityId };

            {
                world.ClearMaterial();
                uint nullMaId;
                uint beamMaId;
                {
                    var ma = new NullMaterial();
                    nullMaId = world.AddMaterial(ma);
                }
                {
                    var ma = new TimoshenkoTLFrameMaterial();
                    ma.Area = b * h;
                    ma.SecondMomentOfArea = (1.0 / 12.0) * b * h * h * h;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    ma.ShearCorrectionFactor = 5.0 / 6.0; // 長方形断面
                    beamMaId = world.AddMaterial(ma);
                }

                uint[] lIds = cad.GetElementIds(CadElementType.Loop).ToArray();
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, nullMaId);
                }
                uint[] eIds = cad.GetElementIds(CadElementType.Edge).ToArray();
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, beamMaId);
                }
            }

            uint[] d1ZeroVIds = { 1 };
            var d1ZeroFixedCads = world.GetZeroFieldFixedCads(d1QuantityId);
            foreach (uint vId in d1ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                d1ZeroFixedCads.Add(fixedCad);
            }
            uint[] d2ZeroVIds = { 1 };
            var d2ZeroFixedCads = world.GetZeroFieldFixedCads(d2QuantityId);
            foreach (uint vId in d2ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                d2ZeroFixedCads.Add(fixedCad);
            }
            uint[] rZeroVIds = { 1 };
            var rZeroFixedCads = world.GetZeroFieldFixedCads(rQuantityId);
            foreach (uint vId in rZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                rZeroFixedCads.Add(fixedCad);
            }

            // load
            // displacement
            FieldFixedCad forceFixedCadD1;
            {
                // FixedDofIndex 0: u
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetForceFieldFixedCads(d1QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                forceFixedCadD1 = world.GetForceFieldFixedCads(d1QuantityId)[0];
            }
            FieldFixedCad forceFixedCadD2;
            {
                // FixedDofIndex 0: v
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetForceFieldFixedCads(d2QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                forceFixedCadD2 = world.GetForceFieldFixedCads(d2QuantityId)[0];
            }
            // rotation(moment)
            FieldFixedCad forceFixedCadR;
            {
                // FixedDofIndex 0: θ
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetForceFieldFixedCads(rQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                forceFixedCadR = world.GetForceFieldFixedCads(rQuantityId)[0];
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
            model.Title = "Frame Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "u,v",
                Minimum = -beamLen / 2.0,
                Maximum = beamLen / 2.0
            };
            var axis2 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "m"
            };
            model.Axes.Add(axis1);
            model.Axes.Add(axis2);
            var series1 = new LineSeries
            {
                Title = "u",
                LineStyle = LineStyle.None,
                MarkerType = MarkerType.Circle
            };
            var series2 = new LineSeries
            {
                Title = "v",
                LineStyle = LineStyle.None,
                MarkerType = MarkerType.Circle
            };
            model.Series.Add(series1);
            model.Series.Add(series2);
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    d1QuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var edgeDrawer0 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer0);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                edgeDrawer.LineWidth = 4;
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            uint endVId = 2;
            int endCoId = world.GetCoordIdsFromCadId(d2QuantityId, endVId, CadElementType.Vertex)[0];
            double t = 0;
            double dt = 0.05;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] forceFixedValueD1 = forceFixedCadD1.GetDoubleValues();
                double[] forceFixedValueD2 = forceFixedCadD2.GetDoubleValues();
                double[] forceFixedValueR = forceFixedCadR.GetDoubleValues();
                forceFixedValueD1[0] = 0.0;
                forceFixedValueD2[0] = -0.5e+6 * Math.Sin(t * 2.0 * Math.PI * 0.1);
                forceFixedValueR[0] = 0.0; // moment

                var FEM = new Elastic2DFEM(world);                
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
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    //FEM.Solver = solver;
                }
                FEM.DisplacementQuantityIds = dQuantityIds.ToList();
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6; // 収束条件を緩めている
                FEM.Solve();
                double[] Uuvt = FEM.U;

                // 変位(u,v)へ変換する
                int coCnt = (int)world.GetCoordCount(d1QuantityId);
                int d1Dof = (int)world.GetDof(d1QuantityId);
                int d2Dof = (int)world.GetDof(d2QuantityId);
                int rDof = (int)world.GetDof(rQuantityId);
                int d1NodeCnt = (int)world.GetNodeCount(d1QuantityId);
                int d2NodeCnt = (int)world.GetNodeCount(d2QuantityId);
                int rNodeCnt = (int)world.GetNodeCount(rQuantityId);
                int d2Offset = d1NodeCnt * d1Dof;
                int rOffset = d2Offset + d2NodeCnt * d2Dof;
                int dof = 2;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    int d1NodeId = world.Coord2Node(d1QuantityId, coId);
                    int d2NodeId = world.Coord2Node(d2QuantityId, coId);
                    int rNodeId = world.Coord2Node(rQuantityId, coId);
                    double u = 0;
                    double v = 0;
                    double theta = 0;
                    if (d1NodeId != -1)
                    {
                        u = Uuvt[d1NodeId];
                    }
                    if (d2NodeId != -1)
                    {
                        v = Uuvt[d2NodeId + d2Offset];
                    }
                    if (rNodeId != -1)
                    {
                        theta = Uuvt[rNodeId + rOffset];
                    }
                    U[coId * dof + 0] = u;
                    U[coId * dof + 1] = v;
                }
                // Note: from CoordValues
                world.UpdateFieldValueValuesFromCoordValues(valueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                {
                    double u = U[endCoId * dof];
                    double v = U[endCoId * dof + 1];
                    double m = forceFixedValueD2[0];
                    series1.Points.Add(new DataPoint(u, m));
                    series2.Points.Add(new DataPoint(v, m));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }
                t += dt;
            }
        }

        public void TimoshenkoTLFrameTDProblem3(MainWindow mainWindow)
        {
            double beamLen = 1.0;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            int divCnt = 10;
            double eLen = 0.95 * beamLen / (double)divCnt;
            Cad2D cad = new Cad2D();
            {
                uint lId0 = 0;
                uint vId1 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(0.0, 0.0)).AddVId;
                uint vId2 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(beamLen, 0.0)).AddVId;

                uint eId1 = cad.ConnectVertexLine(vId1, vId2).AddEId;
            }

            {
                double[] nullLoopColor = { 1.0, 1.0, 1.0 };
                uint[] lIds = cad.GetElementIds(CadElementType.Loop).ToArray();
                foreach (uint lId in lIds)
                {
                    cad.SetLoopColor(lId, nullLoopColor);
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

            Mesher2D mesher = new Mesher2D();
            mesher.SetMeshingModeElemLength();
            {
                IList<uint> eIds = cad.GetElementIds(CadElementType.Edge);
                foreach (uint eId in eIds)
                {
                    mesher.AddMeshingEdgeCadId(eId, eLen);
                }
            }
            mesher.MakeMesh(cad);

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
            uint d1QuantityId; // displacement1
            uint d2QuantityId; // displacement2
            uint rQuantityId; // rotation
            {
                uint d1Dof = 1; // Scalar (u)
                uint d2Dof = 1; // Scalar (v)
                uint rDof = 1; // Scalar (θ)
                uint d1FEOrder = 1;
                uint d2FEOrder = 1;
                uint rFEOrder = 1;
                d1QuantityId = world.AddQuantity(d1Dof, d1FEOrder, FiniteElementType.ScalarLagrange);
                d2QuantityId = world.AddQuantity(d2Dof, d2FEOrder, FiniteElementType.ScalarLagrange);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarLagrange);
            }
            uint[] dQuantityIds = { d1QuantityId, d2QuantityId };

            {
                world.ClearMaterial();
                uint nullMaId;
                uint beamMaId;
                {
                    var ma = new NullMaterial();
                    nullMaId = world.AddMaterial(ma);
                }
                {
                    var ma = new TimoshenkoTLFrameMaterial();
                    ma.Area = b * h;
                    ma.SecondMomentOfArea = (1.0 / 12.0) * b * h * h * h;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    ma.ShearCorrectionFactor = 5.0 / 6.0; // 長方形断面
                    beamMaId = world.AddMaterial(ma);
                }

                uint[] lIds = cad.GetElementIds(CadElementType.Loop).ToArray();
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, nullMaId);
                }
                uint[] eIds = cad.GetElementIds(CadElementType.Edge).ToArray();
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, beamMaId);
                }
            }

            uint[] d1ZeroVIds = { 1 };
            var d1ZeroFixedCads = world.GetZeroFieldFixedCads(d1QuantityId);
            foreach (uint vId in d1ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                d1ZeroFixedCads.Add(fixedCad);
            }
            uint[] d2ZeroVIds = { 1 };
            var d2ZeroFixedCads = world.GetZeroFieldFixedCads(d2QuantityId);
            foreach (uint vId in d2ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                d2ZeroFixedCads.Add(fixedCad);
            }
            uint[] rZeroVIds = { 1 };
            var rZeroFixedCads = world.GetZeroFieldFixedCads(rQuantityId);
            foreach (uint vId in rZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                rZeroFixedCads.Add(fixedCad);
            }

            // load
            // displacement
            FieldFixedCad forceFixedCadD1;
            {
                // FixedDofIndex 0: u
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetForceFieldFixedCads(d1QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                forceFixedCadD1 = world.GetForceFieldFixedCads(d1QuantityId)[0];
            }
            FieldFixedCad forceFixedCadD2;
            {
                // FixedDofIndex 0: v
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetForceFieldFixedCads(d2QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                forceFixedCadD2 = world.GetForceFieldFixedCads(d2QuantityId)[0];
            }
            // rotation(moment)
            FieldFixedCad forceFixedCadR;
            {
                // FixedDofIndex 0: θ
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetForceFieldFixedCads(rQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                forceFixedCadR = world.GetForceFieldFixedCads(rQuantityId)[0];
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
            model.Title = "Frame Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "u,v",
                Minimum = -beamLen / 2.0,
                Maximum = beamLen / 2.0
            };
            var axis2 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "m"
            };
            model.Axes.Add(axis1);
            model.Axes.Add(axis2);
            var series1 = new LineSeries
            {
                Title = "u",
                LineStyle = LineStyle.None,
                MarkerType = MarkerType.Circle
            };
            var series2 = new LineSeries
            {
                Title = "v",
                LineStyle = LineStyle.None,
                MarkerType = MarkerType.Circle
            };
            model.Series.Add(series1);
            model.Series.Add(series2);
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();

            uint d1ValueId = 0;
            uint d1PrevValueId = 0;
            uint d2ValueId = 0;
            uint d2PrevValueId = 0;
            uint rValueId = 0;
            uint rPrevValueId = 0;
            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 表示用
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    d1QuantityId, false, FieldShowType.Real);

                // Newmarkβ
                // Scalar
                d1ValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d1QuantityId, false, FieldShowType.Real);
                d1PrevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d1QuantityId, false, FieldShowType.Real);
                d2ValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d2QuantityId, false, FieldShowType.Real);
                d2PrevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d2QuantityId, false, FieldShowType.Real);
                rValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    rQuantityId, false, FieldShowType.Real);
                rPrevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    rQuantityId, false, FieldShowType.Real);

                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var edgeDrawer0 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer0);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                edgeDrawer.LineWidth = 4;
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }
            IList<uint> valueIds = new List<uint> { d1ValueId, d2ValueId, rValueId };
            IList<uint> prevValueIds = new List<uint> { d1PrevValueId, d2PrevValueId, rPrevValueId };

            uint endVId = 2;
            int endCoId = world.GetCoordIdsFromCadId(d2QuantityId, endVId, CadElementType.Vertex)[0];
            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] forceFixedValueD1 = forceFixedCadD1.GetDoubleValues();
                double[] forceFixedValueD2 = forceFixedCadD2.GetDoubleValues();
                double[] forceFixedValueR = forceFixedCadR.GetDoubleValues();
                forceFixedValueD1[0] = 0.0;
                forceFixedValueD2[0] = -0.5e+6 * Math.Sin(t * 2.0 * Math.PI * 0.1);
                forceFixedValueR[0] = 0.0; // moment

                var FEM = new Elastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueIds, prevValueIds);
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
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    //FEM.Solver = solver;
                }
                FEM.DisplacementQuantityIds = dQuantityIds.ToList();
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6; // 収束条件を緩めている
                FEM.Solve();
                double[] Uuvt = FEM.U;

                // 変位(u,v)へ変換する
                int coCnt = (int)world.GetCoordCount(d1QuantityId);
                int d1Dof = (int)world.GetDof(d1QuantityId);
                int d2Dof = (int)world.GetDof(d2QuantityId);
                int rDof = (int)world.GetDof(rQuantityId);
                int d1NodeCnt = (int)world.GetNodeCount(d1QuantityId);
                int d2NodeCnt = (int)world.GetNodeCount(d2QuantityId);
                int rNodeCnt = (int)world.GetNodeCount(rQuantityId);
                int d2Offset = d1NodeCnt * d1Dof;
                int rOffset = d2Offset + d2NodeCnt * d2Dof;
                int dof = 2;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    int d1NodeId = world.Coord2Node(d1QuantityId, coId);
                    int d2NodeId = world.Coord2Node(d2QuantityId, coId);
                    int rNodeId = world.Coord2Node(rQuantityId, coId);
                    double u = 0;
                    double v = 0;
                    double theta = 0;
                    if (d1NodeId != -1)
                    {
                        u = Uuvt[d1NodeId];
                    }
                    if (d2NodeId != -1)
                    {
                        v = Uuvt[d2NodeId + d2Offset];
                    }
                    if (rNodeId != -1)
                    {
                        theta = Uuvt[rNodeId + rOffset];
                    }
                    U[coId * dof + 0] = u;
                    U[coId * dof + 1] = v;
                }
                // Note: from CoordValues
                world.UpdateFieldValueValuesFromCoordValues(valueId, FieldDerivativeType.Value, U);

                FEM.UpdateFieldValuesTimeDomain();

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                {
                    double u = U[endCoId * dof];
                    double v = U[endCoId * dof + 1];
                    double m = forceFixedValueD2[0];
                    series1.Points.Add(new DataPoint(u, m));
                    series2.Points.Add(new DataPoint(v, m));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }
                t += dt;
            }
        }
    }
}
