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
        public void CadObjLoadFromFile(string cadObjFileName, CadObject2D cad2D, CadEditWindow window)
        {
            using (Serializer arch = new Serializer(cadObjFileName, true))
            {
                cad2D.Serialize(arch);
            }
            window.CadDesign.RefreshDrawerAry();
            window.glControl.Invalidate();
            window.glControl.Update();
        }

        public void CadObjSaveToFile(string cadObjFileName, CadObject2D cad2D)
        {
            using (Serializer arch = new Serializer(cadObjFileName, false))
            {
                cad2D.Serialize(arch);
            }
        }

        public void MeshObjLoadFromFile(string meshObjFileName, Mesher2D mesher2D)
        {
            using (Serializer arch = new Serializer(meshObjFileName, true))
            {
                mesher2D.Serialize(arch);
            }
        }

        public void MeshObjSaveToFile(string meshObjFileName, Mesher2D mesher2D)
        {
            using (Serializer arch = new Serializer(meshObjFileName, false))
            {
                mesher2D.Serialize(arch);
            }
        }

        public void MeshObjFileTest(string meshObjFileName, CadObject2D cad2D)
        {
            double eLen = 2.0;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);
            MeshObjSaveToFile(meshObjFileName, mesher2D);
            MeshObjLoadFromFile(meshObjFileName, mesher2D);
            var meshWindow = new MeshWindow();
            meshWindow.Init(mesher2D, 100, 100);
            meshWindow.ShowDialog();
        }

        public void CalcSampleProblem(
            CadObject2D cad2D, Camera camera, uint zeroEId, uint moveEId, CadEditWindow window)
        {
            //double eLen = 0.1;
            double eLen = 0.02 * camera.HalfViewHeight;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 2; // Vector2
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point3;

            {
                world.ClearMaterial();
                uint maId = 0;
                {
                    var ma = new LinearElasticMaterial();
                    ma.SetYoungPoisson(10.0, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1.0;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            uint[] zeroEIds = { zeroEId };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                zeroFixedCads.Add(fixedCad);
            }

            FieldFixedCad fixedCadXY;
            {
                // FixedDofIndex 0: X 1: Y
                var fixedCadDatas = new[]
                {
                    new { CadId = moveEId, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(quantityId);
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadXY = world.GetFieldFixedCads(quantityId)[0];
            }

            world.MakeElements();

            uint valueId = 0;
            uint eqStressValueId = 0;
            uint stressValueId = 0;
            var fieldDrawerArray = window.CalcDraw.DrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                window.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                {
                    IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                    fieldDrawerArray.Add(faceDrawer);
                }
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                //camera.Fit(fieldDrawerArray.GetBoundingBox(camera.RotMatrix33()));
                window.CalcDraw.PanelResize();
            }

            double t = 0;
            double dt = 0.05;
            double amp = camera.HalfViewHeight * 0.2;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] fixedValueXY = fixedCadXY.GetDoubleValues();
                fixedValueXY[0] = 0;
                fixedValueXY[1] = amp * Math.Sin(t * 2.0 * Math.PI * 0.1);

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
                window.glControl.Invalidate();
                window.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
