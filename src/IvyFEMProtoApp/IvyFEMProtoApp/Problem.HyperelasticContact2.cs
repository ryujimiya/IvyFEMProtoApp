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
        public void HyperelasticContactTDProblem2(MainWindow mainWindow, bool isMooney)
        {
            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 5.0));
                pts.Add(new OpenTK.Vector2d(0.0, 5.0));
                uint lId1 = cad.AddPolygon(pts).AddLId;
            }

            double eLen = 0.1;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint uQuantityId;
            uint lQuantityId;
            uint cQuantityId; // constraint
            {
                uint uDof = 2; // Vector2
                uint lDof = 1; // Scalar
                uint cDof = 1;
                uint uFEOrder = 1;
                uint lFEOrder = 1;
                uint cFEOrder = 1;
                System.Diagnostics.Debug.Assert(uFEOrder == cFEOrder);
                uQuantityId = world.AddQuantity(uDof, uFEOrder, FiniteElementType.ScalarLagrange);
                lQuantityId = world.AddQuantity(lDof, lFEOrder, FiniteElementType.ScalarLagrange);
                cQuantityId = world.AddQuantity(cDof, cFEOrder, FiniteElementType.ScalarLagrange);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point3;

            {
                world.ClearMaterial();
                uint maId = 0;
                if (isMooney)
                {
                    // Mooney-Rivlin
                    var ma = new MooneyRivlinHyperelasticMaterial();
                    ma.IsCompressible = false;
                    //ma.IsCompressible = true;
                    //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                    ma.C1 = 400;
                    ma.C2 = 400;
                    ma.GravityX = 0;
                    ma.GravityY = -1.0;
                    ma.MassDensity = 1.0;
                    maId = world.AddMaterial(ma);
                }
                else
                {
                    // Odgen
                    var ma = new OgdenHyperelasticMaterial();
                    //double[] alphas = { 1.3, 5.0, -2.0 };
                    //double[] mus = { 6300e3, 1.2e3, -10e3 };
                    double[] alphas = { 2.0, -2.0 };
                    double[] mus = { 800, -800 };
                    System.Diagnostics.Debug.Assert(alphas.Length == mus.Length);
                    ma.IsCompressible = false;
                    //ma.IsCompressible = true;
                    //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                    ma.SetAlphaMu(alphas.Length, alphas, mus);
                    ma.GravityX = 0;
                    ma.GravityY = -1.0;
                    ma.MassDensity = 1.0;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            CircleConstraint circleConstraint;
            {
                var cadIdTypes = new List<KeyValuePair<uint, CadElementType>>();
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(1, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(2, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(3, CadElementType.Edge));
                cadIdTypes.Add(new KeyValuePair<uint, CadElementType>(4, CadElementType.Edge));
                var pt = new OpenTK.Vector2d(0.0, -2.0);
                double r = 1.8;
                circleConstraint = new CircleConstraint(pt, r, EqualityType.GreaterEq);
                MultipointConstraint mpc = new MultipointConstraint(cadIdTypes, circleConstraint);
                world.AddMultipointConstraint(cQuantityId, mpc);
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
                ConstraintDrawer constraintDrawer = new ConstraintDrawer(circleConstraint);
                fieldDrawerArray.Add(constraintDrawer);
                var faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivativeType.Value, false, world);
                // Lagrange未定乗数のサーモグラフィ表示
                //var faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivativeType.Value, false, world,
                //    lValueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    uValueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                var edgeDrawer2 = new EdgeFieldDrawer(
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
            for (int iTime = 0; iTime <= 150; iTime++)
            {
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
