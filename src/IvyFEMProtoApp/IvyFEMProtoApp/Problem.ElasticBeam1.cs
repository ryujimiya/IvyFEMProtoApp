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
        public void BeamProblem1(MainWindow mainWindow, bool isTimoshenko)
        {
            double beamLen = 1.0;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            int divCnt = 10;
            double eLen = 0.95 * beamLen / (double)divCnt;
            //double loadX = beamLen * 0.5;
            double loadX = beamLen * 0.75;
            CadObject2D cad = new CadObject2D();
            {
                uint lId0 = 0;
                uint vId1 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(0.0, 0.0)).AddVId;
                uint vId2 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(loadX, 0.0)).AddVId;
                uint vId3 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(beamLen, 0.0)).AddVId;
                uint eId1 = cad.ConnectVertexLine(vId1, vId2).AddEId;
                uint eId2 = cad.ConnectVertexLine(vId2, vId3).AddEId;
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
            mesher.SetMeshingModeElemLength();
            {
                IList<uint> eIds = cad.GetElementIds(CadElementType.Edge);
                foreach (uint eId in eIds)
                {
                    mesher.AddCutMeshEdgeCadId(eId, eLen);
                }
            }
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
            uint dQuantityId; // displacement
            uint rQuantityId; // rotation
            if (isTimoshenko)
            {
                uint dDof = 1; // Scalar (w)
                uint rDof = 1; // Scalar (θ)
                uint dFEOrder = 1;
                uint rFEOrder = 1;
                dQuantityId = world.AddQuantity(dDof, dFEOrder, FiniteElementType.ScalarLagrange);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarLagrange);
            }
            else
            {
                uint dDof = 1; // Scalar (w)
                uint rDof = 1; // Scalar (θ)
                uint dFEOrder = 3;
                uint rFEOrder = 3;
                dQuantityId = world.AddQuantity(dDof, dFEOrder, FiniteElementType.ScalarHermite);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarHermite);
            }

            {
                world.ClearMaterial();
                uint nullMaId;
                uint beamMaId;
                {
                    var ma = new NullMaterial();
                    nullMaId = world.AddMaterial(ma);
                }
                if (isTimoshenko)
                {
                    var ma = new TimoshenkoBeamMaterial();
                    ma.Area = b * h;
                    ma.SecondMomentOfArea = (1.0 / 12.0) * b * h * h * h;
                    ma.PolarSecondMomentOfArea = (1.0 / 12.0) * b * h * h * h + (1.0 / 12.0) * b * b * b * h;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    ma.TimoshenkoShearCoefficient = 5.0 / 6.0; // 長方形断面
                    beamMaId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new BeamMaterial();
                    ma.Area = b * h;
                    ma.SecondMomentOfArea = (1.0 / 12.0) * b * h * h * h;
                    ma.PolarSecondMomentOfArea = (1.0 / 12.0) * b * h * h * h + (1.0 / 12.0) * b * b * b * h;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    beamMaId = world.AddMaterial(ma);
                }

                uint[] lIds = cad.GetElementIds(CadElementType.Loop).ToArray();
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, nullMaId);
                }
                uint[] eIds = cad.GetElementIds(CadElementType.Edge).ToArray();
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, beamMaId);
                }
            }

            uint[] dZeroVIds = { 1, 3 };
            var dZeroFixedCads = world.GetZeroFieldFixedCads(dQuantityId);
            foreach (uint vId in dZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                dZeroFixedCads.Add(fixedCad);
            }
            uint[] rZeroVIds = { 1, 3 };
            var rZeroFixedCads = world.GetZeroFieldFixedCads(rQuantityId);
            foreach (uint vId in rZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                rZeroFixedCads.Add(fixedCad);
            }

            // displacement
            FieldFixedCad fixedCadD;
            {
                // FixedDofIndex 0: w
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(dQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD = world.GetFieldFixedCads(dQuantityId)[0];
            }
            // rotation
            {
                // FixedDofIndex 0: θ
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(rQuantityId);
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
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    dQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var edgeDrawer0 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer0);
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
                fixedValueD[0] = -0.1 * beamLen * Math.Sin(t * 2.0 * Math.PI * 0.1);

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
                double[] Uwt = FEM.U;

                // 変位(u,w)へ変換する
                int coCnt = (int)world.GetCoordCount(dQuantityId);
                int dNodeCnt = (int)world.GetNodeCount(dQuantityId);
                int rNodeCnt = (int)world.GetNodeCount(rQuantityId);
                int offset = dNodeCnt;
                int dof = 2;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    int dNodeId = world.Coord2Node(dQuantityId, coId);
                    int rNodeId = world.Coord2Node(rQuantityId, coId);
                    double w = 0;
                    double theta = 0;
                    if (dNodeId != -1)
                    {
                        w = Uwt[dNodeId];
                    }
                    if (rNodeId != -1)
                    {
                        theta = Uwt[rNodeId + offset];
                    }
                    U[coId * dof + 0] = 0.0;
                    U[coId * dof + 1] = w;
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

        public void BeamTDProblem1(MainWindow mainWindow, bool isTimoshenko)
        {
            double beamLen = 1.0;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            int divCnt = 10;
            double eLen = 0.95 * beamLen / (double)divCnt;
            //double loadX = beamLen * 0.5;
            double loadX = beamLen * 0.75;
            CadObject2D cad = new CadObject2D();
            {
                uint lId0 = 0;
                uint vId1 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(0.0, 0.0)).AddVId;
                uint vId2 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(loadX, 0.0)).AddVId;
                uint vId3 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(beamLen, 0.0)).AddVId;
                uint eId1 = cad.ConnectVertexLine(vId1, vId2).AddEId;
                uint eId2 = cad.ConnectVertexLine(vId2, vId3).AddEId;
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
            mesher.SetMeshingModeElemLength();
            {
                IList<uint> eIds = cad.GetElementIds(CadElementType.Edge);
                foreach (uint eId in eIds)
                {
                    mesher.AddCutMeshEdgeCadId(eId, eLen);
                }
            }
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
            uint dQuantityId; // displacement
            uint rQuantityId; // rotation
            if (isTimoshenko)
            {
                uint dDof = 1; // Scalar (w)
                uint rDof = 1; // Scalar (θ)
                uint dFEOrder = 1;
                uint rFEOrder = 1;
                dQuantityId = world.AddQuantity(dDof, dFEOrder, FiniteElementType.ScalarLagrange);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarLagrange);
            }
            else
            {
                uint dDof = 1; // Scalar (w)
                uint rDof = 1; // Scalar (θ)
                uint dFEOrder = 3;
                uint rFEOrder = 3;
                dQuantityId = world.AddQuantity(dDof, dFEOrder, FiniteElementType.ScalarHermite);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarHermite);
            }

            {
                world.ClearMaterial();
                uint nullMaId;
                uint beamMaId;
                {
                    var ma = new NullMaterial();
                    nullMaId = world.AddMaterial(ma);
                }
                if (isTimoshenko)
                {
                    var ma = new TimoshenkoBeamMaterial();
                    ma.Area = b * h;
                    ma.SecondMomentOfArea = (1.0 / 12.0) * b * h * h * h;
                    ma.PolarSecondMomentOfArea = (1.0 / 12.0) * b * h * h * h + (1.0 / 12.0) * b * b * b * h;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    ma.TimoshenkoShearCoefficient = 5.0 / 6.0; // 長方形断面
                    beamMaId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new BeamMaterial();
                    ma.Area = b * h;
                    ma.SecondMomentOfArea = (1.0 / 12.0) * b * h * h * h;
                    ma.PolarSecondMomentOfArea = (1.0 / 12.0) * b * h * h * h + (1.0 / 12.0) * b * b * b * h;
                    ma.MassDensity = 2.3e+3;
                    ma.Young = 169.0e+9;
                    ma.Poisson = 0.262;
                    beamMaId = world.AddMaterial(ma);
                }

                uint[] lIds = cad.GetElementIds(CadElementType.Loop).ToArray();
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, nullMaId);
                }
                uint[] eIds = cad.GetElementIds(CadElementType.Edge).ToArray();
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, beamMaId);
                }
            }

            uint[] dZeroVIds = { 1, 3 };
            var dZeroFixedCads = world.GetZeroFieldFixedCads(dQuantityId);
            foreach (uint vId in dZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                dZeroFixedCads.Add(fixedCad);
            }
            uint[] rZeroVIds = { 1, 3 };
            var rZeroFixedCads = world.GetZeroFieldFixedCads(rQuantityId);
            foreach (uint vId in rZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                rZeroFixedCads.Add(fixedCad);
            }

            // displacement
            FieldFixedCad fixedCadD;
            {
                // FixedDofIndex 0: w
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(dQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD = world.GetFieldFixedCads(dQuantityId)[0];
            }
            // rotation
            {
                // FixedDofIndex 0: θ
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)2, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(rQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            world.MakeElements();

            uint dValueId = 0;
            uint dPrevValueId = 0;
            uint rValueId = 0;
            uint rPrevValueId = 0;
            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 表示用
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    dQuantityId, false, FieldShowType.Real);
                // Newmarkβ
                // Scalar
                dValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    dQuantityId, false, FieldShowType.Real);
                dPrevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    dQuantityId, false, FieldShowType.Real);
                rValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    rQuantityId, false, FieldShowType.Real);
                rPrevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    rQuantityId, false, FieldShowType.Real);

                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var edgeDrawer0 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer0);
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
            IList<uint> valueIds = new List<uint> { dValueId, rValueId };
            IList<uint> prevValueIds = new List<uint> { dPrevValueId, rPrevValueId };

            double t = 0;
            double dt = 0.5;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] fixedValueD = fixedCadD.GetDoubleValues();
                fixedValueD[0] = -0.1 * beamLen * Math.Sin(t * 2.0 * Math.PI * 0.01);

                var FEM = new Elastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueIds, prevValueIds);
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
                double[] Uwt = FEM.U;

                // 変位(u,w)へ変換する
                int coCnt = (int)world.GetCoordCount(dQuantityId);
                int dNodeCnt = (int)world.GetNodeCount(dQuantityId);
                int rNodeCnt = (int)world.GetNodeCount(rQuantityId);
                int offset = dNodeCnt;
                int dof = 2;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    int dNodeId = world.Coord2Node(dQuantityId, coId);
                    int rNodeId = world.Coord2Node(rQuantityId, coId);
                    double w = 0;
                    double theta = 0;
                    if (dNodeId != -1)
                    {
                        w = Uwt[dNodeId];
                    }
                    if (rNodeId != -1)
                    {
                        theta = Uwt[rNodeId + offset];
                    }
                    U[coId * dof + 0] = 0.0;
                    U[coId * dof + 1] = w;
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
