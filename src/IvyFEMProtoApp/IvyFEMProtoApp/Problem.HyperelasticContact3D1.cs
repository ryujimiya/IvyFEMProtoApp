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
        public void HyperelasticContact3DTD1Problem(MainWindow mainWindow, bool isMooney)
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
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 5.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 5.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 5.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 5.0));
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
            uint cQuantityId; // constraint
            {
                uint uDof = 3; // Vector3
                uint lDof = 1; // Scalar
                uint cDof = 1;
                uint uFEOrder = 1;
                uint lFEOrder = 1;
                uint cFEOrder = 1;
                System.Diagnostics.Debug.Assert(uFEOrder == cFEOrder);
                uQuantityId = world.AddQuantity(uDof, uFEOrder, FiniteElementType.ScalarLagrange);
                lQuantityId = world.AddQuantity(lDof, lFEOrder, FiniteElementType.ScalarLagrange);
                cQuantityId = world.AddQuantity(cDof, cFEOrder, FiniteElementType.ScalarLagrange);
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
                    ma1.C1 = 400;
                    ma1.C2 = 400;
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
                    //double[] alphas = { 1.3, 5.0, -2.0 };
                    //double[] mus = { 6300e3, 1.2e3, -10e3 };
                    double[] alphas = { 2.0, -2.0 };
                    double[] mus = { 800, -800 };
                    System.Diagnostics.Debug.Assert(alphas.Length == mus.Length);
                    ma1.IsCompressible = false;
                    //ma1.IsCompressible = true;
                    //ma1.D1 = 1.0; // 非圧縮性のときは必要なし
                    ma1.SetAlphaMu(alphas.Length, alphas, mus);
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.MassDensity = 1.0;
                    maId1 = world.AddMaterial(ma1);
                }

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);
            }

            FieldFixedCad fixedCadXYZ;
            {
                // FixedDofIndex 0: X 1: Y 2: Z
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)6, CadElemType = CadElementType.Loop,
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

            // MPC
            PlaneConstraint planeConstraint;
            {
                var cadIdTypes = new List<KeyValuePair<uint, CadElementType>>();
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(1, CadElementType.Loop));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(2, CadElementType.Loop));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(3, CadElementType.Loop));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(4, CadElementType.Loop));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(5, CadElementType.Loop));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(6, CadElementType.Loop));
                var pt = new OpenTK.Vector3d(0.0, 0.0, -0.1);
                double theta = 15.0 * Math.PI / 180.0;
                var normal = new OpenTK.Vector3d(Math.Cos(theta), 0.0, Math.Sin(theta));
                planeConstraint = new PlaneConstraint(pt, normal, EqualityType.GreaterEq);
                planeConstraint.BoundingBoxLen = 4.0;
                MultipointConstraint mpc = new MultipointConstraint(cadIdTypes, planeConstraint);
                world.AddMultipointConstraint(cQuantityId, mpc);
            }

            world.MakeElements();

            uint uValueId = 0;
            uint prevUValueId = 0;
            uint lValueId = 0;
            ConstraintDrawer constraintDrawer;
            FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            //EdgeFieldDrawer edgeDrawer2;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                uValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, false, FieldShowType.Real);
                prevUValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, false, FieldShowType.Real);
                // Scalar
                lValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    lQuantityId, false, FieldShowType.Real);
                constraintDrawer = new ConstraintDrawer(planeConstraint);
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
                fieldDrawerArray.Add(constraintDrawer);
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
                double[] fixedValueXY = fixedCadXYZ.GetDoubleValues();
                fixedValueXY[0] = -Math.Sin(t * 2.0 * Math.PI * 0.2);
                fixedValueXY[1] = 0;
                fixedValueXY[2] = 0;

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
