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
        public void HyperelasticProblem(MainWindow mainWindow, bool isMooney)
        {
            CadObject2D cad = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                var res = cad.AddPolygon(pts);
            }

            double eLen = 0.1;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint uQuantityId;
            uint lQuantityId;
            {
                uint uDof = 2; // Vector2
                uint lDof = 1; // Scalar
                uint uFEOrder = 1;
                uint lFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder, FiniteElementType.ScalarLagrange);
                lQuantityId = world.AddQuantity(lDof, lFEOrder, FiniteElementType.ScalarLagrange);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point7;

            if (isMooney)
            {
                // Mooney-Rivlin
                world.ClearMaterial();
                uint maId = 0;
                var ma = new MooneyRivlinHyperelasticMaterial();
                ma.IsCompressible = false;
                //ma.IsCompressible = true;
                //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                ma.C1 = 200;
                ma.C2 = 200;
                ma.GravityX = 0;
                ma.GravityY = 0;
                ma.MassDensity = 1.0;
                maId = world.AddMaterial(ma);

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }
            else
            {
                // Odgen
                world.ClearMaterial();
                uint maId = 0;
                var ma = new OgdenHyperelasticMaterial();
                double[] alphas = { 1.3, 5.0, -2.0 };
                double[] mus = { 6300e3, 1.2e3, -10e3 };
                //double[] alphas = { 2.0, -2.0 };
                //double[] mus = { 400, -400 };
                System.Diagnostics.Debug.Assert(alphas.Length == mus.Length);
                ma.IsCompressible = false;
                //ma.IsCompressible = true;
                //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                ma.SetAlphaMu(alphas.Length, alphas, mus);
                ma.GravityX = 0;
                ma.GravityY = 0;
                ma.MassDensity = 1.0;
                maId = world.AddMaterial(ma);

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            uint[] zeroEIds = { 4 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(uQuantityId);
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
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(uQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadXY = world.GetFieldFixedCads(uQuantityId)[0];
            }

            world.MakeElements();

            uint uValueId = 0;
            uint lValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                uValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    uQuantityId, false, FieldShowType.Real);
                // Scalar
                lValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    lQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivativeType.Value, false, world);
                // Lagrange未定乗数のサーモグラフィ表示
                //IFieldDrawer faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivativeType.Value, false, world,
                //    lValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    uValueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(
                    uValueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
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
                double[] fixedValueXY = fixedCadXY.GetDoubleValues();
                fixedValueXY[0] = 0;
                fixedValueXY[1] = Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic2DFEM(world);
                if (isMooney)
                {
                    // Mooney-Rivlin
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
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                        solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                        FEM.Solver = solver;
                    }
                    FEM.ConvRatioToleranceForNonlinearIter = 1.0e-10;
                }
                else
                {
                    // Ogden
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
                    FEM.ConvRatioToleranceForNonlinearIter = 1.0e-10;
                }
                FEM.Solve();
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(uValueId, FieldDerivativeType.Value, U);
                world.UpdateFieldValueValuesFromNodeValues(lValueId, FieldDerivativeType.Value, U);
                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void HyperelasticTDProblem(MainWindow mainWindow, bool isMooney)
        {
            CadObject2D cad = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                var res = cad.AddPolygon(pts);
            }

            double eLen = 0.1;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint uQuantityId;
            uint lQuantityId;
            {
                uint uDof = 2; // Vector2
                uint lDof = 1; // Scalar
                uint uFEOrder = 1;
                uint lFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder, FiniteElementType.ScalarLagrange);
                lQuantityId = world.AddQuantity(lDof, lFEOrder, FiniteElementType.ScalarLagrange);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point7;

            if (isMooney)
            {
                // Mooney-Rivlin
                world.ClearMaterial();
                uint maId = 0;
                var ma = new MooneyRivlinHyperelasticMaterial();
                ma.IsCompressible = false;
                //ma.IsCompressible = true;
                //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                ma.C1 = 200;
                ma.C2 = 200;
                ma.GravityX = 0;
                ma.GravityY = 0;
                ma.MassDensity = 1.0;
                maId = world.AddMaterial(ma);

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }
            else
            {
                // Odgen
                world.ClearMaterial();
                uint maId = 0;
                var ma = new OgdenHyperelasticMaterial();
                double[] alphas = { 1.3, 5.0, -2.0 };
                double[] mus = { 6300e3, 1.2e3, -10e3 };
                //double[] alphas = { 2.0, -2.0 };
                //double[] mus = { 400, -400 };
                System.Diagnostics.Debug.Assert(alphas.Length == mus.Length);
                ma.IsCompressible = false;
                //ma.IsCompressible = true;
                //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                ma.SetAlphaMu(alphas.Length, alphas, mus);
                ma.GravityX = 0;
                ma.GravityY = 0;
                ma.MassDensity = 1.0;
                maId = world.AddMaterial(ma);

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            uint[] zeroEIds = { 4 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(uQuantityId);
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
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } }
                };
                IList<FieldFixedCad> fixedCads = world.GetFieldFixedCads(uQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadXY = world.GetFieldFixedCads(uQuantityId)[0];
            }

            world.MakeElements();

            uint uValueId = 0;
            uint prevUValueId = 0;
            uint lValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                uValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, false, FieldShowType.Real);
                prevUValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, false, FieldShowType.Real);
                // Scalar
                lValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    lQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivativeType.Value, false, world);
                // Lagrange未定乗数のサーモグラフィ表示
                //IFieldDrawer faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivativeType.Value, false, world,
                //    lValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    uValueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(
                    uValueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                double[] fixedValueXY = fixedCadXY.GetDoubleValues();
                fixedValueXY[0] = 0;
                fixedValueXY[1] = Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    uValueId, prevUValueId, lValueId);
                if (isMooney)
                {
                    // Mooney-Rivlin
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
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                        solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                        FEM.Solver = solver;
                    }
                    FEM.ConvRatioToleranceForNonlinearIter = 1.0e-10;
                }
                else
                {
                    // Ogden
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
                    FEM.ConvRatioToleranceForNonlinearIter = 1.0e-10;
                }
                FEM.Solve();
                //double[] U = FEM.U;

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
