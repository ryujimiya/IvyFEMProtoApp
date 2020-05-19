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
        // 正方格子
        public void PBGSquareLatticeProblem1(MainWindow mainWindow)
        {
            double periodicDistanceY = 1.0;
            // 格子定数
            double latticeA = periodicDistanceY;
            // 周期構造距離
            double periodicDistanceX = latticeA;
            // ロッドの半径
            double rodRadius = 0.18 * latticeA;
            // ロッドの比誘電率
            double rodEp = 3.4 * 3.4;
            // 格子１辺の分割数
            const int divForOneLatticeCnt = 12;
            // 境界の総分割数
            const int divCnt = divForOneLatticeCnt;
            // ロッドの円周分割数
            const int rodCircleDiv = 12;
            // ロッドの半径の分割数
            const int rodRadiusDiv = 4;
            // メッシュサイズ
            double eLen = 1.05 * periodicDistanceY / divCnt;

            int betaDiv = 20;
            // Γ-X, X-M, M-Γ
            int regionCnt = 3;
            double[][][] regionCriticalPointss = new double[regionCnt][][];
            double[] regionWavenumbers = new double[regionCnt];
            Func<int, double[]> GetBetaXYGX = index =>
            {
                double[] betaXY = new double[2];
                // Γ-X
                betaXY[0] = index * (Math.PI / periodicDistanceX) / betaDiv;
                betaXY[1] = 0.0;
                return betaXY;
            };
            Func<int, double[]> GetBetaXYXM = index =>
            {
                double[] betaXY = new double[2];
                // X -M
                betaXY[0] = Math.PI / periodicDistanceX;
                betaXY[1] = index * (Math.PI / periodicDistanceY) / betaDiv;
                return betaXY;
            };
            Func<int, double[]> GetBetaXYMG = index =>
            {
                double[] betaXY = new double[2];
                // M - Γ
                betaXY[0] = Math.PI / periodicDistanceX - index * (Math.PI / periodicDistanceX) / betaDiv;
                betaXY[1] = Math.PI / periodicDistanceY - index * (Math.PI / periodicDistanceY) / betaDiv;
                return betaXY;
            };
            Func<int, double[]> GetBetaXY = iBeta =>
            {
                double[] betaXY = null;
                // 長方形格子
                if (iBeta < betaDiv)
                {
                    // Γ-X
                    int index = iBeta;
                    betaXY = GetBetaXYGX(index);
                }
                else if (iBeta >= betaDiv && iBeta < betaDiv * 2)
                {
                    int index = iBeta - betaDiv;
                    // X -M
                    betaXY = GetBetaXYXM(index);
                }
                else if (iBeta >= betaDiv * 2 && iBeta <= betaDiv * 3)
                {
                    // M - Γ
                    int index = iBeta - betaDiv * 2; 
                    betaXY = GetBetaXYMG(index);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                return betaXY;
            };
            for (int iR = 0; iR < regionCnt; iR++)
            {
                double[] sPt = GetBetaXY(iR * betaDiv);
                double[] ePt = GetBetaXY((iR + 1) * betaDiv);
                double[][] pts = { sPt, ePt };
                regionCriticalPointss[iR] = pts;
                regionWavenumbers[iR] = CadUtils2D.GetDistance2D(sPt, ePt);
            }

            double normalizedFreq1 = 0.000;
            double normalizedFreq2 = 1.000;

            IList<uint> rodLoopIds = new List<uint>();
            Cad2D cad = new Cad2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                // 領域追加
                pts.Add(new OpenTK.Vector2d(0.0, periodicDistanceY));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(periodicDistanceX, 0.0));
                pts.Add(new OpenTK.Vector2d(periodicDistanceX, periodicDistanceY));
                uint lId = cad.AddPolygon(pts).AddLId;
            }

            // 周期構造境界上の頂点を追加
            //  逆から追加しているのは、頂点によって新たに生成される辺に頂点を追加しないようにするため
            // 境界1：左
            {
                uint eId = 1;
                double x1 = 0.0;
                double y1 = periodicDistanceY;
                double x2 = x1;
                double y2 = 0.0;
                PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x2, y2);
            }
            // 境界2：右
            {
                uint eId = 3;
                double x1 = periodicDistanceX;
                double y1 = 0.0;
                double x2 = x1;
                double y2 = periodicDistanceY;
                PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x2, y2);
            }
            // 境界3：下
            {
                uint eId = 2;
                double x1 = 0.0;
                double y1 = 0.0;
                double x2 = periodicDistanceX;
                double y2 = y1;
                PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x2, y2);
            }
            // 境界4：上
            {
                uint id_e = 4;
                double x1 = periodicDistanceX;
                double y1 = periodicDistanceY;
                double x2 = 0.0;
                double y2 = y1;
                PCWaveguideUtils.DivideBoundary(cad, id_e, divCnt, x1, y1, x2, y2);
            }

            // ロッドを追加
            {
                uint baseLoopId = 1;
                double x0 = periodicDistanceX * 0.5;
                double y0 = periodicDistanceY * 0.5;
                uint lId = PCWaveguideUtils.AddRod(cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                rodLoopIds.Add(lId);
            }

            // check
            {
                foreach (uint lId in rodLoopIds)
                {
                    cad.SetLoopColor(lId, new double[] { 1.0, 0.6, 0.6 });
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
            uint quantityId;
            {
                uint dof = 1; // 複素数
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint claddingMaId = 0;
            uint coreMaId = 0;
            {
                world.ClearMaterial();

                // 媒質リスト作成
                DielectricMaterial claddingMa = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricMaterial coreMa = new DielectricMaterial
                {
                    Epxx = rodEp,
                    Epyy = rodEp,
                    Epzz = rodEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };

                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);

                uint[] lIds = new uint[1 + rodLoopIds.Count];
                lIds[0] = 1;
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 1] = rodLoopIds[i];
                }
                uint[] maIds = new uint[1 + rodLoopIds.Count];
                maIds[0] = claddingMaId;
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    maIds[i + 1] = coreMaId;
                }

                for (int i = 0; i < lIds.Length; i++)
                {
                    uint lId = lIds[i];
                    uint maId = maIds[i];
                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            // ポート情報リスト作成
            int portId = 0; // 固定
            bool isPortBc2Reverse = true;
            bool isPortBc4Reverse = true;
            var wgPortInfo = new PCWaveguidePortInfo();
            wgPortInfo.IsSVEA = false; // Φを直接解く
            wgPortInfo.IsPortBc2Reverse = isPortBc2Reverse;
            wgPortInfo.IsPortBc4Reverse = isPortBc4Reverse;
            wgPortInfo.LatticeA = latticeA;
            wgPortInfo.PeriodicDistanceX = periodicDistanceX;
            wgPortInfo.PeriodicDistanceY = periodicDistanceY;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // 周期構造導波路
            {
                // ワールド座標系のループIDを取得
                uint[] lIds = new uint[1 + rodLoopIds.Count];
                lIds[0] = 1;
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 1] = rodLoopIds[i];
                }
                wgPortInfo.LoopIds = new List<uint>(lIds);
            }
            // 周期構造境界
            int bcCnt = 4;
            uint[] bcEIds0 = { 1, 3, 2, 4 };
            uint[][] bcEIdss = new uint[bcCnt][];
            for (int bcIndex = 0; bcIndex < bcCnt; bcIndex++)
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                eIds[0] = bcEIds0[bcIndex];
                maIds[0] = claddingMaId;

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    eIds[i] = (uint)(4 + (divCnt - 1) * (bcIndex + 1) - (i - 1));
                    maIds[i] = claddingMaId;
                }

                for (int i = 0; i < eIds.Length; i++)
                {
                    uint eId = eIds[i];
                    uint maId = maIds[i];
                    world.SetCadEdgeMaterial(eId, maId);
                }
                bcEIdss[bcIndex] = eIds;
            }
            wgPortInfo.BcEdgeIds1 = new List<uint>(bcEIdss[0]);
            wgPortInfo.BcEdgeIds2 = new List<uint>(bcEIdss[1]);
            wgPortInfo.BcEdgeIds3 = new List<uint>(bcEIdss[2]);
            wgPortInfo.BcEdgeIds4 = new List<uint>(bcEIdss[3]);

            // ポート条件
            IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
            {
                IList<uint> lIds = wgPortInfo.LoopIds;
                IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                IList<uint> bcEIds2 = wgPortInfo.BcEdgeIds2;
                IList<uint> bcEIds3 = wgPortInfo.BcEdgeIds3;
                IList<uint> bcEIds4 = wgPortInfo.BcEdgeIds4;
                PortCondition portCondition = new PortCondition(
                    lIds, bcEIds1, bcEIds2, bcEIds3, bcEIds4, FieldValueType.ZScalar, new List<uint> { 0 }, 0);
                portConditions.Add(portCondition);
            }
            /*
            // 強制境界
            uint[] zeroEIds = { };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }
            */

            world.MakeElements();

            // ポートの境界上の節点を準備する
            {
                wgPortInfo.SetupAfterMakeElements(world, quantityId, (uint)portId);
            }

            double[] criticalPointNormalizedWaveNumbers = new double[regionCnt + 1];
            string[] criticalPointLabels = { "G", "X", "M", "G" };
            System.Diagnostics.Debug.Assert(criticalPointLabels.Length == criticalPointNormalizedWaveNumbers.Length);
            double maxRegionNormalizedWavenumber = 0;
            criticalPointNormalizedWaveNumbers[0] = 0;
            for (int iR = 0; iR < regionCnt; iR++)
            {
                maxRegionNormalizedWavenumber += latticeA / (2.0 * Math.PI) * regionWavenumbers[iR];
                criticalPointNormalizedWaveNumbers[iR + 1] = maxRegionNormalizedWavenumber;
            }
            Func<double, string> criticalPointLabelFormatter = value =>
            {
                string label = "";
                //double th = Constants.PrecisionLowerLimit;
                double th = 0.05 * maxRegionNormalizedWavenumber; // MajorStepの半分
                for (int i = 0; i < criticalPointLabels.Length; i++)
                {
                    string criticalPointLabel = criticalPointLabels[i];
                    double normalizedWaveNumber = criticalPointNormalizedWaveNumbers[i];
                    if (Math.Abs(value - normalizedWaveNumber) < th)
                    {
                        label = criticalPointLabel;
                        break;
                    }
                }
                return label;
            };

            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closing += ChartWindow1_Closing;
            }
            var chartWin = ChartWindow1;
            chartWin.Owner = mainWindow;
            chartWin.Left = mainWindow.Left + mainWindow.Width;
            chartWin.Top = mainWindow.Top;
            chartWin.Show();
            chartWin.TextBox1.Text = "";
            var model = new PlotModel();
            chartWin.Plot.Model = model;
            model.Title = "PBG Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                //Title = "β(a/(2π))",
                Minimum = 0,
                Maximum = maxRegionNormalizedWavenumber,
                MajorStep = 0.1 * maxRegionNormalizedWavenumber, // critical Pointを認識させるため
                MajorTickSize = 0, // 目盛を表示させない
                MinorTickSize = 0, // 目盛を表示させない
                ExtraGridlines = criticalPointNormalizedWaveNumbers,
                LabelFormatter = criticalPointLabelFormatter                
            };
            var axis2 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "a/λ",
                Minimum = normalizedFreq1,
                Maximum = normalizedFreq2
            };
            model.Axes.Add(axis1);
            model.Axes.Add(axis2);
            var series1 = new LineSeries
            {
                //Title = "a/λ",
                LineStyle = LineStyle.None,
                MarkerType = MarkerType.Circle
            };
            var series2 = new AreaSeries
            {
                //Title = "PBG",
                MarkerType = MarkerType.None
            };
            model.Series.Add(series1);
            model.Series.Add(series2);
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.ZReal);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
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

            IList<double[]> freqss = new List<double[]>();
            for (int iBeta = 0; iBeta < betaDiv * 3; iBeta++)
            {
                double[] betaXY = GetBetaXY(iBeta);
                double freq1 = normalizedFreq1 * Constants.C0 / latticeA;
                double freq2 = normalizedFreq2 * Constants.C0 / latticeA;

                var FEM = new PCWaveguide2DEigenFEMByBeta(world, quantityId, (uint)portId, wgPortInfo);
                FEM.BetaX = betaXY[0];
                FEM.BetaY = betaXY[1];
                FEM.Solve();
                double[] freqs = FEM.Frequencys;
                System.Numerics.Complex[][] eVecs = FEM.EVecs;
                freqss.Add(freqs);

                double gapMinFreq;
                double gapMaxFreq;
                PCWaveguide2DEigenFEMByBeta.GetPBG(freqss, freq1, freq2, out gapMinFreq, out gapMaxFreq);

                int modeCnt = freqs.Length;
                System.Numerics.Complex[] eigenEz = null;
                {
                    int iMode = 0;
                    int nodeCnt = (int)world.GetNodeCount(quantityId);
                    eigenEz = new System.Numerics.Complex[nodeCnt];
                    if (iMode >= modeCnt || iMode < 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No defect mode found for iMode = " + iMode);
                    }
                    else
                    {
                        System.Numerics.Complex[] eVec = eVecs[iMode];
                        int portNodeCnt = (int)world.GetPortNodeCount(quantityId, (uint)portId);
                        System.Diagnostics.Debug.Assert(portNodeCnt == eVec.Length);
                        for (int portNodeId = 0; portNodeId < portNodeCnt; portNodeId++)
                        {
                            int coId = world.PortNode2Coord(quantityId, (uint)portId, portNodeId);
                            int nodeId = world.Coord2Node(quantityId, coId);
                            System.Numerics.Complex value = eVec[portNodeId];
                            eigenEz[nodeId] = value;
                        }
                    }
                }

                int curRegionIndex = iBeta / betaDiv;
                double beta = CadUtils2D.GetDistance2D(regionCriticalPointss[curRegionIndex][0], betaXY);
                for (int iRegion = 1; iRegion <= curRegionIndex; iRegion++)
                {
                    beta += regionWavenumbers[iRegion - 1];
                }
                double normalizedWavenumber = beta * latticeA / (2.0 * Math.PI);
                for (int iMode = 0; iMode < modeCnt; iMode++)
                {
                    double freq = freqs[iMode];
                    double waveLength = Constants.C0 / freq;
                    double normalizedFreq = latticeA / waveLength;
                    if (normalizedFreq < normalizedFreq1 || normalizedFreq > normalizedFreq2)
                    {
                        continue;
                    }
                    series1.Points.Add(new DataPoint(normalizedWavenumber, normalizedFreq));
                }
                //
                {
                    double gapMinWaveLength = Constants.C0 / gapMinFreq;
                    double gapMinNormalizedFreq = latticeA / gapMinWaveLength;
                    double gapMaxWaveLength = Constants.C0 / gapMaxFreq;
                    double gapMaxNormalizedFreq = latticeA / gapMaxWaveLength;
                    series2.Points.Clear();
                    series2.Points2.Clear();
                    series2.Points.Add(new DataPoint(0, gapMinNormalizedFreq));
                    series2.Points.Add(new DataPoint(maxRegionNormalizedWavenumber, gapMinNormalizedFreq));
                    series2.Points2.Add(new DataPoint(0, gapMaxNormalizedFreq));
                    series2.Points2.Add(new DataPoint(maxRegionNormalizedWavenumber, gapMaxNormalizedFreq));

                    chartWin.TextBox1.Text = string.Format(
                        "gap: min = {0:G4} max = {1:G4}", gapMinNormalizedFreq, gapMaxNormalizedFreq);
                }
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();

                // eigenEz
                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, eigenEz);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
