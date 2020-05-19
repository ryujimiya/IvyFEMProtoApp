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
        public void PoissonProblem(MainWindow mainWindow)
        {
            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                uint lId1 = cad.AddPolygon(pts).AddLId;
                System.Diagnostics.Debug.Assert(lId1 == 1);

                // ソース
                uint lId2 = cad.AddCircle(new OpenTK.Vector2d(0.5, 0.5), 0.1, lId1).AddLId;
                System.Diagnostics.Debug.Assert(lId2 == 2);

                // 導体
                IList<OpenTK.Vector2d> pts2 = new List<OpenTK.Vector2d>();
                pts2.Add(new OpenTK.Vector2d(0.2, 0.2));
                pts2.Add(new OpenTK.Vector2d(0.3, 0.2));
                pts2.Add(new OpenTK.Vector2d(0.3, 0.3));
                pts2.Add(new OpenTK.Vector2d(0.2, 0.3));
                uint lId3 = cad.AddPolygon(pts2, lId1).AddLId;
                System.Diagnostics.Debug.Assert(lId3 == 3);
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad2DDrawer(cad);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            double eLen = 0.05;
            //Mesher2D mesher = new Mesher2D(cad, eLen);
            Mesher2D mesher = new Mesher2D();
            mesher.SetMeshingModeElemLength();
            uint hollowLId = 3;
            IList<uint> lIds = cad.GetElementIds(CadElementType.Loop);
            foreach (uint lId in lIds)
            {
                if (lId == hollowLId)
                {
                    continue;
                }
                mesher.AddMeshingLoopCadId(lId, eLen);
            }
            mesher.MakeMesh(cad);

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
                PoissonMaterial ma1 = new PoissonMaterial
                {
                    Alpha = 1.0,
                    F = 0.0
                };
                PoissonMaterial ma2 = new PoissonMaterial
                {
                    Alpha = 1.0,
                    F = 100.0
                };
                uint maId1 = world.AddMaterial(ma1);
                uint maId2 = world.AddMaterial(ma2);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId1);

                uint lId2 = 2;
                world.SetCadLoopMaterial(lId2, maId2);

                uint lId3 = 3;
                world.SetCadLoopMaterial(lId3, maId1);
            }

            uint[] zeroEIds = { 1, 2, 3, 4, 9, 10, 11, 12 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                zeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // スカラー
                valueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            {
                var FEM = new Poisson2DFEM(world);
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
            }
        }
    }
}
