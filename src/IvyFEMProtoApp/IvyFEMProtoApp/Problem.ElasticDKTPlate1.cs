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
        public void DKTPlateProblem1(MainWindow mainWindow)
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
            uint d1QuantityId; // displacement1
            uint d2QuantityId; // displacement2
            uint r1QuantityId; // rotation1
            uint r2QuantityId; // rotation2
            {
                uint d1Dof = 2; // Vector2 (u,v)
                uint d2Dof = 1; // Scalar (w)
                uint r1Dof = 2; // Vector2 (θx,θy)
                uint r2Dof = 1; // Scalar (θz)
                uint d1FEOrder = 1;
                uint d2FEOrder = 1;
                uint r1FEOrder = 1;
                uint r2FEOrder = 1;
                d1QuantityId = world.AddQuantity(d1Dof, d1FEOrder, FiniteElementType.ScalarLagrange);
                d2QuantityId = world.AddQuantity(d2Dof, d2FEOrder, FiniteElementType.ScalarLagrange);
                r1QuantityId = world.AddQuantity(r1Dof, r1FEOrder, FiniteElementType.ScalarLagrange);
                r2QuantityId = world.AddQuantity(r2Dof, r2FEOrder, FiniteElementType.ScalarLagrange);
            }
            uint[] dQuantityIds = { d1QuantityId, d2QuantityId };

            {
                world.ClearMaterial();
                uint maId = 0;
                {
                    var ma = new PlateMaterial();
                    ma.Thickness = plateThickness;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    ma.ShearCorrectionFactor = 5.0 / 6.0; // 長方形断面
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            // 頂点の支点
            uint[] d1ZeroVIds = { 1, 3, 4 };
            var d1ZeroFixedCads = world.GetZeroFieldFixedCads(d1QuantityId);
            foreach (uint vId in d1ZeroVIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Vector2);
                d1ZeroFixedCads.Add(fixedCad);
            }
            uint[] d2ZeroVIds = { 1, 3, 4 };
            var d2ZeroFixedCads = world.GetZeroFieldFixedCads(d2QuantityId);
            foreach (uint vId in d2ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                d2ZeroFixedCads.Add(fixedCad);
            }
            uint[] r1ZeroVIds = { 1, 3, 4 };
            var r1ZeroFixedCads = world.GetZeroFieldFixedCads(r1QuantityId);
            foreach (uint vId in r1ZeroVIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Vector2);
                r1ZeroFixedCads.Add(fixedCad);
            }
            uint[] r2ZeroVIds = { 1, 3, 4 };
            var r2ZeroFixedCads = world.GetZeroFieldFixedCads(r2QuantityId);
            foreach (uint vId in r2ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                r2ZeroFixedCads.Add(fixedCad);
            }

            // displacement
            FieldFixedCad fixedCadD1;
            {
                // FixedDofIndex 0: u,v
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(d1QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD1 = world.GetFieldFixedCads(d1QuantityId)[0];
            }
            FieldFixedCad fixedCadD2;
            {
                // FixedDofIndex 0: w
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(d2QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD2 = world.GetFieldFixedCads(d2QuantityId)[0];
            }
            // rotation1
            {
                // FixedDofIndex 0: θx,θy
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(r1QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }
            // rotation2
            {
                // FixedDofIndex 0: θz
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(r2QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
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
                    d1QuantityId, false, FieldShowType.Real);
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
                double[] fixedValue01 = fixedCadD1.GetDoubleValues();
                double[] fixedValueD2 = fixedCadD2.GetDoubleValues();
                fixedValue01[0] = 0.0;
                fixedValue01[1] = 0.0;
                fixedValueD2[0] = -0.4 * plateA * Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic3DFEM(world);
                {
                    var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.IsOrderingToBandMatrix = true;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    FEM.Solver = solver;
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
                double[] Uuvt = FEM.U;

                // 変位(u,v,w)へ変換する
                int coCnt = (int)world.GetCoordCount(d1QuantityId);
                int d1Dof = (int)world.GetDof(d1QuantityId);
                int d2Dof = (int)world.GetDof(d2QuantityId);
                int r1Dof = (int)world.GetDof(r1QuantityId);
                int r2Dof = (int)world.GetDof(r2QuantityId);
                int d1NodeCnt = (int)world.GetNodeCount(d1QuantityId);
                int d2NodeCnt = (int)world.GetNodeCount(d2QuantityId);
                int r1NodeCnt = (int)world.GetNodeCount(r1QuantityId);
                int r2NodeCnt = (int)world.GetNodeCount(r2QuantityId);
                int d2Offset = d1NodeCnt * d1Dof;
                int r1Offset = d2Offset + d2NodeCnt * d2Dof;
                int r2Offset = r1Offset + r1NodeCnt * r1Dof;
                int dof = 3;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    int d1NodeId = world.Coord2Node(d1QuantityId, coId);
                    int d2NodeId = world.Coord2Node(d2QuantityId, coId);
                    int r1NodeId = world.Coord2Node(r1QuantityId, coId);
                    int r2NodeId = world.Coord2Node(r2QuantityId, coId);
                    double u = 0;
                    double v = 0;
                    double w = 0;
                    double tx = 0;
                    double ty = 0;
                    double tz = 0;
                    if (d1NodeId != -1)
                    {
                        u = Uuvt[d1NodeId * d1Dof];
                        v = Uuvt[d1NodeId * d1Dof + 1];
                    }
                    if (d2NodeId != -1)
                    {
                        w = Uuvt[d2NodeId + d2Offset];
                    }
                    if (r1NodeId != -1)
                    {
                        tx = Uuvt[r1NodeId * r1Dof + r1Offset];
                        ty = Uuvt[r1NodeId * r1Dof + 1 + r1Offset];
                    }
                    if (r2NodeId != -1)
                    {
                        tz = Uuvt[r2NodeId + r2Offset];
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

        public void DKTPlateTDProblem1(MainWindow mainWindow)
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
            uint d1QuantityId; // displacement1
            uint d2QuantityId; // displacement2
            uint r1QuantityId; // rotation1
            uint r2QuantityId; // rotation2
            {
                uint d1Dof = 2; // Vector2 (u,v)
                uint d2Dof = 1; // Scalar (w)
                uint r1Dof = 2; // Vector2 (θx,θy)
                uint r2Dof = 1; // Scalar (θz)
                uint d1FEOrder = 1;
                uint d2FEOrder = 1;
                uint r1FEOrder = 1;
                uint r2FEOrder = 1;
                d1QuantityId = world.AddQuantity(d1Dof, d1FEOrder, FiniteElementType.ScalarLagrange);
                d2QuantityId = world.AddQuantity(d2Dof, d2FEOrder, FiniteElementType.ScalarLagrange);
                r1QuantityId = world.AddQuantity(r1Dof, r1FEOrder, FiniteElementType.ScalarLagrange);
                r2QuantityId = world.AddQuantity(r2Dof, r2FEOrder, FiniteElementType.ScalarLagrange);
            }
            uint[] dQuantityIds = { d1QuantityId, d2QuantityId };

            {
                world.ClearMaterial();
                uint maId = 0;
                {
                    var ma = new PlateMaterial();
                    ma.Thickness = plateThickness;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    ma.ShearCorrectionFactor = 5.0 / 6.0; // 長方形断面
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            // 頂点の支点
            uint[] d1ZeroVIds = { 1, 3, 4 };
            var d1ZeroFixedCads = world.GetZeroFieldFixedCads(d1QuantityId);
            foreach (uint vId in d1ZeroVIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Vector2);
                d1ZeroFixedCads.Add(fixedCad);
            }
            uint[] d2ZeroVIds = { 1, 3, 4 };
            var d2ZeroFixedCads = world.GetZeroFieldFixedCads(d2QuantityId);
            foreach (uint vId in d2ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                d2ZeroFixedCads.Add(fixedCad);
            }
            uint[] r1ZeroVIds = { 1, 3, 4 };
            var r1ZeroFixedCads = world.GetZeroFieldFixedCads(r1QuantityId);
            foreach (uint vId in r1ZeroVIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Vector2);
                r1ZeroFixedCads.Add(fixedCad);
            }
            uint[] r2ZeroVIds = { 1, 3, 4 };
            var r2ZeroFixedCads = world.GetZeroFieldFixedCads(r2QuantityId);
            foreach (uint vId in r2ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                r2ZeroFixedCads.Add(fixedCad);
            }

            // displacement
            FieldFixedCad fixedCadD1;
            {
                // FixedDofIndex 0: u,v
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(d1QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD1 = world.GetFieldFixedCads(d1QuantityId)[0];
            }
            FieldFixedCad fixedCadD2;
            {
                // FixedDofIndex 0: w
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(d2QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD2 = world.GetFieldFixedCads(d2QuantityId)[0];
            }
            // rotation1
            {
                // FixedDofIndex 0: θx,θy
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(r1QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }
            // rotation2
            {
                // FixedDofIndex 0: θz
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(r2QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            world.MakeElements();

            uint d1ValueId = 0;
            uint d1PrevValueId = 0;
            uint d2ValueId = 0;
            uint d2PrevValueId = 0;
            uint r1ValueId = 0;
            uint r1PrevValueId = 0;
            uint r2ValueId = 0;
            uint r2PrevValueId = 0;
            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 表示用
                // Vector3
                valueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    d1QuantityId, false, FieldShowType.Real);

                // Newmarkβ
                d1ValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d1QuantityId, false, FieldShowType.Real);
                d1PrevValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d1QuantityId, false, FieldShowType.Real);
                d2ValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d2QuantityId, false, FieldShowType.Real);
                d2PrevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d2QuantityId, false, FieldShowType.Real);
                r1ValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    r1QuantityId, false, FieldShowType.Real);
                r1PrevValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    r1QuantityId, false, FieldShowType.Real);
                r2ValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    r2QuantityId, false, FieldShowType.Real);
                r2PrevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    r2QuantityId, false, FieldShowType.Real);

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
            IList<uint> valueIds = new List<uint> { d1ValueId, d2ValueId, r1ValueId, r2ValueId };
            IList<uint> prevValueIds = new List<uint> { d1PrevValueId, d2PrevValueId, r1PrevValueId, r2PrevValueId };

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] fixedValue01 = fixedCadD1.GetDoubleValues();
                double[] fixedValueD2 = fixedCadD2.GetDoubleValues();
                fixedValue01[0] = 0.0;
                fixedValue01[1] = 0.0;
                fixedValueD2[0] = -0.4 * plateA * Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic3DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueIds, prevValueIds);
                {
                    var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.IsOrderingToBandMatrix = true;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    FEM.Solver = solver;
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
                double[] Uuvt = FEM.U;

                // 変位(u,v,w)へ変換する
                int coCnt = (int)world.GetCoordCount(d1QuantityId);
                int d1Dof = (int)world.GetDof(d1QuantityId);
                int d2Dof = (int)world.GetDof(d2QuantityId);
                int r1Dof = (int)world.GetDof(r1QuantityId);
                int r2Dof = (int)world.GetDof(r2QuantityId);
                int d1NodeCnt = (int)world.GetNodeCount(d1QuantityId);
                int d2NodeCnt = (int)world.GetNodeCount(d2QuantityId);
                int r1NodeCnt = (int)world.GetNodeCount(r1QuantityId);
                int r2NodeCnt = (int)world.GetNodeCount(r2QuantityId);
                int d2Offset = d1NodeCnt * d1Dof;
                int r1Offset = d2Offset + d2NodeCnt * d2Dof;
                int r2Offset = r1Offset + r1NodeCnt * r1Dof;
                int dof = 3;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    int d1NodeId = world.Coord2Node(d1QuantityId, coId);
                    int d2NodeId = world.Coord2Node(d2QuantityId, coId);
                    int r1NodeId = world.Coord2Node(r1QuantityId, coId);
                    int r2NodeId = world.Coord2Node(r2QuantityId, coId);
                    double u = 0;
                    double v = 0;
                    double w = 0;
                    double tx = 0;
                    double ty = 0;
                    double tz = 0;
                    if (d1NodeId != -1)
                    {
                        u = Uuvt[d1NodeId * d1Dof];
                        v = Uuvt[d1NodeId * d1Dof + 1];
                    }
                    if (d2NodeId != -1)
                    {
                        w = Uuvt[d2NodeId + d2Offset];
                    }
                    if (r1NodeId != -1)
                    {
                        tx = Uuvt[r1NodeId * r1Dof + r1Offset];
                        ty = Uuvt[r1NodeId * r1Dof + 1 + r1Offset];
                    }
                    if (r2NodeId != -1)
                    {
                        tz = Uuvt[r2NodeId + r2Offset];
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
