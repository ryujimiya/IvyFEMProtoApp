using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;

namespace IvyFEMProtoApp
{
    class ProblemCadEdit
    {
        public void CadLoadFromFile(CadObject2D cad2D, string cadObjFileName, CadEditWindow window)
        {
            using (Serializer arch = new Serializer(cadObjFileName, true))
            {
                cad2D.Serialize(arch);
            }
            window.CadDesign.CadPanelResize();
            window.glControl.Invalidate();
            window.glControl.Update();
        }

        public void CadSaveToFile(CadObject2D cad2D, string cadObjFileName)
        {
            using (Serializer arch = new Serializer(cadObjFileName, false))
            {
                cad2D.Serialize(arch);
            }
        }

        public void ElasticProblem(CadObject2D cad2D, Camera camera, CadEditWindow window)
        {
            double moveDelta = 20.0; // 100x100の領域に図形を描いているので分割幅や移動幅を大きくしなければならない
            //double eLen = 0.1;
            double eLen = 0.1 * moveDelta;
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

            uint[] zeroEIds = { 4 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                uint dof = 2; // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2, dof);
                zeroFixedCads.Add(fixedCad);
            }

            FieldFixedCad fixedCadXY;
            {
                // FixedDofIndex 0: X 1: Y
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new double[] { 0.0, 0.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(quantityId);
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    uint dof = 2; // Vector2
                    var fixedCad = new FieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, dof, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadXY = world.GetFieldFixedCads(quantityId)[0];
            }

            world.MakeElements();

            uint valueId = 0;
            uint eqStressValueId = 0;
            uint stressValueId = 0;
            var fieldDrawerArray = window.CalcDraw.FieldDrawerArray;
            {
                uint dof = 2; // Vector2
                world.ClearFieldValue();
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    quantityId, dof, false, FieldShowType.Real);
                window.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                {
                    IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                    fieldDrawerArray.Add(faceDrawer);
                }
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(valueId, FieldDerivativeType.Value, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                //camera.Fit(fieldDrawerArray.GetBoundingBox(camera.RotMatrix33()));
                window.CalcDraw.CadPanelResize();
            }

            double t = 0;
            double dt = 0.05;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                fixedCadXY.DoubleValues[0] = 0;
                fixedCadXY.DoubleValues[1] = moveDelta * Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic2DFEM(world);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
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
