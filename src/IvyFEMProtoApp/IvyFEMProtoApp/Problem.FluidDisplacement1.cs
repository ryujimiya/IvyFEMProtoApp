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
        public void FluidDisplacementTDProblem1(MainWindow mainWindow)
        {
            Cad2D cad = new Cad2D();
            {
                uint lId1 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 2.0));
                    pts.Add(new OpenTK.Vector2d(0.0, 2.0));
                    lId1 = cad.AddPolygon(pts).AddLId;
                    System.Diagnostics.Debug.Assert(lId1 == 1);
                }
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

            //double eLen = 0.08;
            //double eLen = 0.20;
            double eLen = 0.20;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint vQuantityId;
            uint pQuantityId;
            uint dQuantityId;
            /*
            uint cQuantityId1;
            uint cQuantityId2;
            uint cQuantityId3;
            */
            {
                uint vDof = 2; // v // 2次元ベクトル
                uint pDof = 1; // p // スカラー
                /*
                uint cDof1 = 1; // MPC
                uint cDof2 = 1; // MPC
                uint cDof3= 1; // MPC
                */
                uint vFEOrder = 1;
                uint pFEOrder = 1;
                uint qFEOrder = 1;
                /*
                uint cFEOrder1 = 1;
                uint cFEOrder2 = 1;
                uint cFEOrder3 = 1;
                */
                vQuantityId = world.AddQuantity(vDof, vFEOrder, FiniteElementType.ScalarLagrange);
                pQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarLagrange);
                /*
                cQuantityId1 = world.AddQuantity(cDof1, cFEOrder1, FiniteElementType.ScalarLagrange);
                cQuantityId2 = world.AddQuantity(cDof2, cFEOrder2, FiniteElementType.ScalarLagrange);
                cQuantityId3 = world.AddQuantity(cDof3, cFEOrder3, FiniteElementType.ScalarLagrange);
                */
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point7;

            {
                world.ClearMaterial();

                NewtonFluidMaterial ma1 = null;
                /*
                // water
                ma1 = new NewtonFluidMaterial
                {
                    MassDensity = 1.0e+3,
                    GravityX = 0.0,
                    GravityY = -1.0e+1,
                    Mu = 1.0e-3
                };
                */
                ma1 = new NewtonFluidMaterial
                {
                    MassDensity = 1.0,
                    GravityX = 0.0,
                    GravityY = -1.0e+1,
                    Mu = 0.02
                };
                uint maId1 = world.AddMaterial(ma1);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId1);
            }

            /*
            uint[] zeroEIds = { };
            var vZeroFixedCads = world.GetZeroFieldFixedCads(vQuantityId);
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                vZeroFixedCads.Add(fixedCad);
            }
            */

            /*
            // MPC
            var lineSegConstraints = new List<LineSegConstraint>();
            {
                var cadIdTypes = new List<KeyValuePair<uint, CadElementType>>();
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(1, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(2, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(3, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(4, CadElementType.Edge));
                var condition1 = new
                {
                    Pt1 = new OpenTK.Vector2d(0.0, 0.0),
                    Pt2 = new OpenTK.Vector2d(4.0, 0.0),
                    Normal = new OpenTK.Vector2d(0.0, 1.0)
                };
                var condition2 = new
                {
                    Pt1 = new OpenTK.Vector2d(0.0, 0.0),
                    Pt2 = new OpenTK.Vector2d(0.0, 4.0),
                    Normal = new OpenTK.Vector2d(1.0, 0.0)
                };
                var condition3 = new
                {
                    Pt1 = new OpenTK.Vector2d(4.0, 0.0),
                    Pt2 = new OpenTK.Vector2d(4.0, 4.0),
                    Normal = new OpenTK.Vector2d(-1.0, 0.0)
                };
                var conditions = new[] { condition1, condition2, condition3 };
                uint[] cQuantityIds = new uint[] { cQuantityId1, cQuantityId2, cQuantityId3 };
                for (int i = 0; i < conditions.Length; i++)
                {
                    var lineSegConstraint1 = new LineSegConstraint(
                        conditions[i].Pt1, conditions[i].Pt2, conditions[i].Normal, EqualityType.GreaterEq);
                    lineSegConstraints.Add(lineSegConstraint1);
                    MultipointConstraint mpc1 = new MultipointConstraint(cadIdTypes, lineSegConstraint1);
                    world.AddMultipointConstraint(cQuantityIds[i], mpc1);
                }
            }
            */

            //壁の境界条件
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)1, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)4, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(vQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            world.MakeElements();

            uint vValueId = 0;
            uint prevVValueId = 0;
            uint vValueId2 = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                vValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    vQuantityId, false, FieldShowType.Real);
                prevVValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    vQuantityId, false, FieldShowType.Real);
                // 座標用
                vValueId2 = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value,
                    vQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                /*
                foreach (var lineSegConstraint1 in lineSegConstraints)
                {
                    ConstraintDrawer constraintDrawer1 = new ConstraintDrawer(lineSegConstraint1);
                    fieldDrawerArray.Add(constraintDrawer1);
                }
                */
                var faceDrawer = new FaceFieldDrawer(vValueId, FieldDerivativeType.Value, false, world);
                faceDrawer.Color = new double[] { 0.0, 0.0, 1.0 };
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    vValueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                // 縮小
                mainWindow.DoZoom(20, false);
            }

            double t = 0;
            double dt = 1.0e-2;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            double[] updatedCoord = null;
            int nTime = 1000;
            for (int iTime = 0; iTime <= nTime; iTime++)
            {
                var FEM = new FluidDisplacement2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma, vValueId, prevVValueId);
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
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6;
                FEM.Counter = iTime;
                FEM.UpdatedCoord = updatedCoord;
                FEM.Solve();
                double[] U = FEM.U;
                updatedCoord = FEM.UpdatedCoord;

                FEM.UpdateFieldValuesTimeDomain(); // for vValueId, prevVValueId

                //-------------------------------------------------
                int nDim = 2;
                int vNodeCnt = updatedCoord.Length / nDim;
                double[] displacements = new double[vNodeCnt * nDim]; 
                for (int nodeId = 0; nodeId < vNodeCnt; nodeId++)
                {
                    int coId = world.Node2Coord(vQuantityId, nodeId);
                    double[] co = world.GetCoord(vQuantityId, coId);
                    for (int iDof = 0; iDof < nDim; iDof++)
                    {
                        displacements[nodeId * nDim + iDof] = updatedCoord[nodeId * nDim + iDof] - co[iDof];
                    }
                }
                world.UpdateFieldValueValuesFromNodeValues(vValueId, FieldDerivativeType.Value, displacements);
                //-------------------------------------------------

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
