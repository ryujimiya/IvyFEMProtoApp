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
        public void EMWaveguide3DPMLProblem1(MainWindow mainWindow, uint feOrder)
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

            double sFreq = 1.1;//1.0;
            double eFreq = 1.6;//2.0;
            int freqDiv = 50;

            // 形状設定で使用する単位長さ
            double unitLen = waveguideWidth / 20.0;
            // PML層の厚さ
            //double pmlThickness = 30 * unitLen;
            double pmlThickness = 10 * unitLen;
            // 導波管不連続領域の長さ
            //double disconLength0 = 1.0 * waveguideWidth; // 誘電体が格納できない
            double disconLength0 = 2.0 * waveguideWidth;
            double disconLength = disconLength0 + 2.0 * pmlThickness;
            // PML位置
            double port1PMLPosX = pmlThickness;
            double port2PMLPosX = disconLength - pmlThickness;
            // 励振位置 ポート1と一致させる
            // 観測点
            int port1OfsX = 5;
            int port2OfsX = 5;
            double refPort1PosX = port1PMLPosX + port1OfsX * unitLen;
            double refPort2PosX = port2PMLPosX - port2OfsX * unitLen;

            //---------------------------
            // 誘電体
            double wa1 = (0.556 / 2.0) * waveguideWidth;
            double wa2 = (0.888 / 2.0) * waveguideWidth;
            double wb1 = (0.399 / 2.0) * waveguideWidth;
            double wc1 = (0.8 / 2.0) * waveguideWidth;
            double pos1X = (disconLength - refPort1PosX - (disconLength - refPort2PosX) - wc1) / 2.0;
            double pos1Y = wa1;
            double pos2X = pos1X + wc1;
            double pos2Y = pos1Y + wa2;
            System.Diagnostics.Debug.Assert(pos1X > 0 && pos1Y > 0);

            double dielectricEp = 6.0;
            //---------------------------

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(port1PMLPosX, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(port1PMLPosX, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(port1PMLPosX, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(port1PMLPosX, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(refPort1PosX, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(refPort1PosX, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(refPort1PosX, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(refPort1PosX, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(refPort2PosX, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(refPort2PosX, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(refPort2PosX, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(refPort2PosX, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(port2PMLPosX, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(port2PMLPosX, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(port2PMLPosX, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(port2PMLPosX, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(disconLength, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(disconLength, waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector3d(disconLength, waveguideWidth, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(disconLength, 0.0, waveguideHeight));

                uint layerCnt = 5;
                cad.AddCubeWithMultiLayers(pts, layerCnt);
            }
            {
                Loop3D tmpLoop = cad.GetLoop(10);
                //cad.AddRectLoop(10, new OpenTK.Vector2d(pos1X, pos1Y), new OpenTK.Vector2d(pos2X, pos2Y));
                //座標の与え方はXDirに依存する!!!!!
                System.Diagnostics.Debug.Assert(Math.Abs(tmpLoop.XDir.Y - 1.0) < 1.0e-12); // Y方向がローカルXDir
                cad.AddRectLoop(10, new OpenTK.Vector2d(pos1Y, pos1X), new OpenTK.Vector2d(pos2Y, pos2X));
                cad.LiftLoop(27, cad.GetLoop(27).Normal * (+wb1));
            }
            {
                var eIds = new List<uint> { 45, 46, 47, 48 };
                cad.MakeRadialLoop(eIds);
            }
            System.Diagnostics.Debug.Assert(cad.IsElementId(CadElementType.Loop, 32)); // 32までのはず
            System.Diagnostics.Debug.Assert(!cad.IsElementId(CadElementType.Loop, 33)); // 32までのはず

            {
                IList<uint> lIds1 = new List<uint> {
                    1, 2, 3, 4, 5, 23
                };
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(lIds1, holes1, insideVIds1);

                IList<uint> lIds2 = new List<uint> {
                    23, 6, 7, 8, 9, 24
                };
                IList<OpenTK.Vector3d> holes2 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds2 = new List<uint>();
                uint sId2 = cad.AddSolid(lIds2, holes2, insideVIds2);

                IList<uint> lIds3 = new List<uint> {
                    24, 10,
                    27, 28, 29, 30, 31,
                    11, 12, 13, 25
                };
                IList<OpenTK.Vector3d> holes3 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds3 = new List<uint>();
                uint sId3 = cad.AddSolid(lIds3, holes3, insideVIds3);

                IList<uint> lIds4 = new List<uint> {
                    25, 14, 15, 16, 17, 26
                };
                IList<OpenTK.Vector3d> holes4 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds4 = new List<uint>();
                uint sId4 = cad.AddSolid(lIds4, holes4, insideVIds4);

                IList<uint> lIds5 = new List<uint> {
                    26, 18, 19, 20, 21, 22
                };
                IList<OpenTK.Vector3d> holes5 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds5 = new List<uint>();
                uint sId5 = cad.AddSolid(lIds5, holes5, insideVIds5);

                IList<uint> lIds6 = new List<uint> {
                    27, 28, 29, 30, 31, 32
                };
                IList<OpenTK.Vector3d> holes6 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds6 = new List<uint>();
                uint sId7 = cad.AddSolid(lIds6, holes6, insideVIds6);
            }
            /*
            // 電気壁
            cad.SetLoopColor(1, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(2, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(4, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(6, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(12, new double[] { 0.0, 0.0, 0.0 });
            // ポート
            cad.SetLoopColor(5, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(4, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(5, new double[] { 0.0, 0.0, 1.0 });

            cad.SetLoopColor(3, new double[] { 1.0, 0.0, 0.0 });
            cad.SetEdgeColor(2, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(6, new double[] { 0.0, 0.0, 1.0 });
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
                eLen = 0.10;//0.15;
            }
            else if (feOrder == 2)
            {
                eLen = 0.20;
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
                IList<uint> pmlMaIds = new List<uint>();
                uint vacuumMaId = 0;
                uint dielectricMaId = 0;

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
                DielectricPMLMaterial pmlMa1 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // X方向PML
                    OriginPoint3D = new OpenTK.Vector3d(port1PMLPosX, 0.0, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0,
                    ZThickness = 0.0
                };
                DielectricPMLMaterial pmlMa2 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // X方向PML
                    OriginPoint3D = new OpenTK.Vector3d(port2PMLPosX, 0.0, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0,
                    ZThickness = 0.0
                };
                DielectricMaterial dielectricMa = new DielectricMaterial
                {
                    Epxx = dielectricEp,
                    Epyy = dielectricEp,
                    Epzz = dielectricEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                vacuumMaId = world.AddMaterial(vacuumMa);
                dielectricMaId = world.AddMaterial(dielectricMa);
                uint pmlMaId1 = world.AddMaterial(pmlMa1);
                pmlMaIds.Add(pmlMaId1);
                uint pmlMaId2 = world.AddMaterial(pmlMa2);
                pmlMaIds.Add(pmlMaId2);

                uint[] sIds = { 2, 3, 4 };
                foreach (uint sId1 in sIds)
                {
                    world.SetCadSolidMaterial(sId1, vacuumMaId);
                }

                uint pmlsId1 = 1;
                uint pmlsId2 = 5;
                world.SetCadSolidMaterial(pmlsId1, pmlMaId1);
                world.SetCadSolidMaterial(pmlsId2, pmlMaId2);

                uint dielectricSId = 6;
                world.SetCadSolidMaterial(dielectricSId, dielectricMaId);

                uint[] lIds = { 24, 25, 24 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, vacuumMaId);
                }
            }

            int refPortCnt = 2;
            {
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                // 参照面1, 参照面2, 励振面の順
                uint[] lIds = { 24, 25, 24 };
                uint[][] dirEIdss =
                {
                    new uint[] { 17, 18 }, new uint[] { 25, 26 },
                    new uint[] { 17, 18 }
                };
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
                uint[] lIds = { 24, 25, 24 };
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

            uint[] zeroLIds = {
                2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 32 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint lId in zeroLIds)
            {
                // 複素数(辺方向成分)
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }
            uint[] scalarZeroLIds = {
                2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 32 };
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
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波数
                double k0 = normalizedFreq * Math.PI / waveguideWidth;
                // 角周波数
                double omega = k0 * Constants.C0;
                // 周波数
                double freq = omega / (2.0 * Math.PI);
                System.Diagnostics.Debug.WriteLine("2W/λ: " + normalizedFreq);

                var FEM = new EMWaveguide3DPMLFEM(world);
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
           }
        }
    }
}
