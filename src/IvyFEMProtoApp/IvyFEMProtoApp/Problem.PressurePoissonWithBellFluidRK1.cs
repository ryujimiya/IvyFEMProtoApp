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
        public void PressurePoissonWithBellFluidRKTDProblem1(MainWindow mainWindow)
        {
            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            CadObject2D cad = new CadObject2D();
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
            IDrawer drawer = new CadObject2DDrawer(cad);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            //double eLen = 0.08;
            double eLen = 0.16;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint vQuantityId;
            uint pQuantityId;
            uint pxQuantityId;
            uint pyQuantityId;
            uint pxxQuantityId;
            uint pxyQuantityId;
            uint pyyQuantityId;
            {
                uint vDof = 2; // 2次元ベクトル
                uint pDof = 1; // スカラー
                uint vFEOrder = 2;
                uint pFEOrder = 5; // Bell's element
                vQuantityId = world.AddQuantity(vDof, vFEOrder, FiniteElementType.ScalarLagrange);
                int pQuantityBaseOffset = (int)vQuantityId + 1;
                pQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarBell, pQuantityBaseOffset);
                pxQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarBell, pQuantityBaseOffset);
                pyQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarBell, pQuantityBaseOffset);
                pxxQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarBell, pQuantityBaseOffset);
                pxyQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarBell, pQuantityBaseOffset);
                pyyQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarBell, pQuantityBaseOffset);
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
                    Mu = 0.02//0.002//0.00002
                };
                uint maId = world.AddMaterial(ma);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId);

                uint[] eIds1 = { 1, 2, 3, 4 };
                foreach (uint eId in eIds1)
                {
                    world.SetCadEdgeMaterial(eId, maId);
                }
            }

            uint[] zeroEIds = { 1, 2, 4 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(vQuantityId);
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                zeroFixedCads.Add(fixedCad);
            }

            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.5, 0.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(vQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            // p境界
            {
                var portConditions = world.GetPortConditions(pQuantityId);
                {
                    FlowPressureBCType bcType = FlowPressureBCType.NoConstraint;
                    var portDatas = new[]
                    {
                        new { EId = (uint)1 },
                        new { EId = (uint)2 },
                        new { EId = (uint)3 },
                        new { EId = (uint)4 }
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

            uint vValueId = 0;
            uint prevVValueId = 0;
            uint bubbleVValueId = 0;
            uint pValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                vValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value , vQuantityId, false, FieldShowType.Real);
                prevVValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value, vQuantityId, false, FieldShowType.Real);
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
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            double nu;
            {
                uint maId = 1;
                NewtonFluidMaterial ma = world.GetMaterial(maId) as NewtonFluidMaterial;
                nu = ma.Mu / ma.MassDensity;
            }
            double t = 0;
            //double dt = 0.02;
            //int nTime = 100;
            //double dt = 0.1 * (1.0 / 4.0) * (eLen * eLen / nu);
            double dt = 0.002;
            int nTime = 200;
            for (int iTime = 0; iTime <= nTime; iTime++)
            {
                var FEM = new Fluid2DRKTDFEM(world, dt, vValueId, prevVValueId);
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
                    //solver.ILUFillinLevel = 1;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.BiCGSTAB;
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
