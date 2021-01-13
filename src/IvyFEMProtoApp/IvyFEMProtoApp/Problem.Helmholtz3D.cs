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
        public void Helmholtz3DProblem(MainWindow mainWindow)
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
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }
            uint sourceVId;
            {
                // ソース
                uint addVId = cad.AddVertex(CadElementType.Solid, 0, new OpenTK.Vector3d(0.5, 0.5, 0.5)).AddVId;
                System.Diagnostics.Debug.Assert(addVId == 9);
                sourceVId = addVId;
            }
            {
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint> { sourceVId };
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

            double eLen = 0.05;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 1; // スカラー
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            {
                world.ClearMaterial();
                HelmholtzMaterial ma1 = new HelmholtzMaterial
                {
                    Velocity = 340,
                    F = (System.Numerics.Complex)0.0
                };
                uint maId1 = world.AddMaterial(ma1);

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);

                uint[] lIds = { 1, 2, 3, 4, 5, 6 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId1);
                }
            }

            // 吸収境界
            {
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                IList<uint> abcLIds = new List<uint>();
                uint[] lIds = { 1, 2, 3, 4, 5, 6 };
                foreach (uint lId in lIds)
                {
                    abcLIds.Add(lId);
                }
                PortCondition portCondition = new PortCondition(abcLIds, CadElementType.Loop, FieldValueType.ZScalar);
                portConditions.Add(portCondition);
            }

            {
                var fixedCadDatas = new[]
                {
                    new { CadId = sourceVId, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new System.Numerics.Complex[] { 1.0 } }
                };
                var fixedCads = world.GetFieldFixedCads(quantityId);
                foreach (var data in fixedCadDatas)
                {
                    // 複素数
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.ZScalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }
            world.MakeElements();

            uint valueId = 0;
            FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.ZReal);
                faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                //faceDrawer.IsMask = true;
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////
            // 断面の分布表示
            Cad3D cadA = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.5));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 0.5));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.5));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.5));
                uint lIdA1 = cadA.AddPolygon(pts).AddLId;
            }
            double eLenA = eLen;
            Mesher3D mesherA = new Mesher3D(cadA, eLenA);

            FEWorld worldA = new FEWorld();
            worldA.Mesh = mesherA;
            uint quantityIdA;
            {
                uint dofA = 1; // スカラー
                uint feOrderA = 1;
                quantityIdA = worldA.AddQuantity(dofA, feOrderA, FiniteElementType.ScalarLagrange);
            }
            {
                // dummy
                HelmholtzMaterial maA1 = new HelmholtzMaterial
                {
                    Velocity = 0.0,
                    F = (System.Numerics.Complex)0.0
                };
                uint maIdA1 = worldA.AddMaterial(maA1);

                uint lIdA1 = 1;
                worldA.SetCadLoopMaterial(lIdA1, maIdA1);
            }

            worldA.MakeElements();

            uint valueIdA = 0;
            FaceFieldDrawer faceDrawerA;
            EdgeFieldDrawer edgeDrawerA;
            var fieldDrawerArrayA = mainWindow.FieldDrawerArrayA;
            {
                worldA.ClearFieldValue();
                // スカラー
                valueIdA = worldA.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityIdA, false, FieldShowType.ZReal);
                faceDrawerA = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, worldA,
                    valueIdA, FieldDerivativeType.Value);
                edgeDrawerA = new EdgeFieldDrawer(
                    valueIdA, FieldDerivativeType.Value, true, false, worldA);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////

            double freq = 1200;
            {
                var FEM = new Helmholtz3DFEM(world);
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
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.COCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    //FEM.Solver = solver;
                }
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, U);

                {
                    mainWindow.IsFieldDraw = true;
                    fieldDrawerArray.Clear();
                    fieldDrawerArray.Add(faceDrawer);
                    fieldDrawerArray.Add(edgeDrawer);
                    mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                    mainWindow.GLControl_ResizeProc();
                }

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                ////////////////////////////////////////////////////////////////////////////////////////////////
                // 断面の分布表示
                WPFUtils.DoEvents(1000 * 5);
                {
                    fieldDrawerArray.Clear();
                    fieldDrawerArray.Add(edgeDrawer);
                    mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                    mainWindow.GLControl_ResizeProc();
                }
                {
                    mainWindow.IsFieldDraw = true;
                    fieldDrawerArrayA.Clear();
                    fieldDrawerArrayA.Add(faceDrawerA);
                    fieldDrawerArrayA.Add(edgeDrawerA);
                }
                // 平面の分布を取得する
                System.Numerics.Complex[] UA;
                {
                    uint coCntA = worldA.GetCoordCount(quantityIdA);
                    uint dofA = worldA.GetDof(quantityIdA);
                    UA = new System.Numerics.Complex[coCntA * dofA];
                    for (int coIdA = 0; coIdA < coCntA; coIdA++)
                    {
                        double[] coA = worldA.GetCoord(quantityIdA, coIdA);
                        System.Numerics.Complex[] value = world.GetComplexPointValueFromNodeValues(quantityId, coA, U);
                        if (value == null)
                        {
                            continue;
                        }
                        System.Diagnostics.Debug.Assert(value.Length == dofA);
                        for (int iDofA = 0; iDofA < dofA; iDofA++)
                        {
                            UA[coIdA * dofA + iDofA] = value[iDofA];
                        }
                    }
                }
                {
                    worldA.UpdateFieldValueValuesFromCoordValues(valueIdA, FieldDerivativeType.Value, UA);

                    fieldDrawerArray.Update(world);
                    fieldDrawerArrayA.Update(worldA);
                    mainWindow.GLControl.Invalidate();
                    mainWindow.GLControl.Update();
                    WPFUtils.DoEvents();
                }
                ////////////////////////////////////////////////////////////////////////////////////////////////
            }
        }
    }
}
