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
        public void HyperelasticTwoBodyContactTDProblem(MainWindow mainWindow, bool isMooney)
        {
            CadObject2D cad = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(-1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 2.0));
                pts.Add(new OpenTK.Vector2d(-1.0, 2.0));
                var res = cad.AddPolygon(pts);
            }
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(-5.0, -3.0));
                pts.Add(new OpenTK.Vector2d(5.0, -3.0));
                pts.Add(new OpenTK.Vector2d(5.0, -2.0));
                pts.Add(new OpenTK.Vector2d(-5.0, -2.0));
                var res = cad.AddPolygon(pts);
            }

            double eLen = 0.2;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint uQuantityId;
            uint lQuantityId;
            uint cQuantityId; // constraint
            {
                uint uDof = 2; // Vector2
                uint lDof = 1; // Scalar
                //uint cDof = 1; // Scalar
                uint cDof = 2; // Vector2 for mortar
                uint uFEOrder = 1;
                uint lFEOrder = 1;
                uint cFEOrder = 1;
                System.Diagnostics.Debug.Assert(uFEOrder == cFEOrder);
                uQuantityId = world.AddQuantity(uDof, uFEOrder, FiniteElementType.ScalarLagrange);
                lQuantityId = world.AddQuantity(lDof, lFEOrder, FiniteElementType.ScalarLagrange);
                cQuantityId = world.AddQuantity(cDof, cFEOrder, FiniteElementType.ScalarLagrange);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point3;

            world.ClearMaterial();
            IList<uint> maIds = new List<uint>();

            if (isMooney)
            {
                // Mooney-Rivlin
                {
                    var ma = new MooneyRivlinHyperelasticMaterial();
                    ma.IsCompressible = false;
                    //ma.IsCompressible = true;
                    //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                    ma.C1 = 400;
                    ma.C2 = 400;
                    ma.GravityX = 0;
                    ma.GravityY = -10.0;
                    ma.MassDensity = 1.0;
                    uint maId = world.AddMaterial(ma);
                    maIds.Add(maId);
                }
                {
                    var ma = new MooneyRivlinHyperelasticMaterial();
                    ma.IsCompressible = false;
                    //ma.IsCompressible = true;
                    //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                    ma.C1 = 200;
                    ma.C2 = 200;
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1.0;
                    uint maId = world.AddMaterial(ma);
                    maIds.Add(maId);
                }
            }
            else
            {
                // Odgen
                {
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
                    ma.GravityY = -10.0;
                    ma.MassDensity = 1.0;
                    uint maId = world.AddMaterial(ma);
                    maIds.Add(maId);
                }
                {
                    var ma = new OgdenHyperelasticMaterial();
                    //double[] alphas = { 1.3, 5.0, -2.0 };
                    //double[] mus = { 6300e3, 1.2e3, -10e3 };
                    double[] alphas = { 2.0, -2.0 };
                    double[] mus = { 400, -400 };
                    System.Diagnostics.Debug.Assert(alphas.Length == mus.Length);
                    ma.IsCompressible = false;
                    //ma.IsCompressible = true;
                    //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                    ma.SetAlphaMu(alphas.Length, alphas, mus);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1.0;
                    uint maId = world.AddMaterial(ma);
                    maIds.Add(maId);
                }
            }
            uint[] lIds = { 1, 2 };
            for (int i = 0; i < lIds.Length; i++)
            {
                world.SetCadLoopMaterial(lIds[i], maIds[i]);
            }

            uint[] zeroEIds = { 5 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(uQuantityId);
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                zeroFixedCads.Add(fixedCad);
            }

            uint slaveEId = 1;
            uint masterEId = 7;
            IList<uint> contactSlaveEIds = world.GetContactSlaveEIds(cQuantityId);
            contactSlaveEIds.Add(slaveEId);
            IList<uint> contactMasterEIds = world.GetContactMasterEIds(cQuantityId);
            contactMasterEIds.Add(masterEId);

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
            for (int iTime = 0; iTime <= 100; iTime++)
            {
                var FEM = new Hyperelastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    uValueId, prevUValueId, lValueId);
                /*
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
                */
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
                }
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-10;
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
