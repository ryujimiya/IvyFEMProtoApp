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
        public void Waveguide2DEigenOpenProblem3(MainWindow mainWindow, uint feOrder)
        {
            double waveguideWidth = 1.0;
            double waveguideHeight = 0.75 * waveguideWidth;
            //double coreHeight = (1.0 / 4.0) * waveguideHeight;
            double coreHeight = (1.0 / 2.0) * waveguideHeight;
            double coreWidth = 2.0 * coreHeight;
            double coreEpxx = 2.31;
            double coreEpyy = 2.19;
            double coreEpzz = 2.31;
            double claddingEp = 2.05;
            double maxEp = coreEpxx * 1.00;
            // 磁界？ default: false
            // false: 電界
            // true: 磁界
            bool isMagneticField = false;
            System.Diagnostics.Debug.Assert(!isMagneticField);
            //double eLen = coreHeight * 0.25;
            double eLen = 0.0;
            if (feOrder == 1)
            {
                eLen = coreHeight * 0.25;
            }
            else if (feOrder == 2)
            {
                eLen = coreHeight * 0.50;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            // 規格化周波数: h/λ
            double sFreq = 0.0;
            double eFreq = 15.0 / (2.0 * Math.PI);
            int freqDiv = 50;
            //double sFreq = 15.0 / (2.0 * Math.PI) * 0.5; //DEBUG
            //double eFreq = 15.0 / (2.0 * Math.PI); // DEBUG
            //int freqDiv = 1; // DEBUG

            double coreOffsetX = (waveguideWidth - coreWidth) / 2.0;
            double coreOffsetY = (waveguideHeight - coreHeight) / 2.0;
            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideHeight));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector2d(waveguideWidth, waveguideHeight));
                uint lId1 = cad.AddPolygon(pts).AddLId;

                IList<OpenTK.Vector2d> corePts = new List<OpenTK.Vector2d>();
                corePts.Add(new OpenTK.Vector2d(coreOffsetX, coreOffsetY + coreHeight));
                corePts.Add(new OpenTK.Vector2d(coreOffsetX, coreOffsetY));
                corePts.Add(new OpenTK.Vector2d(coreOffsetX + coreWidth, coreOffsetY));
                corePts.Add(new OpenTK.Vector2d(coreOffsetX + coreWidth, coreOffsetY + coreHeight));
                uint lId2 = cad.AddPolygon(corePts, lId1).AddLId;
            }

            // check
            uint coreLId = 2;
            {
                double[] coreColor = { 0.5, 1.0, 0.5 };
                cad.SetLoopColor(coreLId, coreColor);
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
            uint tQuantityId;
            uint zQuantityId;
            {
                uint tdof = 1; // 複素数(辺方向成分)
                uint tfeOrder = feOrder;
                tQuantityId = world.AddQuantity(tdof, tfeOrder, FiniteElementType.Edge);

                uint zdof = 1; // 複素数
                uint zfeOrder = feOrder;
                zQuantityId = world.AddQuantity(zdof, zfeOrder, FiniteElementType.ScalarLagrange);
            }

            uint claddingMaId;
            uint coreMaId;
            {
                world.ClearMaterial();
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
                    Epxx = coreEpxx,
                    Epyy = coreEpyy,
                    Epzz = coreEpzz,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);

                uint[] lIds = { 1, 2 };
                uint[] maIds = { claddingMaId, coreMaId };

                for (int i = 0; i < lIds.Length; i++)
                {
                    uint lId = lIds[i];
                    uint maId = maIds[i];
                    world.SetCadLoopMaterial(lId, maId);
                }

                // 外側境界
                uint[] bcEIds = { 1, 2, 3, 4 };
                uint[] bcMaIds = {
                    claddingMaId, claddingMaId, claddingMaId, claddingMaId
                };
                for (int i = 0; i < bcEIds.Length; i++)
                {
                    uint eId = bcEIds[i];
                    uint maId = bcMaIds[i];
                    world.SetCadEdgeMaterial(eId, maId);
                }
            }

            System.Diagnostics.Debug.Assert(!isMagneticField);
            /*
            {
                // 電界
                uint[] zeroEIds = { };
                var tzeroFixedCads = world.GetZeroFieldFixedCads(tQuantityId);
                foreach (uint eId in zeroEIds)
                {
                    // 複素数(辺方向成分)
                    var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                    tzeroFixedCads.Add(fixedCad);
                }
                var zzeroFixedCads = world.GetZeroFieldFixedCads(zQuantityId);
                foreach (uint eId in zeroEIds)
                {
                    // 複素数
                    var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                    zzeroFixedCads.Add(fixedCad);
                }
            }
            */
            // ポート条件
            // Evanescent ABCを適用する境界(左右と上)
            uint[][] portEIdss = new uint[4][] {
                new uint[] { 1 },
                new uint[] { 2 },
                new uint[] { 3 },
                new uint[] { 4 }
            };
            double[] portEp = new double[4] {
                claddingEp, claddingEp, claddingEp, claddingEp
            };
            IList<PortCondition> portConditions = world.GetPortConditions(tQuantityId);
            foreach (uint[] portEIds in portEIdss)
            {
                PortCondition portCondition = new PortCondition(portEIds, CadElementType.Edge, FieldValueType.ZScalar);
                portConditions.Add(portCondition);
            }

            world.MakeElements();

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
            model.Title = "Waveguide2D Eigen Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "h/λ",
                Minimum = sFreq,
                Maximum = eFreq
            };
            var axis2 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "β/k0",
                Minimum = Math.Sqrt(claddingEp),
                Maximum = Math.Sqrt(Math.Max(coreEpzz, Math.Max(coreEpxx, coreEpyy)))
            };
            model.Axes.Add(axis1);
            model.Axes.Add(axis2);
            var series1 = new LineSeries
            {
                //Title = "β/k0",
                LineStyle = LineStyle.None,
                MarkerType = MarkerType.Circle
            };
            model.Series.Add(series1);
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();

            uint valueId = 0;
            uint vecValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    zQuantityId, false, FieldShowType.ZReal);
                // Vector2
                vecValueId = world.AddFieldValue(FieldValueType.ZVector2, FieldDerivativeType.Value,
                    zQuantityId, true, FieldShowType.ZReal);

                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                var vectorDrawer = new VectorFieldDrawer(
                    vecValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
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
                // 波数
                double k0 = normalizedFreq * 2.0 * Math.PI / coreHeight;
                // 角周波数
                double omega = k0 * Constants.C0;
                // 周波数
                double freq = omega / (2.0 * Math.PI);
                System.Diagnostics.Debug.WriteLine("h/λ: " + normalizedFreq);

                var FEM = new EMWaveguide2DOpenEigenFEM(world);
                FEM.IsMagneticField = isMagneticField;
                FEM.MaxEp = maxEp;
                FEM.PortEp = portEp.ToList();
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] betas = FEM.Betas;
                System.Numerics.Complex[][] etVecs = FEM.EtEVecs;
                System.Numerics.Complex[][] ezVecs = FEM.EzEVecs;
                System.Numerics.Complex[][] coordExEyVecs = FEM.CoordExEyEVecs;

                int modeCnt = betas.Length;
                System.Diagnostics.Debug.Assert(modeCnt > 0);
                {
                    int iMode = 0; // 基本モードが収束するように求めているので基本モードのみ取得
                    System.Numerics.Complex beta = betas[iMode];
                    System.Numerics.Complex normalizedBeta = beta / k0;
                    double realNormalizedBeta = normalizedBeta.Real;
                    series1.Points.Add(new DataPoint(normalizedFreq, realNormalizedBeta));
                }
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();

                int targetModeId = 0;
                // eigenExEy
                int dof = 2; // x,y成分
                int coCnt = coordExEyVecs[targetModeId].Length / dof;
                System.Numerics.Complex[] eigenCoordExEy = new System.Numerics.Complex[coCnt * dof];
                coordExEyVecs[targetModeId].CopyTo(eigenCoordExEy, 0);
                // ExEyを表示用にスケーリングする
                {
                    double maxValue = 0;
                    int cnt = eigenCoordExEy.Length;
                    // eigenCoordExEyはx,y成分の順に並んでいる
                    foreach (System.Numerics.Complex value in eigenCoordExEy)
                    {
                        double abs = value.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.2 * waveguideWidth;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            eigenCoordExEy[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                world.UpdateBubbleFieldValueValuesFromCoordValues(vecValueId, FieldDerivativeType.Value, eigenCoordExEy);

                // eigenEz
                int nodeCnt = ezVecs[targetModeId].Length;
                System.Numerics.Complex[] eigenEz = new System.Numerics.Complex[nodeCnt];
                ezVecs[targetModeId].CopyTo(eigenEz, 0);
                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, eigenEz);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
