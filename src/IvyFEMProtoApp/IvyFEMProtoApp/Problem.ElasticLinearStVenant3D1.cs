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
        public void ElasticLinearStVenant3DProblem1(MainWindow mainWindow, bool isStVenant)
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
            uint quantityId;
            {
                uint dof = 3; // Vector3
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }
            world.TetIntegrationPointCount = TetrahedronIntegrationPointCount.Point4;

            {
                world.ClearMaterial();
                uint maId1 = 0;
                if (isStVenant)
                {
                    var ma1 = new StVenantHyperelasticMaterial();
                    ma1.Young = 10.0;
                    ma1.Poisson = 0.3;
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.GravityZ = 0;
                    ma1.MassDensity = 1.0;
                    maId1 = world.AddMaterial(ma1);
                }
                else
                {
                    var ma1 = new LinearElasticMaterial();
                    ma1.Young = 10.0;
                    ma1.Poisson = 0.3;
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
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
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
                var fixedCads = world.GetFieldFixedCads(quantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadXYZ = world.GetFieldFixedCads(quantityId)[0];
            }

            world.MakeElements();

            uint valueId = 0;
            FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            //EdgeFieldDrawer edgeDrawer2;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                valueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                //edgeDrawer2 = new EdgeFieldDrawer(
                //    valueId, FieldDerivativeType.Value, true, true, world);
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
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void ElasticLinearStVenant3DTDProblem1(MainWindow mainWindow, bool isStVenant)
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
            uint quantityId;
            {
                uint dof = 3; // Vector3
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }
            world.TetIntegrationPointCount = TetrahedronIntegrationPointCount.Point4;

            {
                world.ClearMaterial();
                uint maId1 = 0;
                if (isStVenant)
                {
                    var ma1 = new StVenantHyperelasticMaterial();
                    ma1.Young = 50.0;
                    ma1.Poisson = 0.3;
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.GravityZ = 0;
                    ma1.MassDensity = 1.0;
                    maId1 = world.AddMaterial(ma1);
                }
                else
                {
                    var ma1 = new LinearElasticMaterial();
                    ma1.Young = 50.0;
                    ma1.Poisson = 0.3;
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
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
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
                var fixedCads = world.GetFieldFixedCads(quantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadXYZ = world.GetFieldFixedCads(quantityId)[0];
            }

            world.MakeElements();

            uint valueId = 0;
            uint prevValueId = 0;
            FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            //EdgeFieldDrawer edgeDrawer2;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                valueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);
                faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                //edgeDrawer2 = new EdgeFieldDrawer(
                //    valueId, FieldDerivativeType.Value, true, true, world);
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
                    valueId, prevValueId);
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
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.Solve();
                double[] U = FEM.U;

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
