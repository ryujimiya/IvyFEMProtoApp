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
        public void VorticityFluidProblem1(MainWindow mainWindow, FluidEquationType fluidEquationType)
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
            uint wQuantityId;
            uint pQuantityId;
            {
                uint wDof = 1; // ω
                uint pDof = 1; // ψ
                uint wFEOrder = 1;
                uint pFEOrder = 1;
                if (fluidEquationType == FluidEquationType.StdGVorticity)
                {
                    wFEOrder = 1;
                    pFEOrder = 1;
                }
                else if (fluidEquationType == FluidEquationType.SUPGVorticity)
                {
                    wFEOrder = 1;
                    pFEOrder = 1;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                wQuantityId = world.AddQuantity(wDof, wFEOrder);
                pQuantityId = world.AddQuantity(pDof, pFEOrder);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point7;

            {
                world.ClearMaterial();
                NewtonFluidMaterial ma = null;
                if (fluidEquationType == FluidEquationType.StdGVorticity)
                {
                    ma = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        Mu = 0.02//0.002//0.00002
                    };
                }
                else if (fluidEquationType == FluidEquationType.SUPGVorticity)
                {
                    ma = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        Mu = 0.0002//0.00002
                    };
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                uint maId = world.AddMaterial(ma);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId);

                uint[] eIds = { 1, 2, 3, 4 };
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, maId);
                }
            }

            // 入口(境界3)も流線が平行なので0
            uint[] pZeroEIds = { 1, 2, 3, 4 };
            var pZeroFixedCads = world.GetZeroFieldFixedCads(pQuantityId);
            pZeroFixedCads.Clear();
            foreach (uint eId in pZeroEIds)
            {
                // Scalar
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                pZeroFixedCads.Add(fixedCad);
            }

            // ωを差分法の式で指定
            {
                var portDatas = new[]
                {
                    // 境界 (xに平行な境界ならvx方向 = dψ/dyを指定、vy方向 = 0)
                    new { EId = (uint)3, Parameters = new List<double> { 0.0, 0.5 } },
                    new { EId = (uint)1, Parameters = new List<double> { 0.0, 0.0 } },
                    new { EId = (uint)2, Parameters = new List<double> { 0.0, 0.0 } },
                    new { EId = (uint)4, Parameters = new List<double> { 0.0, 0.0 } },
                };
                var portConditions = world.GetPortConditions(wQuantityId);
                portConditions.Clear();
                foreach (var data in portDatas)
                {
                    // Scalar
                    IList<uint> eIds = new List<uint>();
                    eIds.Add(data.EId);
                    var portCondition = new PortCondition(eIds, FieldValueType.Scalar);
                    portCondition.IntAdditionalParameters = new List<int> { };
                    portCondition.DoubleAdditionalParameters = data.Parameters;
                    portConditions.Add(portCondition);
                }
            }

            world.MakeElements();

            uint wValueId = 0;
            uint pValueId = 0;
            uint vValueId = 0;
            uint bubbleVValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Scalar
                wValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    wQuantityId, false, FieldShowType.Real);
                // Vector2 (ψからvを求める)
                vValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                bubbleVValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    pQuantityId, true, FieldShowType.Real);
                // Scalar
                pValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer vectorDrawer = new VectorFieldDrawer(
                    bubbleVValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                IFieldDrawer faceDrawer = new FaceFieldDrawer(wValueId, FieldDerivativeType.Value, true, world,
                    wValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    wValueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            {
                var FEM = new Fluid2DFEM(world);
                FEM.EquationType = fluidEquationType;
                if (fluidEquationType == FluidEquationType.StdGVorticity)
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
                        solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                        FEM.Solver = solver;
                    }
                }
                else if (fluidEquationType == FluidEquationType.SUPGVorticity)
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
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                if (fluidEquationType == FluidEquationType.StdGVorticity)
                {
                    // default
                }
                else if (fluidEquationType == FluidEquationType.SUPGVorticity)
                {
                    FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-6;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                FEM.Solve();
                double[] U = FEM.U;
                double[] V = FEM.CoordV;

                world.UpdateFieldValueValuesFromNodeValues(wValueId, FieldDerivativeType.Value, U);
                world.UpdateFieldValueValuesFromNodeValues(pValueId, FieldDerivativeType.Value, U);
                world.UpdateBubbleFieldValueValuesFromCoordValues(bubbleVValueId, FieldDerivativeType.Value, V);

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
            }
        }

        public void VorticityFluidTDProblem1(MainWindow mainWindow, FluidEquationType fluidEquationType)
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
            uint wQuantityId;
            uint pQuantityId;
            {
                uint wDof = 1; // ω
                uint pDof = 1; // ψ
                uint wFEOrder = 1;
                uint pFEOrder = 1;
                if (fluidEquationType == FluidEquationType.StdGVorticity)
                {
                    wFEOrder = 1;
                    pFEOrder = 1;
                }
                else if (fluidEquationType == FluidEquationType.SUPGVorticity)
                {
                    wFEOrder = 1;
                    pFEOrder = 1;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                wQuantityId = world.AddQuantity(wDof, wFEOrder);
                pQuantityId = world.AddQuantity(pDof, pFEOrder);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point7;

            {
                world.ClearMaterial();
                NewtonFluidMaterial ma = null;
                if (fluidEquationType == FluidEquationType.StdGVorticity)
                {
                    ma = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        Mu = 0.02//0.002//0.00002
                    };
                }
                else if (fluidEquationType == FluidEquationType.SUPGVorticity)
                {
                    ma = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        Mu = 0.0002//0.00002
                    };
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                uint maId = world.AddMaterial(ma);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId);

                uint[] eIds = { 1, 2, 3, 4 };
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, maId);
                }
            }

            // 入口(境界3)も流線が平行なので0
            uint[] pZeroEIds = { 1, 2, 3, 4 };
            var pZeroFixedCads = world.GetZeroFieldFixedCads(pQuantityId);
            pZeroFixedCads.Clear();
            foreach (uint eId in pZeroEIds)
            {
                // Scalar
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                pZeroFixedCads.Add(fixedCad);
            }

            // ωを差分法の式で指定
            {
                var portDatas = new[]
                {
                    // 境界 (xに平行な境界ならvx方向 = dψ/dyを指定、vy方向 = 0)
                    new { EId = (uint)3, Parameters = new List<double> { 0.0, 0.5 } },
                    new { EId = (uint)1, Parameters = new List<double> { 0.0, 0.0 } },
                    new { EId = (uint)2, Parameters = new List<double> { 0.0, 0.0 } },
                    new { EId = (uint)4, Parameters = new List<double> { 0.0, 0.0 } }
                };
                var portConditions = world.GetPortConditions(wQuantityId);
                portConditions.Clear();
                foreach (var data in portDatas)
                {
                    // Scalar
                    IList<uint> eIds = new List<uint>();
                    eIds.Add(data.EId);
                    var portCondition = new PortCondition(eIds, FieldValueType.Scalar);
                    portCondition.IntAdditionalParameters = new List<int> { };
                    portCondition.DoubleAdditionalParameters = data.Parameters;
                    portConditions.Add(portCondition);
                }
            }

            world.MakeElements();

            uint wValueId = 0;
            uint prevWValueId = 0;
            uint pValueId = 0;
            uint vValueId = 0;
            uint bubbleVValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Scalar
                wValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    wQuantityId, false, FieldShowType.Real);
                prevWValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    wQuantityId, false, FieldShowType.Real);
                // Vector2 (ψからvを求める)
                vValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                bubbleVValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    pQuantityId, true, FieldShowType.Real);
                // Scalar
                pValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer vectorDrawer = new VectorFieldDrawer(
                    bubbleVValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                IFieldDrawer faceDrawer = new FaceFieldDrawer(wValueId, FieldDerivativeType.Value, true, world,
                    wValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    wValueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 30.0;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 10; iTime++)
            {
                var FEM = new Fluid2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma, wValueId, prevWValueId);
                FEM.EquationType = fluidEquationType;
                if (fluidEquationType == FluidEquationType.StdGVorticity)
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
                        solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                        FEM.Solver = solver;
                    }
                }
                else if (fluidEquationType == FluidEquationType.SUPGVorticity)
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
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                if (fluidEquationType == FluidEquationType.StdGVorticity)
                {
                    // default
                }
                else if (fluidEquationType == FluidEquationType.SUPGVorticity)
                {
                    FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-6;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                FEM.Solve();
                double[] U = FEM.U;
                double[] V = FEM.CoordV;

                FEM.UpdateFieldValuesTimeDomain(); // for wValueId, prevWValueId
                world.UpdateFieldValueValuesFromNodeValues(pValueId, FieldDerivativeType.Value, U);
                world.UpdateBubbleFieldValueValuesFromCoordValues(bubbleVValueId, FieldDerivativeType.Value, V);

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
