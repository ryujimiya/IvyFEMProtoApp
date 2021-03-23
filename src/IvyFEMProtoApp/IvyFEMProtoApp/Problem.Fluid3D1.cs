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
        public void Fluid3DProblem1(MainWindow mainWindow, FluidEquationType fluidEquationType)
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

            //FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes
            //FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;

            IList<uint> shellLIds1;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }

            {
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(shellLIds1, holes1, insideVIds1);
            }

            // check
            cad.SetLoopColor(3, new double[3] { 0.0, 0.0, 1.0 });
            cad.SetLoopColor(5, new double[3] { 1.0, 0.0, 0.0 });

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad3DDrawer(cad);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            //double eLen = 0.08;
            //double eLen = 0.20;
            //double eLen = 0.30;
            double eLen = 0.20;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint vQuantityId;
            uint pQuantityId;
            {
                uint vDof = 3; // 3次元ベクトル
                uint pDof = 1; // スカラー
                uint vFEOrder = 2;// 1;
                uint pFEOrder = 1;
                /*
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
                    vFEOrder = 2;
                    pFEOrder = 1;
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
                    vFEOrder = 2;// 1;
                    pFEOrder = 2;// 1;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                */
                vQuantityId = world.AddQuantity(vDof, vFEOrder, FiniteElementType.ScalarLagrange);
                pQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarLagrange);
            }
            world.TetIntegrationPointCount = TetrahedronIntegrationPointCount.Point5;

            {
                world.ClearMaterial();
                NewtonFluidMaterial ma = null;
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
                    ma = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        GravityZ = 0.0,
                        Mu = 0.02//0.002//0.00002
                    };
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
                    ma = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        GravityZ = 0.0,
                        //Mu = 0.0002//0.00002
                        Mu = 0.02
                    };
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                uint maId = world.AddMaterial(ma);

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId);

                uint[] lIds = { 1, 2, 3, 4, 5, 6 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            //uint[] zeroLIds = { 1, 2, 3, 4, 5 }; // 方程式が解けない(不能?)
            //uint[] zeroLIds = { 1, 3, 5 }; // x側面だけ - 解ける
            uint[] zeroLIds = { 1, 2, 3, 4, 5 }; // FIXME
            var zeroFixedCads = world.GetZeroFieldFixedCads(vQuantityId);
            foreach (uint lId in zeroLIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Vector3);
                zeroFixedCads.Add(fixedCad);
            }
            {
                var fixedCadDatas = new[]
                {
                    // source
                    new { CadId = (uint)6, CadElemType = CadElementType.Loop,
                        FixedDofIndexs = new List<uint> { 0, 1, 2 }, Values = new List<double> { 0.5, 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(vQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            world.MakeElements();

            uint vValueId = 0;
            uint bubbleVValueId = 0;
            uint pValueId = 0;
            VectorFieldDrawer vectorDrawer;
            //FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                vValueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    vQuantityId, false, FieldShowType.Real);
                bubbleVValueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    vQuantityId, true, FieldShowType.Real);
                // Scalar
                pValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                vectorDrawer = new VectorFieldDrawer(
                    bubbleVValueId, FieldDerivativeType.Value, world);
                vectorDrawer.Color = new double[3] { 1.0, 0.0, 0.0 };
                //faceDrawer = new FaceFieldDrawer(pValueId, FieldDerivativeType.Value, true, world,
                //    pValueId, FieldDerivativeType.Value);
                edgeDrawer = new EdgeFieldDrawer(
                    vValueId, FieldDerivativeType.Value, true, false, world);
            }
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(vectorDrawer);
                //fieldDrawerArray.Add(faceDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }

            {
                var FEM = new Fluid3DFEM(world);
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

                world.UpdateFieldValueValuesFromNodeValues(vValueId, FieldDerivativeType.Value, U);
                world.UpdateFieldValueValuesFromNodeValues(pValueId, FieldDerivativeType.Value, U);
                world.UpdateBubbleFieldValueValuesFromNodeValues(bubbleVValueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
            }
        }

        public void Fluid3DTDProblem1(MainWindow mainWindow, FluidEquationType fluidEquationType)
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

            //FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes
            //FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            IList<uint> shellLIds1;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }

            {
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(shellLIds1, holes1, insideVIds1);
            }

            // check
            cad.SetLoopColor(3, new double[3] { 0.0, 0.0, 1.0 });
            cad.SetLoopColor(5, new double[3] { 1.0, 0.0, 0.0 });

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad3DDrawer(cad);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            //double eLen = 0.08;
            //double eLen = 0.20;
            //double eLen = 0.30;
            double eLen = 0.20;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint vQuantityId;
            uint pQuantityId;
            {
                uint vDof = 3; // 3次元ベクトル
                uint pDof = 1; // スカラー
                uint vFEOrder = 2;// 1;
                uint pFEOrder = 1;
                /*
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
                    vFEOrder = 2;
                    pFEOrder = 1;
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
                    vFEOrder = 2;// 1;
                    pFEOrder = 2;// 1;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                */
                vQuantityId = world.AddQuantity(vDof, vFEOrder, FiniteElementType.ScalarLagrange);
                pQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarLagrange);
            }
            world.TetIntegrationPointCount = TetrahedronIntegrationPointCount.Point5;

            {
                world.ClearMaterial();

                NewtonFluidMaterial ma = null;
                if (fluidEquationType == FluidEquationType.StdGNavierStokes)
                {
                    ma = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        Mu = 0.02//0.002//0.00002
                    };
                }
                else if (fluidEquationType == FluidEquationType.SUPGNavierStokes)
                {
                    ma = new NewtonFluidMaterial
                    {
                        MassDensity = 1.2,
                        GravityX = 0.0,
                        GravityY = 0.0,
                        //Mu = 0.0002//0.00002
                        Mu = 0.02
                    };
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                uint maId = world.AddMaterial(ma);

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId);

                uint[] lIds = { 1, 2, 3, 4, 5, 6 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            //uint[] zeroLIds = { 1, 2, 3, 4, 5 }; // 方程式が解けない(不能?)
            //uint[] zeroLIds = { 1, 3, 5 }; // x側面だけ - 解ける
            uint[] zeroLIds = { 1, 2, 3, 4, 5 }; // FIXME
            var zeroFixedCads = world.GetZeroFieldFixedCads(vQuantityId);
            foreach (uint lId in zeroLIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Vector3);
                zeroFixedCads.Add(fixedCad);
            }
            {
                var fixedCadDatas = new[]
                {
                    // source
                    new { CadId = (uint)6, CadElemType = CadElementType.Loop,
                        FixedDofIndexs = new List<uint> { 0, 1, 2 }, Values = new List<double> { 0.5, 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(vQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            world.MakeElements();

            uint vValueId = 0;
            uint prevVValueId = 0;
            uint bubbleVValueId = 0;
            uint pValueId = 0;
            VectorFieldDrawer vectorDrawer;
            //FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                vValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    vQuantityId, false, FieldShowType.Real);
                prevVValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    vQuantityId, false, FieldShowType.Real);
                bubbleVValueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    vQuantityId, true, FieldShowType.Real);
                // Scalar
                pValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                vectorDrawer = new VectorFieldDrawer(
                    bubbleVValueId, FieldDerivativeType.Value, world);
                vectorDrawer.Color = new double[3] { 1.0, 0.0, 0.0 };
                //faceDrawer = new FaceFieldDrawer(pValueId, FieldDerivativeType.Value, true, world,
                //    pValueId, FieldDerivativeType.Value);
                edgeDrawer = new EdgeFieldDrawer(
                    vValueId, FieldDerivativeType.Value, true, false, world);
            }
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(vectorDrawer);
                //fieldDrawerArray.Add(faceDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }

            double t = 0;
            double dt = 30.0;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 10; iTime++)
            {
                var FEM = new Fluid3DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma, vValueId, prevVValueId);
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
