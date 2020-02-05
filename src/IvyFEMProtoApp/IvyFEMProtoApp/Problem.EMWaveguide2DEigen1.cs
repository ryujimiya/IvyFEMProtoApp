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
        public void Waveguide2DEigenProblem1(MainWindow mainWindow, bool isMagneticField)
        {
            double waveguideWidth = 1.0;
            double waveguideHeight = 0.5 * waveguideWidth;
            // 磁界？ default: false
            // false: 電界
            // true: 磁界
            //bool isMagneticField = false;
            //bool isMagneticField = true;
            double eLen = waveguideWidth * 0.05;
            double sFreq = 1.0;
            double eFreq = 2.0;
            int freqDiv = 50;

            CadObject2D cad = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideHeight));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(waveguideWidth, 0.0));
                pts.Add(new OpenTK.Vector2d(waveguideWidth, waveguideHeight));
                var res = cad.AddPolygon(pts);
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
            uint tQuantityId;
            uint zQuantityId;
            {
                uint tdof = 1; // 複素数(辺方向成分)
                uint tfeOrder = 1;
                tQuantityId = world.AddQuantity(tdof, tfeOrder, FiniteElementType.Edge);

                uint zdof = 1; // 複素数
                uint zfeOrder = 1;
                zQuantityId = world.AddQuantity(zdof, zfeOrder, FiniteElementType.ScalarLagrange);
            }

            int loopCnt = 1;
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
                uint maId = world.AddMaterial(vacuumMa);

                for (int i = 0; i < loopCnt; i++)
                {
                    uint lId = (uint)(i + 1);
                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            if (isMagneticField)
            {
                // 磁界
                // 自然境界
            }
            else
            {
                // 電界
                uint[] zeroEIds = { 1, 2, 3, 4 };
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
                Title = "2W/λ",
                Minimum = sFreq,
                Maximum = eFreq
            };
            var axis2 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "β/k0",
                Minimum = 0,
                //Maximum = 1.0
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
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer vectorDrawer = new VectorFieldDrawer(
                    vecValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
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
                double k0 = normalizedFreq * Math.PI / waveguideWidth;
                // 角周波数
                double omega = k0 * Constants.C0;
                // 周波数
                double freq = omega / (2.0 * Math.PI);
                System.Diagnostics.Debug.WriteLine("2W/λ: " + normalizedFreq);

                var FEM = new EMWaveguide2DEigenFEM(world);
                FEM.IsMagneticField = isMagneticField;
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] betas = FEM.Betas;
                System.Numerics.Complex[][] etVecs = FEM.EtEVecs;
                System.Numerics.Complex[][] ezVecs = FEM.EzEVecs;
                System.Numerics.Complex[][] coordExEyVecs = FEM.CoordExEyEVecs;

                int modeCnt = betas.Length;
                for (int iMode = 0; iMode < modeCnt; iMode++)
                {
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
