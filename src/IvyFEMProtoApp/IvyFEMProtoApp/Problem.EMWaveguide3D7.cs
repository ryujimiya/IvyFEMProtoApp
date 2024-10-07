using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using IvyFEM;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void EMWaveguide3DProblem7(MainWindow mainWindow)
        {
            /////////////////////
            Dimension = 3; // 3次元
            var camera3D = mainWindow.Camera as Camera3D;
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 70.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            /////////////////////

            double wa = 1.0e+3;
            double cavityA = 280.0e-3 * wa;
            double cavityB = 285.0e-3 * wa;
            double cavityH = 200.0e-3 * wa;
            double waveguideWidth = 96.0e-3 * wa;
            //double waveguideHeight = 27.0e-3 * wa;
            double waveguideHeight = 48.0e-3 * wa;
            double inputWgLen = 100.0e-3 * wa;
            double inputWgPos1Y = 20.0e-3 * wa;
            double inputWgPos1Z = 20.0e-3 * wa;
            double inputWgPos2Y = inputWgPos1Y + waveguideWidth;
            double inputWgPos2Z = inputWgPos1Z + waveguideHeight;
            double dielectricA = 215.0e-3 * wa;
            double dielectricB = 146.0e-3 * wa;
            double dielectricH = 20.0e-3 * wa;
            double dielectricPos1X = (cavityA - dielectricA) / 2.0;
            double dielectricPos1Y = (cavityB - dielectricB) / 2.0;
            double dielectricPos2X = dielectricPos1X + dielectricA;
            double dielectricPos2Y = dielectricPos1Y + dielectricB;
            //System.Numerics.Complex dielectricEp = new System.Numerics.Complex(70.0, 0.0);//TEST
            System.Numerics.Complex dielectricEp = new System.Numerics.Complex(70.0, -10.0); /// 寒天
            //System.Numerics.Complex dielectricEp = new System.Numerics.Complex(5.0, -5.0 * 0.01); /// ガラス
            //System.Numerics.Complex dielectricEp = new System.Numerics.Complex(1.0, 0.0); /// 空気
            double sFreq = 2.45e+9 / wa;
            double eFreq = sFreq; // 1点だけ計算
            int freqDiv = 1; // 1点だけ計算

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(cavityA, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(cavityA, cavityB, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, cavityB, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, cavityH));
                pts.Add(new OpenTK.Vector3d(cavityA, 0.0, cavityH));
                pts.Add(new OpenTK.Vector3d(cavityA, cavityB, cavityH));
                pts.Add(new OpenTK.Vector3d(0.0, cavityB, cavityH));
                cad.AddCube(pts);
            }

            cad.AddRectLoop(
                1, new OpenTK.Vector2d(dielectricPos1X, dielectricPos1Y),
                new OpenTK.Vector2d(dielectricPos2X, dielectricPos2Y));
            cad.LiftLoop(7, cad.GetLoop(7).Normal * (+dielectricH));

            cad.AddRectLoop(
                5, new OpenTK.Vector2d(inputWgPos1Y, inputWgPos1Z),
                new OpenTK.Vector2d(inputWgPos2Y, inputWgPos2Z));
            cad.LiftLoop(12, cad.GetLoop(12).Normal * (-inputWgLen));

            {
                var eIds = new List<uint> { 13, 14, 15, 16 };
                cad.MakeRadialLoop(eIds);
            }

            System.Diagnostics.Debug.Assert(cad.IsElementId(CadElementType.Loop, 17)); // 17までのはず
            System.Diagnostics.Debug.Assert(!cad.IsElementId(CadElementType.Loop, 18)); // 17までのはず
            {
                IList<uint> lIds1 = new List<uint> {
                    1, 2, 3, 4, 5, 6,
                    7, 8, 9, 10, 11,
                    12, 13, 14, 15, 16
                };
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(lIds1, holes1, insideVIds1);

                IList<uint> lIds2 = new List<uint> {
                    7, 8, 9, 10, 11, 17
                };
                IList<OpenTK.Vector3d> holes2 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds2 = new List<uint>();
                uint sId2 = cad.AddSolid(lIds2, holes2, insideVIds2);
            }
            /*
            // 電気壁
            cad.SetLoopColor(1, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(2, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(3, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(4, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(5, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(6, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(17, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(13, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(14, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(15, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(16, new double[] { 0.0, 0.0, 0.0 });
            // ポート
            cad.SetLoopColor(12, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(33, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(34, new double[] { 0.0, 0.0, 1.0 });
            */

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad3DDrawer(cad);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            double eLen = 0.20 * waveguideWidth;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            var drawerArray1 = mainWindow.DrawerArray;
            drawerArray1.Clear();
            var drawer1 = new Mesher3DDrawer(mesher);
            drawer1.IsMask = true;
            mainWindow.DrawerArray.Add(drawer1);
            mainWindow.Camera.Fit(drawerArray1.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            uint scalarQuantityId; // ポートの固有値問題にスカラー節点が必要
            uint vector3QuantityId; // 表示用
            {
                uint dof1 = 1; // スカラー
                uint dof2 = 1;
                uint dof3 = 3;
                uint feOrder1 = 1;
                uint feOrder2 = 1;
                uint feOrder3 = 1;
                quantityId = world.AddQuantity(dof1, feOrder1, FiniteElementType.Edge);
                scalarQuantityId = world.AddQuantity(dof2, feOrder2, FiniteElementType.ScalarLagrange);
                vector3QuantityId = world.AddQuantity(dof3, feOrder3, FiniteElementType.ScalarLagrange);
            }

            {
                world.ClearMaterial();
                DielectricMaterial vacuumMa = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricMaterial dielectricMa = new DielectricMaterial
                {
                    ComplexEpxx = dielectricEp,
                    ComplexEpyy = dielectricEp,
                    ComplexEpzz = dielectricEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                uint maId1 = world.AddMaterial(vacuumMa);
                uint maId2 = world.AddMaterial(dielectricMa);

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);

                uint sId2 = 2;
                world.SetCadSolidMaterial(sId2, maId2);

                uint[] lIds = { 12 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId1);
                }
            }

            {
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                uint[] lIds = { 12 };
                uint[][] dirEIdss = { new uint[] { 33, 34 } };
                IList<IList<uint>> portLIdss = new List<IList<uint>>();
                foreach (uint lId in lIds)
                {
                    IList<uint> portLIds = new List<uint>();
                    {
                        portLIds.Add(lId);
                    }
                    portLIdss.Add(portLIds);
                }
                for (int portId = 0; portId < portLIdss.Count; portId++)
                {
                    IList<uint> portLIds = portLIdss[portId];
                    uint[] dirEIds = dirEIdss[portId];
                    PortCondition portCondition = new PortCondition(portLIds, CadElementType.Loop, FieldValueType.ZScalar);
                    portCondition.IntAdditionalParameters = new int[] { (int)dirEIds[0], (int)dirEIds[1] };
                    portConditions.Add(portCondition);
                }
            }
            {
                IList<PortCondition> portConditions = world.GetPortConditions(scalarQuantityId);
                uint[] lIds = { 12 };
                IList<IList<uint>> portLIdss = new List<IList<uint>>();
                foreach (uint lId in lIds)
                {
                    IList<uint> portLIds = new List<uint>();
                    {
                        portLIds.Add(lId);
                    }
                    portLIdss.Add(portLIds);
                }
                foreach (IList<uint> portLIds in portLIdss)
                {
                    PortCondition portCondition = new PortCondition(portLIds, CadElementType.Loop, FieldValueType.ZScalar);
                    portConditions.Add(portCondition);
                }
            }

            uint[] zeroLIds = { 1, 2, 3, 4, 5, 6, 17, 13, 14, 15, 16 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint lId in zeroLIds)
            {
                // 複素数(辺方向成分)
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }
            uint[] scalarZeroLIds = { 1, 2, 3, 4, 5, 6, 17, 13, 14, 15, 16 };
            var scalarZeroFixedCads = world.GetZeroFieldFixedCads(scalarQuantityId);
            foreach (uint lId in scalarZeroLIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.ZScalar);
                scalarZeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            uint valueId = 0;
            uint vecValueId = 0;
            VectorFieldDrawer vectorDrawer;
            EdgeFieldDrawer edgeDrawer;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数(辺方向成分)
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                // Vector3
                vecValueId = world.AddFieldValue(FieldValueType.ZVector3, FieldDerivativeType.Value,
                    quantityId, true, FieldShowType.ZReal);
                vectorDrawer = new VectorFieldDrawer(
                    vecValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////
            // 断面の分布表示
            Cad3D cadA = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, dielectricH));
                pts.Add(new OpenTK.Vector3d(cavityA, 0.0, dielectricH));
                pts.Add(new OpenTK.Vector3d(cavityA, cavityB, dielectricH));
                pts.Add(new OpenTK.Vector3d(0.0, cavityB, dielectricH));
                uint lIdA1 = cadA.AddPolygon(pts).AddLId;
            }
            double eLenA = eLen;
            Mesher3D mesherA = new Mesher3D(cadA, eLenA);

            FEWorld worldA = new FEWorld();
            worldA.Mesh = mesherA;
            uint quantityIdA;
            {
                uint dofA = 1; // スカラー
                uint feOrderA = 1;
                quantityIdA = worldA.AddQuantity(dofA, feOrderA, FiniteElementType.ScalarLagrange);
            }
            {
                // dummy
                DielectricMaterial maA1 = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                uint maIdA1 = worldA.AddMaterial(maA1);

                uint lIdA1 = 1;
                worldA.SetCadLoopMaterial(lIdA1, maIdA1);
            }

            worldA.MakeElements();

            uint valueIdA = 0;
            FaceFieldDrawer faceDrawerA;
            EdgeFieldDrawer edgeDrawerA;
            var fieldDrawerArrayA = mainWindow.FieldDrawerArrayA;
            {
                worldA.ClearFieldValue();
                // スカラー
                valueIdA = worldA.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    quantityIdA, false, FieldShowType.Real);
                faceDrawerA = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, worldA,
                    valueIdA, FieldDerivativeType.Value);
                edgeDrawerA = new EdgeFieldDrawer(
                    valueIdA, FieldDerivativeType.Value, true, false, worldA);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////

            /*
            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closing += ChartWindow1_Closing;
            }
            ChartWindow chartWin = ChartWindow1;
            chartWin.Owner = mainWindow;
            chartWin.Left = mainWindow.Left + mainWindow.Width;
            chartWin.Top = mainWindow.Top;
            chartWin.Show();
            chartWin.TextBox1.Text = "";
            var model = new PlotModel();
            chartWin.Plot.Model = model;
            model.Title = "Waveguide Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "2W/λ",
                Minimum = sFreq,
                Maximum = eFreq
            };
            var axis2 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "|S|",
                Minimum = 0.0,
                Maximum = 1.0
            };
            model.Axes.Add(axis1);
            model.Axes.Add(axis2);
            var series1 = new LineSeries
            {
                Title = "|S11|"
            };
            var series2 = new LineSeries
            {
                Title = "|S21|"
            };
            model.Series.Add(series1);
            model.Series.Add(series2);
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();
            */

            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(vectorDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }

            for (int iFreq = 0; iFreq < (freqDiv + 1); iFreq++)
            {
                double freq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                System.Diagnostics.Debug.WriteLine("freq: " + freq);

                var FEM = new EMWaveguide3DFEM(world);
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
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.COCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    //FEM.Solver = solver;
                }
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] E = FEM.E;
                System.Numerics.Complex[] coordExyz = FEM.CoordExyz;
                System.Numerics.Complex[][] S = FEM.S;

                // ポート1のみ
                System.Numerics.Complex S11 = S[0][0];
                double S11Abs = S11.Magnitude;
                double total = S11Abs * S11Abs;

                string ret;
                string CRLF = System.Environment.NewLine;
                ret = "freq: " + freq + CRLF;
                ret += "|S11| = " + S11Abs + CRLF +
                      "|S11|^2 = " + total + CRLF;
                System.Diagnostics.Debug.WriteLine(ret);
                if (AlertWindow1 != null)
                {
                    AlertWindow1.Close();
                }
                AlertWindow1 = new AlertWindow();
                AlertWindow1.Owner = mainWindow;
                AlertWindow1.Closing += AlertWindow1_Closing;
                AlertWindow1.Left = mainWindow.Left + mainWindow.Width;
                AlertWindow1.Top = mainWindow.Top;
                AlertWindow1.TextBox1.Text = ret;
                AlertWindow1.Show();
                /*
                series1.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                series2.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();
                */

                // Exyzを表示用にスケーリングする
                System.Numerics.Complex[] showCoordExyz = new System.Numerics.Complex[coordExyz.Length];
                coordExyz.CopyTo(showCoordExyz, 0);
                {
                    double maxValue = 0;
                    int cnt = showCoordExyz.Length;
                    foreach (System.Numerics.Complex value in showCoordExyz)
                    {
                        double abs = value.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.2 * cavityH;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            showCoordExyz[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                world.UpdateBubbleFieldValueValuesFromCoordValues(vecValueId, FieldDerivativeType.Value, showCoordExyz);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                ////////////////////////////////////////////////////////////////////////////////////////////////
                // 断面の分布表示
                WPFUtils.DoEvents(1000 * 5);
                {
                    fieldDrawerArray.Clear();
                    fieldDrawerArray.Add(edgeDrawer);
                    mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                    mainWindow.GLControl_ResizeProc();
                }
                {
                    mainWindow.IsFieldDraw = true;
                    fieldDrawerArrayA.Clear();
                    fieldDrawerArrayA.Add(faceDrawerA);
                    fieldDrawerArrayA.Add(edgeDrawerA);
                }
                // 平面の分布を取得する
                double[] UA;
                {
                    System.Numerics.Complex[] offsetCoordExyz;
                    {
                        int len = coordExyz.Length;
                        int offset = world.GetOffset(vector3QuantityId);
                        offsetCoordExyz = new System.Numerics.Complex[len + offset];
                        coordExyz.CopyTo(offsetCoordExyz, offset);
                    }

                    uint coCntA = worldA.GetCoordCount(quantityIdA);
                    UA = new double[coCntA]; // 電界の絶対値をとる
                    for (int coIdA = 0; coIdA < coCntA; coIdA++)
                    {
                        double[] coA = worldA.GetCoord(quantityIdA, coIdA);
                        System.Numerics.Complex[] value = 
                            world.GetComplexPointValueFromCoordValues(vector3QuantityId, coA, offsetCoordExyz);
                        if (value == null)
                        {
                            continue;
                        }
                        System.Diagnostics.Debug.Assert(value.Length == 3);
                        {
                            double absEx = value[0].Magnitude;
                            double absEy = value[1].Magnitude;
                            double absEz = value[2].Magnitude;
                            UA[coIdA] = Math.Sqrt(absEx * absEx + absEy * absEy + absEz * absEz);
                        }
                    }
                }
                {
                    worldA.UpdateFieldValueValuesFromCoordValues(valueIdA, FieldDerivativeType.Value, UA);

                    fieldDrawerArray.Update(world);
                    fieldDrawerArrayA.Update(worldA);
                    mainWindow.GLControl.Invalidate();
                    mainWindow.GLControl.Update();
                    WPFUtils.DoEvents();
                }
                ////////////////////////////////////////////////////////////////////////////////////////////////
            }
        }
    }
}
