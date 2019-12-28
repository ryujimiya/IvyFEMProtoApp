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
        // 45°三角形格子
        public void PBGTriangleLatticeProblem2(MainWindow mainWindow)
        {
            // 空孔？
            //bool isAirHole = false; // 誘電体ロッド
            //bool isAirHole = true; //エアホール
            bool isAirHole = false;
            //bool isTMMode = true;  // TMモード
            //bool isTMMode = false;  // TEモード
            bool isTMMode = false;
            double periodicDistanceY = 1.0;
            // 三角形格子の内角
            double latticeTheta = 45.0;
            // ロッドの半径
            double rodRadiusRatio = 0.18;
            // ロッドの比誘電率
            double rodEps = 3.4 * 3.4;
            // 格子１辺の分割数
            const int divForOneLatticeCnt = 12;
            // 境界の総分割数(目安)
            const int desiredDivCnt = divForOneLatticeCnt;
            // ロッドの円周分割数
            const int rodCircleDiv = 16;
            // ロッドの半径の分割数
            const int rodRadiusDiv = 5;

            // 格子定数
            double latticeA = periodicDistanceY / Math.Sin(latticeTheta * Math.PI / 180.0);
            // 周期構造距離
            double periodicDistanceX = periodicDistanceY * 2.0 / Math.Tan(latticeTheta * Math.PI / 180.0);
            // ロッドの半径
            double rodRadius = rodRadiusRatio * latticeA;
            // 斜め領域？
            bool isAslantX = true;
            // 斜め領域 X方向オフセット
            double offsetX = periodicDistanceY / Math.Tan(latticeTheta * Math.PI / 180.0);
            // メッシュサイズ
            // 斜め領域
            double eLen = 1.05 * (periodicDistanceY / Math.Sin(latticeTheta * Math.PI / 180.0)) / divForOneLatticeCnt;

            int betaDiv = 20;
            System.Diagnostics.Debug.Assert(Math.Abs(latticeTheta - 45.0) < Constants.PrecisionLowerLimit);
            // Γ-K, K-M, M-Γ
            int regionCnt = 3;
            double[][][] regionCriticalPointss = new double[regionCnt][][];
            double[] regionWavenumbers = new double[regionCnt];
            double theta2 = Math.PI / 2.0 - latticeTheta * (Math.PI / 180.0);
            double[] criticalPointM = { Math.PI / latticeA, Math.PI * Math.Tan(theta2) / latticeA };
            double[] criticalPointK = { Math.PI / (Math.Cos(theta2) * Math.Cos(theta2) * latticeA), 0.0 };
            Func<int, double[]> GetBetaXYGK = index =>
            {
                double[] betaXY = new double[2];
                // Γ-K
                double pos = index / (double)betaDiv;
                betaXY[0] = pos * criticalPointK[0];
                betaXY[1] = 0.0;
                return betaXY;
            };
            Func<int, double[]> GetBetaXYKM = index =>
            {
                double[] betaXY = new double[2];
                // K -M
                double pos = index / (double)betaDiv;
                betaXY[0] = pos * (criticalPointM[0] - criticalPointK[0]) + criticalPointK[0];
                betaXY[1] = pos * criticalPointM[1];
                return betaXY;
            };
            Func<int, double[]> GetBetaXYMG = index =>
            {
                double[] betaXY = new double[2];
                double pos = index / (double)betaDiv;
                betaXY[0] = pos * (-1.0 * criticalPointM[0]) + criticalPointM[0];
                betaXY[1] = pos * (-1.0 * criticalPointM[1]) + criticalPointM[1];
                return betaXY;
            };
            Func<int, double[]> GetBetaXY = iBeta =>
            {
                double[] betaXY = null;
                // 三角形格子
                if (iBeta < betaDiv)
                {
                    // Γ-K
                    int index = iBeta;
                    betaXY = GetBetaXYGK(index);
                }
                else if (iBeta >= betaDiv && iBeta < betaDiv * 2)
                {
                    int index = iBeta - betaDiv;
                    // K -M
                    betaXY = GetBetaXYKM(index);
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
                regionWavenumbers[iR] = CadUtils.GetDistance2D(sPt, ePt);
            }

            double normalizedFreq1 = 0.000;
            double normalizedFreq2 = 1.000;

            IList<uint> rodLoopIds = new List<uint>();
            CadObject2D cad = new CadObject2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                // 領域追加
                pts.Add(new OpenTK.Vector2d(offsetX, periodicDistanceY));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(periodicDistanceX, 0.0));
                pts.Add(new OpenTK.Vector2d(periodicDistanceX + offsetX, periodicDistanceY));
                uint lId = cad.AddPolygon(pts).AddLId;
            }

            int bcCnt = 4;
            uint[] bcEIds0 = { 1, 3, 2, 4 };
            double[] bcX0s = { 0.0, periodicDistanceX, 0.0, offsetX };
            double bc1Len = periodicDistanceY / Math.Cos((90.0 - latticeTheta) * (Math.PI / 180.0));
            IList<uint>[] bcVIdss = new List<uint>[bcCnt];
            uint[][] rodVIdss = new uint[bcCnt][];
            double[] rodRatiosB1 = { rodRadius / bc1Len, (bc1Len - rodRadius) / bc1Len };
            double[] rodRatiosB3 = { 
                rodRadius / periodicDistanceX, (periodicDistanceX - rodRadius) / periodicDistanceX
            };
            for (int bcIndex = 0; bcIndex < bcCnt; bcIndex++)
            {
                IList<uint> bcVIds = new List<uint>();
                bcVIdss[bcIndex] = bcVIds;
                uint eId0 = bcEIds0[bcIndex];
                double x0 = bcX0s[bcIndex];
                double xlen = 0.0;
                double ylen = 0.0;
                double[] rodRatios = null;
                if (bcIndex == 0 || bcIndex == 1)
                {
                    rodRatios = rodRatiosB1;
                    xlen = bc1Len * Math.Cos(latticeTheta * Math.PI / 180.0);
                    ylen = periodicDistanceY;
                }
                else if (bcIndex == 2 || bcIndex == 3)
                {
                    rodRatios = rodRatiosB3;
                    xlen = periodicDistanceX;
                    ylen = periodicDistanceY;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                List<double> ratios = new List<double>();
                foreach (double ratio in rodRatios)
                {
                    ratios.Add(ratio);
                }
                for (int iRatio = 0; iRatio < (desiredDivCnt - 1); iRatio++)
                {
                    double ratio = (iRatio + 1) / (double)desiredDivCnt;
                    bool isTooClose = false;
                    for (int rodVIndex = 0; rodVIndex < rodRatios.Length; rodVIndex++)
                    {
                        double radiusMargin = 0.01;
                        double rodRatio = rodRatios[rodVIndex];
                        if (Math.Abs(ratio - rodRatio) < radiusMargin)
                        {
                            isTooClose = true;
                            break;
                        }
                    }
                    if (!isTooClose)
                    {
                        ratios.Add(ratio);
                    }
                }
                ratios.Sort();

                uint[] rodVIds = new uint[rodRatios.Length];
                rodVIdss[bcIndex] = rodVIds;
                int[] rodVertexIndexs = new int[rodRatios.Length];
                for (int rodVIndex = 0; rodVIndex < rodRatios.Length; rodVIndex++)
                {
                    double rodRatio = rodRatios[rodVIndex];
                    int hitVertexIndex = -1;
                    for(int vertexIndex = 0; vertexIndex < ratios.Count; vertexIndex++)
                    {
                        double ratio = ratios[vertexIndex];
                        if (Math.Abs(ratio - rodRatio) < Constants.PrecisionLowerLimit)
                        {
                            hitVertexIndex = vertexIndex;
                            break;
                        }
                    }
                    System.Diagnostics.Debug.Assert(hitVertexIndex != -1);
                    rodVertexIndexs[rodVIndex] = hitVertexIndex;
                }

                int vertexCnt = ratios.Count;
                for (int vertexIndex = 0; vertexIndex < vertexCnt; vertexIndex++)
                {
                    double x = 0;
                    double y = 0;
                    if (bcIndex == 0)
                    {
                        double ratio = ratios[vertexIndex];
                        x = ratio * xlen + x0;
                        y = ratio * ylen;
                    }
                    else if (bcIndex == 1)
                    {
                        double ratio = ratios[vertexCnt - 1 - vertexIndex];
                        x = ratio * xlen + x0;
                        y = ratio * ylen;
                    }
                    else if (bcIndex == 2)
                    {
                        double ratio = ratios[vertexCnt - 1 - vertexIndex];
                        x = ratio * xlen + x0;
                        y = 0;
                    }
                    else if (bcIndex == 3)
                    {
                        double ratio = ratios[vertexIndex];
                        x = ratio * xlen + x0;
                        y = periodicDistanceY;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                    var res = cad.AddVertex(CadElementType.Edge, eId0, new OpenTK.Vector2d(x, y));
                    uint addVId = res.AddVId;
                    uint addEId = res.AddEId;
                    System.Diagnostics.Debug.Assert(addVId != 0);
                    System.Diagnostics.Debug.Assert(addEId != 0);
                    bcVIds.Add(addVId);

                    int rodIndex = Array.IndexOf(rodVertexIndexs, vertexIndex);
                    if (rodIndex != -1)
                    {
                        rodVIds[rodIndex] = addVId;
                    }
                }
            }
            // 下の1/4ロッド
            uint[] bottomRodCenterVIds = { 2, 3 };
            for (int rodIndex = 0; rodIndex < 2; rodIndex++)
            {
                uint vId0 = 0;
                uint vId1 = 0;
                uint vId2 = 0;
                double x0 = 0.0;
                double y0 = 0.0;
                double startAngle = 0.0;
                double endAngle = 0.0;
                bool isReverseAddVertex = false;
                uint baseLoopId = 1;
                if (rodIndex == 0)
                {
                    // 左下
                    var rodVIdsB1 = rodVIdss[0];
                    var rodVIdsB3 = rodVIdss[2];
                    vId0 = rodVIdsB1[0];
                    vId1 = bottomRodCenterVIds[rodIndex];
                    vId2 = rodVIdsB3[rodVIdsB3.Length - 1];
                    x0 = 0.0;
                    y0 = 0.0;
                    startAngle = latticeTheta;
                    endAngle = 0.0;
                    isReverseAddVertex = true;
                }
                else if (rodIndex == 1)
                {
                    // 右下
                    var rodVIdsB2 = rodVIdss[1];
                    var rodVIdsB3 = rodVIdss[2];
                    vId0 = rodVIdsB3[0];
                    vId1 = bottomRodCenterVIds[rodIndex];
                    vId2 = rodVIdsB2[rodVIdsB2.Length - 1];
                    x0 = periodicDistanceX;
                    y0 = 0.0;
                    startAngle = 180.0;
                    endAngle = latticeTheta;
                    isReverseAddVertex = true;
                }
                uint lId = 0;
                // 下の1/4ロッド
                lId = PCWaveguideUtils.AddPartialRod(
                    cad,
                    baseLoopId,
                    vId0,
                    vId1,
                    vId2,
                    x0,
                    y0,
                    rodRadius,
                    rodCircleDiv,
                    rodRadiusDiv,
                    startAngle,
                    endAngle,
                    isReverseAddVertex);
                rodLoopIds.Add(lId);
            }

            // 上のロッド
            uint[] topRodCenterVIds = { 1, 4 };
            for (int rodIndex = 0; rodIndex < 2; rodIndex++)
            {
                uint vId0 = 0;
                uint vId1 = 0;
                uint vId2 = 0;
                double x0 = 0.0;
                double y0 = 0.0;
                double startAngle = 0.0;
                double endAngle = 0.0;
                bool isReverseAddVertex = false;
                uint baseLoopId = 1;
                if (rodIndex == 0)
                {
                    // 左上
                    var rodVIdsB1 = rodVIdss[0];
                    var rodVIdsB4 = rodVIdss[3];
                    vId0 = rodVIdsB4[0];
                    vId1 = topRodCenterVIds[rodIndex];
                    vId2 = rodVIdsB1[rodVIdsB1.Length - 1];
                    x0 = 0.0 + offsetX;
                    y0 = periodicDistanceY;
                    startAngle = 360.0;
                    endAngle = 180.0 + latticeTheta;
                    isReverseAddVertex = true;
                }
                else if (rodIndex == 1)
                {
                    // 右上
                    var rodVIdsB2 = rodVIdss[1];
                    var rodVIdsB4 = rodVIdss[3];
                    vId0 = rodVIdsB2[0];
                    vId1 = topRodCenterVIds[rodIndex];
                    vId2 = rodVIdsB4[rodVIdsB4.Length - 1];
                    x0 = periodicDistanceX + offsetX;
                    y0 = periodicDistanceY;
                    startAngle = 180.0 + latticeTheta;
                    endAngle = 180.0;
                    isReverseAddVertex = true;
                }
                uint lId = 0;
                // 上の1/4ロッド
                lId = PCWaveguideUtils.AddPartialRod(
                    cad,
                    baseLoopId,
                    vId0,
                    vId1,
                    vId2,
                    x0,
                    y0,
                    rodRadius,
                    rodCircleDiv,
                    rodRadiusDiv,
                    startAngle,
                    endAngle,
                    isReverseAddVertex);
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
            IDrawer drawer = new CadObject2DDrawer(cad);
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
            IDrawer meshDrawer = new Mesher2DDrawer(mesher);
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
                double claddingEp = 0.0;
                double coreEp = 0.0;
                if (isAirHole)
                {
                    // エアホール型
                    claddingEp = rodEps;
                    coreEp = 1.0;
                }
                else
                {
                    // ロッド型
                    claddingEp = 1.0;
                    coreEp = rodEps;
                }
                DielectricMaterial claddingMa = new DielectricMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricMaterial coreMa = new DielectricMaterial
                {
                    Epxx = coreEp,
                    Epyy = coreEp,
                    Epzz = coreEp,
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
            wgPortInfo.IsAslantX = isAslantX;

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
            uint[][] bcEIdss = new uint[bcCnt][];
            int[] bcDivCnts = new int[bcCnt];
            for (int bcIndex = 0; bcIndex < bcCnt; bcIndex++)
            {
                var bcVIds = bcVIdss[bcIndex];
                int divCnt = bcVIds.Count;
                bcDivCnts[bcIndex] = divCnt;
            }
            for (int bcIndex = 0; bcIndex < bcCnt; bcIndex++)
            {
                int divCnt = bcDivCnts[bcIndex];
                int prevDivCnt = 0;
                for (int workbcIndex = 0; workbcIndex < bcIndex; workbcIndex++)
                {
                    prevDivCnt += bcDivCnts[workbcIndex];
                }
                var bcVIds = bcVIdss[bcIndex];
                var rodVIds = rodVIdss[bcIndex];
                System.Diagnostics.Debug.Assert(rodVIds.Length == 2);
                int[] rodVertexIndexs = new int[2];
                for (int rodVIndex = 0; rodVIndex < rodVertexIndexs.Length; rodVIndex++)
                {
                    rodVertexIndexs[rodVIndex] = bcVIds.IndexOf(rodVIds[rodVIndex]);
                }

                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                eIds[0] = bcEIds0[bcIndex];
                maIds[0] = coreMaId; // 注意

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    eIds[i] = (uint)(4 + prevDivCnt + (divCnt - 1) - (i - 1));
                    uint maId = claddingMaId;
                    if (i < rodVertexIndexs[0] || i >= rodVertexIndexs[1])
                    {
                        maId = coreMaId;
                    }
                    else
                    {
                        maId = claddingMaId;
                    }
                    maIds[i] = maId;
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
            string[] criticalPointLabels = { "G", "K", "M", "G" };
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
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
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
                FEM.IsTMMode = isTMMode;
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
                double beta = CadUtils.GetDistance2D(regionCriticalPointss[curRegionIndex][0], betaXY);
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
