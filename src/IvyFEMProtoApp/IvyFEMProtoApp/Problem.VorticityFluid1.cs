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
            //FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            //FluidEquationType fluidEquationType = FluidEquationType.SUPGGVorticity;
            Cad2D cad = new Cad2D();
            {
                uint lId1 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 1.0));
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

            double eLen = 0.08;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
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
                wQuantityId = world.AddQuantity(wDof, wFEOrder, FiniteElementType.ScalarLagrange);
                pQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarLagrange);
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
            foreach (uint eId in pZeroEIds)
            {
                // Scalar
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                pZeroFixedCads.Add(fixedCad);
            }

            //////////////////////////
            // dψ/dx, dψ/dyの分布
            Func<double, double> pxFunc = x => 0.0;
            Func<double, double> pyFunc = x => 0.5;

            // ω 境界
            DistributedPortCondition wSrcPortCondition;
            {
                var portConditions = world.GetPortConditions(wQuantityId);

                // 境界に平行な速度
                {
                    FlowVorticityBCType bcType = FlowVorticityBCType.TangentialFlow;
                    var portDatas = new[]
                    {
                        new { EId = (uint)3 }
                    };
                    // Scalar
                    IList<uint> fixedDofIndexs = new List<uint>(); // dummy
                    // dψ/dx、dψ / dy
                    uint additionalParamDof = 2;
                    foreach (var data in portDatas)
                    {
                        // Scalar
                        IList<uint> eIds = new List<uint>();
                        eIds.Add(data.EId);
                        var portCondition = new DistributedPortCondition(
                            eIds, FieldValueType.Scalar, fixedDofIndexs, additionalParamDof);
                        portCondition.IntAdditionalParameters = new List<int> { (int)bcType };
                        portConditions.Add(portCondition);
                    }

                    wSrcPortCondition = portConditions[0] as DistributedPortCondition;
                }
                // 境界に平行な速度
                {
                    FlowVorticityBCType bcType = FlowVorticityBCType.TangentialFlow;
                    var portDatas = new[]
                    {
                        //new { EId = (uint)3, Parameters = new List<double> { 0.0, 0.5 } }, // 一定速度の場合
                        new { EId = (uint)1, Parameters = new List<double> { 0.0, 0.0 } },
                        new { EId = (uint)2, Parameters = new List<double> { 0.0, 0.0 } },
                        new { EId = (uint)4, Parameters = new List<double> { 0.0, 0.0 } },
                    };
                    // Scalar
                    IList<uint> fixedDofIndexs = new List<uint>(); // dummy
                    IList<double> fixedValues = new List<double>(); // dummy
                    // dψ/dx、dψ / dy
                    uint additionalParamDof = 2;
                    foreach (var data in portDatas)
                    {
                        // Scalar
                        IList<uint> eIds = new List<uint>();
                        eIds.Add(data.EId);
                        var portCondition = new ConstPortCondition(
                            eIds, FieldValueType.Scalar, fixedDofIndexs, fixedValues, additionalParamDof);
                        portCondition.IntAdditionalParameters = new List<int> { (int)bcType };
                        double[] param = portCondition.GetDoubleAdditionalParameters();
                        System.Diagnostics.Debug.Assert(data.Parameters.Count == param.Length);
                        data.Parameters.CopyTo(param, 0);
                        portConditions.Add(portCondition);
                    }
                }
            }

            world.MakeElements();

            uint wValueId = 0;
            uint pValueId = 0;
            uint bubbleVValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Scalar
                wValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    wQuantityId, false, FieldShowType.Real);
                // Vector2 (ψからvを求める)
                bubbleVValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    pQuantityId, true, FieldShowType.Real);
                // Scalar
                pValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var vectorDrawer = new VectorFieldDrawer(
                    bubbleVValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                var faceDrawer = new FaceFieldDrawer(wValueId, FieldDerivativeType.Value, true, world,
                    wValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    wValueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            {
                // ポートω分布条件
                {
                    wSrcPortCondition.InitDoubleAdditionalParameters();
                    foreach (int coId in wSrcPortCondition.CoIds)
                    {
                        double[] coord = world.GetCoord(pQuantityId, coId);
                        double[] values = wSrcPortCondition.GetDoubleAdditionalParameters(coId);
                        values[0] = pxFunc(coord[0]);
                        values[1] = pyFunc(coord[0]);
                    }
                }

                var FEM = new Fluid2DFEM(world);
                FEM.EquationType = fluidEquationType;
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
                if (fluidEquationType == FluidEquationType.StdGVorticity)
                {
                    // default
                }
                else if (fluidEquationType == FluidEquationType.SUPGVorticity)
                {
                    FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                FEM.Solve();
                double[] U = FEM.U;
                double[] V = FEM.CoordVelocity;

                world.UpdateFieldValueValuesFromNodeValues(wValueId, FieldDerivativeType.Value, U);
                world.UpdateFieldValueValuesFromNodeValues(pValueId, FieldDerivativeType.Value, U);
                world.UpdateBubbleFieldValueValuesFromCoordValues(bubbleVValueId, FieldDerivativeType.Value, V);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
            }
        }

        public void VorticityFluidTDProblem1(MainWindow mainWindow, FluidEquationType fluidEquationType)
        {
            //FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            //FluidEquationType fluidEquationType = FluidEquationType.SUPGGVorticity;
            Cad2D cad = new Cad2D();
            {
                uint lId1 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 1.0));
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

            double eLen = 0.08;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
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
                wQuantityId = world.AddQuantity(wDof, wFEOrder, FiniteElementType.ScalarLagrange);
                pQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarLagrange);
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
            foreach (uint eId in pZeroEIds)
            {
                // Scalar
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                pZeroFixedCads.Add(fixedCad);
            }

            //////////////////////////
            // dψ/dx, dψ/dyの分布
            Func<double, double> pxFunc = x => 0.0;
            Func<double, double> pyFunc = x => 0.5;

            // ω 境界
            DistributedPortCondition wSrcPortCondition;
            {
                var portConditions = world.GetPortConditions(wQuantityId);

                // 境界に平行な速度
                {
                    FlowVorticityBCType bcType = FlowVorticityBCType.TangentialFlow;
                    var portDatas = new[]
                    {
                        new { EId = (uint)3 }
                    };
                    // Scalar
                    IList<uint> fixedDofIndexs = new List<uint>(); // dummy
                    // dψ/dx、dψ / dy
                    uint additionalParamDof = 2;
                    foreach (var data in portDatas)
                    {
                        // Scalar
                        IList<uint> eIds = new List<uint>();
                        eIds.Add(data.EId);
                        var portCondition = new DistributedPortCondition(
                            eIds, FieldValueType.Scalar, fixedDofIndexs, additionalParamDof);
                        portCondition.IntAdditionalParameters = new List<int> { (int)bcType };
                        portConditions.Add(portCondition);
                    }

                    wSrcPortCondition = portConditions[0] as DistributedPortCondition;
                }
                // 境界に平行な速度
                {
                    FlowVorticityBCType bcType = FlowVorticityBCType.TangentialFlow;
                    var portDatas = new[]
                    {
                        //new { EId = (uint)3, Parameters = new List<double> { 0.0, 0.5 } }, // 一定速度の場合
                        new { EId = (uint)1, Parameters = new List<double> { 0.0, 0.0 } },
                        new { EId = (uint)2, Parameters = new List<double> { 0.0, 0.0 } },
                        new { EId = (uint)4, Parameters = new List<double> { 0.0, 0.0 } },
                    };
                    // Scalar
                    IList<uint> fixedDofIndexs = new List<uint>(); // dummy
                    IList<double> fixedValues = new List<double>(); // dummy
                    // dψ/dx、dψ / dy
                    uint additionalParamDof = 2;
                    foreach (var data in portDatas)
                    {
                        // Scalar
                        IList<uint> eIds = new List<uint>();
                        eIds.Add(data.EId);
                        var portCondition = new ConstPortCondition(
                            eIds, FieldValueType.Scalar, fixedDofIndexs, fixedValues, additionalParamDof);
                        portCondition.IntAdditionalParameters = new List<int> { (int)bcType };
                        double[] param = portCondition.GetDoubleAdditionalParameters();
                        System.Diagnostics.Debug.Assert(data.Parameters.Count == param.Length);
                        data.Parameters.CopyTo(param, 0);
                        portConditions.Add(portCondition);
                    }
                }
            }

            world.MakeElements();

            uint wValueId = 0;
            uint prevWValueId = 0;
            uint pValueId = 0;
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
                bubbleVValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    pQuantityId, true, FieldShowType.Real);
                // Scalar
                pValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var vectorDrawer = new VectorFieldDrawer(
                    bubbleVValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                var faceDrawer = new FaceFieldDrawer(wValueId, FieldDerivativeType.Value, true, world,
                    wValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    wValueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 30.0;
            int nTime = 10;
            if (fluidEquationType == FluidEquationType.StdGVorticity)
            {
                dt = 30.0;
                nTime = 10;
            }
            else if (fluidEquationType == FluidEquationType.SUPGVorticity)
            {
                dt = 180.0;
                nTime = 5;
                //dt = 1.0;
                //nTime = 300;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= nTime; iTime++)
            {
                // ポートω分布条件
                {
                    wSrcPortCondition.InitDoubleAdditionalParameters();
                    foreach (int coId in wSrcPortCondition.CoIds)
                    {
                        double[] coord = world.GetCoord(pQuantityId, coId);
                        double[] values = wSrcPortCondition.GetDoubleAdditionalParameters(coId);
                        values[0] = pxFunc(coord[0]);
                        values[1] = pyFunc(coord[0]);
                    }
                }

                var FEM = new Fluid2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma, wValueId, prevWValueId);
                FEM.EquationType = fluidEquationType;
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

                if (fluidEquationType == FluidEquationType.StdGVorticity)
                {
                    // default
                }
                else if (fluidEquationType == FluidEquationType.SUPGVorticity)
                {
                    FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                FEM.Solve();
                double[] U = FEM.U;
                double[] V = FEM.CoordVelocity;

                FEM.UpdateFieldValuesTimeDomain(); // for wValueId, prevWValueId
                world.UpdateFieldValueValuesFromNodeValues(pValueId, FieldDerivativeType.Value, U);
                world.UpdateBubbleFieldValueValuesFromCoordValues(bubbleVValueId, FieldDerivativeType.Value, V);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
