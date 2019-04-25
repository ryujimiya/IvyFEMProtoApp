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
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                var res = cad2D.AddPolygon(pts);
                uint addLId = res.AddLId;
                System.Diagnostics.Debug.Assert(addLId == 1);

                // ソース
                var res2 = cad2D.AddCircle(new OpenTK.Vector2d(0.5, 0.5), 0.1, addLId);
                System.Diagnostics.Debug.Assert(res2.AddLId == 2);

                // 導体
                IList<OpenTK.Vector2d> pts2 = new List<OpenTK.Vector2d>();
                pts2.Add(new OpenTK.Vector2d(0.2, 0.2));
                pts2.Add(new OpenTK.Vector2d(0.3, 0.2));
                pts2.Add(new OpenTK.Vector2d(0.3, 0.3));
                pts2.Add(new OpenTK.Vector2d(0.2, 0.3));
                var res3 = cad2D.AddPolygon(pts2, addLId);
                System.Diagnostics.Debug.Assert(res3.AddLId == 3);
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
            WPFUtils.DoEvents();

            double eLen = 0.05;
            //Mesher2D mesher2D = new Mesher2D(cad2D, eLen);
            Mesher2D mesher2D = new Mesher2D();
            mesher2D.SetMeshingModeElemLength(eLen);
            uint hollowLId = 3;
            IList<uint> lIds = cad2D.GetElementIds(CadElementType.Loop);
            foreach (uint lId in lIds)
            {
                if (lId == hollowLId)
                {
                    continue;
                }
                mesher2D.AddCutMeshLCadId(lId);
            }
            mesher2D.Meshing(cad2D);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 1; // スカラー
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder);
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
            zeroFixedCads.Clear();
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
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
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
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
