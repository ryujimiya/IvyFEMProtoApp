﻿using System;
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
        public void CorotationalFrameProblem1(MainWindow mainWindow, bool isTimoshenko)
        {
            double beamLen = 1.0;
            int beamCntX = 2;
            double beamsLen = beamLen * beamCntX;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            int divCnt = 10;
            double eLen = 0.95 * beamLen / (double)divCnt;
            double loadX = beamLen + beamLen * 0.5;
            double offsetX = beamLen * (1.0 / 2.0);
            double offsetY = beamLen * (Math.Sqrt(3.0) / 2.0);
            double endX = offsetX + beamLen * (beamCntX - 1); 
            CadObject2D cad = new CadObject2D();
            {
                uint lId0 = 0;
                uint vId1 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(0.0, 0.0)).AddVId;
                uint vId2 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(beamLen, 0.0)).AddVId;
                uint vId3 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(loadX, 0.0)).AddVId;
                uint vId4 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(beamsLen, 0.0)).AddVId;
                uint vId5 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(offsetX, offsetY)).AddVId;
                uint vId6 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(endX, offsetY)).AddVId;

                uint eId1 = cad.ConnectVertexLine(vId1, vId2).AddEId;
                uint eId2 = cad.ConnectVertexLine(vId2, vId3).AddEId;
                uint eId3 = cad.ConnectVertexLine(vId3, vId4).AddEId;
                uint eId4 = cad.ConnectVertexLine(vId1, vId5).AddEId;
                uint eId5 = cad.ConnectVertexLine(vId5, vId2).AddEId;
                uint eId6 = cad.ConnectVertexLine(vId2, vId6).AddEId;
                uint eId7 = cad.ConnectVertexLine(vId6, vId4).AddEId;
                uint eId8 = cad.ConnectVertexLine(vId5, vId6).AddEId;
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
            uint d1QuantityId; // displacement1
            uint d2QuantityId; // displacement2
            uint rQuantityId; // rotation
            {
                uint d1Dof = 1; // Scalar (u)
                uint d2Dof = 1; // Scalar (v)
                uint rDof = 1; // Scalar (θ)
                uint d1FEOrder = 1;
                uint d2FEOrder = 1;
                uint rFEOrder = 1;
                d1QuantityId = world.AddQuantity(d1Dof, d1FEOrder, FiniteElementType.ScalarLagrange);
                d2QuantityId = world.AddQuantity(d2Dof, d2FEOrder, FiniteElementType.ScalarLagrange);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarLagrange);
            }
            uint[] dQuantityIds = { d1QuantityId, d2QuantityId };

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
                    var ma = new TimoshenkoCorotationalFrameMaterial();
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
                    var ma = new CorotationalFrameMaterial();
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

            uint[] d1ZeroVIds = { 1, 4 };
            var d1ZeroFixedCads = world.GetZeroFieldFixedCads(d1QuantityId);
            foreach (uint vId in d1ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                d1ZeroFixedCads.Add(fixedCad);
            }
            uint[] d2ZeroVIds = { 1, 4 };
            var d2ZeroFixedCads = world.GetZeroFieldFixedCads(d2QuantityId);
            foreach (uint vId in d2ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                d2ZeroFixedCads.Add(fixedCad);
            }
            uint[] rZeroVIds = { 1, 4, 2, 5, 6 };
            var rZeroFixedCads = world.GetZeroFieldFixedCads(rQuantityId);
            foreach (uint vId in rZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                rZeroFixedCads.Add(fixedCad);
            }

            // displacement
            FieldFixedCad fixedCadD1;
            {
                // FixedDofIndex 0: u
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)3, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(d1QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD1 = world.GetFieldFixedCads(d1QuantityId)[0];
            }
            FieldFixedCad fixedCadD2;
            {
                // FixedDofIndex 0: v
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)3, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(d2QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD2 = world.GetFieldFixedCads(d2QuantityId)[0];
            }
            // rotation
            {
                // FixedDofIndex 0: θ
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)3, CadElemType = CadElementType.Vertex,
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
                    d1QuantityId, false, FieldShowType.Real);
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
                double[] fixedValue01 = fixedCadD1.GetDoubleValues();
                double[] fixedValueD2 = fixedCadD2.GetDoubleValues();
                fixedValue01[0] = 0.0;
                fixedValueD2[0] = -0.2 * beamLen * Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic2DFEM(world);                
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
                FEM.DisplacementQuantityIds = dQuantityIds.ToList();
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6; // 収束条件を緩めている
                FEM.Solve();
                double[] Uuvt = FEM.U;

                // 変位(u,w)へ変換する
                int coCnt = (int)world.GetCoordCount(d1QuantityId);
                int d1Dof = (int)world.GetDof(d1QuantityId);
                int d2Dof = (int)world.GetDof(d2QuantityId);
                int rDof = (int)world.GetDof(rQuantityId);
                int d1NodeCnt = (int)world.GetNodeCount(d1QuantityId);
                int d2NodeCnt = (int)world.GetNodeCount(d2QuantityId);
                int rNodeCnt = (int)world.GetNodeCount(rQuantityId);
                int d2Offset = d1NodeCnt * d1Dof;
                int rOffset = d2Offset + d2NodeCnt * d2Dof;
                int dof = 2;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    int d1NodeId = world.Coord2Node(d1QuantityId, coId);
                    int d2NodeId = world.Coord2Node(d2QuantityId, coId);
                    int rNodeId = world.Coord2Node(rQuantityId, coId);
                    double u = 0;
                    double v = 0;
                    double theta = 0;
                    if (d1NodeId != -1)
                    {
                        u = Uuvt[d1NodeId];
                    }
                    if (d2NodeId != -1)
                    {
                        v = Uuvt[d2NodeId + d2Offset];
                    }
                    if (rNodeId != -1)
                    {
                        theta = Uuvt[rNodeId + rOffset];
                    }
                    U[coId * dof + 0] = u;
                    U[coId * dof + 1] = v;
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

        public void CorotationalFrameTDProblem1(MainWindow mainWindow, bool isTimoshenko)
        {
            double beamLen = 1.0;
            int beamCntX = 2;
            double beamsLen = beamLen * beamCntX;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            int divCnt = 10;
            double eLen = 0.95 * beamLen / (double)divCnt;
            double loadX = beamLen + beamLen * 0.5;
            double offsetX = beamLen * (1.0 / 2.0);
            double offsetY = beamLen * (Math.Sqrt(3.0) / 2.0);
            double endX = offsetX + beamLen * (beamCntX - 1);
            CadObject2D cad = new CadObject2D();
            {
                uint lId0 = 0;
                uint vId1 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(0.0, 0.0)).AddVId;
                uint vId2 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(beamLen, 0.0)).AddVId;
                uint vId3 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(loadX, 0.0)).AddVId;
                uint vId4 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(beamsLen, 0.0)).AddVId;
                uint vId5 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(offsetX, offsetY)).AddVId;
                uint vId6 = cad.AddVertex(CadElementType.Loop, lId0, new OpenTK.Vector2d(endX, offsetY)).AddVId;

                uint eId1 = cad.ConnectVertexLine(vId1, vId2).AddEId;
                uint eId2 = cad.ConnectVertexLine(vId2, vId3).AddEId;
                uint eId3 = cad.ConnectVertexLine(vId3, vId4).AddEId;
                uint eId4 = cad.ConnectVertexLine(vId1, vId5).AddEId;
                uint eId5 = cad.ConnectVertexLine(vId5, vId2).AddEId;
                uint eId6 = cad.ConnectVertexLine(vId2, vId6).AddEId;
                uint eId7 = cad.ConnectVertexLine(vId6, vId4).AddEId;
                uint eId8 = cad.ConnectVertexLine(vId5, vId6).AddEId;
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
            uint d1QuantityId; // displacement1
            uint d2QuantityId; // displacement2
            uint rQuantityId; // rotation
            {
                uint d1Dof = 1; // Scalar (u)
                uint d2Dof = 1; // Scalar (v)
                uint rDof = 1; // Scalar (θ)
                uint d1FEOrder = 1;
                uint d2FEOrder = 1;
                uint rFEOrder = 1;
                d1QuantityId = world.AddQuantity(d1Dof, d1FEOrder, FiniteElementType.ScalarLagrange);
                d2QuantityId = world.AddQuantity(d2Dof, d2FEOrder, FiniteElementType.ScalarLagrange);
                rQuantityId = world.AddQuantity(rDof, rFEOrder, FiniteElementType.ScalarLagrange);
            }
            uint[] dQuantityIds = { d1QuantityId, d2QuantityId };

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
                    var ma = new TimoshenkoCorotationalFrameMaterial();
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
                    var ma = new CorotationalFrameMaterial();
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

            uint[] d1ZeroVIds = { 1, 4 };
            var d1ZeroFixedCads = world.GetZeroFieldFixedCads(d1QuantityId);
            foreach (uint vId in d1ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                d1ZeroFixedCads.Add(fixedCad);
            }
            uint[] d2ZeroVIds = { 1, 4 };
            var d2ZeroFixedCads = world.GetZeroFieldFixedCads(d2QuantityId);
            foreach (uint vId in d2ZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                d2ZeroFixedCads.Add(fixedCad);
            }
            uint[] rZeroVIds = { 1, 4, 2, 5, 6 };
            var rZeroFixedCads = world.GetZeroFieldFixedCads(rQuantityId);
            foreach (uint vId in rZeroVIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(vId, CadElementType.Vertex, FieldValueType.Scalar);
                rZeroFixedCads.Add(fixedCad);
            }

            // displacement
            FieldFixedCad fixedCadD1;
            {
                // FixedDofIndex 0: u
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)3, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(d1QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD1 = world.GetFieldFixedCads(d1QuantityId)[0];
            }
            FieldFixedCad fixedCadD2;
            {
                // FixedDofIndex 0: v
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)3, CadElemType = CadElementType.Vertex,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(d2QuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadD2 = world.GetFieldFixedCads(d2QuantityId)[0];
            }
            // rotation
            {
                // FixedDofIndex 0: θ
                var fixedCadDatas = new[]
                {
                    // 可動部
                    new { CadId = (uint)3, CadElemType = CadElementType.Vertex,
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

            uint d1ValueId = 0;
            uint d1PrevValueId = 0;
            uint d2ValueId = 0;
            uint d2PrevValueId = 0;
            uint rValueId = 0;
            uint rPrevValueId = 0;
            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 表示用
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    d1QuantityId, false, FieldShowType.Real);

                // Newmarkβ
                // Scalar
                d1ValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d1QuantityId, false, FieldShowType.Real);
                d1PrevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d1QuantityId, false, FieldShowType.Real);
                d2ValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d2QuantityId, false, FieldShowType.Real);
                d2PrevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    d2QuantityId, false, FieldShowType.Real);
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
            IList<uint> valueIds = new List<uint> { d1ValueId, d2ValueId, rValueId };
            IList<uint> prevValueIds = new List<uint> { d1PrevValueId, d2PrevValueId, rPrevValueId };

            double t = 0;
            double dt = 0.5;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] fixedValue01 = fixedCadD1.GetDoubleValues();
                double[] fixedValueD2 = fixedCadD2.GetDoubleValues();
                fixedValue01[0] = 0.0;
                fixedValueD2[0] = -0.2 * beamLen * Math.Sin(t * 2.0 * Math.PI * 0.01);

                var FEM = new Elastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueIds, prevValueIds);
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
                FEM.DisplacementQuantityIds = dQuantityIds.ToList();
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-6; // 収束条件を緩めている
                FEM.Solve();
                double[] Uuvt = FEM.U;

                // 変位(u,w)へ変換する
                int coCnt = (int)world.GetCoordCount(d1QuantityId);
                int d1Dof = (int)world.GetDof(d1QuantityId);
                int d2Dof = (int)world.GetDof(d2QuantityId);
                int rDof = (int)world.GetDof(rQuantityId);
                int d1NodeCnt = (int)world.GetNodeCount(d1QuantityId);
                int d2NodeCnt = (int)world.GetNodeCount(d2QuantityId);
                int rNodeCnt = (int)world.GetNodeCount(rQuantityId);
                int d2Offset = d1NodeCnt * d1Dof;
                int rOffset = d2Offset + d2NodeCnt * d2Dof;
                int dof = 2;
                double[] U = new double[coCnt * dof];
                for (int coId = 0; coId < coCnt; coId++)
                {
                    int d1NodeId = world.Coord2Node(d1QuantityId, coId);
                    int d2NodeId = world.Coord2Node(d2QuantityId, coId);
                    int rNodeId = world.Coord2Node(rQuantityId, coId);
                    double u = 0;
                    double v = 0;
                    double theta = 0;
                    if (d1NodeId != -1)
                    {
                        u = Uuvt[d1NodeId];
                    }
                    if (d2NodeId != -1)
                    {
                        v = Uuvt[d2NodeId + d2Offset];
                    }
                    if (rNodeId != -1)
                    {
                        theta = Uuvt[rNodeId + rOffset];
                    }
                    U[coId * dof + 0] = u;
                    U[coId * dof + 1] = v;
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
