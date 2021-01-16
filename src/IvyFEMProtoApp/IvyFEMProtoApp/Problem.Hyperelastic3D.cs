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
        public void Hyperelastic3DProblem(MainWindow mainWindow, bool isMooney)
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

            IList<uint> shellLIds1;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(5.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(5.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(5.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(5.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }

            {
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(shellLIds1, holes1, insideVIds1);
            }

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

            //double eLen = 0.1;
            double eLen = 0.2;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint uQuantityId;
            uint lQuantityId;
            {
                uint uDof = 3; // Vector3
                uint lDof = 1; // Scalar
                uint uFEOrder = 1;
                uint lFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder, FiniteElementType.ScalarLagrange);
                lQuantityId = world.AddQuantity(lDof, lFEOrder, FiniteElementType.ScalarLagrange);
            }
            world.TetIntegrationPointCount = TetrahedronIntegrationPointCount.Point5;

            {
                world.ClearMaterial();
                uint maId1 = 0;
                if (isMooney)
                {
                    // Mooney-Rivlin
                    var ma1 = new MooneyRivlinHyperelasticMaterial();
                    ma1.IsCompressible = false;
                    //ma1.IsCompressible = true;
                    //ma1.D1 = 1.0; // 非圧縮性のときは必要なし
                    ma1.C1 = 200;
                    ma1.C2 = 200;
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.GravityZ = 0;
                    ma1.MassDensity = 1.0;
                    maId1 = world.AddMaterial(ma1);
                }
                else
                {
                    // Odgen
                    var ma1 = new OgdenHyperelasticMaterial();
                    double[] alphas = { 1.3, 5.0, -2.0 };
                    double[] mus = { 6300e3, 1.2e3, -10e3 };
                    //double[] alphas = { 2.0, -2.0 };
                    //double[] mus = { 400, -400 };
                    System.Diagnostics.Debug.Assert(alphas.Length == mus.Length);
                    ma1.IsCompressible = false;
                    //ma1.IsCompressible = true;
                    //ma1.D1 = 1.0; // 非圧縮性のときは必要なし
                    ma1.SetAlphaMu(alphas.Length, alphas, mus);
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.GravityZ = 0;
                    ma1.MassDensity = 1.0;
                    maId1 = world.AddMaterial(ma1);
                }

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);

                uint[] lIds = { 1, 2, 3, 4, 5, 6 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId1);
                }
            }

            uint[] zeroLIds = { 5 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(uQuantityId);
            foreach (uint lId in zeroLIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Vector3);
                zeroFixedCads.Add(fixedCad);
            }

            FieldFixedCad fixedCadXYZ;
            {
                // FixedDofIndex 0: X 1: Y 2: Z
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)3, CadElemType = CadElementType.Loop,
                        FixedDofIndexs = new List<uint> { 0, 1, 2 }, Values = new List<double> { 0.0, 0.0, 0.0 } }
                };
                var fixedCads = world.GetFieldFixedCads(uQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadXYZ = world.GetFieldFixedCads(uQuantityId)[0];
            }

            world.MakeElements();

            uint uValueId = 0;
            uint lValueId = 0;
            FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            //EdgeFieldDrawer edgeDrawer2;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                uValueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    uQuantityId, false, FieldShowType.Real);
                // Scalar
                lValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    lQuantityId, false, FieldShowType.Real);
                faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivativeType.Value, false, world);
                // Lagrange未定乗数のサーモグラフィ表示
                //faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivativeType.Value, false, world,
                //    lValueId, FieldDerivativeType.Value);
                edgeDrawer = new EdgeFieldDrawer(
                    uValueId, FieldDerivativeType.Value, false, true, world);
                //edgeDrawer2 = new EdgeFieldDrawer(
                //    uValueId, FieldDerivativeType.Value, true, true, world);
            }
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(faceDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                //fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
           }

            double t = 0;
            double dt = 0.05;
            for (int iTime = 0; iTime <= 50; iTime++)
            {
                double[] fixedValueXYZ = fixedCadXYZ.GetDoubleValues();
                fixedValueXYZ[0] = 0;
                fixedValueXYZ[1] = 0;
                fixedValueXYZ[2] = Math.Sin(t * 2.0 * Math.PI * 0.4);

                var FEM = new Elastic3DFEM(world);
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
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-10;
                FEM.Solve();
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(uValueId, FieldDerivativeType.Value, U);
                world.UpdateFieldValueValuesFromNodeValues(lValueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void Hyperelastic3DTDProblem(MainWindow mainWindow, bool isMooney)
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

            IList<uint> shellLIds1;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(5.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(5.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(5.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(5.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }

            {
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(shellLIds1, holes1, insideVIds1);
            }

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

            //double eLen = 0.1;
            double eLen = 0.2;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint uQuantityId;
            uint lQuantityId;
            {
                uint uDof = 3; // Vector3
                uint lDof = 1; // Scalar
                uint uFEOrder = 1;
                uint lFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder, FiniteElementType.ScalarLagrange);
                lQuantityId = world.AddQuantity(lDof, lFEOrder, FiniteElementType.ScalarLagrange);
            }
            world.TetIntegrationPointCount = TetrahedronIntegrationPointCount.Point5;

            if (isMooney)
            {
                // Mooney-Rivlin
                world.ClearMaterial();
                uint maId1 = 0;
                var ma1 = new MooneyRivlinHyperelasticMaterial();
                ma1.IsCompressible = false;
                //ma1.IsCompressible = true;
                //ma1.D1 = 1.0; // 非圧縮性のときは必要なし
                ma1.C1 = 200;
                ma1.C2 = 200;
                ma1.GravityX = 0;
                ma1.GravityY = 0;
                ma1.GravityZ = 0;
                ma1.MassDensity = 1.0;
                maId1 = world.AddMaterial(ma1);

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);

                uint[] lIds = { 1, 2, 3, 4, 5, 6 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId1);
                }
            }
            else
            {
                // Odgen
                world.ClearMaterial();
                uint maId1 = 0;
                var ma1 = new OgdenHyperelasticMaterial();
                double[] alphas = { 1.3, 5.0, -2.0 };
                double[] mus = { 6300e3, 1.2e3, -10e3 };
                //double[] alphas = { 2.0, -2.0 };
                //double[] mus = { 400, -400 };
                System.Diagnostics.Debug.Assert(alphas.Length == mus.Length);
                ma1.IsCompressible = false;
                //ma1.IsCompressible = true;
                //ma1.D1 = 1.0; // 非圧縮性のときは必要なし
                ma1.SetAlphaMu(alphas.Length, alphas, mus);
                ma1.GravityX = 0;
                ma1.GravityY = 0;
                ma1.GravityZ = 0;
                ma1.MassDensity = 1.0;
                maId1 = world.AddMaterial(ma1);

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);

                uint[] lIds = { 1, 2, 3, 4, 5, 6 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId1);
                }
            }

            uint[] zeroLIds = { 5 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(uQuantityId);
            foreach (uint lId in zeroLIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Vector3);
                zeroFixedCads.Add(fixedCad);
            }

            FieldFixedCad fixedCadXYZ;
            {
                // FixedDofIndex 0: X 1: Y 2: Z
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)3, CadElemType = CadElementType.Loop,
                        FixedDofIndexs = new List<uint> { 0, 1, 2 }, Values = new List<double> { 0.0, 0.0, 0.0 } }
                };
                var fixedCads = world.GetFieldFixedCads(uQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadXYZ = world.GetFieldFixedCads(uQuantityId)[0];
            }

            world.MakeElements();

            uint uValueId = 0;
            uint prevUValueId = 0;
            uint lValueId = 0;
            FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            //EdgeFieldDrawer edgeDrawer2;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                uValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, false, FieldShowType.Real);
                prevUValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, false, FieldShowType.Real);
                // Scalar
                lValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    lQuantityId, false, FieldShowType.Real);
                faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivativeType.Value, false, world);
                // Lagrange未定乗数のサーモグラフィ表示
                //faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivativeType.Value, false, world,
                //    lValueId, FieldDerivativeType.Value);
                edgeDrawer = new EdgeFieldDrawer(
                    uValueId, FieldDerivativeType.Value, false, true, world);
                //edgeDrawer2 = new EdgeFieldDrawer(
                //    uValueId, FieldDerivativeType.Value, true, true, world);
            }
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(faceDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                //fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 50; iTime++)
            {
                double[] fixedValueXYZ = fixedCadXYZ.GetDoubleValues();
                fixedValueXYZ[0] = 0;
                fixedValueXYZ[1] = 0;
                fixedValueXYZ[2] = Math.Sin(t * 2.0 * Math.PI * 0.4);

                var FEM = new Elastic3DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    uValueId, prevUValueId, lValueId);
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
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-10;
                FEM.Solve();
                //double[] U = FEM.U;

                FEM.UpdateFieldValuesTimeDomain();

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
