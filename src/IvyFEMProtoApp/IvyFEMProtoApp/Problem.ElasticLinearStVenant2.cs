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
        public void ElasticLinearStVenantProblem2(MainWindow mainWindow, bool isStVenant)
        {
            double beamLen = 1.0;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(beamLen, 0.0));
                pts.Add(new OpenTK.Vector2d(beamLen, h));
                pts.Add(new OpenTK.Vector2d(0.0, h));
                uint lId1 = cad.AddPolygon(pts).AddLId;
            }

            double eLen = 0.1;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 2; // Vector2
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point3;

            {
                world.ClearMaterial();
                uint maId = 0;
                double rho  = 2.3e+3;
                double E = 169.0e+9;
                double nu =0.262;
                if (isStVenant)
                {
                    var ma = new StVenantHyperelasticMaterial();
                    ma.Young = E;
                    ma.Poisson = nu;
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = rho;
                    maId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new LinearElasticMaterial();
                    ma.Young = E;
                    ma.Poisson = nu;
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = rho;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            uint[] zeroEIds = { 4 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                zeroFixedCads.Add(fixedCad);
            }

            /*
            // 節点荷重
            // load
            FieldFixedCad forceFixedCad;
            {
                // FixedDofIndex 0: u
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                };
                var fixedCads = world.GetForceFieldFixedCads(quantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                forceFixedCad = world.GetForceFieldFixedCads(quantityId)[0];
            }
            */
            // 分布荷重
            // load
            PortCondition forcePortCondition;
            {
                ElasticBCType bcType = ElasticBCType.ExternalForce;
                var portConditions = world.GetPortConditions(quantityId);
                var portDatas = new[]
                {
                    new { EId = (uint)2, Parameters = new List<double> { 0.0, 0.0 } }
                };
                // Vector2(dummy)
                IList<uint> fixedDofIndexs = new List<uint>(); // dummy
                IList<double> fixedValues = new List<double>(); // dummy
                // fx、fy
                uint additionalParamDof = 2;
                foreach (var data in portDatas)
                {
                    // Vector2
                    IList<uint> eIds = new List<uint>();
                    eIds.Add(data.EId);
                    var portCondition = new ConstPortCondition(
                        eIds, CadElementType.Edge, FieldValueType.Vector2,
                        fixedDofIndexs, fixedValues, additionalParamDof);
                    portCondition.IntAdditionalParameters = new List<int> { (int)bcType };
                    double[] param = portCondition.GetDoubleAdditionalParameters();
                    System.Diagnostics.Debug.Assert(data.Parameters.Count == param.Length);
                    data.Parameters.CopyTo(param, 0);
                    portConditions.Add(portCondition);
                }
                forcePortCondition = portConditions[0];
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
                Minimum = -beamLen,
                Maximum = beamLen
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
            uint eqStressValueId = 0;
            uint stressValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                var edgeDrawer2 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            uint endVId = 2;
            int endCoId = world.GetCoordIdsFromCadId(quantityId, endVId, CadElementType.Vertex)[0];
            int endNodeId = world.Coord2Node(quantityId, endCoId);
            double t = 0;
            double dt = 0.05;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] force;
                /*
                // 節点荷重
                double[] forceFixedValue = forceFixedCad.GetDoubleValues();
                forceFixedValue[0] = 0.0;
                forceFixedValue[1] = -0.5e+6 * Math.Sin(t * 2.0 * Math.PI * 0.1) / (b * h);
                force = forceFixedValue;
                */
                // 分布荷重
                {
                    double[] values = forcePortCondition.GetDoubleAdditionalParameters();
                    values[0] = 0.0;
                    values[1] = -0.5e+6 * Math.Sin(t * 2.0 * Math.PI * 0.1) / (b * h * h);
                    force = values;
                }

                var FEM = new Elastic2DFEM(world);
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
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                if (isStVenant)
                {
                    FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6; // 収束条件を緩める
                }
                FEM.Solve();
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                {
                    double u = U[endNodeId * 2];
                    double v = U[endNodeId * 2 + 1];
                    double m = force[1];
                    series1.Points.Add(new DataPoint(u, m));
                    series2.Points.Add(new DataPoint(v, m));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }
                t += dt;
            }
        }

        public void ElasticLinearStVenantTDProblem2(MainWindow mainWindow, bool isStVenant)
        {
            double beamLen = 1.0;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(beamLen, 0.0));
                pts.Add(new OpenTK.Vector2d(beamLen, h));
                pts.Add(new OpenTK.Vector2d(0.0, h));
                uint lId1 = cad.AddPolygon(pts).AddLId;
            }

            double eLen = 0.1;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 2; // Vector2
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            {
                world.ClearMaterial();
                uint maId = 0;
                double rho = 2.3e+3;
                double E = 169.0e+9;
                double nu = 0.262;
                if (isStVenant)
                {
                    var ma = new StVenantHyperelasticMaterial();
                    ma.Young = E;
                    ma.Poisson = nu;
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = rho;
                    maId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new LinearElasticMaterial();
                    ma.Young = E;
                    ma.Poisson = nu;
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = rho;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            uint[] zeroEIds = { 4 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                zeroFixedCads.Add(fixedCad);
            }

            /*
            // 節点荷重
            // load
            FieldFixedCad forceFixedCad;
            {
                // FixedDofIndex 0: u
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                };
                var fixedCads = world.GetForceFieldFixedCads(quantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                forceFixedCad = world.GetForceFieldFixedCads(quantityId)[0];
            }
            */
            // 分布荷重
            // load
            PortCondition forcePortCondition;
            {
                ElasticBCType bcType = ElasticBCType.ExternalForce;
                var portConditions = world.GetPortConditions(quantityId);
                var portDatas = new[]
                {
                    new { EId = (uint)2, Parameters = new List<double> { 0.0, 0.0 } }
                };
                // Vector2(dummy)
                IList<uint> fixedDofIndexs = new List<uint>(); // dummy
                IList<double> fixedValues = new List<double>(); // dummy
                // fx、fy
                uint additionalParamDof = 2;
                foreach (var data in portDatas)
                {
                    // Vector2
                    IList<uint> eIds = new List<uint>();
                    eIds.Add(data.EId);
                    var portCondition = new ConstPortCondition(
                        eIds, CadElementType.Edge, FieldValueType.Vector2,
                        fixedDofIndexs, fixedValues, additionalParamDof);
                    portCondition.IntAdditionalParameters = new List<int> { (int)bcType };
                    double[] param = portCondition.GetDoubleAdditionalParameters();
                    System.Diagnostics.Debug.Assert(data.Parameters.Count == param.Length);
                    data.Parameters.CopyTo(param, 0);
                    portConditions.Add(portCondition);
                }
                forcePortCondition = portConditions[0];
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
                Minimum = -beamLen,
                Maximum = beamLen
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
            uint prevValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                var edgeDrawer2 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            uint endVId = 2;
            int endCoId = world.GetCoordIdsFromCadId(quantityId, endVId, CadElementType.Vertex)[0];
            int endNodeId = world.Coord2Node(quantityId, endCoId);
            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] force;
                /*
                // 節点荷重
                double[] forceFixedValue = forceFixedCad.GetDoubleValues();
                forceFixedValue[0] = 0.0;
                forceFixedValue[1] = -0.5e+6 * Math.Sin(t * 2.0 * Math.PI * 0.1) / (b * h);
                force = forceFixedValue;
                */
                // 分布荷重
                {
                    double[] values = forcePortCondition.GetDoubleAdditionalParameters();
                    values[0] = 0.0;
                    values[1] = -0.5e+6 * Math.Sin(t * 2.0 * Math.PI * 0.1) / (b * h * h);
                    force = values;
                }

                var FEM = new Elastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueId, prevValueId);
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
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                if (isStVenant)
                {
                    FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6; // 収束条件を緩める
                }
                FEM.Solve();
                double[] U = FEM.U;

                FEM.UpdateFieldValuesTimeDomain();

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                {
                    double u = U[endNodeId * 2];
                    double v = U[endNodeId * 2 + 1];
                    double m = force[1];
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
