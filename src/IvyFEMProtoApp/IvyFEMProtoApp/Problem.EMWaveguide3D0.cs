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
        public void EMWaveguide3DProblem0(MainWindow mainWindow, uint feOrder)
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

            double waveguideWidth = 1.0;
            double waveguideHeight = 0.5;
            double disconLength = 1.0;
            double sFreq = 1.0;
            double eFreq = 2.0;
            int freqDiv = 50;

            IList<uint> shellLIds1;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(disconLength, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(disconLength, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(disconLength, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(disconLength, 0.0, waveguideHeight));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }
            {
                IList<uint> lIds1 = new List<uint>();
                {
                    foreach (uint lId in shellLIds1)
                    {
                        lIds1.Add(lId);
                    }
                }
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(lIds1, holes1, insideVIds1);
            }

            /*
            // 電気壁
            cad.SetLoopColor(2, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(3, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(4, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(5, new double[] { 0.0, 0.0, 0.0 });
            // ポート
            cad.SetLoopColor(1, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(1, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(2, new double[] { 0.0, 0.0, 1.0 });

            cad.SetLoopColor(6, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(9, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(10, new double[] { 0.0, 0.0, 1.0 });
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

            double eLen = 0.0;
            if (feOrder == 1)
            {
                //eLen = 0.05;
                //eLen = 0.20;
                //eLen = 0.10;
                //eLen = 0.15;
                eLen = 0.15;
            }
            else if (feOrder == 2)
            {
                eLen = 0.20;//0.25;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
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
            {
                uint dof1 = 1; // スカラー
                uint dof2 = 1;
                uint feOrder1 = feOrder;
                uint feOrder2 = feOrder;
                quantityId = world.AddQuantity(dof1, feOrder1, FiniteElementType.Edge);
                scalarQuantityId = world.AddQuantity(dof2, feOrder2, FiniteElementType.ScalarLagrange);
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
                uint maId1 = world.AddMaterial(vacuumMa);

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);

                uint[] lIds = { 1, 6 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId1);
                }
            }

            {
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                uint[] lIds = { 1, 6 };
                uint[][] dirEIdss = { new uint[] { 1, 2 }, new uint[] { 9, 10 } };
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
                uint[] lIds = { 1, 6 };
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

            uint[] zeroLIds = { 2, 3, 4, 5 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint lId in zeroLIds)
            {
                // 複素数(辺方向成分)
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }
            uint[] scalarZeroLIds = { 2, 3, 4, 5 };
            var scalarZeroFixedCads = world.GetZeroFieldFixedCads(scalarQuantityId);
            foreach (uint lId in scalarZeroLIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.ZScalar);
                scalarZeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            //-------------------------------------------------------------------
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
            //-------------------------------------------------------------------
            /*
            ////////////////////////////////////////////////////////////////////////////////////////////////
            // 断面の分布表示
            Cad2D cadA = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector2d(waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector2d(0.0, waveguideHeight));
                uint lIdA1 = cadA.AddPolygon(pts).AddLId;
            }
            double eLenA = eLen;
            Mesher2D mesherA = new Mesher2D(cadA, eLenA);

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
                DielectricMaterial maA1 = new DielectricMaterial();
                uint maIdA1 = worldA.AddMaterial(maA1);

                uint lIdA1 = 1;
                worldA.SetCadLoopMaterial(lIdA1, maIdA1);
            }

            worldA.MakeElements();

            uint valueIdA = 0;
            uint vecValueIdA = 0;
            VectorFieldDrawer vectorDrawerA;
            FaceFieldDrawer faceDrawerA;
            EdgeFieldDrawer edgeDrawerA;
            var fieldDrawerArrayA = mainWindow.FieldDrawerArrayA;
            {
                worldA.ClearFieldValue();
                // 複素数
                valueIdA = worldA.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityIdA, false, FieldShowType.ZReal);
                // Vector2
                vecValueIdA = worldA.AddFieldValue(FieldValueType.ZVector2, FieldDerivativeType.Value,
                    quantityIdA, true, FieldShowType.ZReal);
                faceDrawerA = new FaceFieldDrawer(valueIdA, FieldDerivativeType.Value, true, worldA,
                    valueIdA, FieldDerivativeType.Value);
                vectorDrawerA = new VectorFieldDrawer(
                    vecValueIdA, FieldDerivativeType.Value, worldA);
                edgeDrawerA = new EdgeFieldDrawer(
                    valueIdA, FieldDerivativeType.Value, true, false, worldA);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////
            */

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
                //Maximum = 1.0
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

            //------------------------------------------
            // 全体領域
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(vectorDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }
            //------------------------------------------
            /*
            //------------------------------------------
            // 断面
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArrayA.Clear();
                fieldDrawerArrayA.Add(faceDrawerA);
                fieldDrawerArrayA.Add(vectorDrawerA);
                fieldDrawerArrayA.Add(edgeDrawerA);
                mainWindow.Camera.Fit(fieldDrawerArrayA.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }
            //------------------------------------------
            */

            for (int iFreq = 0; iFreq < (freqDiv + 1); iFreq++)
            {
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波数
                double k0 = normalizedFreq * Math.PI / waveguideWidth;
                // 角周波数
                double omega = k0 * Constants.C0;
                // 周波数
                double freq = omega / (2.0 * Math.PI);
                System.Diagnostics.Debug.WriteLine("2W/λ: " + normalizedFreq);

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

                System.Numerics.Complex S11 = S[0][0];
                System.Numerics.Complex S21 = S[1][0];
                double S11Abs = S11.Magnitude;
                double S21Abs = S21.Magnitude;
                double total = S11Abs * S11Abs + S21Abs * S21Abs;

                string ret;
                string CRLF = System.Environment.NewLine;
                ret = "2W/λ: " + normalizedFreq + CRLF;
                ret += "|S11| = " + S11Abs + CRLF +
                      "|S21| = " + S21Abs + CRLF +
                      "|S11|^2 + |S21|^2 = " + total + CRLF;
                System.Diagnostics.Debug.WriteLine(ret);
                //AlertWindow.ShowDialog(ret, "");
                series1.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                series2.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();

                //-------------------------------------------------------------------
                // Exyzを表示用にスケーリングする
                {
                    double maxValue = 0;
                    int cnt = coordExyz.Length;
                    foreach (System.Numerics.Complex value in coordExyz)
                    {
                        double abs = value.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.4;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            coordExyz[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                world.UpdateBubbleFieldValueValuesFromCoordValues(vecValueId, FieldDerivativeType.Value, coordExyz);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                //-------------------------------------------------------------------

                /*
                ////////////////////////////////////////////////////////////////////////////////////////////////
                // 断面の分布表示
                // 平面の分布を取得する
                System.Numerics.Complex[] UtA;
                System.Numerics.Complex[] UzA;
                int portIdA = 0;
                //int portIdA = 1;
                double sectionXA = 0.0; // 断面の位置
                //double sectionXA = disconLength; // 断面の位置
                {
                    var eigenFEMA = FEM.EigenFEMs[portIdA];
                    int iModeA = 0;
                    var betaA = eigenFEMA.Betas[iModeA];
                    var etEVecA = eigenFEMA.EtEVecs[iModeA];
                    var ezEVecA = eigenFEMA.EzEVecs[iModeA];

                    uint coCntA = worldA.GetCoordCount(quantityIdA);
                    uint dofA = 2;
                    UtA = new System.Numerics.Complex[coCntA * dofA];
                    UzA = new System.Numerics.Complex[coCntA];
                    for (int coIdA = 0; coIdA < coCntA; coIdA++)
                    {
                        double[] co2DA = worldA.GetCoord(quantityIdA, coIdA);
                        double[] coA = { sectionXA, co2DA[0], co2DA[1] }; // YZ断面
                        //double[] coA = { co2DA[0], co2DA[1], sectionXA }; // XY断面
                        System.Numerics.Complex[] value =
                            eigenFEMA.CalcModeCoordExyzByCoord(coA, betaA, etEVecA, ezEVecA);
                        System.Diagnostics.Debug.Assert(value.Length == 3);
                        // x,yのみ抽出
                        for (int iDofA = 0; iDofA < dofA; iDofA++)
                        {
                            UtA[coIdA * dofA + iDofA] = value[iDofA];
                        }
                        // z
                        UzA[coIdA] = (1.0 / System.Numerics.Complex.ImaginaryOne) * value[2];
                    }
                }
                // UAを表示用にスケーリングする
                {
                    double maxValue = 0;
                    int cnt = UtA.Length;
                    foreach (System.Numerics.Complex value in UtA)
                    {
                        double abs = value.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.2;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            UtA[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                {
                    worldA.UpdateBubbleFieldValueValuesFromCoordValues(vecValueIdA, FieldDerivativeType.Value, UtA);
                    worldA.UpdateFieldValueValuesFromCoordValues(valueIdA, FieldDerivativeType.Value, UzA);

                    fieldDrawerArrayA.Update(worldA);
                    mainWindow.GLControl.Invalidate();
                    mainWindow.GLControl.Update();
                    WPFUtils.DoEvents();
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////
                */
            }
        }
    }
}
