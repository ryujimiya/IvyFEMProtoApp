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
        public void FluidNSDropletTDProblem1(MainWindow mainWindow)
        {
            //////////////////////////////////////////////////////
            // dropletの位置
            //double[] xd = new double[2] { 0.01, 0.75 };
            double[] xd = new double[2] { 0.01, 0.75 };
            //-------------------------------------
            double minXd = 0.0;
            double maxXd = 1.0;
            double minYd = 0.0;
            double maxYd = 1.0;
            //-------------------------------------
            // 重力
            double[] gravity = new double[] { 0.0, -9.81 };
            // viscosity (fluid)
            double muf = 0.02;
            // 密度(fluid、particle)
            double rhof = 1.2;
            //double rhop = 1.2;
            //double rhop = 100.0;
            double rhop = 100.0;
            // particle体積、半径、質量
            double Rp = 0.1;//0.1;
            double Vp = (4.0 / 3.0) * Math.PI * Rp * Rp * Rp;
            double mp = rhop * Vp;
            //////////////////////////////////////////////////////

            Cad2D cad = new Cad2D();
            {
                uint lId1 = 0;
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(1.0, 1.0));
                    pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                    pts.Add(new OpenTK.Vector2d(0.0, 0.8));
                    pts.Add(new OpenTK.Vector2d(0.0, 0.775));
                    pts.Add(new OpenTK.Vector2d(0.0, 0.750));
                    pts.Add(new OpenTK.Vector2d(0.0, 0.725));
                    pts.Add(new OpenTK.Vector2d(0.0, 0.7));
                    lId1 = cad.AddPolygon(pts).AddLId;
                    System.Diagnostics.Debug.Assert(lId1 == 1);
                }
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad2DDrawer(cad);
            mainWindow.DrawerArray.Add(drawer);
            var drawer2 = new PointsDrawer();
            drawer2.Color = new double[] { 0.0, 0.0, 0.0 };
            mainWindow.DrawerArray.Add(drawer2);
            {
                double displayR1 = 0.1;
                var pair1 = new KeyValuePair<double[], double>(
                    xd, displayR1);
                drawer2.PointRadiuss = new KeyValuePair<double[], double>[1] { pair1 };
            }
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            //double eLen = 0.08;
            //double eLen = 0.05;
            double eLen = 0.05;//0.05;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint vQuantityId;
            uint pQuantityId;
            {
                uint vDof = 2;
                uint pDof = 1;
                uint vFEOrder = 1;
                uint pFEOrder = 1;
                vQuantityId = world.AddQuantity(vDof, vFEOrder, FiniteElementType.ScalarLagrange);
                pQuantityId = world.AddQuantity(pDof, pFEOrder, FiniteElementType.ScalarLagrange);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point7;

            {
                world.ClearMaterial();

                NewtonFluidMaterial ma = null;
                ma = new NewtonFluidMaterial
                {
                    MassDensity = rhof,//1.2,
                    GravityX = 0.0,
                    GravityY = 0.0,
                    Mu = muf//0.02
                };
                uint maId = world.AddMaterial(ma);

                uint lId1 = 1;
                world.SetCadLoopMaterial(lId1, maId);
            }

            /*
            uint[] zeroEIds = { };
            var zeroFixedCads = world.GetZeroFieldFixedCads(vQuantityId);
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                zeroFixedCads.Add(fixedCad);
            }
            uint[] kpZeroEIds = { };
            var kpZeroFixedCads = world.GetZeroFieldFixedCads(kpQuantityId);
            foreach (uint eId in kpZeroEIds)
            {
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                kpZeroFixedCads.Add(fixedCad);
            }
            */

            //double vx0 = 0.5;
            //double vx0 = 6.3;
            //double vx0 = 22.3;
            //double vx0 = 10.0;
            //double vx0 = 5.0;
            double vx0 = 22.3;//5.0;
            double cbc = 0.01;
            double kp0 = cbc * vx0 * vx0;
            double cmu = 0.09;
            double l0 = 1.0;
            double ep0 = cmu * Math.Pow(kp0, 3.0 / 2.0) / l0;
            IList<ConstFieldFixedCad> forceFixedCadsV = new List<ConstFieldFixedCad>();
            IList<ConstFieldFixedCad> forceFixedCadsKp = new List<ConstFieldFixedCad>();
            IList<ConstFieldFixedCad> forceFixedCadsEp = new List<ConstFieldFixedCad>();
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)5, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { vx0, 0.0 } },
                    new { CadId = (uint)6, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { vx0, 0.0 } },
                    new { CadId = (uint)7, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { vx0, 0.0 } },
                    new { CadId = (uint)8, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { vx0, 0.0 } },
                    //
                    new { CadId = (uint)1, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                    new { CadId = (uint)4, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                    new { CadId = (uint)9, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                };
                var fixedCads = world.GetFieldFixedCads(vQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }

                forceFixedCadsV.Add(fixedCads[0] as ConstFieldFixedCad);
                forceFixedCadsV.Add(fixedCads[1] as ConstFieldFixedCad);
                forceFixedCadsV.Add(fixedCads[2] as ConstFieldFixedCad);
                forceFixedCadsV.Add(fixedCads[3] as ConstFieldFixedCad);
            }
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)5, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { kp0 } },
                    new { CadId = (uint)6, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { kp0 } },
                    new { CadId = (uint)7, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { kp0 } },
                    new { CadId = (uint)8, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { kp0 } },
                    //
                    new { CadId = (uint)1, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)4, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)9, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
            }

            world.MakeElements();

            uint vValueId = 0;
            uint prevVValueId = 0;
            uint bubbleVValueId = 0;
            uint pValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            PointsDrawer pointsDrawer;
            {
                world.ClearFieldValue();
                vValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    vQuantityId, false, FieldShowType.Real);
                prevVValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    vQuantityId, false, FieldShowType.Real);
                bubbleVValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    vQuantityId, true, FieldShowType.Real);
                pValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    pQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var vectorDrawer = new VectorFieldDrawer(
                    bubbleVValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                var faceDrawer = new FaceFieldDrawer(pValueId, FieldDerivativeType.Value, true, world,
                    pValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    vValueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                pointsDrawer = new PointsDrawer();
                pointsDrawer.Color = new double[] { 0.0, 0.0, 0.0 };
                fieldDrawerArray.Add(pointsDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            //double dt = 1.0e-1;
            //double dt = 0.5e-1;
            double dt = 0.5e-1;
            //double tmax = 1.0e-1;
            double tmax = 1.0e-1;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 100; iTime++)
            {
                if (xd[0] >= minXd && xd[0] <= maxXd &&
                    xd[1] >= minYd && xd[1] <= maxYd)
                {
                    // OK
                }
                else
                {
                    // 解析範囲外になったので終了
                    break;
                }

                if (t <= tmax)
                {
                    // 噴射中
                }
                else
                {
                    // 噴射終了
                    /*
                    //------------------------------------
                    // 条件を外す
                    {
                        var fixedCads = world.GetFieldFixedCads(vQuantityId);
                        foreach (var forceFixedCad in forceFixedCadsV)
                        {
                            fixedCads.Remove(forceFixedCad);
                        }
                    }
                    //------------------------------------
                    */

                    //速度0
                    foreach (var forceFixedCad in forceFixedCadsV)
                    {
                        double[] value = forceFixedCad.GetDoubleValues();
                        value[0] = 0.0;
                        value[1] = 0.0;
                    }

                }

                var FEM = new FluidNSDroplet2DRKTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    vValueId, prevVValueId);
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
                    //solver.Method = IvyFEM.Lklinear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    //FEM.Solver = solver;
                }
                FEM.Gravity = gravity;
                FEM.Muf = muf;
                // 密度
                FEM.Rhof = rhof;
                FEM.Rhop = rhop;
                FEM.Vp = Vp;
                FEM.Rp = Rp;
                FEM.Mp = mp;
                // droplet位置(in/out)
                FEM.PosXd = xd;
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6;
                FEM.Solve();
                double[] U = FEM.U;
                xd = FEM.PosXd;

                FEM.UpdateFieldValuesTimeDomain(); // for vValueId, prevVValueId
                //--------------------------------------
                // 速度の表示調整
                double[] showU = new double[U.Length];
                int vNodeCnt = (int)world.GetNodeCount(vQuantityId);
                int vDof = 2;
                int nodeCnt1 = vNodeCnt * vDof;
                /*
                double maxVelo = double.MinValue;
                for (int i = 0; i < (nodeCnt1 / vDof); i++)
                {
                    double absV = Math.Sqrt(U[i * vDof] * U[i * vDof] + U[i * vDof + 1] * U[i * vDof + 1]);
                    if (absV > maxVelo)
                    {
                        maxVelo = absV;
                    }
                }
                */
                double maxVelo = vx0;
                for (int i = 0; i < nodeCnt1; i++)
                {
                    showU[i] = U[i] * (1.0 / maxVelo);
                }
                // 速度以外はそのまま
                for (int i = nodeCnt1; i < U.Length; i++)
                {
                    showU[i] = U[i];
                }
                //--------------------------------------
                world.UpdateFieldValueValuesFromNodeValues(pValueId, FieldDerivativeType.Value, U);
                world.UpdateBubbleFieldValueValuesFromNodeValues(bubbleVValueId, FieldDerivativeType.Value, showU);

                {
                    //double displayR1 = FEM.Rp;
                    double displayR1 = 0.1;
                    var pair1 = new KeyValuePair<double[], double>(
                        xd, displayR1);
                    pointsDrawer.PointRadiuss = new KeyValuePair<double[], double>[1] { pair1 };
                }

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
