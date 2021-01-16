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
        public void ElasticTwoBodyContactProblem(MainWindow mainWindow, bool isStVenant)
        {
            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(-1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 2.0));
                pts.Add(new OpenTK.Vector2d(-1.0, 2.0));
                uint lId1 = cad.AddPolygon(pts).AddLId;
            }
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(-5.0, -3.0));
                pts.Add(new OpenTK.Vector2d(5.0, -3.0));
                pts.Add(new OpenTK.Vector2d(5.0, -2.0));
                pts.Add(new OpenTK.Vector2d(-5.0, -2.0));
                uint lId2 = cad.AddPolygon(pts).AddLId;
            }

            double eLen = 0.2;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint uQuantityId;
            uint cQuantityId;
            {
                uint uDof = 2; // Vector2
                uint uFEOrder = 1;
                uint cDof = 2; // Vector2 for mortar
                uint cFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder, FiniteElementType.ScalarLagrange);
                cQuantityId = world.AddQuantity(cDof, cFEOrder, FiniteElementType.ScalarLagrange);
            }

            {
                world.ClearMaterial();
                IList<uint> maIds = new List<uint>();
                if (isStVenant)
                {
                    {
                        var ma = new StVenantHyperelasticMaterial();
                        ma.Young = 100.0;
                        ma.Poisson = 0.3;
                        ma.GravityX = 0;
                        ma.GravityY = 0;
                        ma.MassDensity = 1.0;
                        uint maId = world.AddMaterial(ma);
                        maIds.Add(maId);
                    }
                    {
                        var ma = new StVenantHyperelasticMaterial();
                        ma.Young = 50.0;
                        ma.Poisson = 0.3;
                        ma.GravityX = 0;
                        ma.GravityY = 0;
                        ma.MassDensity = 1.0;
                        uint maId = world.AddMaterial(ma);
                        maIds.Add(maId);
                    }
                }
                else
                {
                    {
                        var ma = new LinearElasticMaterial();
                        ma.Young = 100.0;
                        ma.Poisson = 0.3;
                        ma.GravityX = 0;
                        ma.GravityY = 0;
                        ma.MassDensity = 1.0;
                        uint maId = world.AddMaterial(ma);
                        maIds.Add(maId);
                    }
                    {
                        var ma = new LinearElasticMaterial();
                        ma.Young = 50.0;
                        ma.Poisson = 0.3;
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
            }

            uint[] zeroEIds = { 5 };
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
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } }
                };
                var fixedCads = world.GetFieldFixedCads(uQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
                fixedCadXY = world.GetFieldFixedCads(uQuantityId)[0];
            }

            uint slaveEId = 1;
            uint masterEId = 7;
            IList<uint> contactSlaveEIds = world.GetContactSlaveEIds(cQuantityId);
            contactSlaveEIds.Add(slaveEId);
            IList<uint> contactMasterEIds = world.GetContactMasterEIds(cQuantityId);
            contactMasterEIds.Add(masterEId);

            world.MakeElements();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    uQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 0.05;
            for (int iTime = 0; iTime <= 100; iTime++)
            {
                double[] fixedValueXY = fixedCadXY.GetDoubleValues();
                fixedValueXY[0] = 0;
                fixedValueXY[1] = -2.25 * Math.Sin(t * 2.0 * Math.PI * 0.1);

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
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.ConvRatioToleranceForNonlinearIter = 1.0e-10;
                FEM.Solve();
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void ElasticTwoBodyContactTDProblem(MainWindow mainWindow, bool isStVenant)
        {
            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(-1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 0.0));
                pts.Add(new OpenTK.Vector2d(1.0, 2.0));
                pts.Add(new OpenTK.Vector2d(-1.0, 2.0));
                uint lId1 = cad.AddPolygon(pts).AddLId;
            }
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(-5.0, -3.0));
                pts.Add(new OpenTK.Vector2d(5.0, -3.0));
                pts.Add(new OpenTK.Vector2d(5.0, -2.0));
                pts.Add(new OpenTK.Vector2d(-5.0, -2.0));
                uint lId2 = cad.AddPolygon(pts).AddLId;
            }

            double eLen = 0.2;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint uQuantityId;
            uint cQuantityId;
            {
                uint uDof = 2; // Vector2
                uint uFEOrder = 1;
                uint cDof = 2; // Vector2 for mortar
                uint cFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder, FiniteElementType.ScalarLagrange);
                cQuantityId = world.AddQuantity(cDof, cFEOrder, FiniteElementType.ScalarLagrange);
            }

            {
                world.ClearMaterial();
                IList<uint> maIds = new List<uint>();
                if (isStVenant)
                {
                    {
                        var ma = new StVenantHyperelasticMaterial();
                        ma.Young = 2000.0;
                        ma.Poisson = 0.3;
                        ma.GravityX = 0;
                        ma.GravityY = -10.0;
                        ma.MassDensity = 1.0;
                        uint maId = world.AddMaterial(ma);
                        maIds.Add(maId);
                    }
                    {
                        var ma = new StVenantHyperelasticMaterial();
                        ma.Young = 2000.0;
                        ma.Poisson = 0.3;
                        ma.GravityX = 0;
                        ma.GravityY = 0;
                        ma.MassDensity = 1.0;
                        uint maId = world.AddMaterial(ma);
                        maIds.Add(maId);
                    }
                }
                else
                {
                    {
                        var ma = new LinearElasticMaterial();
                        ma.Young = 500.0;
                        ma.Poisson = 0.3;
                        ma.GravityX = 0;
                        ma.GravityY = -10.0;
                        ma.MassDensity = 1.0;
                        uint maId = world.AddMaterial(ma);
                        maIds.Add(maId);
                    }
                    {
                        var ma = new LinearElasticMaterial();
                        ma.Young = 500.0;
                        ma.Poisson = 0.3;
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

            uint valueId = 0;
            uint prevValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    uQuantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                //var edgeDrawer2 = new EdgeFieldDrawer(valueId, FieldDerivativeType.Value, true, world);
                //fieldDrawerArray.Add(edgeDrawer2);
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
                var FEM = new Elastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueId, prevValueId);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.IsOrderingToBandMatrix = true;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    //FEM.ConvRatioToleranceForNonlinearIter = 1.0e-10;
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
