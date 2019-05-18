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
        public void FluidProblem2(MainWindow mainWindow, FluidEquationType fluidEquationType)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                uint lId1 = 0;
                uint lId2 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0, 0.7));
                    pts.Add(new OpenTK.Vector2d(0.5, 0.7));
                    pts.Add(new OpenTK.Vector2d(0.5, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.5, 0.0));
                    pts.Add(new OpenTK.Vector2d(2.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(2.0, 1.0));
                    pts.Add(new OpenTK.Vector2d(1.5, 1.0));
                    pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                    lId1 = cad2D.AddPolygon(pts).AddLId;
                    System.Diagnostics.Debug.Assert(lId1 == 1);
                    lId2 = cad2D.ConnectVertexLine(4, 7).AddLId;
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

            double eLen = 0.08;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint vQuantityId;
            uint pQuantityId;
            {
                uint vDof = 2; // 2次元ベクトル
                uint pDof = 1; // スカラー
                uint vFEOrder = 2;
                uint pFEOrder = 1;
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
                    vFEOrder = 2;
                    pFEOrder = 1;
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
                    vFEOrder = 1;
                    pFEOrder = 1;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                vQuantityId = world.AddQuantity(vDof, vFEOrder);
                pQuantityId = world.AddQuantity(pDof, pFEOrder);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point7;

            {
                world.ClearMaterial();
                NewtonFluidMaterial ma1 = null;
                NewtonFluidMaterial ma2 = null;
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
                    ma1 = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        Mu = 0.02//0.002//0.00002
                    };
                    ma2 = new NewtonFluidMaterial(ma1);
                    ma2.Mu = ma1.Mu * 10.0;
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
                    ma1 = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        Mu = 0.0002//0.00002
                    };
                    ma2 = new NewtonFluidMaterial(ma1);
                    ma2.Mu = ma1.Mu * 10.0;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                uint maId1 = world.AddMaterial(ma1);
                uint maId2 = world.AddMaterial(ma2);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId1);

                uint lId2 = 2;
                world.SetCadLoopMaterial(lId2, maId2);
            }

            uint[] zeroEIds = { 1, 2, 3, 4, 6, 7 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(vQuantityId);
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                zeroFixedCads.Add(fixedCad);
            }

            ///////////
            // 分布速度
            Func<double, double> vFunc = y => -0.4 * 4.0 / (0.3 * 0.3) * (y - 0.7) * (y - 1.0);
            //////////
            DistributedFieldFixedCad srcFixedCad;
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)8, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(vQuantityId);
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    // Note: 分布速度条件
                    var fixedCad = new DistributedFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs);
                    fixedCads.Add(fixedCad);
                }

                srcFixedCad = fixedCads[0] as DistributedFieldFixedCad;
            }

            world.MakeElements();

            uint vValueId = 0;
            uint bubbleVValueId = 0;
            uint pValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                vValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    vQuantityId, false, FieldShowType.Real);
                bubbleVValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    vQuantityId, true, FieldShowType.Real);
                // Scalar
                pValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer vectorDrawer = new VectorFieldDrawer(
                    bubbleVValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                IFieldDrawer faceDrawer = new FaceFieldDrawer(pValueId, FieldDerivativeType.Value, true, world,
                    pValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    vValueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            {
                // 分布速度条件
                {
                    srcFixedCad.InitDoubleValues();
                    foreach (int coId in srcFixedCad.CoIds)
                    {
                        double[] coord = world.GetCoord(vQuantityId, coId);
                        double[] values = srcFixedCad.GetDoubleValues(coId);
                        values[0] = vFunc(coord[1]);
                        values[1] = 0;
                    }
                }

                var FEM = new Fluid2DFEM(world);
                FEM.EquationType = fluidEquationType;
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
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
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
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
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                        solver.ILUFillinLevel = 1;
                        solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.BiCGSTAB;
                        FEM.Solver = solver;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
                    // default
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
                    // 収束が遅いのでこの精度で様子を見る
                    FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-6;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                FEM.Solve();
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(vValueId, FieldDerivativeType.Value, U);
                world.UpdateFieldValueValuesFromNodeValues(pValueId, FieldDerivativeType.Value, U);
                world.UpdateBubbleFieldValueValuesFromNodeValues(bubbleVValueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
            }
        }

        public void FluidTDProblem2(MainWindow mainWindow, FluidEquationType fluidEquationType)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                uint lId1 = 0;
                uint lId2 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0, 0.7));
                    pts.Add(new OpenTK.Vector2d(0.5, 0.7));
                    pts.Add(new OpenTK.Vector2d(0.5, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.5, 0.0));
                    pts.Add(new OpenTK.Vector2d(2.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(2.0, 1.0));
                    pts.Add(new OpenTK.Vector2d(1.5, 1.0));
                    pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                    lId1 = cad2D.AddPolygon(pts).AddLId;
                    System.Diagnostics.Debug.Assert(lId1 == 1);
                    lId2 = cad2D.ConnectVertexLine(4, 7).AddLId;
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

            double eLen = 0.08;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint vQuantityId;
            uint pQuantityId;
            {
                uint vDof = 2; // 2次元ベクトル
                uint pDof = 1; // スカラー
                uint vFEOrder = 2;
                uint pFEOrder = 1;
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
                    vFEOrder = 2;
                    pFEOrder = 1;
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
                    vFEOrder = 1;
                    pFEOrder = 1;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                vQuantityId = world.AddQuantity(vDof, vFEOrder);
                pQuantityId = world.AddQuantity(pDof, pFEOrder);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point7;

            {
                world.ClearMaterial();
                NewtonFluidMaterial ma1 = null;
                NewtonFluidMaterial ma2 = null;
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
                    ma1 = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        Mu = 0.02//0.002//0.00002
                    };
                    ma2 = new NewtonFluidMaterial(ma1);
                    ma2.Mu = ma1.Mu * 10.0;
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
                    ma1 = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        Mu = 0.0002//0.00002
                    };
                    ma2 = new NewtonFluidMaterial(ma1);
                    ma2.Mu = ma1.Mu * 10.0;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                uint maId1 = world.AddMaterial(ma1);
                uint maId2 = world.AddMaterial(ma2);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId1);

                uint lId2 = 2;
                world.SetCadLoopMaterial(lId2, maId2);
            }

            uint[] zeroEIds = { 1, 2, 3, 4, 6, 7 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(vQuantityId);
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                zeroFixedCads.Add(fixedCad);
            }

            ///////////
            // 分布速度
            Func<double, double> vFunc = y => -0.4 * 4.0 / (0.3 * 0.3) * (y - 0.7) * (y - 1.0);
            //////////
            DistributedFieldFixedCad srcFixedCad;
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)8, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(vQuantityId);
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    // Note: 分布速度条件
                    var fixedCad = new DistributedFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs);
                    fixedCads.Add(fixedCad);
                }

                srcFixedCad = fixedCads[0] as DistributedFieldFixedCad;
            }

            world.MakeElements();

            uint vValueId = 0;
            uint prevVValueId = 0;
            uint bubbleVValueId = 0;
            uint pValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                vValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    vQuantityId, false, FieldShowType.Real);
                prevVValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    vQuantityId, false, FieldShowType.Real);
                bubbleVValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    vQuantityId, true, FieldShowType.Real);
                // Scalar
                pValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer vectorDrawer = new VectorFieldDrawer(
                    bubbleVValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                IFieldDrawer faceDrawer = new FaceFieldDrawer(pValueId, FieldDerivativeType.Value, true, world,
                    pValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    vValueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 10.0;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 20; iTime++)
            {
                // 分布速度条件
                {
                    srcFixedCad.InitDoubleValues();
                    foreach (int coId in srcFixedCad.CoIds)
                    {
                        double[] coord = world.GetCoord(vQuantityId, coId);
                        double[] values = srcFixedCad.GetDoubleValues(coId);
                        values[0] = vFunc(coord[1]);
                        values[1] = 0;
                    }
                }

                var FEM = new Fluid2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma, vValueId, prevVValueId);
                FEM.EquationType = fluidEquationType;
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
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
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
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
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                        solver.ILUFillinLevel = 1;
                        solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.BiCGSTAB;
                        FEM.Solver = solver;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
                    // default
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
                    // 収束が遅いのでこの精度で様子を見る
                    FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-6;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                FEM.Solve();
                double[] U = FEM.U;

                FEM.UpdateFieldValuesTimeDomain(); // for vValueId, prevVValueId
                world.UpdateFieldValueValuesFromNodeValues(pValueId, FieldDerivativeType.Value, U);
                world.UpdateBubbleFieldValueValuesFromNodeValues(bubbleVValueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
