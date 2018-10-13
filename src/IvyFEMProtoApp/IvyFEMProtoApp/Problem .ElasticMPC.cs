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
        public void ElasticMultipointConstraintProblem(MainWindow mainWindow, bool isCalcStress, bool isSaintVenant)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0 / Math.Sqrt(2.0), 5.0 / Math.Sqrt(2.0)));
                pts.Add(new OpenTK.Vector2d(4.0 / Math.Sqrt(2.0), 6.0 / Math.Sqrt(2.0)));
                pts.Add(new OpenTK.Vector2d(-1.0 / Math.Sqrt(2.0), 1.0 / Math.Sqrt(2.0)));
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
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point3;

            {
                world.ClearMaterial();
                uint maId = 0;
                if (isSaintVenant)
                {
                    var ma = new SaintVenantHyperelasticMaterial();
                    ma.SetYoungPoisson(10.0, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1;
                    maId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new LinearElasticMaterial();
                    ma.SetYoungPoisson(10.0, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1;
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
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge, Dof = 2,
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
                uint eId = 4;
                var pt = new OpenTK.Vector2d(0.0, 0.0);
                var normal = new OpenTK.Vector2d(1.0 / Math.Sqrt(2.0), 1.0 / Math.Sqrt(2.0));
                lineConstraint = new LineConstraint(pt, normal);
                MultipointConstraint mpc = new MultipointConstraint(eId, CadElementType.Edge, lineConstraint);
                world.AddMultipointConstraint(cQuantityId, mpc);
            }

            world.MakeElements();

            uint valueId = 0;
            uint eqStressValueId = 0;
            uint stressValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                uint dof = 2; // Vector2
                world.ClearFieldValue();
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivationType.Value,
                    uQuantityId, dof, false, FieldShowType.Real);
                if (isCalcStress)
                {
                    const int eqStressDof = 1;
                    eqStressValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivationType.Value,
                        uQuantityId, eqStressDof, true, FieldShowType.Real);
                    const int stressDof = 3;
                    stressValueId = world.AddFieldValue(FieldValueType.SymmetricTensor2, FieldDerivationType.Value,
                        uQuantityId, stressDof, true, FieldShowType.Real);
                }
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                if (isCalcStress)
                {
                    IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.Value, false, world,
                        eqStressValueId, FieldDerivationType.Value, 0, 0.5);
                    fieldDrawerArray.Add(faceDrawer);
                    IFieldDrawer vectorDrawer = new VectorFieldDrawer(stressValueId, FieldDerivationType.Value, world);
                    fieldDrawerArray.Add(vectorDrawer);
                }
                else
                {
                    IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.Value, false, world);
                    fieldDrawerArray.Add(faceDrawer);
                }
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivationType.Value, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(valueId, FieldDerivationType.Value, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }
            var constraintDrawerArray = mainWindow.ConstraintDrawerArray;
            {
                ConstraintDrawer constraintDrawer = new ConstraintDrawer(lineConstraint);
                constraintDrawerArray.Add(constraintDrawer);
            }

            double t = 0;
            double dt = 0.05;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                fixedCadXY.DoubleValues[0] = -Math.Sin(t * 2.0 * Math.PI * 0.1);
                fixedCadXY.DoubleValues[1] = Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic2DFEM(world);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.IsOrderingToBandMatrix = true;
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
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivationType.Value, U);
                if (isCalcStress)
                {
                    Elastic2DDerivedBaseFEM.SetStressValue(valueId, stressValueId, eqStressValueId, world);
                }

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void ElasticMultipointConstraintTDProblem(MainWindow mainWindow, bool isSaintVenant)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0 / Math.Sqrt(2.0), 5.0 / Math.Sqrt(2.0)));
                pts.Add(new OpenTK.Vector2d(4.0 / Math.Sqrt(2.0), 6.0 / Math.Sqrt(2.0)));
                pts.Add(new OpenTK.Vector2d(-1.0 / Math.Sqrt(2.0), 1.0 / Math.Sqrt(2.0)));
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
                    //ma.SetYoungPoisson(10.0, 0.3);
                    ma.SetYoungPoisson(300.0, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1;
                    maId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new LinearElasticMaterial();
                    //ma.SetYoungPoisson(10.0, 0.3);
                    ma.SetYoungPoisson(300.0, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1;
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
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge, Dof = 2,
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
                uint eId = 4;
                var pt = new OpenTK.Vector2d(0.0, 0.0);
                var normal = new OpenTK.Vector2d(1.0 / Math.Sqrt(2.0), 1.0 / Math.Sqrt(2.0));
                lineConstraint = new LineConstraint(pt, normal);
                MultipointConstraint mpc = new MultipointConstraint(eId, CadElementType.Edge, lineConstraint);
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
                    FieldDerivationType.Value | FieldDerivationType.Velocity | FieldDerivationType.Acceleration,
                    uQuantityId, dof, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivationType.Value | FieldDerivationType.Velocity | FieldDerivationType.Acceleration,
                    uQuantityId, dof, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivationType.Value, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(valueId, FieldDerivationType.Value, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }
            var constraintDrawerArray = mainWindow.ConstraintDrawerArray;
            {
                ConstraintDrawer constraintDrawer = new ConstraintDrawer(lineConstraint);
                constraintDrawerArray.Add(constraintDrawer);
            }

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 400; iTime++)
            {
                fixedCadXY.DoubleValues[0] = -Math.Sin(t * 2.0 * Math.PI * 0.1);
                fixedCadXY.DoubleValues[1] = Math.Sin(t * 2.0 * Math.PI * 0.1);

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
