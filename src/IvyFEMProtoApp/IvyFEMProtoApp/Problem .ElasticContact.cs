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
        public void ElasticContactTDProblem(MainWindow mainWindow, bool isSaintVenant)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 5.0));
                pts.Add(new OpenTK.Vector2d(0.0, 5.0));
                var res = cad2D.AddPolygon(pts);
            }

            double eLen = 0.1;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint uQuantityId;
            uint cQuantityId; // constraint
            {
                uint uDof = 2; // Vector2
                uint cDof = 1;
                uint uFEOrder = 1;
                uint cFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder);
                cQuantityId = world.AddQuantity(cDof, cFEOrder);
            }

            {
                world.ClearMaterial();
                uint maId = 0;
                if (isSaintVenant)
                {
                    var ma = new SaintVenantHyperelasticMaterial();
                    ma.SetYoungPoisson(300, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1.0;
                    maId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new LinearElasticMaterial();
                    ma.SetYoungPoisson(300, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1.0;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            FieldFixedCad fixedCadXY;
            {
                // FixedDofIndex 0: X 1: Y
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new double[] { 0.0, 0.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(uQuantityId);
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    uint dof = 2; // Vector2
                    var fixedCad = new FieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, dof, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadXY = world.GetFieldFixedCads(uQuantityId)[0];
            }

            // MPC
            LineConstraint lineConstraint;
            {
                var cadIdTypes = new List<KeyValuePair<uint, CadElementType>>();
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(1, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(2, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(3, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(4, CadElementType.Edge));
                var pt = new OpenTK.Vector2d(0.0, -0.1);
                double theta = 15.0 * Math.PI / 180.0;
                var normal = new OpenTK.Vector2d(Math.Cos(theta), Math.Sin(theta));
                lineConstraint = new LineConstraint(pt, normal, EqualityType.GreaterEq);
                MultipointConstraint mpc = new MultipointConstraint(cadIdTypes, lineConstraint);
                world.AddMultipointConstraint(cQuantityId, mpc);
            }

            world.MakeElements();

            uint valueId = 0;
            uint prevValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                uint dof = 2; // Vector2
                world.ClearFieldValue();
                valueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, dof, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, dof, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }
            var constraintDrawerArray = mainWindow.ConstraintDrawerArray;
            {
                constraintDrawerArray.Clear();
                ConstraintDrawer constraintDrawer = new ConstraintDrawer(lineConstraint);
                constraintDrawerArray.Add(constraintDrawer);
            }

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 100; iTime++)
            {
                fixedCadXY.DoubleValues[0] = -Math.Sin(t * 2.0 * Math.PI * 0.1);
                fixedCadXY.DoubleValues[1] = 0;

                var FEM = new Elastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueId, prevValueId);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
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
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-10;
                    FEM.Solver = solver;
                }
                FEM.Solve();
                //double[] U = FEM.U;

                FEM.UpdateFieldValues();

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void ElasticCircleContactTDProblem(MainWindow mainWindow, bool isSaintVenant)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 5.0));
                pts.Add(new OpenTK.Vector2d(0.0, 5.0));
                var res = cad2D.AddPolygon(pts);
            }

            double eLen = 0.1;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint uQuantityId;
            uint cQuantityId; // constraint
            {
                uint uDof = 2; // Vector2
                uint cDof = 1;
                uint uFEOrder = 1;
                uint cFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder);
                cQuantityId = world.AddQuantity(cDof, cFEOrder);
            }

            {
                world.ClearMaterial();
                uint maId = 0;
                if (isSaintVenant)
                {
                    var ma = new SaintVenantHyperelasticMaterial();
                    ma.SetYoungPoisson(300, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = -1.0;
                    ma.MassDensity = 1.0;
                    maId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new LinearElasticMaterial();
                    ma.SetYoungPoisson(300, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = -1.0;
                    ma.MassDensity = 1.0;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            // MPC
            CircleConstraint circleConstraint;
            {
                var cadIdTypes = new List<KeyValuePair<uint, CadElementType>>();
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(1, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(2, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(3, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(4, CadElementType.Edge));
                var pt = new OpenTK.Vector2d(0.0, -2.0);
                double r = 1.8;
                circleConstraint = new CircleConstraint(pt, r, EqualityType.GreaterEq);
                MultipointConstraint mpc = new MultipointConstraint(cadIdTypes, circleConstraint);
                world.AddMultipointConstraint(cQuantityId, mpc);
            }

            world.MakeElements();

            uint valueId = 0;
            uint prevValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                uint dof = 2; // Vector2
                world.ClearFieldValue();
                valueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, dof, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, dof, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }
            var constraintDrawerArray = mainWindow.ConstraintDrawerArray;
            {
                constraintDrawerArray.Clear();
                ConstraintDrawer constraintDrawer = new ConstraintDrawer(circleConstraint);
                constraintDrawerArray.Add(constraintDrawer);
            }

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 150; iTime++)
            {
                var FEM = new Elastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueId, prevValueId);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
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
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-10;
                    FEM.Solver = solver;
                }
                FEM.Solve();
                //double[] U = FEM.U;

                FEM.UpdateFieldValues();

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
