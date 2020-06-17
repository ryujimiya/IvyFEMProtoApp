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
        public void MITCLinearPlateProblem1(MainWindow mainWindow)
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

            double plateA = 1.0;
            double plateB = plateA;
            double plateThickness = 0.2 * plateA;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(plateA, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(plateA, plateB, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, plateB, 0.0));
                uint lId1 = cad.AddPolygon(pts).AddLId;
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad3DDrawer(cad);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            double eLen = 0.1;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            var meshDrawer = new Mesher3DDrawer(mesher);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint dQuantityId; // displacement
            uint rQuantityId; // rotation
            {
                uint dDof = 3; // Vector3 (u,v,w)
                uint rDof = 3; // Vector2 (θx,θy,θz)
                uint dFEOrder = 1;
                uint rFEOrder = 1;
                dQuantityId = world.AddQuantity(dDof, dFEOrder, FiniteElementType.ScalarLagrange);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarLagrange);
            }
            uint[] dQuantityIds = { dQuantityId };

            {
                world.ClearMaterial();
                uint maId = 0;
                {
                    var ma = new MITCLinearPlateMaterial();
                    ma.Thickness = plateThickness;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    ma.ShearCorrectionFactor = 1.0;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            // 頂点の支点
            uint[] dZeroVIds = { 1, 3, 4 };
            var dZeroFixedCads = world.GetZeroFieldFixedCads(dQuantityId);
            foreach (uint vId in dZeroVIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Vector3);
                dZeroFixedCads.Add(fixedCad);
            }
            uint[] rZeroVIds = { 1, 3, 4 };
            var rZeroFixedCads = world.GetZeroFieldFixedCads(rQuantityId);
            foreach (uint vId in rZeroVIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Vector3);
                rZeroFixedCads.Add(fixedCad);
            }

            // displacement
            FieldFixedCad fixedCadD;
            {
                // FixedDofIndex 0: u,v,w
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1, 2 }, Values = new List<double> { 0.0, 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(dQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD = world.GetFieldFixedCads(dQuantityId)[0];
            }
            // rotation
            {
                // FixedDofIndex 0: θx,θy,θz
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1, 2 }, Values = new List<double> { 0.0, 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(rQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            world.MakeElements();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                valueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    dQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var edgeDrawer0 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer0);
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 0.05;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] fixedValueD = fixedCadD.GetDoubleValues();
                fixedValueD[0] = 0.0;
                fixedValueD[1] = 0.0;
                fixedValueD[2] = -0.4 * plateA * Math.Sin(t * 2.0 * Math.PI * 0.1);

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
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.DisplacementQuantityIds = dQuantityIds.ToList();
                FEM.Solve();
                double[] Uut = FEM.U;

                // 変位(u,v,w)へ変換する
                int coCnt = (int)world.GetCoordCount(dQuantityId);
                int dDof = (int)world.GetDof(dQuantityId);
                int rDof = (int)world.GetDof(rQuantityId);
                int dNodeCnt = (int)world.GetNodeCount(dQuantityId);
                int rNodeCnt = (int)world.GetNodeCount(rQuantityId);
                int rOffset = dNodeCnt * dDof;
                int r2Offset = rOffset + rNodeCnt * rDof;
                int dof = 3;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    int dNodeId = world.Coord2Node(dQuantityId, coId);
                    int rNodeId = world.Coord2Node(rQuantityId, coId);
                    double u = 0;
                    double v = 0;
                    double w = 0;
                    double tx = 0;
                    double ty = 0;
                    double tz = 0;
                    if (dNodeId != -1)
                    {
                        u = Uut[dNodeId * dDof];
                        v = Uut[dNodeId * dDof + 1];
                        w = Uut[dNodeId * dDof + 2];
                    }
                    if (rNodeId != -1)
                    {
                        tx = Uut[rNodeId * rDof + rOffset];
                        ty = Uut[rNodeId * rDof + 1 + rOffset];
                        tz = Uut[rNodeId * rDof + 2 + rOffset];
                    }
                    U[coId * dof + 0] = u;
                    U[coId * dof + 1] = v;
                    U[coId * dof + 2] = w;
                }
                // Note: from CoordValues
                world.UpdateFieldValueValuesFromCoordValues(valueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void MITCLinearPlateTDProblem1(MainWindow mainWindow)
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

            double plateA = 1.0;
            double plateB = plateA;
            double plateThickness = 0.2 * plateA;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(plateA, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(plateA, plateB, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, plateB, 0.0));
                uint lId1 = cad.AddPolygon(pts).AddLId;
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad3DDrawer(cad);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            double eLen = 0.1;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            var meshDrawer = new Mesher3DDrawer(mesher);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint dQuantityId; // displacement
            uint rQuantityId; // rotation
            {
                uint dDof = 3; // Vector3 (u,v,w)
                uint rDof = 3; // Vector2 (θx,θy,θz)
                uint dFEOrder = 1;
                uint rFEOrder = 1;
                dQuantityId = world.AddQuantity(dDof, dFEOrder, FiniteElementType.ScalarLagrange);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarLagrange);
            }
            uint[] dQuantityIds = { dQuantityId };

            {
                world.ClearMaterial();
                uint maId = 0;
                {
                    var ma = new MITCLinearPlateMaterial();
                    ma.Thickness = plateThickness;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    ma.ShearCorrectionFactor = 1.0;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            // 頂点の支点
            uint[] dZeroVIds = { 1, 3, 4 };
            var dZeroFixedCads = world.GetZeroFieldFixedCads(dQuantityId);
            foreach (uint vId in dZeroVIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Vector3);
                dZeroFixedCads.Add(fixedCad);
            }
            uint[] rZeroVIds = { 1, 3, 4 };
            var rZeroFixedCads = world.GetZeroFieldFixedCads(rQuantityId);
            foreach (uint vId in rZeroVIds)
            {
                // Vector3
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Vector3);
                rZeroFixedCads.Add(fixedCad);
            }

            // displacement
            FieldFixedCad fixedCadD;
            {
                // FixedDofIndex 0: u,v,w
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1, 2 }, Values = new List<double> { 0.0, 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(dQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD = world.GetFieldFixedCads(dQuantityId)[0];
            }
            // rotation
            {
                // FixedDofIndex 0: θx,θy,θz
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1, 2 }, Values = new List<double> { 0.0, 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(rQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector3
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector3, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            world.MakeElements();

            uint dValueId = 0;
            uint dPrevValueId = 0;
            uint rValueId = 0;
            uint rPrevValueId = 0;
            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 表示用
                // Vector3
                valueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    dQuantityId, false, FieldShowType.Real);

                // Newmarkβ
                dValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    dQuantityId, false, FieldShowType.Real);
                dPrevValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    dQuantityId, false, FieldShowType.Real);
                rValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    rQuantityId, false, FieldShowType.Real);
                rPrevValueId = world.AddFieldValue(FieldValueType.Vector3,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    rQuantityId, false, FieldShowType.Real);

                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var edgeDrawer0 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer0);
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }
            IList<uint> valueIds = new List<uint> { dValueId, rValueId };
            IList<uint> prevValueIds = new List<uint> { dPrevValueId, rPrevValueId };

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] fixedValueD = fixedCadD.GetDoubleValues();
                fixedValueD[0] = 0.0;
                fixedValueD[1] = 0.0;
                fixedValueD[2] = -0.4 * plateA * Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic3DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueIds, prevValueIds);
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
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.DisplacementQuantityIds = dQuantityIds.ToList();
                FEM.Solve();
                double[] Uut = FEM.U;

                // 変位(u,v,w)へ変換する
                int coCnt = (int)world.GetCoordCount(dQuantityId);
                int dDof = (int)world.GetDof(dQuantityId);
                int rDof = (int)world.GetDof(rQuantityId);
                int dNodeCnt = (int)world.GetNodeCount(dQuantityId);
                int rNodeCnt = (int)world.GetNodeCount(rQuantityId);
                int rOffset = dNodeCnt * dDof;
                int r2Offset = rOffset + rNodeCnt * rDof;
                int dof = 3;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    int dNodeId = world.Coord2Node(dQuantityId, coId);
                    int rNodeId = world.Coord2Node(rQuantityId, coId);
                    double u = 0;
                    double v = 0;
                    double w = 0;
                    double tx = 0;
                    double ty = 0;
                    double tz = 0;
                    if (dNodeId != -1)
                    {
                        u = Uut[dNodeId * dDof];
                        v = Uut[dNodeId * dDof + 1];
                        w = Uut[dNodeId * dDof + 2];
                    }
                    if (rNodeId != -1)
                    {
                        tx = Uut[rNodeId * rDof + rOffset];
                        ty = Uut[rNodeId * rDof + 1 + rOffset];
                        tz = Uut[rNodeId * rDof + 2 + rOffset];
                    }
                    U[coId * dof + 0] = u;
                    U[coId * dof + 1] = v;
                    U[coId * dof + 2] = w;
                }
                // Note: from CoordValues
                world.UpdateFieldValueValuesFromCoordValues(valueId, FieldDerivativeType.Value, U);

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
