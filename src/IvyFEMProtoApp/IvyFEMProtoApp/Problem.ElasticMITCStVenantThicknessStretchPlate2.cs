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
        public void MITCStVenantThicknessStretchPlateProblem2(MainWindow mainWindow)
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

            double plateA = 1.0;
            double plateB = plateA;
            double plateThickness = 0.2 * plateA;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(plateA, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(plateA, plateB, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, plateB, 0.0));
                uint lId1 = cad.AddPolygon(pts).AddLId;
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad3DDrawer(cad);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            double eLen = 0.1;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            var meshDrawer = new Mesher3DDrawer(mesher);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint dQuantityId; // displacement
            uint rQuantityId; // rotation
            uint lQuantityId; // stretch
            {
                uint dDof = 3; // Vector3 (u,v,w)
                uint rDof = 3; // Vector3 (θx,θy,θz)
                uint lDof = 2; // Vector2 (λ1、λ2)
                uint dFEOrder = 1;
                uint rFEOrder = 1;
                uint lFEOrder = 0; // constant element
                dQuantityId = world.AddQuantity(dDof, dFEOrder, FiniteElementType.ScalarLagrange);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarLagrange);
                lQuantityId = world.AddQuantity(lDof, lFEOrder, FiniteElementType.ScalarConstant);
            }
            uint[] dQuantityIds = { dQuantityId };

            {
                world.ClearMaterial();
                uint maId = 0;
                {
                    var ma = new MITCStVenantThicknessStretchPlateMaterial();
                    ma.Thickness = plateThickness;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    ma.ShearCorrectionFactor = 1.0;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            // 頂点の支点
            uint[] dZeroVIds = { 1, 3, 4 };
            var dZeroFixedCads = world.GetZeroFieldFixedCads(dQuantityId);
            foreach (uint vId in dZeroVIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Vector3);
                dZeroFixedCads.Add(fixedCad);
            }
            uint[] rZeroVIds = { 1, 3, 4 };
            var rZeroFixedCads = world.GetZeroFieldFixedCads(rQuantityId);
            foreach (uint vId in rZeroVIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Vector3);
                rZeroFixedCads.Add(fixedCad);
            }

            // 節点荷重
            // load
            FieldFixedCad forceFixedCadD;
            {
                // FixedDofIndex 0: u,v
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1, 2 }, Values = new List<double> { 0.0, 0.0, 0.0 } },
                };
                var fixedCads = world.GetForceFieldFixedCads(dQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                forceFixedCadD = world.GetForceFieldFixedCads(dQuantityId)[0];
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
            model.Title = "Plate Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "u,v,w",
                Minimum = -plateA,
                Maximum = plateA
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

            uint uValueId = 0;
            uint vnValueId = 0;
            uint v1ValueId = 0;
            uint v2ValueId = 0;
            uint lValueId = 0;
            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 表示
                // Vector3
                valueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    dQuantityId, false, FieldShowType.Real);

                uValueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    dQuantityId, FieldValueNodeType.Node, FieldShowType.Real);
                vnValueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    dQuantityId, FieldValueNodeType.ElementNode, FieldShowType.Real);
                v1ValueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    dQuantityId, FieldValueNodeType.ElementNode, FieldShowType.Real);
                v2ValueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    dQuantityId, FieldValueNodeType.ElementNode, FieldShowType.Real);
                lValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    dQuantityId, FieldValueNodeType.Bubble, FieldShowType.Real);

                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var edgeDrawer0 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer0);
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }
            uint[] additionalValueIds = new uint[] { uValueId, vnValueId, v1ValueId, v2ValueId, lValueId };

            uint observeVId = 2;
            int observeCoId = world.GetCoordIdsFromCadId(dQuantityId, observeVId, CadElementType.Vertex)[0];
            double t = 0;
            double dt = 0.05;
            int timeCnt = 200;
            for (int iTime = 0; iTime <= timeCnt; iTime++)
            {
                double[] force = new double[3];
                // 節点荷重
                {
                    double[] forceFixedValueD = forceFixedCadD.GetDoubleValues();
                    forceFixedValueD[0] = 0.0;
                    forceFixedValueD[1] = 0.0;
                    double forceValue = 1.0e+8;
                    if (iTime >= 0 && iTime < (timeCnt / 4))
                    {
                        forceFixedValueD[2] = -forceValue * (4.0 / (double)timeCnt) * iTime;
                    }
                    else if (iTime >= (timeCnt / 4) && iTime < (timeCnt / 2))
                    {
                        forceFixedValueD[2] =
                            -forceValue + forceValue * (4.0 / (double)timeCnt) * (iTime - (timeCnt / 4));
                    }
                    else if (iTime >= (timeCnt / 2) && iTime < ((timeCnt * 3) / 4))
                    {
                        forceFixedValueD[2] = forceValue * (4.0 / (double)timeCnt) * (iTime - (timeCnt / 2));
                    }
                    else if (iTime >= ((timeCnt * 3) / 4))
                    {
                        forceFixedValueD[2] =
                            forceValue - forceValue * (4.0 / (double)timeCnt) * (iTime - ((timeCnt * 3) / 4));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                        forceFixedValueD[2] = 0.0;
                    }
                    force[0] = forceFixedValueD[0];
                    force[1] = forceFixedValueD[1];
                    force[2] = forceFixedValueD[2];
                }

                var FEM = new Elastic3DFEM(world);
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
                FEM.AdditionalValueIds = additionalValueIds;
                FEM.IsUseInit = true;
                FEM.IsUseUpdate = true;
                FEM.TimeIndexForInit = iTime;
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6;
                FEM.Solve();

                FieldValue uFV = world.GetFieldValue(uValueId);
                // 変位(u,v,w)へ変換する
                int coCnt = (int)world.GetCoordCount(dQuantityId);
                int dDof = (int)world.GetDof(dQuantityId);
                int rDof = (int)world.GetDof(rQuantityId);
                int lDof = (int)world.GetDof(lQuantityId);
                int dNodeCnt = (int)world.GetNodeCount(dQuantityId);
                int rNodeCnt = (int)world.GetNodeCount(rQuantityId);
                int lNodeCnt = (int)world.GetNodeCount(lQuantityId);
                int rOffset = dNodeCnt * dDof;
                int lOffset = rOffset + rNodeCnt * rDof;
                int dof = 3;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    double[] uValue = uFV.GetDoubleValue(coId, FieldDerivativeType.Value);
                    U[coId * dof + 0] = uValue[0];
                    U[coId * dof + 1] = uValue[1];
                    U[coId * dof + 2] = uValue[2];
                }
                // Note: from CoordValues
                world.UpdateFieldValueValuesFromCoordValues(valueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                {
                    double u = U[observeCoId * dof];
                    double v = U[observeCoId * dof + 1];
                    double w = U[observeCoId * dof + 2];
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
