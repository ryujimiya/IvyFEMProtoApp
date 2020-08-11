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
        public void ElasticLambWaveguideFirstOrderABCProblem1(MainWindow mainWindow)
        {
            double waveguideWidth = 1.0;
            double halfWaveguideWidth = waveguideWidth * 0.5;
            //double crackHalfTheta = 30.0 * Math.PI / 180.0;
            double crackHalfTheta = 5.0 * Math.PI / 180.0;
            //double crackH = 0.5 * halfWaveguideWidth; // fn=1のとき|S11|=0.4
            //double crackH = 0.8 * halfWaveguideWidth; // fn=1のとき|S11|=0.7
            double crackH = 0.8/*0.5*/ * halfWaveguideWidth;
            double crackHalfW = crackH * Math.Tan(crackHalfTheta);

            //double disconLength = 2.0 * waveguideWidth;
            //double disconLength = 1.0 * waveguideWidth;
            //double disconLength = (1.0 / 2.0) * waveguideWidth;
            //double disconLength = (1.0 / 4.0) * waveguideWidth; 
            //double disconLength = (1.0 / 8.0) * waveguideWidth;
            //double disconLength = (1.0 / 16.0) * waveguideWidth;
            //double disconLength = /*(1.0 / 8.0)*/(1.0 / 6.0) * waveguideWidth;
            double disconLength = (1.0 / 6.0) * waveguideWidth;
            System.Diagnostics.Debug.Assert(disconLength >= crackHalfW * 2.0 * 1.05);
            double crackPosX0 = disconLength / 2.0; // 中央
            double crackPosX1 = crackPosX0 - crackHalfW;
            double crackPosX2 = crackPosX0 + crackHalfW;
            double crackPosY0 = halfWaveguideWidth - crackH;

            double sFreq = 0.0;
            //double eFreq = 2.0; // ほぼ基本モードのみの周波数領域
            double eFreq = 1.0;
            int freqDiv = 50;

            // 半分の領域
            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, halfWaveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, halfWaveguideWidth));
                pts.Add(new OpenTK.Vector2d(crackPosX2, halfWaveguideWidth));
                pts.Add(new OpenTK.Vector2d(crackPosX0, crackPosY0));
                pts.Add(new OpenTK.Vector2d(crackPosX1, halfWaveguideWidth));
                uint lId1 = cad.AddPolygon(pts).AddLId;
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad2DDrawer(cad);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            //double eLen = halfWaveguideWidth * (1.0 / 40.0) * 0.95;
            //double eLen = halfWaveguideWidth * (1.0 / 20.0) * 0.95;
            //double eLen = halfWaveguideWidth * (1.0 / 10.0) * 0.95;
            //double eLen = halfWaveguideWidth * (2.0 / 10.0) * 0.95;
            double eLen = halfWaveguideWidth * (1.0 / 10.0) * 0.95;
            Mesher2D mesher = new Mesher2D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            var meshDrawer = new Mesher2DDrawer(mesher);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            int uDof = 2;
            int sDof = 2;
            uint uQuantityId;
            {
                uint uFEOrder = 2;//1;
                uQuantityId = world.AddQuantity((uint)uDof, uFEOrder, FiniteElementType.ScalarLagrange);
            }

            uint eId1 = 1;
            uint eId2 = 3;
            uint eIdSrc = eId1;
            int loopCnt = 1;
            double rho;
            double lambda;
            double mu;
            {
                world.ClearMaterial();
                LinearElasticMaterial substrateMa = new LinearElasticMaterial
                {
                    IsPlainStressLame = true,
                    MassDensity = 1.0e+0,
                    Young = 1.0e+0,
                    Poisson = 0.31//0.31
                };
                uint maId = world.AddMaterial(substrateMa);

                rho = substrateMa.MassDensity;
                lambda = substrateMa.LameLambda;
                mu = substrateMa.LameMu;

                uint[] eIds = { eId1, eId2, eIdSrc };
                foreach (uint eId in eIds)
                {
                    world.SetCadEdgeMaterial(eId, maId);
                }
                for (int i = 0; i < loopCnt; i++)
                {
                    uint lId = (uint)(i + 1);
                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            int incidentModeIndex = 0;
            int portCnt;
            {
                IList<PortCondition> uPortConditions = world.GetPortConditions(uQuantityId);

                world.SetIncidentPortId(uQuantityId, 0);
                world.SetIncidentModeId(uQuantityId, incidentModeIndex);

                uint[] eIds = { eId1, eId2, eIdSrc };
                double[] normalX = { -1.0, 1.0, -1.0 };
                IList<IList<uint>> portEIdss = new List<IList<uint>>();
                foreach (uint eId in eIds)
                {
                    IList<uint> portEIds = new List<uint>();
                    {
                        portEIds.Add(eId);
                    }
                    portEIdss.Add(portEIds);
                }
                for (int portId = 0; portId < eIds.Length; portId++)
                {
                    IList<uint> portEIds = portEIdss[portId];
                    IList<uint> fixedDofIndexs = new List<uint>();
                    IList<System.Numerics.Complex> fixedValues = new List<System.Numerics.Complex>();
                    uint additionalParameterDof = 1; // for normalX
                    PortCondition portCondition = new ConstPortCondition(
                        portEIds, FieldValueType.ZVector2, fixedDofIndexs, fixedValues, additionalParameterDof);
                    portCondition.GetComplexAdditionalParameters()[0] = normalX[portId];
                    uPortConditions.Add(portCondition);
                }
                portCnt = uPortConditions.Count;
                portCnt = portCnt - 1; // 励振源を引く
            }

            /*
            uint[] zeroEIds = { };
            var uZeroFixedCads = world.GetZeroFieldFixedCads(uQuantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZVector2);
                uZeroFixedCads.Add(fixedCad);
            }
            */

            // mid-planeの境界条件 (uy = 0, σxy=0)
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<System.Numerics.Complex> { 0.0 } }
                };
                var fixedCads = world.GetFieldFixedCads(uQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // ZVector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.ZVector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }
            // 外側領域（真空）との境界
            // (σ・n=0 --> σxy = 0、[σyy=0])

            world.MakeElements();

            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closing += ChartWindow1_Closing;
            }
            if (ChartWindow2 == null)
            {
                ChartWindow2 = new ChartWindow();
                ChartWindow2.Closing += ChartWindow2_Closing;
            }
            if (ChartWindow3 == null)
            {
                ChartWindow3 = new ChartWindow();
                ChartWindow3.Closing += ChartWindow3_Closing;
            }
            ChartWindow chartWin1 = ChartWindow1;
            chartWin1.Owner = mainWindow;
            chartWin1.Left = mainWindow.Left + mainWindow.Width;
            chartWin1.Top = mainWindow.Top;
            chartWin1.Show();
            chartWin1.TextBox1.Text = "";
            var model1 = new PlotModel();
            chartWin1.Plot.Model = model1;
            model1.Title = "Waveguide Example";
            var axis11 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "ks W/π",
                Minimum = sFreq,
                Maximum = eFreq
            };
            var axis12 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "|S|",
                Minimum = 0.0,
                Maximum = 1.0
            };
            model1.Axes.Add(axis11);
            model1.Axes.Add(axis12);
            var series11 = new LineSeries
            {
                Title = "|S11|"
            };
            var series12 = new LineSeries
            {
                Title = "|S21|"
            };
            model1.Series.Add(series11);
            model1.Series.Add(series12);
            model1.InvalidatePlot(true);
            WPFUtils.DoEvents();

            ChartWindow chartWin2 = ChartWindow2;
            chartWin2.Owner = mainWindow;
            chartWin2.Left = mainWindow.Left + mainWindow.Width;
            chartWin2.Top = mainWindow.Top + chartWin1.Height;
            chartWin2.Show();
            chartWin2.TextBox1.Text = "";
            var model2 = new PlotModel();
            chartWin2.Plot.Model = model2;
            model2.Title = "Waveguide Example";
            var axis21 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "ks W/π",
                Minimum = sFreq,
                Maximum = eFreq
            };
            var axis22 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "β/ks",
                Minimum = 0.0,
                //Maximum = 1.0
            };
            model2.Axes.Add(axis21);
            model2.Axes.Add(axis22);
            var series21 = new LineSeries
            {
                //Title = "β/ks",
                LineStyle = LineStyle.None,
                MarkerType = MarkerType.Circle
            };
            model2.Series.Add(series21);
            model2.InvalidatePlot(true);
            WPFUtils.DoEvents();

            ChartWindow chartWin3 = ChartWindow3;
            chartWin3.Owner = mainWindow;
            chartWin3.Left = mainWindow.Left + mainWindow.Width + chartWin2.Width;
            chartWin3.Top = mainWindow.Top + chartWin1.Height;
            chartWin3.Show();
            chartWin3.TextBox1.Text = "";
            var model3 = new PlotModel();
            chartWin3.Plot.Model = model3;
            model3.Title = "Waveguide Example";
            var axis31 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "y",
                //Minimum = 0.0,
                //Maximum = waveguideWidth
            };
            var axis32 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "u",
                //Minimum = -1.0,
                //Maximum = 1.0
            };
            model3.Axes.Add(axis31);
            model3.Axes.Add(axis32);
            var series31 = new LineSeries
            {
                Title = "Re(ux)",
                //LineStyle = LineStyle.None,
                //MarkerType = MarkerType.Circle
            };
            var series32 = new LineSeries
            {
                Title = "Im(ux)",
                //LineStyle = LineStyle.None,
                //MarkerType = MarkerType.Circle
            };
            var series33 = new LineSeries
            {
                Title = "Re(uy)",
                //LineStyle = LineStyle.None,
                //MarkerType = MarkerType.Circle
            };
            var series34 = new LineSeries
            {
                Title = "Im(uy)",
                //LineStyle = LineStyle.None,
                //MarkerType = MarkerType.Circle
            };
            model3.Series.Add(series31);
            model3.Series.Add(series32);
            model3.Series.Add(series33);
            model3.Series.Add(series34);
            model3.InvalidatePlot(true);
            WPFUtils.DoEvents();

            uint valueId = 0;
            uint bubbleUValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.ZVector2, FieldDerivativeType.Value,
                    uQuantityId, false, FieldShowType.ZReal);
                // Vector2
                bubbleUValueId = world.AddFieldValue(FieldValueType.ZVector2, FieldDerivativeType.Value,
                    uQuantityId, true, FieldShowType.ZReal);

                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var vectorDrawer = new VectorFieldDrawer(
                    bubbleUValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                //var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                //    valueId, FieldDerivativeType.Value);
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            for (int iFreq = 0; iFreq < (freqDiv + 1); iFreq++)
            {

                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                //!! 解けない
                if (normalizedFreq < 1.0e-12)
                {
                    continue;
                }
                // 波数
                double ks = normalizedFreq * Math.PI / waveguideWidth;
                // 角周波数
                double omega = ks * Math.Sqrt(mu / rho); 
                // 周波数
                double freq = omega / (2.0 * Math.PI);
                System.Diagnostics.Debug.WriteLine("ω√(ρ/μ) W/π: " + normalizedFreq);

                var FEM = new ElasticLambWaveguide2DFirstOrderABCFEM(world);
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
                System.Numerics.Complex[] U = FEM.U;
                System.Numerics.Complex[] coordU = FEM.CoordU;
                System.Numerics.Complex[] coordSigma = FEM.CoordSigma;
                System.Numerics.Complex[][] S = FEM.S;

                int coCnt = coordU.Length / uDof;
                //-----------------------------------------------
                // 表示用に hat uy = -juy, hatσxx = jσxxに変換
                {
                    for (int iCoId = 0; iCoId < coCnt; iCoId++)
                    {
                        // hat uy
                        coordU[iCoId * uDof + 1] *= -1.0 * System.Numerics.Complex.ImaginaryOne;

                        // hat σxx
                        coordSigma[iCoId * sDof] *= System.Numerics.Complex.ImaginaryOne;
                    }
                }

                //-----------------------------------------------
                // ベクトルを表示用にスケーリング
                // uを表示用にスケーリングする
                {
                    double maxValue = 0;
                    int cnt = coordU.Length;
                    foreach (System.Numerics.Complex value in coordU)
                    {
                        double abs = value.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.1 * halfWaveguideWidth;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            coordU[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                // σを表示用にスケーリングする
                {
                    double maxValue = 0;
                    int cnt = coordSigma.Length;
                    foreach (System.Numerics.Complex value in coordSigma)
                    {
                        double abs = value.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.1 * waveguideWidth;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            coordSigma[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                //-----------------------------------------------

                // eigen
                System.Numerics.Complex[][] portBetas = new System.Numerics.Complex[portCnt][];
                // hat u
                System.Numerics.Complex[][][] portHUEVecs = new System.Numerics.Complex[portCnt][][];
                for (int portId = 0; portId < portCnt; portId++)
                {
                    var eigenFEM = FEM.EigenFEMs[portId];
                    portBetas[portId] = eigenFEM.Betas;
                    portHUEVecs[portId] = eigenFEM.HUEVecs;

                    int modeCnt = portBetas[portId].Length;
                    // DEBUG
                    //for (int iMode = 0; iMode < modeCnt; iMode++)
                    //{
                    //    System.Numerics.Complex beta = portBetas[portId][iMode];
                    //    System.Diagnostics.Debug.WriteLine("β[{0}]:{1}", iMode, beta);
                    //}

                    if (portId == 0)
                    {
                        for (int iMode = 0; iMode < modeCnt; iMode++)
                        {
                            System.Numerics.Complex beta = portBetas[portId][iMode];
                            if (ks < Constants.PrecisionLowerLimit)
                            {
                                continue;
                            }
                            series21.Points.Add(new DataPoint(normalizedFreq, beta.Real / ks));
                        }
                        model2.InvalidatePlot(true);
                        WPFUtils.DoEvents();

                        if (modeCnt > incidentModeIndex)
                        {
                            series31.Points.Clear();
                            series32.Points.Clear();
                            series33.Points.Clear();
                            series34.Points.Clear();
                            int iMode = incidentModeIndex;
                            System.Numerics.Complex[] uEVec = portHUEVecs[portId][iMode];
                            int portNodeCnt = uEVec.Length / uDof;
                            for (int portNodeId = 0; portNodeId < portNodeCnt; portNodeId++)
                            {
                                int coId = world.PortNode2Coord(uQuantityId, (uint)portId, portNodeId);
                                double[] coord = world.GetCoord(uQuantityId, coId);
                                double ptX = coord[0] / halfWaveguideWidth;
                                double ptY = coord[1] / halfWaveguideWidth;
                                System.Numerics.Complex ux = uEVec[portNodeId * uDof];
                                System.Numerics.Complex uy = uEVec[portNodeId * uDof + 1];
                                series31.Points.Add(new DataPoint(ptY, ux.Real));
                                series32.Points.Add(new DataPoint(ptY, ux.Imaginary));
                                series33.Points.Add(new DataPoint(ptY, uy.Real));
                                series34.Points.Add(new DataPoint(ptY, uy.Imaginary));
                            }
                        }
                        model3.InvalidatePlot(true);
                        WPFUtils.DoEvents();
                    }
                }

                // eigen
                int targetModeId = incidentModeIndex;
                int nodeCnt = (int)world.GetNodeCount(uQuantityId);
                System.Numerics.Complex[] eigenU = new System.Numerics.Complex[nodeCnt];
                for (int portId = 0; portId < portCnt; portId++)
                {
                    int modeCnt = portBetas[portId].Length;
                    if (targetModeId >= modeCnt)
                    {
                        System.Diagnostics.Debug.WriteLine("No propagation mode found at port: " + portId);
                        continue;
                    }
                    System.Numerics.Complex beta = portBetas[portId][targetModeId];
                    System.Numerics.Complex[] eVec = portHUEVecs[portId][targetModeId];
                    int portNodeCnt = (int)world.GetPortNodeCount(uQuantityId, (uint)portId);
                    System.Diagnostics.Debug.Assert(portNodeCnt * uDof == eVec.Length);
                    for (int portNodeId = 0; portNodeId < portNodeCnt; portNodeId++)
                    {
                        int coId = world.PortNode2Coord(uQuantityId, (uint)portId, portNodeId);
                        int nodeId = world.Coord2Node(uQuantityId, coId);
                        System.Numerics.Complex value = eVec[portNodeId];
                        eigenU[nodeId] = value;
                    }
                }

                if (S[0].Length > 0 && S[1].Length > 0)
                {
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
                    series11.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                    series12.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                    model1.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }

                // eigen
                //world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, eigenU);
                // U
                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, U);
                world.UpdateBubbleFieldValueValuesFromCoordValues(bubbleUValueId, FieldDerivativeType.Value, coordU);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
