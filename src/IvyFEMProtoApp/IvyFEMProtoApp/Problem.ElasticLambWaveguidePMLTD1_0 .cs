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
        public void ElasticLambWaveguidePMLTDProblem1_0(MainWindow mainWindow)
        {
            double[] freqs;
            System.Numerics.Complex[] freqDomainAmpsInc;
            SolveElasticLambWaveguidePMLTDProblem1_0(
                mainWindow, out freqs, out freqDomainAmpsInc);
        }

        public void SolveElasticLambWaveguidePMLTDProblem1_0(
            MainWindow mainWindow,
            out double[] retFreqs,
            out System.Numerics.Complex[] retFreqDomainAmpsInc)
        {
            retFreqs = null;
            retFreqDomainAmpsInc = null;

            double waveguideWidth = 1.0;
            double halfWaveguideWidth = waveguideWidth * 0.5;

            //double disconLength = 2.0 * waveguideWidth;
            //double disconLength = 1.0 * waveguideWidth;
            //double disconLength = (1.0 / 2.0) * waveguideWidth;
            //double disconLength = (1.0 / 4.0) * waveguideWidth; 
            //double disconLength = (1.0 / 8.0) * waveguideWidth;
            //double disconLength = (1.0 / 16.0) * waveguideWidth;
            double disconLength = 1.0 * waveguideWidth;

            // 形状設定で使用する単位長さ
            double unitLen = waveguideWidth / 20.0;
            // PML層の厚さ
            //double pmlThicknessX1 = 10.0 * unitLen;
            //double pmlThicknessX1 = 20.0 * unitLen;
            //double pmlThicknessX1 = 30.0 * unitLen;
            //double pmlThicknessX1 = 40.0 * unitLen;
            //double pmlThicknessX1 = 45.0 * unitLen;
            double pmlThicknessX1 = 45.0 * unitLen;
            //double pmlThicknessX2 = 10.0 * unitLen;
            //double pmlThicknessX2 = 20.0 * unitLen;
            //double pmlThicknessX2 = 25.0 * unitLen;
            //double pmlThicknessX2 = 30.0 * unitLen;
            //double pmlThicknessX2 = 40.0 * unitLen;
            //double pmlThicknessX2 = 45.0 * unitLen;
            double pmlThicknessX2 = 45.0 * unitLen;
            // 参照位置
            double refXDistance = (1.0 / 16.0) * waveguideWidth;

            // 導波路不連続領域の長さ(加算）
            disconLength += pmlThicknessX1 + pmlThicknessX2;
            // 導波路不連続領域の長さ(加算）
            disconLength += 2.0 * refXDistance;
            // PML位置
            double port1PMLPosX = pmlThicknessX1;
            double port2PMLPosX = disconLength - pmlThicknessX2;
            // 参照位置
            double port1RefPosX = port1PMLPosX + refXDistance;
            double port2RefPosX = port2PMLPosX - refXDistance;

            //double eLen = halfWaveguideWidth * (1.0 / 40.0) * 0.95;
            //double eLen = halfWaveguideWidth * (1.0 / 20.0) * 0.95;
            //double eLen = halfWaveguideWidth * (1.0 / 10.0) * 0.95;
            //double eLen = halfWaveguideWidth * (2.0 / 10.0) * 0.95;
            //double eLen = halfWaveguideWidth * (1.0 / 10.0) * 0.95; // 基準
            double eLen = halfWaveguideWidth * (2.0 / 10.0) * 0.95; // 粗いメッシュ

            // 時間刻み幅の算出
            //double courantNumber = 0.25;
            //double courantNumber = 0.5;
            //double courantNumber = 1.0; // 1.0以下が望ましい
            double courantNumber = 1.0;
            // Note: timeLoopCnt は 2^mでなければならない
            //int timeLoopCnt = 2048;
            //int timeLoopCnt = 1024;
            int timeLoopCnt = 2048;
            // 励振源
            // 規格化周波数
            double srcNormalizedFreq = 0.5;
            // 計算する周波数領域
            double normalizedFreq1 = 0.0;
            double normalizedFreq2 = 1.0;

            // ガウシアンパルス
            GaussianType gaussianType = GaussianType.SinModulation; // 正弦波変調
            // 搬送波の振動回数
            //int nCycle = 5;
            //int nCycle = 1; // 素のガウシアン
            int nCycle = 1;
            // ガウシアンパルスの振幅
            double gaussianAmp = 1.0;

            uint loopCnt = 5;
            uint[] pmlLIds1 = { 1 };
            uint[] pmlLIds2 = { 5 };
            uint[][] pmlLIdss = { pmlLIds1, pmlLIds2 };
            uint eIdRef1 = 14;
            uint eIdRef2 = 15;
            //uint eIdSrc = eIdRef1;// 入射面と参照面を一致させる
            uint eIdSrc = 13;// PMLとの境界

            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, halfWaveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PMLPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port1RefPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port2RefPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port2PMLPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, halfWaveguideWidth));
                pts.Add(new OpenTK.Vector2d(port2PMLPosX, halfWaveguideWidth));
                pts.Add(new OpenTK.Vector2d(port2RefPosX, halfWaveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1RefPosX, halfWaveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1PMLPosX, halfWaveguideWidth));
                uint _lId1 = cad.AddPolygon(pts).AddLId;
                uint _lId2 = cad.ConnectVertexLine(3, 12).AddLId;
                uint _lId3 = cad.ConnectVertexLine(4, 11).AddLId;
                uint _lId4 = cad.ConnectVertexLine(5, 10).AddLId;
                uint _lId5 = cad.ConnectVertexLine(6, 9).AddLId;
            }

            // check
            {
                double[] pmlColor = { 0.5, 0.5, 0.5 };
                foreach (uint[] lIds in pmlLIdss)
                {
                    foreach (uint lId in lIds)
                    {
                        cad.SetLoopColor(lId, pmlColor);
                    }
                }
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
            mainWindow.DoZoom(10, true);
            WPFUtils.DoEvents();

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

            double rho = 1.0e+0;
            double E = 1.0e+0;
            double nu = 0.31;
            double lambda;
            double mu;
            //double reflection0X1 = 1.0e-8;
            //double reflection0X1 = 1.0e-4; // PML終端の発散抑制
            //double reflection0X1 = 1.0e-2;
            double reflection0X1 = 1.0e-2;
            //double reflection0X2 = 1.0e-8;
            //double reflection0X2 = 1.0e-4; // PML終端の発散抑制
            //double reflection0X2 = 1.0e-2;
            double reflection0X2 = 1.0e-2;
            //double scalingfactor0 = 1.0;
            //double scalingfactor0 = 2.0; // stretch こちらを使う
            //double scalingfactor0 = 0.5; // compress
            //double scalingfactor0 = 4.0;
            double scalingfactor0 = 1.0;
            uint substrateMaId;
            IList<uint> pmlMaIds = new List<uint>();
            {
                world.ClearMaterial();
                LinearElasticMaterial substrateMa = new LinearElasticMaterial
                {
                    IsPlainStressLame = true,
                    MassDensity = rho,
                    Young = E,
                    Poisson = nu
                };
                LinearElasticPMLMaterial pmlMa1 = new LinearElasticPMLMaterial
                {
                    IsPlainStressLame = true,
                    MassDensity = rho,
                    Young = E,
                    Poisson = nu,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, 0.0),
                    XThickness = pmlThicknessX1,
                    YThickness = 0.0,
                    Reflection0 = reflection0X1,
                    ScalingFactor0 = scalingfactor0
                };
                LinearElasticPMLMaterial pmlMa2 = new LinearElasticPMLMaterial
                {
                    IsPlainStressLame = true,
                    MassDensity = rho,
                    Young = E,
                    Poisson = nu,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port2PMLPosX, 0.0),
                    XThickness = pmlThicknessX2,
                    YThickness = 0.0,
                    Reflection0 = reflection0X2,
                    ScalingFactor0 = scalingfactor0
                };

                substrateMaId = world.AddMaterial(substrateMa);
                uint pmlMaId1 = world.AddMaterial(pmlMa1);
                pmlMaIds.Add(pmlMaId1);
                uint pmlMaId2 = world.AddMaterial(pmlMa2);
                pmlMaIds.Add(pmlMaId2);

                rho = substrateMa.MassDensity;
                lambda = substrateMa.LameLambda;
                mu = substrateMa.LameMu;

                System.Diagnostics.Debug.Assert(pmlLIdss.Length == pmlMaIds.Count);

                uint[] eIds = { eIdRef1, eIdRef2, eIdSrc };
                foreach (uint eId in eIds)
                {
                    uint maId = substrateMaId;
                    world.SetCadEdgeMaterial(eId, maId);
                }
                for (int i = 0; i < loopCnt; i++)
                {
                    uint lId = (uint)(i + 1);
                    uint maId = substrateMaId;
                    int hitPMLIndex = -1;
                    for (int pmlIndex = 0; pmlIndex < pmlLIdss.Length; pmlIndex++)
                    {
                        uint[] lIds = pmlLIdss[pmlIndex];
                        if (lIds.Contains(lId))
                        {
                            hitPMLIndex = pmlIndex;
                            break;
                        }
                    }
                    if (hitPMLIndex != -1)
                    {
                        maId = pmlMaIds[hitPMLIndex];
                    }
                    else
                    {
                        maId = substrateMaId;
                    }

                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            // P波の速度
            double PWaveVelocity = Math.Sqrt((lambda + 2.0 * mu) / rho);
            // 時刻刻み幅
            double timeStep = courantNumber * eLen / (PWaveVelocity * Math.Sqrt(2.0));
            // 励振源
            // 波数
            double srcKs = srcNormalizedFreq * Math.PI / waveguideWidth;
            // 角周波数
            double srcOmega = srcKs * Math.Sqrt(mu / rho);
            // 周波数
            double srcFreq = srcOmega / (2.0 * Math.PI);
            // 計算する周波数領域
            // 波数
            double ks1 = normalizedFreq1 * Math.PI / waveguideWidth;
            double ks2 = normalizedFreq2 * Math.PI / waveguideWidth;
            // 角周波数
            double omega1 = ks1 * Math.Sqrt(mu / rho);
            double omega2 = ks2 * Math.Sqrt(mu / rho);
            // 周波数
            double freq1 = omega1 / (2.0 * Math.PI);
            double freq2 = omega2 / (2.0 * Math.PI);
            // 規格化周波数変換
            Func<double, double> toNormalizedFreq = freq =>
            {
                double omega = freq * (2.0 * Math.PI);
                double ks = omega * Math.Sqrt(rho / mu);
                double normalizedFreq = ks * waveguideWidth / Math.PI;
                return normalizedFreq;
            };

            // ガウシアンパルス
            double gaussianT0 = 1.00 * (1.0 / srcFreq) * nCycle / 2.0;
            double gaussianTp = gaussianT0 / (2.0 * Math.Sqrt(2.0 * Math.Log(2.0)));

            int portCnt;
            {
                IList<PortCondition> uPortConditions = world.GetPortConditions(uQuantityId);

                uint[] eIds = { eIdRef1, eIdRef2, eIdSrc };
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
                    IList<double> fixedValues = new List<double>();
                    uint additionalParameterDof = 1; // for normalX
                    PortCondition portCondition = new ConstPortCondition(
                        portEIds, FieldValueType.Vector2, fixedDofIndexs, fixedValues, additionalParameterDof);
                    portCondition.GetDoubleAdditionalParameters()[0] = normalX[portId];
                    uPortConditions.Add(portCondition);
                }
                portCnt = uPortConditions.Count;
                portCnt = portCnt - 1; // 励振源を引く
            }

            /*
            uint[] zeroEIds = {  };
            var zeroFixedCads = world.GetZeroFieldFixedCads(uQuantityId);
            foreach (uint eId in zeroEIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                zeroFixedCads.Add(fixedCad);
            }
            */

            // mid-planeの境界条件 (uy = 0, σxy=0)
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)4, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)5, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)6, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 1 }, Values = new List<double> { 0.0 } }
                };
                var fixedCads = world.GetFieldFixedCads(uQuantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Vector2
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }
            // 外側領域（真空）との境界
            // (σ・n=0 --> σxy = 0、[σyy=0])

            //!!!!!!!!!!
            // PML終端
            // ux = uy = 0
            {
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)1, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0, 1 }, Values = new List<double> { 0.0, 0.0 } },
                    new { CadId = (uint)7, CadElemType = CadElementType.Edge,
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
            }

            world.MakeElements();

            uint valueId = 0;
            uint prevValueId = 0;
            uint bubbleUValueId = 0;
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
                // Vector2
                bubbleUValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    uQuantityId, true, FieldShowType.Real);

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
                mainWindow.DoZoom(10, true);
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = timeStep;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            var FEM = new ElasticLambWaveguide2DPMLTDFEM(
                world, dt,
                newmarkBeta, newmarkGamma,
                valueId, prevValueId);
            FEM.TimeLoopCnt = timeLoopCnt;
            FEM.TimeIndex = 0;
            FEM.TimeStep = timeStep;
            FEM.GaussianType = gaussianType;
            FEM.GaussianT0 = gaussianT0;
            FEM.GaussianTp = gaussianTp;
            FEM.SrcFrequency = srcFreq;
            FEM.GaussianAmp = gaussianAmp;
            FEM.StartFrequencyForSMatrix = freq1;
            FEM.EndFrequencyForSMatrix = freq2;

            {
                var solver = new IvyFEM.Linear.LapackEquationSolver();
                //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                solver.IsOrderingToBandMatrix = true;
                solver.IsRepeatSolve = true;
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

            if (ChartWindow2 == null)
            {
                ChartWindow2 = new ChartWindow();
                ChartWindow2.Closing += ChartWindow2_Closing;
            }
            {
                ChartWindow chartWin = ChartWindow2;
                chartWin.Owner = mainWindow;
                chartWin.Left = mainWindow.Left + mainWindow.Width;
                chartWin.Top = mainWindow.Top;
                chartWin.Show();
                chartWin.TextBox1.Text = "";
                var model = new PlotModel();
                chartWin.Plot.Model = model;
                model.Title = "ux(t): Time Domain";
                var axis1 = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "t",
                    //Minimum = 0,
                    //Maximum = timeLoopCnt
                };
                var axis2 = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "ux(t)"
                };
                model.Axes.Add(axis1);
                model.Axes.Add(axis2);
                var series1 = new LineSeries
                {
                    Title = "Port1"
                };
                var series2 = new LineSeries
                {
                    Title = "Port2"
                };
                model.Series.Add(series1);
                model.Series.Add(series2);
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();
            }

            for (int iTime = 0; iTime < timeLoopCnt; iTime++)
            {
                // 解く
                FEM.Solve();
                // 時間領域の変位
                double[] U = FEM.U;

                FEM.UpdateFieldValuesTimeDomain();

                //-----------------------------------------------
                // ベクトルを表示用にスケーリング
                // uを表示用にスケーリングする
                double[] showUxy = new double[U.Length];
                U.CopyTo(showUxy, 0);
                {
                    double maxValue = 0;
                    int cnt = U.Length;
                    foreach (double value in U)
                    {
                        double abs = Math.Abs(value);
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.2 * halfWaveguideWidth;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            showUxy[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                //-----------------------------------------------
                // 表示用uの値をセット
                world.UpdateBubbleFieldValueValuesFromNodeValues(bubbleUValueId, FieldDerivativeType.Value, showUxy);

                //-----------------------------------------------
                // u(t)の表示
                {
                    int timeIndex = FEM.TimeIndex;
                    int nodeCntB1 = FEM.RefTimeUsss[0][timeIndex].Length / uDof;
                    int refNodeIdB1 = nodeCntB1 - 1; // mid plane
                    int nodeCntB2 = FEM.RefTimeUsss[1][timeIndex].Length / uDof;
                    int refNodeIdB2 = nodeCntB2 - 1; // mid plane
                    double uxPort1 = FEM.RefTimeUsss[0][timeIndex][refNodeIdB1 * uDof];
                    double uxPort2 = FEM.RefTimeUsss[1][timeIndex][refNodeIdB2 * uDof];
                    var chartWin = ChartWindow2;
                    var model = chartWin.Plot.Model;
                    var series = model.Series;
                    var series1 = series[0] as LineSeries;
                    var series2 = series[1] as LineSeries;
                    series1.Points.Add(new DataPoint(timeIndex, uxPort1));
                    series2.Points.Add(new DataPoint(timeIndex, uxPort2));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                FEM.TimeIndex++;
            }

            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closing += ChartWindow1_Closing;
            }
            {
                ChartWindow chartWin = ChartWindow1;
                chartWin.Owner = mainWindow;
                chartWin.Left = mainWindow.Left + mainWindow.Width;
                chartWin.Top = mainWindow.Top + ChartWindow2.Height;
                chartWin.Show();
                chartWin.TextBox1.Text = "";
                var model = new PlotModel();
                chartWin.Plot.Model = model;
                model.Title = "Waveguide Example";
                var axis1 = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "ks W/π",
                    Minimum = normalizedFreq1,
                    Maximum = normalizedFreq2
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
            }

            // S11、S21の周波数特性
            double[] freqs;
            System.Numerics.Complex[] _freqDomainAmpsInc = null; // 直線導波路の場合
            IList<System.Numerics.Complex[]> freqDomainAmpss;
            IList<System.Numerics.Complex[]> Sss;
            FEM.CalcSParameter(_freqDomainAmpsInc, out freqs, out freqDomainAmpss, out Sss);
            int freqCnt = freqs.Length;
            for (int iFreq = 0; iFreq < freqCnt; iFreq++)
            {
                // 周波数
                double freq = freqs[iFreq];
                // 規格化周波数
                double normalizedFreq = toNormalizedFreq(freq);
                if (normalizedFreq < normalizedFreq1)
                {
                    continue;
                }
                if (normalizedFreq > normalizedFreq2)
                {
                    break;
                }
                // S
                System.Numerics.Complex S11 = Sss[0][iFreq];
                System.Numerics.Complex S21 = Sss[1][iFreq];
                double S11Abs = S11.Magnitude;
                double S21Abs = S21.Magnitude;
                double total = S11Abs * S11Abs + S21Abs * S21Abs;

                string ret;
                string CRLF = System.Environment.NewLine;
                ret = "ks W/π: " + normalizedFreq + CRLF;
                ret += "|S11| = " + S11Abs + CRLF +
                      "|S21| = " + S21Abs + CRLF +
                      "|S11|^2 + |S21|^2 = " + total + CRLF;
                System.Diagnostics.Debug.WriteLine(ret);
                //AlertWindow.ShowDialog(ret, "");
                var chartWin = ChartWindow1;
                var model = chartWin.Plot.Model;
                var series = model.Series;
                var series1 = series[0] as LineSeries;
                var series2 = series[1] as LineSeries;
                series1.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                series2.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();
            }

            // 他の導波路解析のSマトリクス計算時の入射波に利用
            retFreqs = freqs.ToArray();
            retFreqDomainAmpsInc = freqDomainAmpss[0].ToArray();
        }
    }
}
