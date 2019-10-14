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
        public void HelmholtzProblem(MainWindow mainWindow)
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
                uint addVId = cad2D.AddVertex(CadElementType.Loop, addLId, new OpenTK.Vector2d(0.5, 0.5)).AddVId;
                System.Diagnostics.Debug.Assert(addVId == 5);
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            double eLen = 0.05;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 1; // スカラー
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            {
                world.ClearMaterial();
                HelmholtzMaterial ma = new HelmholtzMaterial
                {
                    Velocity = 340,
                    F = (System.Numerics.Complex)0.0
                };
                uint maId = world.AddMaterial(ma);

                uint[] eIds1 = { 1, 2, 3, 4 };
                foreach (uint eId in eIds1)
                {
                    world.SetCadEdgeMaterial(eId, maId);
                }
                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId);
            }

            // 吸収境界
            {
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                IList<uint> abcEIds = new List<uint>();
                uint[] eIds = { 1, 2, 3, 4 };
                foreach (uint eId in eIds)
                {
                    abcEIds.Add(eId);
                }
                PortCondition portCondition = new PortCondition(abcEIds, FieldValueType.ZScalar);
                portConditions.Add(portCondition);
            }

            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)5, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new System.Numerics.Complex[] { 1.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(quantityId);
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
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.ZReal);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            double freq = 1200;
            {
                var FEM = new Helmholtz2DFEM(world);
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
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.COCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
