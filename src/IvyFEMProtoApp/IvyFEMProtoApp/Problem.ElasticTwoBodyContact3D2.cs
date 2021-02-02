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
        public void ElasticTwoBodyContact3DTDProblem2(MainWindow mainWindow, bool isStVenant)
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
            IList<uint> shellLIds2;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(-1.0, -1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, -1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(-1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(-1.0, -1.0, 2.0));
                pts.Add(new OpenTK.Vector3d(1.0, -1.0, 2.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 2.0));
                pts.Add(new OpenTK.Vector3d(-1.0, 1.0, 2.0));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(-3.0, -3.0, -3.0));
                pts.Add(new OpenTK.Vector3d(3.0, -3.0, -3.0));
                pts.Add(new OpenTK.Vector3d(3.0, 3.0, -3.0));
                pts.Add(new OpenTK.Vector3d(-3.0, 3.0, -3.0));
                pts.Add(new OpenTK.Vector3d(-3.0, -3.0, -2.0));
                pts.Add(new OpenTK.Vector3d(3.0, -3.0, -2.0));
                pts.Add(new OpenTK.Vector3d(3.0, 3.0, -2.0));
                pts.Add(new OpenTK.Vector3d(-3.0, 3.0, -2.0));
                var res = cad.AddCube(pts);
                shellLIds2 = res.AddLIds;
            }

            {
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(shellLIds1, holes1, insideVIds1);

                IList<OpenTK.Vector3d> holes2 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds2 = new List<uint>();
                uint sId2 = cad.AddSolid(shellLIds2, holes2, insideVIds2);
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

            //double eLen = 0.2;
            //double eLen = 0.4;
            //double eLen = 0.8;
            double eLen = 0.8;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint uQuantityId;
            uint cQuantityId;
            {
                uint uDof = 3; // Vector3
                uint uFEOrder = 1;
                uint cDof = 3; // Vector3 for mortar
                uint cFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder, FiniteElementType.ScalarLagrange);
                cQuantityId = world.AddQuantity(cDof, cFEOrder, FiniteElementType.ScalarLagrange);
            }

            {
                world.ClearMaterial();
                IList<uint> maIds = new List<uint>();
                if (isStVenant)
                {
                    {
                        var ma = new StVenantHyperelasticMaterial();
                        ma.Young = 2000.0;
                        ma.Poisson = 0.3;
                        ma.GravityX = 0;
                        ma.GravityY = 0;
                        ma.GravityZ = -10.0;
                        ma.MassDensity = 1.0;
                        uint maId = world.AddMaterial(ma);
                        maIds.Add(maId);
                    }
                    {
                        var ma = new StVenantHyperelasticMaterial();
                        ma.Young = 2000.0;
                        ma.Poisson = 0.3;
                        ma.GravityX = 0;
                        ma.GravityY = 0;
                        ma.GravityZ = 0;
                        ma.MassDensity = 1.0;
                        uint maId = world.AddMaterial(ma);
                        maIds.Add(maId);
                    }
                }
                else
                {
                    {
                        var ma = new LinearElasticMaterial();
                        ma.Young = 500.0;
                        ma.Poisson = 0.3;
                        ma.GravityX = 0;
                        ma.GravityY = 0;
                        ma.GravityZ = -10.0;
                        ma.MassDensity = 1.0;
                        uint maId = world.AddMaterial(ma);
                        maIds.Add(maId);
                    }
                    {
                        var ma = new LinearElasticMaterial();
                        ma.Young = 500.0;
                        ma.Poisson = 0.3;
                        ma.GravityX = 0;
                        ma.GravityY = 0;
                        ma.GravityZ = 0;
                        ma.MassDensity = 1.0;
                        uint maId = world.AddMaterial(ma);
                        maIds.Add(maId);
                    }
                }

                uint[] sIds = { 1, 2 };
                for (int i = 0; i < sIds.Length; i++)
                {
                    world.SetCadSolidMaterial(sIds[i], maIds[i]);
                }
            }

            uint[] zeroLIds = { 7 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(uQuantityId);
            foreach (uint lId in zeroLIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Vector3);
                zeroFixedCads.Add(fixedCad);
            }

            uint slaveLId = 1;
            uint masterLId = 12;
            IList<uint> contactSlaveLIds = world.GetContactSlaveCadIds(cQuantityId);
            contactSlaveLIds.Add(slaveLId);
            IList<uint> contactMasterLIds = world.GetContactMasterCadIds(cQuantityId);
            contactMasterLIds.Add(masterLId);

            world.MakeElements();

            uint valueId = 0;
            uint prevValueId = 0;
            FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                valueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, false, FieldShowType.Real);
                faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
            }
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(faceDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                var FEM = new Elastic3DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueId, prevValueId);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.IsOrderingToBandMatrix = true;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    //FEM.ConvRatioToleranceForNonlinearIter = 1.0e-10;
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
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
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
