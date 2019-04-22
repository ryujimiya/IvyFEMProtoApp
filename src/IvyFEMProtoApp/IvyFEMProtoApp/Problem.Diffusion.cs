using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        // 拡散方程式の定常状態はPoisson方程式
        public void DiffusionProblem(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                uint lId1 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 1.0));
                    pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                    lId1 = cad2D.AddPolygon(pts).AddLId;
                    System.Diagnostics.Debug.Assert(lId1 == 1);
                }
                uint lId2 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0.7, 0.5));
                    pts.Add(new OpenTK.Vector2d(0.8, 0.5));
                    pts.Add(new OpenTK.Vector2d(0.8, 0.9));
                    pts.Add(new OpenTK.Vector2d(0.7, 0.9));
                    lId2 = cad2D.AddPolygon(pts, lId1).AddLId;
                    System.Diagnostics.Debug.Assert(lId2 == 2);
                }
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
            WPFUtils.DoEvents();

            double eLen = 0.05;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 1; // スカラー
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder);
            }

            {
                world.ClearMaterial();
                PoissonMaterial ma = new PoissonMaterial
                {
                    Alpha = 1.0,
                    F = 0.0
                };
                uint maId = world.AddMaterial(ma);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId);

                uint lId2 = 2;
                world.SetCadLoopMaterial(lId2, maId);
            }

            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)2, CadElemType = CadElementType.Loop,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new double[] { 1.0 } },
                    new { CadId = (uint)1, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new double[] { 1.0 } },
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new double[] { -1.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(quantityId);
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            world.MakeElements();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // スカラー
                valueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
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
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            {
                var FEM = new Poisson2DFEM(world);
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
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.Solve();
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
            }
        }

        public void DiffusionTDProblem(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                uint lId1 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 1.0));
                    pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                    lId1 = cad2D.AddPolygon(pts).AddLId;
                    System.Diagnostics.Debug.Assert(lId1 == 1);
                }
                uint lId2 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0.7, 0.5));
                    pts.Add(new OpenTK.Vector2d(0.8, 0.5));
                    pts.Add(new OpenTK.Vector2d(0.8, 0.9));
                    pts.Add(new OpenTK.Vector2d(0.7, 0.9));
                    lId2 = cad2D.AddPolygon(pts, lId1).AddLId;
                    System.Diagnostics.Debug.Assert(lId2 == 2);
                }
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
            WPFUtils.DoEvents();

            double eLen = 0.05;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 1; // スカラー
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder);
            }

            {
                world.ClearMaterial();
                DiffusionMaterial ma = new DiffusionMaterial
                {
                    MassDensity = 1.0,
                    Capacity = 30.0,
                    DiffusionCoef = 1.0,
                    F = 0.0
                };
                uint maId = world.AddMaterial(ma);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId);

                uint lId2 = 2;
                world.SetCadLoopMaterial(lId2, maId);
            }

            FieldFixedCad srcFixedCad; 
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)2, CadElemType = CadElementType.Loop,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new double[] { 0.0 } },
                    new { CadId = (uint)1, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new double[] { 1.0 } },
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new double[] { -1.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(quantityId);
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }

                srcFixedCad = fixedCads[0];
            }

            world.MakeElements();

            uint valueId = 0;
            uint prevValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // スカラー
                valueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
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
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] srcFixedValues = srcFixedCad.GetDoubleValues();
                srcFixedValues[0] = Math.Floor(1 + 0.8 * Math.Cos(2.0 * Math.PI * t + 0.1));

                var FEM = new Diffusion2DTDFEM(world, dt,
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
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.Solve();
                //double[] U = FEM.U;
                FEM.UpdateFieldValuesTimeDomain();

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
