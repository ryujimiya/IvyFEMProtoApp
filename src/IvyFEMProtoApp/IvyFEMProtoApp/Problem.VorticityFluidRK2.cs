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
        public void VorticityFluidRKTDProblem2(MainWindow mainWindow)
        {
            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
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
            uint wQuantityId;
            uint pQuantityId;
            {
                uint wDof = 1; // ω
                uint pDof = 1; // ψ
                uint wFEOrder = 1;
                uint pFEOrder = 1;
                wQuantityId = world.AddQuantity(wDof, wFEOrder);
                pQuantityId = world.AddQuantity(pDof, pFEOrder);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point7;

            {
                world.ClearMaterial();
                NewtonFluidMaterial ma1 = null;
                NewtonFluidMaterial ma2 = null;
                ma1 = new NewtonFluidMaterial
                {
                    MassDensity = 1.2,
                    GravityX = 0.0,
                    GravityY = 0.0,
                    Mu = 0.02//0.002//0.00002
                };
                ma2 = new NewtonFluidMaterial(ma1);
                ma2.Mu = ma1.Mu * 10.0;
                uint maId1 = world.AddMaterial(ma1);
                uint maId2 = world.AddMaterial(ma2);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId1);

                uint lId2 = 2;
                world.SetCadLoopMaterial(lId2, maId2);

                uint[] eIds1 = { 1, 2, 3, 7, 8 };
                foreach (uint eId in eIds1)
                {
                    world.SetCadEdgeMaterial(eId, maId1);
                }

                uint[] eIds2 = { 4, 5, 6 };
                foreach (uint eId in eIds2)
                {
                    world.SetCadEdgeMaterial(eId, maId2);
                }
            }

            // 1-4: ψ = 0, 6-7: ψ = const (!= 0)
            uint[] pZeroEIds = { 1, 2, 3, 4 };
            var pZeroFixedCads = world.GetZeroFieldFixedCads(pQuantityId);
            pZeroFixedCads.Clear();
            foreach (uint eId in pZeroEIds)
            {
                // Scalar
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                pZeroFixedCads.Add(fixedCad);
            }

            //////////////////////////
            // ψ、ωの分布
            // vx方向 = 0.4(一定)
            //Func<double, double> pFunc = y => 0.4 * (y - 0.7);
            //Func<double, double> wFunc = y => 0;
            // Parabolic inflow
            Func<double, double> pFunc = y => (4.0 * 0.4 / (0.3 * 0.3)) *
                (-(1.0 / 3.0) * y * y * y + (1.0 / 2.0) * (0.7 + 1.0) * y * y - 0.7 * 1.0 * y -
                (-(1.0 / 3.0) * 0.7 * 0.7 * 0.7 + (1.0 / 2.0) * (0.7 + 1.0) * 0.7 * 0.7 - 0.7 * 1.0 * 0.7));
            Func<double, double> wFunc = y => (4.0 * 0.4 / (0.3 * 0.3)) *
                (2.0 * y - (0.7 + 1.0));
            //////////////////////////

            // ω(分布)を指定
            DistributedFieldFixedCad wSrcFixedCad;
            {
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(wQuantityId);
                fixedCads.Clear();

                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)8, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 } },
                };
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new DistributedFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs);
                    fixedCads.Add(fixedCad);
                }
                wSrcFixedCad = fixedCads[0] as DistributedFieldFixedCad;
            }

            DistributedFieldFixedCad pSrcFixedCad;
            {
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(pQuantityId);
                fixedCads.Clear();

                // ψ(分布)を指定
                {
                    var fixedCadDatas = new[]
                    {
                        new { CadId = (uint)8, CadElemType = CadElementType.Edge,
                            FixedDofIndexs = new List<uint> { 0 } }
                    };
                    foreach (var data in fixedCadDatas)
                    {
                        // Scalar
                        var fixedCad = new DistributedFieldFixedCad(data.CadId, data.CadElemType,
                            FieldValueType.Scalar, data.FixedDofIndexs);
                        fixedCads.Add(fixedCad);
                    }
                    pSrcFixedCad = fixedCads[0] as DistributedFieldFixedCad;
                }

                // ψ(const)を指定
                {
                    var fixedCadDatas = new[]
                    {
                        new { CadId = (uint)6, CadElemType = CadElementType.Edge,
                            FixedDofIndexs = new List<uint> { 0 }, Values = new List<double>{ pFunc(1.0) } },
                        new { CadId = (uint)7, CadElemType = CadElementType.Edge,
                            FixedDofIndexs = new List<uint> { 0 }, Values = new List<double>{ pFunc(1.0) } }
                    };
                    foreach (var data in fixedCadDatas)
                    {
                        // Scalar
                        var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                            FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                        fixedCads.Add(fixedCad);
                    }
                }
            }

            // ω 境界
            {
                var portConditions = world.GetPortConditions(wQuantityId);
                portConditions.Clear();

                {
                    FlowVorticityBCType bcType = FlowVorticityBCType.Outflow;
                    var portDatas = new[]
                    {
                        // outflow
                        new { EId = (uint)5 }
                    };
                    foreach (var data in portDatas)
                    {
                        // Scalar
                        IList<uint> eIds = new List<uint>();
                        eIds.Add(data.EId);
                        var portCondition = new PortCondition(eIds, FieldValueType.Scalar);
                        portCondition.IntAdditionalParameters = new List<int> { (int)bcType };
                        portConditions.Add(portCondition);
                    }
                }
                {
                    FlowVorticityBCType bcType = FlowVorticityBCType.TangentialFlow;
                    var portDatas = new[]
                    {
                        // inflow/outflowは指定しない
                        // その他の境界 (xに平行な境界ならvx方向 = dψ/dyを指定、vy方向 = 0)
                        new { EId = (uint)1, Parameters = new List<double> { 0.0, 0.0 } },
                        new { EId = (uint)2, Parameters = new List<double> { 0.0, 0.0 } },
                        new { EId = (uint)3, Parameters = new List<double> { 0.0, 0.0 } },
                        new { EId = (uint)4, Parameters = new List<double> { 0.0, 0.0 } },
                        new { EId = (uint)6, Parameters = new List<double> { 0.0, 0.0 } },
                        new { EId = (uint)7, Parameters = new List<double> { 0.0, 0.0 } }
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

            // ψ 境界
            {
                var portConditions = world.GetPortConditions(pQuantityId);
                portConditions.Clear();

                {
                    FlowVorticityBCType bcType = FlowVorticityBCType.Outflow;
                    var portDatas = new[]
                    {
                        // outflow
                        new { EId = (uint)5 }
                    };
                    foreach (var data in portDatas)
                    {
                        // Scalar
                        IList<uint> eIds = new List<uint>();
                        eIds.Add(data.EId);
                        var portCondition = new PortCondition(eIds, FieldValueType.Scalar);
                        portCondition.IntAdditionalParameters = new List<int> { (int)bcType };
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
                    FieldDerivativeType.Value, wQuantityId, false, FieldShowType.Real);
                prevWValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value, wQuantityId, false, FieldShowType.Real);
                // Vector2 (ψからvを求める)
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

            double nu;
            {
                uint maId = 1;
                NewtonFluidMaterial ma = world.GetMaterial(maId) as NewtonFluidMaterial;
                nu = ma.Mu / ma.MassDensity;
            }
            double t = 0;
            //double dt = 0.002;
            //int nTime = 100;
            double dt = 0.01 * (1.0 / 4.0) * (eLen * eLen / nu);
            int nTime = 200;
            for (int iTime = 0; iTime <= nTime; iTime++)
            {
                {
                    // ψ分布条件
                    {
                        pSrcFixedCad.InitDoubleValues();
                        foreach (int coId in pSrcFixedCad.CoIds)
                        {
                            double[] coord = world.GetCoord(pQuantityId, coId);
                            double[] values = pSrcFixedCad.GetDoubleValues(coId);
                            values[0] = pFunc(coord[1]);
                        }
                    }
                    // ω分布条件
                    {
                        wSrcFixedCad.InitDoubleValues();
                        foreach (int coId in wSrcFixedCad.CoIds)
                        {
                            double[] coord = world.GetCoord(wQuantityId, coId);
                            double[] values = wSrcFixedCad.GetDoubleValues(coId);
                            values[0] = wFunc(coord[1]);
                        }
                    }
                }

                var FEM = new Fluid2DRKTDFEM(world, dt, wValueId, prevWValueId);
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
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6;
                FEM.Solve();
                double[] U = FEM.U;
                double[] V = FEM.CoordVelocity;

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
