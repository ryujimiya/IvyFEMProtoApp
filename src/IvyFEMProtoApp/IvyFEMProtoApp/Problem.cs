using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using IvyFEM;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace IvyFEMProtoApp
{
    class Problem
    {
        private double WaveguideWidth = 0;
        private double InputWGLength = 0;

        public Problem()
        {
            WaveguideWidth = 1.0;
            InputWGLength = 1.0 * WaveguideWidth;
        }

        public void MakeBluePrint(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<Vector2> pts = new List<Vector2>();
                pts.Add(new Vector2(0.0f, (float)WaveguideWidth));  // 頂点1
                pts.Add(new Vector2(0.0f, 0.0f)); // 頂点2
                pts.Add(new Vector2((float)InputWGLength, 0.0f)); // 頂点3
                pts.Add(new Vector2((float)InputWGLength, (float)(-InputWGLength))); // 頂点4
                pts.Add(new Vector2((float)(InputWGLength + WaveguideWidth), (float)(-InputWGLength))); // 頂点5
                pts.Add(new Vector2((float)(InputWGLength + WaveguideWidth), (float)WaveguideWidth)); // 頂点6
                var res = cad2D.AddPolygon(pts);
                //System.Diagnostics.Debug.WriteLine(res.Dump());
                //System.Diagnostics.Debug.WriteLine(cad2D.Dump());
                //AlertWindow.ShowText(res.Dump());
                //AlertWindow.ShowText(cad2D.Dump());
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
        }

        public void MakeCoarseMesh(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<Vector2> pts = new List<Vector2>();
                pts.Add(new Vector2(0.0f, (float)WaveguideWidth));  // 頂点1
                pts.Add(new Vector2(0.0f, 0.0f)); // 頂点2
                pts.Add(new Vector2((float)InputWGLength, 0.0f)); // 頂点3
                pts.Add(new Vector2((float)InputWGLength, (float)(-InputWGLength))); // 頂点4
                pts.Add(new Vector2((float)(InputWGLength + WaveguideWidth), (float)(-InputWGLength))); // 頂点5
                pts.Add(new Vector2((float)(InputWGLength + WaveguideWidth), (float)WaveguideWidth)); // 頂点6
                var res = cad2D.AddPolygon(pts);
            }

            Mesher2D mesher2D = new Mesher2D(cad2D);

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
        }

        public void MakeMesh(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<Vector2> pts = new List<Vector2>();
                pts.Add(new Vector2(0.0f, (float)WaveguideWidth));  // 頂点1
                pts.Add(new Vector2(0.0f, 0.0f)); // 頂点2
                pts.Add(new Vector2((float)InputWGLength, 0.0f)); // 頂点3
                pts.Add(new Vector2((float)InputWGLength, (float)(-InputWGLength))); // 頂点4
                pts.Add(new Vector2((float)(InputWGLength + WaveguideWidth), (float)(-InputWGLength))); // 頂点5
                pts.Add(new Vector2((float)(InputWGLength + WaveguideWidth), (float)WaveguideWidth)); // 頂点6
                var res = cad2D.AddPolygon(pts);
            }

            double eLen = 0.1;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
        }

        public void InterseMatrixSample()
        {
            //       1  1 -1
            // A =  -2  0  1
            //       0  2  1
            double[] a = new double[9] { 1, -2, 0, 1, 0, 2, -1, 1, 1 };
            var A = new IvyFEM.Lapack.DoubleMatrix(a, 3, 3, false);
            var X = IvyFEM.Lapack.DoubleMatrix.Inverse(A);
            string CRLF = System.Environment.NewLine;
            string ret =
                "      1  1 -1" + CRLF +
                "A =  -2  0  1" + CRLF +
                "      0  2  1" + CRLF +
                CRLF +
                "            1  -2  -3  1" + CRLF +
                "X = A^-1 = ---  2   1  1" + CRLF +
                "            4  -4  -2  2" + CRLF +
                CRLF +
                CRLF +
                X.Dump();

            var C = A * X;
            ret += "C = A * X" + CRLF +
                " will be identity matrix" + CRLF +
                C.Dump();

            System.Diagnostics.Debug.WriteLine(ret);
            //AlertWindow.ShowText(ret);
        }

        public void EigenValueSample()
        {
            //      3  4 -5  3
            //      0  1  8  0
            // X =  0  0  2 -1
            //      0  0  0  1
            double[] x = new double[16]
            {
                3, 0, 0, 0,
                4, 1, 0, 0,
                -5, 8, 2, 0,
                3, 0, -1, 1
            };
            var X = new IvyFEM.Lapack.DoubleMatrix(x, 4, 4, false);
            System.Numerics.Complex[] eVals;
            System.Numerics.Complex[][] eVecs;
            IvyFEM.Lapack.Functions.dgeev(X.Buffer, X.RowSize, X.ColumnSize,
                out eVals, out eVecs);

            string ret;
            string CRLF = System.Environment.NewLine;

            ret = "      3  4 -5  3" + CRLF +
                  "      0  1  8  0" + CRLF +
                  " X =  0  0  2 -1" + CRLF +
                  "      0  0  0  1" + CRLF;
            ret += "Eigen Value : 3, 2, 1, 1" + CRLF;
            ret += CRLF;
            ret += CRLF;
            for (int i = 0; i < eVals.Length; i++)
            {
                ret += "eVal[" + i + "] = " + eVals[i].ToString() + CRLF;
            }
            System.Diagnostics.Debug.WriteLine(ret);
            AlertWindow.ShowDialog(ret);
        }

        public void WaveguideProblem(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<Vector2> pts = new List<Vector2>();
                pts.Add(new Vector2(0.0f, (float)WaveguideWidth));  // 頂点1
                pts.Add(new Vector2(0.0f, 0.0f)); // 頂点2
                pts.Add(new Vector2((float)InputWGLength, 0.0f)); // 頂点3
                pts.Add(new Vector2((float)InputWGLength, (float)(-InputWGLength))); // 頂点4
                pts.Add(new Vector2((float)(InputWGLength + WaveguideWidth), (float)(-InputWGLength))); // 頂点5
                pts.Add(new Vector2((float)(InputWGLength + WaveguideWidth), (float)WaveguideWidth)); // 頂点6
                var res = cad2D.AddPolygon(pts);
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            WPFUtils.DoEvents();

            double eLen = WaveguideWidth * 0.05;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            IDrawer meshDrawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;

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

            uint eId1 = 1;
            uint eId2 = 4;
            uint lId = 1;
            world.SetCadEdgeMaterial(eId1, maId);
            world.SetCadEdgeMaterial(eId2, maId);
            world.SetCadLoopMaterial(lId, maId);

            world.IncidentPortId = 0;
            world.IncidentModeId = 0;
            IList<uint> port1EIds = new List<uint>();
            IList<uint> port2EIds = new List<uint>();
            port1EIds.Add(eId1);
            port2EIds.Add(eId2);
            var portEIdss = world.PortEIdss;
            portEIdss.Clear();
            portEIdss.Add(port1EIds);
            portEIdss.Add(port2EIds);

            uint[] zeroEIds = { 2, 3, 5, 6 };
            IList<uint> zeroECadIds = world.ZeroECadIds;
            zeroECadIds.Clear();
            foreach (uint eId in zeroEIds)
            {
                zeroECadIds.Add(eId);
            }

            world.MakeElements();

            double sFreq = 1.0;
            double eFreq = 2.0;
            int freqDiv = 50;

            var chartWin = new ChartWindow();
            chartWin.Show();
            var model = new PlotModel();
            chartWin.plot.Model = model;
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
            var datas = new List<DataPoint>();
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();

            const int dof = 2; // 複素数
            world.ClearFieldValue();
            uint valueId = world.AddFieldValue(FieldValueType.ZSCALAR, FieldDerivationType.VALUE,
                dof, false, FieldShowType.ABS);
            mainWindow.IsFieldDraw = true;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            fieldDrawerArray.Clear();
            IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.VALUE, true, world,
                valueId, FieldDerivationType.VALUE);
            fieldDrawerArray.Add(faceDrawer);
            //IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivationType.VALUE, true, world);
            //fieldDrawerArray.Add(edgeDrawer);
            mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            //mainWindow.glControl.Invalidate();
            //WPFUtils.DoEvents();

            for (int iFreq = 0; iFreq < freqDiv + 1; iFreq++)
            {
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波数
                double k0 = normalizedFreq * Math.PI / WaveguideWidth;
                // 波長
                double waveLength = 2.0 * Math.PI / k0;
                System.Diagnostics.Debug.WriteLine("2W/λ: " + normalizedFreq);

                var FEM = new EMWaveguide2DHPlaneFEM(world);
                FEM.WaveLength = waveLength;
                FEM.Solve();
                System.Numerics.Complex[] Ez = FEM.Ez;
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

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivationType.VALUE, Ez);

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                WPFUtils.DoEvents();
            }
        }

        public void ElasticProblem(MainWindow mainWindow, bool isCalcStress)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<Vector2> pts = new List<Vector2>();
                pts.Add(new Vector2(0.0f, 0.0f));
                pts.Add(new Vector2(5.0f, 0.0f));
                pts.Add(new Vector2(5.0f, 1.0f));
                pts.Add(new Vector2(0.0f, 1.0f));
                var res = cad2D.AddPolygon(pts);
            }

            /*
            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            */

            double eLen = 0.2;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;

            world.ClearMaterial();
            ElasticMaterial elasticMa = new ElasticMaterial();
            elasticMa.SetYoungPoisson(10.0, 0.3);
            elasticMa.GravityX = 0;
            elasticMa.GravityY = 0;
            uint maId = world.AddMaterial(elasticMa);

            uint lId = 1;
            world.SetCadLoopMaterial(lId, maId);

            uint[] zeroEIds = { 4 };
            IList<uint> zeroECadIds = world.ZeroECadIds;
            zeroECadIds.Clear();
            foreach (uint eId in zeroEIds)
            {
                zeroECadIds.Add(eId);
            }

            /*
            uint[] fixedCadIds = { 2 };
            CadElementType[] fixedCadElemType = { CadElementType.EDGE };
            int[] fixedDofIndexs = new int[] { 1 };  // 0: X, 1: Y
            double[] fixedValues = new double[] { 0 };
            */
            uint[] fixedCadIds = { 2, 2 };
            CadElementType[] fixedCadElemType = { CadElementType.EDGE, CadElementType.EDGE };
            int[] fixedDofIndexs = new int[] { 0, 1 };  // 0: X, 1: Y
            double[] fixedValues = new double[] { 0, 0 };
            System.Diagnostics.Debug.Assert(fixedCadIds.Length == fixedCadElemType.Length);
            System.Diagnostics.Debug.Assert(fixedCadIds.Length == fixedDofIndexs.Length);
            System.Diagnostics.Debug.Assert(fixedCadIds.Length == fixedValues.Length);

            IList<FieldFixedCad> fixedCads = world.FieldFixedCads;
            fixedCads.Clear();
            for (int iCad = 0; iCad < fixedCadIds.Length; iCad++)
            {
                uint cadId = fixedCadIds[iCad];
                CadElementType cadElemType = fixedCadElemType[iCad];
                int dofIndex = fixedDofIndexs[iCad];
                double value = fixedValues[iCad];
                var fixedCad = new FieldFixedCad(cadId, cadElemType,
                    FieldValueType.VECTOR2, dofIndex, value);
                fixedCads.Add(fixedCad);
            }
            /*
            var fixedCadY = world.FieldFixedCads[0];
            */
            var fixedCadX = world.FieldFixedCads[0];
            var fixedCadY = world.FieldFixedCads[1];

            world.MakeElements();

            const int dof = 2; // VECTOR2
            world.ClearFieldValue();
            uint valueId = world.AddFieldValue(FieldValueType.VECTOR2, FieldDerivationType.VALUE,
                dof, false, FieldShowType.SCALAR);
            uint eqStressValueId = 0;
            uint stressValueId = 0;
            if (isCalcStress)
            {
                const int eqStressDof = 1;
                eqStressValueId = world.AddFieldValue(FieldValueType.SCALAR, FieldDerivationType.VALUE,
                    eqStressDof, true, FieldShowType.SCALAR);
                const int stressDof = 6;
                stressValueId = world.AddFieldValue(FieldValueType.SYMMETRICAL_TENSOR2, FieldDerivationType.VALUE,
                    stressDof, true, FieldShowType.SCALAR);
            }
            mainWindow.IsFieldDraw = true;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            fieldDrawerArray.Clear();
            if (isCalcStress)
            {
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.VALUE, false, world,
                    eqStressValueId, FieldDerivationType.VALUE, 0, 0.5);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer vectorDrawer = new VectorFieldDrawer(stressValueId, FieldDerivationType.VALUE, world);
                fieldDrawerArray.Add(vectorDrawer);
            }
            else
            {
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.VALUE, false, world);
                fieldDrawerArray.Add(faceDrawer);
            }
            IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivationType.VALUE, false, world);
            fieldDrawerArray.Add(edgeDrawer);
            IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(valueId, FieldDerivationType.VALUE, true, world);
            fieldDrawerArray.Add(edgeDrawer2);
            mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            //mainWindow.glControl.Invalidate();
            //WPFUtils.DoEvents();

            double t = 0;
            double dt = 0.5;
            for (int itr = 0; itr <= 100; itr++)
            {
                /*
                fixedCadY.Value = Math.Sin(t * Math.PI * 0.1 * 2);
                */
                fixedCadX.Value = 0;
                fixedCadY.Value = Math.Sin(t * Math.PI * 0.1 * 2);

                var FEM = new Elastic2DFEM(world);
                FEM.Solve();
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValue(valueId, FieldDerivationType.VALUE, U);
                if (isCalcStress)
                {
                    Elastic2DFEM.SetStressValue(valueId, stressValueId, eqStressValueId, world);
                }

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void ElasticTDProblem(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<Vector2> pts = new List<Vector2>();
                pts.Add(new Vector2(0.0f, 0.0f));
                pts.Add(new Vector2(5.0f, 0.0f));
                pts.Add(new Vector2(5.0f, 1.0f));
                pts.Add(new Vector2(0.0f, 1.0f));
                var res = cad2D.AddPolygon(pts);
            }

            /*
            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            */

            double eLen = 0.2;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;

            world.ClearMaterial();
            ElasticMaterial elasticMa = new ElasticMaterial();
            //elasticMa.SetYoungPoisson(10.0, 0.3);
            elasticMa.SetYoungPoisson(50.0, 0.3);
            elasticMa.GravityX = 0;
            elasticMa.GravityY = 0;
            uint maId = world.AddMaterial(elasticMa);

            uint lId = 1;
            world.SetCadLoopMaterial(lId, maId);

            uint[] zeroEIds = { 4 };
            IList<uint> zeroECadIds = world.ZeroECadIds;
            zeroECadIds.Clear();
            foreach (uint eId in zeroEIds)
            {
                zeroECadIds.Add(eId);
            }

            uint[] fixedCadIds = { 2, 2 };
            CadElementType[] fixedCadElemType = { CadElementType.EDGE, CadElementType.EDGE };
            int[] fixedDofIndexs = new int[] { 0, 1 };  // 0: X, 1: Y
            double[] fixedValues = new double[] { 0, 0 };
            System.Diagnostics.Debug.Assert(fixedCadIds.Length == fixedCadElemType.Length);
            System.Diagnostics.Debug.Assert(fixedCadIds.Length == fixedDofIndexs.Length);
            System.Diagnostics.Debug.Assert(fixedCadIds.Length == fixedValues.Length);

            IList<FieldFixedCad> fixedCads = world.FieldFixedCads;
            fixedCads.Clear();
            for (int iCad = 0; iCad < fixedCadIds.Length; iCad++)
            {
                uint cadId = fixedCadIds[iCad];
                CadElementType cadElemType = fixedCadElemType[iCad];
                int dofIndex = fixedDofIndexs[iCad];
                double value = fixedValues[iCad];
                var fixedCad = new FieldFixedCad(cadId, cadElemType,
                    FieldValueType.VECTOR2, dofIndex, value);
                fixedCads.Add(fixedCad);
            }
            var fixedCadX = world.FieldFixedCads[0];
            var fixedCadY = world.FieldFixedCads[1];

            world.MakeElements();

            const int dof = 2; // VECTOR2
            world.ClearFieldValue();
            uint valueId = world.AddFieldValue(FieldValueType.VECTOR2,
                FieldDerivationType.VALUE|FieldDerivationType.VELOCITY|FieldDerivationType.ACCELERATION,
                dof, false, FieldShowType.SCALAR);
            uint prevValueId = world.AddFieldValue(FieldValueType.VECTOR2,
                FieldDerivationType.VALUE | FieldDerivationType.VELOCITY | FieldDerivationType.ACCELERATION,
                dof, false, FieldShowType.SCALAR);
            mainWindow.IsFieldDraw = true;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            fieldDrawerArray.Clear();
            IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.VALUE, false, world);
            fieldDrawerArray.Add(faceDrawer);
            IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivationType.VALUE, false, world);
            fieldDrawerArray.Add(edgeDrawer);
            IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(valueId, FieldDerivationType.VALUE, true, world);
            fieldDrawerArray.Add(edgeDrawer2);
            mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            //mainWindow.glControl.Invalidate();
            //WPFUtils.DoEvents();

            double t = 0;
            double dt = 0.5;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int itr = 0; itr <= 200; itr++)
            {
                fixedCadX.Value = 0;
                fixedCadY.Value = Math.Sin(t * Math.PI * 0.1 * 2);

                var FEM = new Elastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueId, prevValueId);
                FEM.Solve();
                //double[] U = FEM.U;

                FEM.UpdateFieldValues();

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
