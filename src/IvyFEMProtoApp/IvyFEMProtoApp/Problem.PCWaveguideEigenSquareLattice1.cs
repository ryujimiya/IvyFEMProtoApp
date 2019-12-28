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
        public void PCWaveguideEigenSquareLatticeProblem1(MainWindow mainWindow)
        {
            double waveguideWidth = 1.0;

            // 計算するモード
            int targetModeIndex = 0; // 一番小さい周波数

            // フォトニック導波路
            // ロッドの数（半分）
            //const int rodCntHalf = 5;
            const int rodCntHalf = 5;
            // 欠陥ロッド数
            const int defectRodCnt = 1;
            // 格子数
            const int latticeCnt = rodCntHalf * 2 + defectRodCnt;
            // 格子定数
            double latticeA = waveguideWidth / (double)latticeCnt;
            // 周期構造距離
            double periodicDistance = latticeA;
            // ロッドの半径
            double rodRadius = 0.18 * latticeA;
            // ロッドの比誘電率
            double rodEps = 3.4 * 3.4;
            // 格子１辺の分割数
            const int divForOneLatticeCnt = 6;
            // 境界の総分割数
            const int divCnt = latticeCnt * divForOneLatticeCnt;
            // ロッドの円周分割数
            const int rodCircleDiv = 8;
            // ロッドの半径の分割数
            const int rodRadiusDiv = 1;
            // メッシュサイズ
            double eLen = 1.05 * waveguideWidth / divCnt;

            // 計算範囲
            // 規格化伝搬定数 βd/(2π)
            double sBeta = 0.000;
            double eBeta = 0.500;
            int betaDiv = 50;

            // フォトニック結晶導波路の場合、a/λを規格化周波数とする
            double normalizedFreq1 = 0.330;
            double normalizedFreq2 = 0.440;

            IList<uint> rodLoopIds = new List<uint>();
            CadObject2D cad = new CadObject2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                // 領域追加
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(periodicDistance, 0.0));
                pts.Add(new OpenTK.Vector2d(periodicDistance, waveguideWidth));
                uint lId = cad.AddPolygon(pts).AddLId;
            }

            // 入出力導波路の周期構造境界上の頂点を追加
            //  逆から追加しているのは、頂点によって新たに生成される辺に頂点を追加しないようにするため
            {
                uint eId = 1;
                double x1 = 0.0;
                double y1 = waveguideWidth;
                double y2 = 0.0;
                PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x1, y2);
            }
            {
                uint eId = 3;
                double x1 = latticeA;
                double y1 = 0.0;
                double y2 = waveguideWidth;
                PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x1, y2);
            }
            // ロッドを追加
            {
                uint baseLoopId = 1;
                for (int row = 0; row < rodCntHalf; row++)
                {
                    double x0 = latticeA * 0.5;
                    double y0 = waveguideWidth - row * latticeA - latticeA * 0.5;
                    uint lId = PCWaveguideUtils.AddRod(
                        cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                    rodLoopIds.Add(lId);
                }
                for (int row = 0; row < rodCntHalf; row++)
                {
                    double x0 = latticeA * 0.5;
                    double y0 = latticeA * rodCntHalf - row * latticeA - latticeA * 0.5;
                    uint lId = PCWaveguideUtils.AddRod(
                        cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                    rodLoopIds.Add(lId);
                }
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
                    Epxx = rodEps,
                    Epyy = rodEps,
                    Epzz = rodEps,
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

            double freq1 = normalizedFreq1 * Constants.C0 / latticeA;
            double freq2 = normalizedFreq2 * Constants.C0 / latticeA;
            // ポート情報リスト作成
            int portId = 0; // 固定
            bool isPortBc2Reverse = true;
            var wgPortInfo = new PCWaveguidePortInfo();
            wgPortInfo.IsSVEA = false; // Φを直接解く
            wgPortInfo.IsPortBc2Reverse = isPortBc2Reverse;
            wgPortInfo.LatticeA = latticeA;
            wgPortInfo.PeriodicDistanceX = periodicDistance;
            wgPortInfo.MinFrequency = freq1;
            wgPortInfo.MaxFrequency = freq2;

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
            // 周期構造境界1
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];

                eIds[0] = 1;
                maIds[0] = claddingMaId;

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    eIds[i] = (uint)(4 + (divCnt - 1) - (i - 1));
                    maIds[i] = claddingMaId;
                }

                for (int i = 0; i < eIds.Length; i++)
                {
                    uint eId = eIds[i];
                    uint maId = maIds[i];
                    world.SetCadEdgeMaterial(eId, maId);
                }
                wgPortInfo.BcEdgeIds1 = new List<uint>(eIds);
            }
            // 周期構造境界2
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                eIds[0] = 3;
                maIds[0] = claddingMaId;

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    eIds[i] = (uint)(4 + (divCnt - 1) * 2 - (i - 1));
                    maIds[i] = claddingMaId;
                }

                for (int i = 0; i < eIds.Length; i++)
                {
                    uint eId = eIds[i];
                    uint maId = maIds[i];
                    world.SetCadEdgeMaterial(eId, maId);
                }
                wgPortInfo.BcEdgeIds2 = new List<uint>(eIds);
            }

            // ポート条件
            IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
            {
                IList<uint> lIds = wgPortInfo.LoopIds;
                IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                IList<uint> bcEIds2 = wgPortInfo.BcEdgeIds2;
                PortCondition portCondition = new PortCondition(
                    lIds, bcEIds1, bcEIds2, FieldValueType.ZScalar, new List<uint> { 0 }, 0);
                portConditions.Add(portCondition);
            }
            // 強制境界
            uint[] zeroEIds = { 2, 4 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            // ポートの境界上の節点を準備する
            {
                wgPortInfo.SetupAfterMakeElements(world, quantityId, (uint)portId);
            }
            // フォトニック結晶導波路チャンネル上節点を取得する
            {
                wgPortInfo.PCChannelCoIds = new List<IList<int>>();

                // 全座標IDの取得
                IList<int> coIds = new List<int>();
                IList<uint> lIds = wgPortInfo.LoopIds;
                foreach (uint lId in lIds)
                {
                    IList<int> tmpCoIds = world.GetCoordIdsFromCadId(quantityId, lId, CadElementType.Loop);
                    foreach (int coId in tmpCoIds)
                    {
                        if (coIds.IndexOf(coId) == -1)
                        {
                            coIds.Add(coId);
                        }
                    }
                }
                // チャンネル
                {
                    IList<int> channelCoIds = new List<int>();
                    wgPortInfo.PCChannelCoIds.Add(channelCoIds);

                    int nodeId0 = wgPortInfo.BcNodess[0][0];
                    int coId0 = world.PortNode2Coord(quantityId, (uint)portId, nodeId0);
                    double[] coord0 = world.GetCoord(quantityId, coId0);
                    // 以下の処理はX方向周期構造の場合
                    System.Diagnostics.Debug.Assert(!wgPortInfo.IsYDirectionPeriodic);
                    foreach (int coId in coIds)
                    {
                        // 座標からチャンネル(欠陥部)を判定する
                        double[] coord = world.GetCoord(quantityId, coId);
                        // X方向周期構造
                        double y = Math.Abs(coord[1] - coord0[1]); // Y座標
                        if (y >= (waveguideWidth - latticeA * (rodCntHalf + defectRodCnt)) &&
                            y <= (waveguideWidth - latticeA * (rodCntHalf)))
                        {
                            channelCoIds.Add(coId);
                        }
                    }
                }
            }

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
            model.Title = "PCWaveguide Eigen Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "β(d/(2π))",
                Minimum = sBeta,
                Maximum = eBeta
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
            // max light line
            double maxLightLine = eBeta * latticeA / periodicDistance;
            if (maxLightLine < normalizedFreq2)
            {
                maxLightLine = normalizedFreq2;
            }
            var series1 = new LineSeries
            {
                Title = "a/λ"
            };
            var series2 = new AreaSeries
            {
                Title = "light line",
                ConstantY2 = maxLightLine
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

            for (int iBeta = 0; iBeta < (betaDiv + 1); iBeta++)
            {
                // β
                double normalizedBeta = sBeta + (iBeta / (double)betaDiv) * (eBeta - sBeta);
                double beta = normalizedBeta * 2.0 * Math.PI / periodicDistance;
                System.Diagnostics.Debug.WriteLine("β(d/(2π)): " + normalizedBeta + " β:" + beta);

                var FEM = new PCWaveguide2DEigenFEMByBeta(world, quantityId, (uint)portId, wgPortInfo);
                FEM.BetaX = beta;
                FEM.Solve();
                double[] freqs = FEM.Frequencys;
                System.Numerics.Complex[][] eVecs = FEM.EVecs;

                double normalizedFreq = double.MaxValue;
                System.Numerics.Complex[] eigenEz = null;
                int iMode = targetModeIndex;
                int nodeCnt = (int)world.GetNodeCount(quantityId);
                eigenEz = new System.Numerics.Complex[nodeCnt];
                int modeCnt = freqs.Length;
                if (iMode >= modeCnt)
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
                    double freq = freqs[iMode];
                    double waveLength = Constants.C0 / freq;
                    normalizedFreq = latticeA / waveLength;
                }
                // light line
                double lightLine = normalizedBeta * latticeA / periodicDistance;

                string ret;
                string CRLF = System.Environment.NewLine;
                ret = "βd/(2π)" + normalizedBeta + CRLF;
                ret += "a/λ = " + normalizedFreq + CRLF +
                      "light line = " + lightLine + CRLF;
                System.Diagnostics.Debug.WriteLine(ret);
                if (normalizedFreq < double.MaxValue)
                {
                    series1.Points.Add(new DataPoint(normalizedBeta, normalizedFreq));
                }
                series2.Points.Add(new DataPoint(normalizedBeta, lightLine));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();

                if (normalizedFreq < double.MaxValue)
                {
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
}
