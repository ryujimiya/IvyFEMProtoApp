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
        public void FluidLESTDProblem2(MainWindow mainWindow)
        {
            Cad2D cad = new Cad2D();
            {
                uint lId1 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, -1.0));
                    pts.Add(new OpenTK.Vector2d(0.0, -1.0));
                    pts.Add(new OpenTK.Vector2d(0.0, -2.0));
                    pts.Add(new OpenTK.Vector2d(2.0, -2.0));
                    pts.Add(new OpenTK.Vector2d(2.0, 1.0));
                    pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                    lId1 = cad.AddPolygon(pts).AddLId;
                    System.Diagnostics.Debug.Assert(lId1 == 1);
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

            //double eLen = 0.08;
            double eLen = 0.20;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint vQuantityId;
            uint pQuantityId;
            uint kpQuantityId;
            {
                uint vDof = 2;
                uint pDof = 1;
                uint kpDof = 1;
                uint vFEOrder = 1;
                uint pFEOrder = 1;
                uint kpFEOrder = 1;
                vQuantityId = world.AddQuantity(vDof, vFEOrder, FiniteElementType.ScalarLagrange);
                pQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarLagrange);
                kpQuantityId = world.AddQuantity(kpDof, kpFEOrder, FiniteElementType.ScalarLagrange);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point7;

            {
                world.ClearMaterial();

                NewtonFluidMaterial ma = null;
                ma = new NewtonFluidMaterial
                {
                    MassDensity = 1.2,
                    GravityX = 0.0,
                    GravityY = 0.0,
                    Mu = 0.02
                };
                uint maId = world.AddMaterial(ma);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId);
            }

            /*
            uint[] zeroEIds = { };
            var zeroFixedCads = world.GetZeroFieldFixedCads(vQuantityId);
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                zeroFixedCads.Add(fixedCad);
            }
            uint[] kpZeroEIds = { };
            var kpZeroFixedCads = world.GetZeroFieldFixedCads(kpQuantityId);
            foreach (uint eId in kpZeroEIds)
            {
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                kpZeroFixedCads.Add(fixedCad);
            }
            */

            //double vx0 = 0.5;
            //double vx0 = 0.1;
            //double vx0 = 0.05;
            double vx0 = 0.5;// 0.05;
            //double cbc = 0.001;///0.01;
            //double kp0 = cbc * vx0 * vx0;
            double kp0 = 0.0;
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)8, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { vx0, 0.0 } },
                    //
                    new { CadId = (uint)1, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)5, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)6, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)7, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<double> { 0.0 } },
                    //
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                    new { CadId = (uint)3, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                    new { CadId = (uint)6, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                    new { CadId = (uint)7, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(vQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)8, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { kp0 } },
                    //
                    new { CadId = (uint)1, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)5, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)6, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)7, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(kpQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            world.MakeElements();

            uint vValueId = 0;
            uint prevVValueId = 0;
            uint kpValueId = 0;
            uint prevKpValueId = 0;
            uint bubbleVValueId = 0;
            uint pValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                vValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    vQuantityId, false, FieldShowType.Real);
                prevVValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    vQuantityId, false, FieldShowType.Real);
                bubbleVValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    vQuantityId, true, FieldShowType.Real);
                pValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                kpValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    kpQuantityId, false, FieldShowType.Real);
                prevKpValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    kpQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var vectorDrawer = new VectorFieldDrawer(
                    bubbleVValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                var faceDrawer = new FaceFieldDrawer(pValueId, FieldDerivativeType.Value, true, world,
                    pValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    vValueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 1.0e-3;//30.0;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            //for (int iTime = 0; iTime <= 10; iTime++)
            for (int iTime = 0; (t <= 30.0 * 10); iTime++)
            {
                var FEM = new FluidLES2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    vValueId, prevVValueId,
                    kpValueId, prevKpValueId);
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
                    //solver.Method = IvyFEM.Lklinear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    //FEM.Solver = solver;
                }
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6;
                FEM.Solve();
                double[] U = FEM.U;

                FEM.UpdateFieldValuesTimeDomain(); // for vValueId, prevVValueId
                world.UpdateFieldValueValuesFromNodeValues(pValueId, FieldDerivativeType.Value, U);
                world.UpdateBubbleFieldValueValuesFromNodeValues(bubbleVValueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
