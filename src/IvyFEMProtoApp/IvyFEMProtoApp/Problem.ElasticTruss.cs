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
        public void TrussProblem(MainWindow mainWindow)
        {
            double trussLen = 1.0;
            double b = Math.Sqrt(0.1);
            CadObject2D cad = new CadObject2D();
            {
                uint lId0 = 0;
                uint vId1 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(0.0, trussLen)).AddVId;
                uint vId2 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(0.0, 0.0)).AddVId;
                uint vId3 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(trussLen, 0.0)).AddVId;
                uint eId1 = cad.ConnectVertexLine(vId1, vId2).AddEId;
                uint eId2 = cad.ConnectVertexLine(vId2, vId3).AddEId;
                uint eId3 = cad.ConnectVertexLine(vId3, vId1).AddEId;
            }

            {
                double[] nullLoopColor = { 1.0, 1.0, 1.0 };
                uint[] lIds = cad.GetElementIds(CadElementType.Loop).ToArray();
                foreach (uint lId in lIds)
                {
                    cad.SetLoopColor(lId, nullLoopColor);
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

            Mesher2D mesher = new Mesher2D();
            mesher.SetMeshingModeZero(); // 最大メッシュ(分割しない)
            mesher.Meshing(cad);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            IDrawer meshDrawer = new Mesher2DDrawer(mesher);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 2; // Vector2
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            {
                world.ClearMaterial();
                uint nullMaId;
                uint trussMaId;
                {
                    var ma = new NullMaterial();
                    nullMaId = world.AddMaterial(ma);
                }
                {
                    var ma = new TrussMaterial();
                    ma.Area = b * b;
                    ma.MassDensity = 1.0;
                    ma.Young = 70.0e+9;
                    trussMaId = world.AddMaterial(ma);
                }

                uint[] lIds = cad.GetElementIds(CadElementType.Loop).ToArray();
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, nullMaId);
                }
                uint[] eIds = cad.GetElementIds(CadElementType.Edge).ToArray();
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, trussMaId);
                }
            }

            FieldFixedCad fixedCadD;
            {
                // FixedDofIndex 0: X 1: Y
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)3, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                    // 固定部
                    new { CadId = (uint)1, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(quantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD = world.GetFieldFixedCads(quantityId)[0];
            }

            world.MakeElements();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                edgeDrawer.LineWidth = 4;
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
                fixedValueD[0] = -0.5e-1 * Math.Sin(t * 2.0 * Math.PI * 0.1);
                fixedValueD[1] = -1.0e-1 * Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic2DFEM(world);
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

        public void TrussTDProblem(MainWindow mainWindow)
        {
            double trussLen = 1.0;
            double b = Math.Sqrt(0.1);
            CadObject2D cad = new CadObject2D();
            {
                uint lId0 = 0;
                uint vId1 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(0.0, trussLen)).AddVId;
                uint vId2 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(0.0, 0.0)).AddVId;
                uint vId3 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(trussLen, 0.0)).AddVId;
                uint eId1 = cad.ConnectVertexLine(vId1, vId2).AddEId;
                uint eId2 = cad.ConnectVertexLine(vId2, vId3).AddEId;
                uint eId3 = cad.ConnectVertexLine(vId3, vId1).AddEId;
            }

            {
                double[] nullLoopColor = { 1.0, 1.0, 1.0 };
                uint[] lIds = cad.GetElementIds(CadElementType.Loop).ToArray();
                foreach (uint lId in lIds)
                {
                    cad.SetLoopColor(lId, nullLoopColor);
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

            Mesher2D mesher = new Mesher2D();
            mesher.SetMeshingModeZero(); // 最大メッシュ(分割しない)
            mesher.Meshing(cad);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            IDrawer meshDrawer = new Mesher2DDrawer(mesher);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 2; // Vector2
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            {
                world.ClearMaterial();
                uint nullMaId;
                uint trussMaId;
                {
                    var ma = new NullMaterial();
                    nullMaId = world.AddMaterial(ma);
                }
                {
                    var ma = new TrussMaterial();
                    ma.Area = b * b;
                    ma.MassDensity = 1.0;
                    ma.Young = 70.0e+9;
                    trussMaId = world.AddMaterial(ma);
                }

                uint[] lIds = cad.GetElementIds(CadElementType.Loop).ToArray();
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, nullMaId);
                }
                uint[] eIds = cad.GetElementIds(CadElementType.Edge).ToArray();
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, trussMaId);
                }
            }

            FieldFixedCad fixedCadD;
            {
                // FixedDofIndex 0: X 1: Y
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)3, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                    // 固定部
                    new { CadId = (uint)1, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(quantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD = world.GetFieldFixedCads(quantityId)[0];
            }

            world.MakeElements();

            uint valueId = 0;
            uint prevValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 表示用/Newmark β兼用
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                edgeDrawer.LineWidth = 4;
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 0.5;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] fixedValueD = fixedCadD.GetDoubleValues();
                fixedValueD[0] = -0.5e-1 * Math.Sin(t * 2.0 * Math.PI * 0.01);
                fixedValueD[1] = -1.0e-1 * Math.Sin(t * 2.0 * Math.PI * 0.01);

                var FEM = new Elastic2DTDFEM(world, dt,
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
