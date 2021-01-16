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
        public void ElasticLinearStVenant3DProblem2(MainWindow mainWindow, bool isStVenant)
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

            double beamLen = 1.0;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            IList<uint> shellLIds1;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, b, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, b, h));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, h));
                pts.Add(new OpenTK.Vector3d(beamLen, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(beamLen, b, 0.0));
                pts.Add(new OpenTK.Vector3d(beamLen, b, h));
                pts.Add(new OpenTK.Vector3d(beamLen, 0.0, h));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }

            {
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(shellLIds1, holes1, insideVIds1);
            }

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

            double eLen = 0.1;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 3; // Vector3
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }
            world.TetIntegrationPointCount = TetrahedronIntegrationPointCount.Point4;

            {
                world.ClearMaterial();
                uint maId1 = 0;
                double rho  = 2.3e+3;
                double E = 169.0e+9;
                double nu =0.262;
                if (isStVenant)
                {
                    var ma1 = new StVenantHyperelasticMaterial();
                    ma1.Young = E;
                    ma1.Poisson = nu;
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.GravityZ = 0;
                    ma1.MassDensity = rho;
                    maId1 = world.AddMaterial(ma1);
                }
                else
                {
                    var ma1 = new LinearElasticMaterial();
                    ma1.Young = E;
                    ma1.Poisson = nu;
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.GravityZ = 0;
                    ma1.MassDensity = rho;
                    maId1 = world.AddMaterial(ma1);
                }

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);

                uint[] lIds = { 1, 2, 3, 4, 5, 6 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId1);
                }
            }

            uint[] zeroLIds = { 1 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint lId in zeroLIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Vector3);
                zeroFixedCads.Add(fixedCad);
            }

            // 分布荷重
            // load
            PortCondition forcePortCondition;
            {
                ElasticBCType bcType = ElasticBCType.ExternalForce;
                var portConditions = world.GetPortConditions(quantityId);
                var portDatas = new[]
                {
                    new { LId = (uint)6, Parameters = new List<double> { 0.0, 0.0, 0.0 } }
                };
                // Vector3(dummy)
                IList<uint> fixedDofIndexs = new List<uint>(); // dummy
                IList<double> fixedValues = new List<double>(); // dummy
                // fx、fy, fz
                uint additionalParamDof = 3;
                foreach (var data in portDatas)
                {
                    // Vector2
                    IList<uint> lIds = new List<uint>();
                    lIds.Add(data.LId);
                    var portCondition = new ConstPortCondition(
                        lIds, CadElementType.Loop, FieldValueType.Vector3,
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
                Title = "u,v,w",
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
            var series3 = new LineSeries
            {
                Title = "w",
                LineStyle = LineStyle.None,
                MarkerType = MarkerType.Circle
            };
            model.Series.Add(series1);
            model.Series.Add(series2);
            model.Series.Add(series3);
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();

            uint valueId = 0;
            FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            //EdgeFieldDrawer edgeDrawer2;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                valueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                //edgeDrawer2 = new EdgeFieldDrawer(
                //    valueId, FieldDerivativeType.Value, true, true, world);
            }
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(faceDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                //fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }

            uint endVId = 8;
            int endCoId = world.GetCoordIdsFromCadId(quantityId, endVId, CadElementType.Vertex)[0];
            int endNodeId = world.Coord2Node(quantityId, endCoId);
            double t = 0;
            double dt = 0.05;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] force;
                // 分布荷重
                {
                    double[] values = forcePortCondition.GetDoubleAdditionalParameters();
                    values[0] = 0.0;
                    values[1] = 0.0;
                    values[2] = -0.5e+9 * Math.Sin(t * 2.0 * Math.PI * 0.1);
                    force = values;
                }

                var FEM = new Elastic3DFEM(world);
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
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6; // 収束条件を緩める
                FEM.Solve();
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                {
                    double u = U[endNodeId * 3];
                    double v = U[endNodeId * 3 + 1];
                    double w = U[endNodeId * 3 + 2];
                    double m = force[2];
                    series1.Points.Add(new DataPoint(u, m));
                    series2.Points.Add(new DataPoint(v, m));
                    series3.Points.Add(new DataPoint(w, m));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }
                t += dt;
            }
        }

        public void ElasticLinearStVenant3DTDProblem2(MainWindow mainWindow, bool isStVenant)
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

            double beamLen = 1.0;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            IList<uint> shellLIds1;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, b, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, b, h));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, h));
                pts.Add(new OpenTK.Vector3d(beamLen, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(beamLen, b, 0.0));
                pts.Add(new OpenTK.Vector3d(beamLen, b, h));
                pts.Add(new OpenTK.Vector3d(beamLen, 0.0, h));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }

            {
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(shellLIds1, holes1, insideVIds1);
            }

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

            double eLen = 0.1;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 3; // Vector3
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }
            world.TetIntegrationPointCount = TetrahedronIntegrationPointCount.Point4;

            {
                world.ClearMaterial();
                uint maId1 = 0;
                double rho = 2.3e+3;
                double E = 169.0e+9;
                double nu = 0.262;
                if (isStVenant)
                {
                    var ma1 = new StVenantHyperelasticMaterial();
                    ma1.Young = E;
                    ma1.Poisson = nu;
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.GravityZ = 0;
                    ma1.MassDensity = rho;
                    maId1 = world.AddMaterial(ma1);
                }
                else
                {
                    var ma1 = new LinearElasticMaterial();
                    ma1.Young = E;
                    ma1.Poisson = nu;
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.GravityZ = 0;
                    ma1.MassDensity = rho;
                    maId1 = world.AddMaterial(ma1);
                }

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);

                uint[] lIds = { 1, 2, 3, 4, 5, 6 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId1);
                }
            }

            uint[] zeroLIds = { 1 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint lId in zeroLIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Vector3);
                zeroFixedCads.Add(fixedCad);
            }

            // 分布荷重
            // load
            PortCondition forcePortCondition;
            {
                ElasticBCType bcType = ElasticBCType.ExternalForce;
                var portConditions = world.GetPortConditions(quantityId);
                var portDatas = new[]
                {
                    new { LId = (uint)6, Parameters = new List<double> { 0.0, 0.0, 0.0 } }
                };
                // Vector3(dummy)
                IList<uint> fixedDofIndexs = new List<uint>(); // dummy
                IList<double> fixedValues = new List<double>(); // dummy
                // fx、fy, fz
                uint additionalParamDof = 3;
                foreach (var data in portDatas)
                {
                    // Vector2
                    IList<uint> lIds = new List<uint>();
                    lIds.Add(data.LId);
                    var portCondition = new ConstPortCondition(
                        lIds, CadElementType.Loop, FieldValueType.Vector3,
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
                Title = "u,v,w",
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
            var series3 = new LineSeries
            {
                Title = "w",
                LineStyle = LineStyle.None,
                MarkerType = MarkerType.Circle
            };
            model.Series.Add(series1);
            model.Series.Add(series2);
            model.Series.Add(series3);
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();

            uint valueId = 0;
            uint prevValueId = 0;
            FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            //EdgeFieldDrawer edgeDrawer2;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                valueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);
                faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                //edgeDrawer2 = new EdgeFieldDrawer(
                //    valueId, FieldDerivativeType.Value, true, true, world);
            }
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(faceDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                //fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }

            uint endVId = 8;
            int endCoId = world.GetCoordIdsFromCadId(quantityId, endVId, CadElementType.Vertex)[0];
            int endNodeId = world.Coord2Node(quantityId, endCoId);
            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] force;
                // 分布荷重
                {
                    double[] values = forcePortCondition.GetDoubleAdditionalParameters();
                    values[0] = 0.0;
                    values[1] = 0.0;
                    values[2] = -0.5e+9 * Math.Sin(t * 2.0 * Math.PI * 0.1);
                    force = values;
                }

                var FEM = new Elastic3DTDFEM(world, dt,
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
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6; // 収束条件を緩める
                FEM.Solve();
                double[] U = FEM.U;

                FEM.UpdateFieldValuesTimeDomain();

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                {
                    double u = U[endNodeId * 3];
                    double v = U[endNodeId * 3 + 1];
                    double w = U[endNodeId * 3 + 2];
                    double m = force[2];
                    series1.Points.Add(new DataPoint(u, m));
                    series2.Points.Add(new DataPoint(v, m));
                    series3.Points.Add(new DataPoint(w, m));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }
                t += dt;
            }
        }
    }
}
