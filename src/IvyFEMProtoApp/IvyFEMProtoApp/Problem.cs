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
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, WaveguideWidth));  // 頂点1
                pts.Add(new OpenTK.Vector2d(0.0, 0.0)); // 頂点2
                pts.Add(new OpenTK.Vector2d(InputWGLength, 0.0)); // 頂点3
                pts.Add(new OpenTK.Vector2d(InputWGLength, (-InputWGLength))); // 頂点4
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), (-InputWGLength))); // 頂点5
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), WaveguideWidth)); // 頂点6
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
            mainWindow.glControl.Update();
        }

        public void MakeCoarseMesh(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, WaveguideWidth));  // 頂点1
                pts.Add(new OpenTK.Vector2d(0.0, 0.0)); // 頂点2
                pts.Add(new OpenTK.Vector2d(InputWGLength, 0.0)); // 頂点3
                pts.Add(new OpenTK.Vector2d(InputWGLength, (-InputWGLength))); // 頂点4
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), (-InputWGLength))); // 頂点5
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), WaveguideWidth)); // 頂点6
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
            mainWindow.glControl.Update();
        }

        public void MakeMesh(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, WaveguideWidth));  // 頂点1
                pts.Add(new OpenTK.Vector2d(0.0, 0.0)); // 頂点2
                pts.Add(new OpenTK.Vector2d(InputWGLength, 0.0)); // 頂点3
                pts.Add(new OpenTK.Vector2d(InputWGLength, (-InputWGLength))); // 頂点4
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), (-InputWGLength))); // 頂点5
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), WaveguideWidth)); // 頂点6
                var res = cad2D.AddPolygon(pts);
            }

            double eLen = WaveguideWidth * 0.05;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
        }

        public void InterseMatrixExample()
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
            AlertWindow.ShowDialog(ret);
        }

        public void LinearEquationExample()
        {
            //      3 -2  1      7
            // A =  1  2 -2  b = 2
            //      4  3 -2      7
            double[] a = new double[9]
            {
                3, 1, 4,
                -2, 2, 3,
                1, -2, -2
            };
            double[] b = new double[3] { 7, 2, 7 };
            var A = new IvyFEM.Lapack.DoubleMatrix(a, 3, 3, false);
            double[] x;
            int xRow;
            int xCol;
            IvyFEM.Lapack.Functions.dgesv(out x, out xRow, out xCol, 
                A.Buffer, A.RowLength, A.ColumnLength, b, b.Length, 1);

            string ret;
            string CRLF = System.Environment.NewLine;

            ret = "      3 -2  1      7" + CRLF +
                  " A =  1  2 -2  b = 2" + CRLF +
                  "      4  3 -2      7" + CRLF +
                  "Answer : 2, -1, -1" + CRLF;
            ret += CRLF;
            ret += CRLF;
            for (int i = 0; i < x.Length; i++)
            {
                ret += "x[" + i + "] = " + x[i] + CRLF;
            }
            System.Diagnostics.Debug.WriteLine(ret);
            AlertWindow.ShowDialog(ret);
        }

        public void EigenValueExample()
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
            IvyFEM.Lapack.Functions.dgeev(X.Buffer, X.RowLength, X.ColumnLength,
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


        public void LisExample()
        {
            int ret;
            int comm = IvyFEM.Lis.Constants.LisCommWorld;
            int n = 12;
            int gn = 0;
            int @is = 0;
            int ie = 0;

            using (IvyFEM.Lis.LisInitializer LisInitializer = new IvyFEM.Lis.LisInitializer())
            using (var A = new IvyFEM.Lis.LisMatrix(comm))
            using (var b = new IvyFEM.Lis.LisVector(comm))
            using (var u = new IvyFEM.Lis.LisVector(comm))
            using (var x = new IvyFEM.Lis.LisVector(comm))
            using (var solver = new IvyFEM.Lis.LisSolver())
            {
                ret = A.SetSize(0, n);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = A.GetSize(out n, out gn);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = A.GetRange(out @is, out ie);
                System.Diagnostics.Debug.Assert(ret == 0);

                for (int i = @is; i < ie; i++)
                {
                    if (i > 0)
                    {
                        ret = A.SetValue(IvyFEM.Lis.SetValueFlag.LisInsValue, i, i - 1, -1.0);
                        System.Diagnostics.Debug.Assert(ret == 0);
                    }
                    if (i < gn - 1)
                    {
                        ret = A.SetValue(IvyFEM.Lis.SetValueFlag.LisInsValue, i, i + 1, -1.0);
                        System.Diagnostics.Debug.Assert(ret == 0);
                    }
                    ret = A.SetValue(IvyFEM.Lis.SetValueFlag.LisInsValue, i, i, 2.0);
                    System.Diagnostics.Debug.Assert(ret == 0);
                }
                ret = A.SetType(IvyFEM.Lis.MatrixType.LisMatrixCSR);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = A.Assemble();
                System.Diagnostics.Debug.Assert(ret == 0);

                ret = u.SetSize(0, n);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = b.SetSize(0, n);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = x.SetSize(0, n);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = u.SetAll(1.0);
                System.Diagnostics.Debug.Assert(ret == 0);

                ret = IvyFEM.Lis.LisMatrix.Matvec(A, u, b);
                System.Diagnostics.Debug.Assert(ret == 0);

                ret = solver.SetOption("-print mem");
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = solver.SetOptionC();
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = solver.Solve(A, b, x);
                System.Diagnostics.Debug.Assert(ret == 0);
                int iter;
                ret = solver.GetIter(out iter);

                string str = "";
                string CRLF = System.Environment.NewLine;
                str += "number of iterations = " + iter + CRLF;
                {
                    System.Numerics.Complex[] values = new System.Numerics.Complex[n];
                    ret = x.GetValues(0, n, values);
                    System.Diagnostics.Debug.Assert(ret == 0);
                    for (int i = 0; i < n; i++)
                    {
                        str += i + "  " + values[i] + CRLF;
                    }
                }
                System.Diagnostics.Debug.WriteLine(str);
                AlertWindow.ShowDialog(str);
            }

            using (IvyFEM.Lis.LisInitializer LisInitializer = new IvyFEM.Lis.LisInitializer())
            using (var v = new IvyFEM.Lis.LisVector(comm))
            {
                int n1 = 5;
                ret = v.SetSize(0, n1);
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = v.SetAll(new System.Numerics.Complex(1.0, 1.0));
                System.Diagnostics.Debug.Assert(ret == 0);
                ret = v.Conjugate();
                System.Diagnostics.Debug.Assert(ret == 0);
                System.Numerics.Complex[] values = new System.Numerics.Complex[n1];
                ret = v.GetValues(0, n1, values);
                System.Diagnostics.Debug.Assert(ret == 0);
                string str = "";
                string CRLF = System.Environment.NewLine;
                str += "Conjugate of (1, 1)" + CRLF;
                for (int i = 0; i < n1; i++)
                {
                    str += i + "  " + values[i] + CRLF;
                }
                System.Diagnostics.Debug.WriteLine(str);
                AlertWindow.ShowDialog(str);
            }
        }

        public void WaveguideProblem(MainWindow mainWindow)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, WaveguideWidth));  // 頂点1
                pts.Add(new OpenTK.Vector2d(0.0, 0.0)); // 頂点2
                pts.Add(new OpenTK.Vector2d(InputWGLength, 0.0)); // 頂点3
                pts.Add(new OpenTK.Vector2d(InputWGLength, (-InputWGLength))); // 頂点4
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), (-InputWGLength))); // 頂点5
                pts.Add(new OpenTK.Vector2d((InputWGLength + WaveguideWidth), WaveguideWidth)); // 頂点6
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
            mainWindow.glControl.Update();
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
            mainWindow.glControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 1; // 複素数
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder);
            }

            uint eId1 = 1;
            uint eId2 = 4;
            uint lId = 1;
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

                world.SetCadEdgeMaterial(eId1, maId);
                world.SetCadEdgeMaterial(eId2, maId);
                world.SetCadLoopMaterial(lId, maId);
            }
            {
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
            }
            uint[] zeroEIds = { 2, 3, 5, 6 };
            var zeroFixedCads = world.ZeroFieldFixedCads;
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                uint dof = 1; // 複素数
                double value = 0;
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar,
                    quantityId, dof, value);
                zeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            double sFreq = 1.0;
            double eFreq = 2.0;
            int freqDiv = 50;

            var chartWin = new ChartWindow();
            chartWin.Owner = mainWindow;
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

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                uint dof = 1; // 複素数
                world.ClearFieldValue();
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivationType.Value,
                    quantityId, dof, false, FieldShowType.ZAbs);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.Value, true, world,
                    valueId, FieldDerivationType.Value);
                fieldDrawerArray.Add(faceDrawer);
                //IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivationType.VALUE, true, world);
                //fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            for (int iFreq = 0; iFreq < freqDiv + 1; iFreq++)
            {
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波数
                double k0 = normalizedFreq * Math.PI / WaveguideWidth;
                // 波長
                double waveLength = 2.0 * Math.PI / k0;
                System.Diagnostics.Debug.WriteLine("2W/λ: " + normalizedFreq);

                var FEM = new EMWaveguide2DHPlaneFEM(world);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    //FEM.Solver = solver;
                }
                {
                    //var solver = new IvyFEM.Linear.LisEquationSolver();
                    //solver.Method = IvyFEM.Linear.LisEquationSolverMethod.Default;
                    //FEM.Solver = solver;
                }
                {
                    var solver = new IvyFEM.Linear.IvyFEMEquationSolver();
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.COCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
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

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivationType.Value, Ez);

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
            }
        }

        public void ElasticProblem(MainWindow mainWindow, bool isCalcStress, bool isSaintVenant)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                var res = cad2D.AddPolygon(pts);
            }

            double eLen = 0.1;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 2; // Vector2
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder);
            }

            {
                world.ClearMaterial();
                uint maId = 0;
                if (isSaintVenant)
                {
                    var ma = new SaintVenantHyperelasticMaterial();
                    ma.SetYoungPoisson(10.0, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1;
                    maId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new LinearElasticMaterial();
                    ma.SetYoungPoisson(10.0, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            uint[] zeroEIds = { 4 };
            var zeroFixedCads = world.ZeroFieldFixedCads;
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                uint dof = 2; // Vector2
                double value = 0;
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2,
                    quantityId, dof, value);
                zeroFixedCads.Add(fixedCad);
            }

            FieldFixedCad fixedCadX;
            FieldFixedCad fixedCadY;
            {
                // DofIndex 0: X 1: Y
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge, DofIndex = (uint)0, Value = 0.0 },
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge, DofIndex = (uint)1, Value = 0.0 }
                };
                IList<FieldFixedCad> fixedCads = world.FieldFixedCads;
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    var fixedCad = new FieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, quantityId, data.DofIndex, data.Value);
                    fixedCads.Add(fixedCad);
                }
                fixedCadX = world.FieldFixedCads[0];
                fixedCadY = world.FieldFixedCads[1];
            }

            world.MakeElements();

            uint valueId = 0;
            uint eqStressValueId = 0;
            uint stressValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                uint dof = 2; // Vector2
                world.ClearFieldValue();
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivationType.Value,
                    quantityId, dof, false, FieldShowType.Real);
                if (isCalcStress)
                {
                    const int eqStressDof = 1;
                    eqStressValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivationType.Value,
                        quantityId, eqStressDof, true, FieldShowType.Real);
                    const int stressDof = 3;
                    stressValueId = world.AddFieldValue(FieldValueType.SymmetricTensor2, FieldDerivationType.Value,
                        quantityId, stressDof, true, FieldShowType.Real);
                }
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                if (isCalcStress)
                {
                    IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.Value, false, world,
                        eqStressValueId, FieldDerivationType.Value, 0, 0.5);
                    fieldDrawerArray.Add(faceDrawer);
                    IFieldDrawer vectorDrawer = new VectorFieldDrawer(stressValueId, FieldDerivationType.Value, world);
                    fieldDrawerArray.Add(vectorDrawer);
                }
                else
                {
                    IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.Value, false, world);
                    fieldDrawerArray.Add(faceDrawer);
                }
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivationType.Value, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(valueId, FieldDerivationType.Value, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 0.05;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                fixedCadX.DoubleValue = 0;
                fixedCadY.DoubleValue = Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic2DFEM(world);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    //FEM.Solver = solver;
                }
                {
                    //var solver = new IvyFEM.Linear.LisEquationSolver();
                    //solver.Method = IvyFEM.Linear.LisEquationSolverMethod.Default;
                    //FEM.Solver = solver;
                }
                {
                    var solver = new IvyFEM.Linear.IvyFEMEquationSolver();
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.Solve();
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivationType.Value, U);
                if (isCalcStress)
                {
                    Elastic2DBaseFEM.SetStressValue(valueId, stressValueId, eqStressValueId, world);
                }

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void ElasticTDProblem(MainWindow mainWindow, bool isSaintVenant)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                var res = cad2D.AddPolygon(pts);
            }

            double eLen = 0.1;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 2; // Vector2
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder);
            }

            {
                world.ClearMaterial();
                uint maId = 0;
                if (isSaintVenant)
                {
                    var ma = new SaintVenantHyperelasticMaterial();
                    //ma.SetYoungPoisson(10.0, 0.3);
                    ma.SetYoungPoisson(50.0, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1;
                    maId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new LinearElasticMaterial();
                    //ma.SetYoungPoisson(10.0, 0.3);
                    ma.SetYoungPoisson(50.0, 0.3);
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = 1;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            uint[] zeroEIds = { 4 };
            var zeroFixedCads = world.ZeroFieldFixedCads;
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                uint dof = 2; // Vector2
                double value = 0;
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2,
                    quantityId, dof, value);
                zeroFixedCads.Add(fixedCad);
            }

            FieldFixedCad fixedCadX;
            FieldFixedCad fixedCadY;
            {
                // DofIndex 0: X 1: Y
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge, DofIndex = (uint)0, Value = 0.0 },
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge, DofIndex = (uint)1, Value = 0.0 }
                };
                IList<FieldFixedCad> fixedCads = world.FieldFixedCads;
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    var fixedCad = new FieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, quantityId, data.DofIndex, data.Value);
                    fixedCads.Add(fixedCad);
                }
                fixedCadX = world.FieldFixedCads[0];
                fixedCadY = world.FieldFixedCads[1];
            }

            world.MakeElements();

            uint valueId = 0;
            uint prevValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                uint dof = 2; // Vector2
                world.ClearFieldValue();
                valueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivationType.Value | FieldDerivationType.Velocity | FieldDerivationType.Acceleration,
                    quantityId, dof, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivationType.Value | FieldDerivationType.Velocity | FieldDerivationType.Acceleration,
                    quantityId, dof, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivationType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(valueId, FieldDerivationType.Value, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(valueId, FieldDerivationType.Value, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 400; iTime++)
            {
                fixedCadX.DoubleValue = 0;
                fixedCadY.DoubleValue = Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Elastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueId, prevValueId);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    //FEM.Solver = solver;
                }
                {
                    //var solver = new IvyFEM.Linear.LisEquationSolver();
                    //solver.Method = IvyFEM.Linear.LisEquationSolverMethod.Default;
                    //FEM.Solver = solver;
                }
                {
                    var solver = new IvyFEM.Linear.IvyFEMEquationSolver();
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.Solve();
                //double[] U = FEM.U;

                FEM.UpdateFieldValues();

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void HyperelasticProblem(MainWindow mainWindow, bool isMooney)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                var res = cad2D.AddPolygon(pts);
            }

            double eLen = 0.1;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint uQuantityId;
            uint lQuantityId;
            {
                uint uDof = 2; // Vector2
                uint lDof = 1; // Scalar
                uint uFEOrder = 1;
                uint lFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder);
                lQuantityId = world.AddQuantity(lDof, lFEOrder);
            }

            if (isMooney)
            {
                // Mooney-Rivlin
                world.ClearMaterial();
                uint maId = 0;
                var ma = new MooneyRivlinHyperelasticMaterial();
                ma.IsCompressible = false;
                //ma.IsCompressible = true;
                //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                ma.C1 = 200;
                ma.C2 = 200;
                ma.GravityX = 0;
                ma.GravityY = 0;
                ma.MassDensity = 1.0;
                maId = world.AddMaterial(ma);

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }
            else
            {
                // Odgen
                world.ClearMaterial();
                uint maId = 0;
                var ma = new OgdenHyperelasticMaterial();
                double[] alphas = { 1.3, 5.0, -2.0 };
                double[] mus = { 6300e3, 1.2e3, -10e3 };
                //double[] alphas = { 2.0, -2.0 };
                //double[] mus = { 400, -400 };
                System.Diagnostics.Debug.Assert(alphas.Length == mus.Length);
                ma.IsCompressible = false;
                //ma.IsCompressible = true;
                //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                ma.SetAlphaMu(alphas.Length, alphas, mus);
                ma.GravityX = 0;
                ma.GravityY = 0;
                ma.MassDensity = 1.0;
                maId = world.AddMaterial(ma);

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            uint[] zeroEIds = { 4 };
            var zeroFixedCads = world.ZeroFieldFixedCads;
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                uint dof = 2; // Vector2
                double value = 0;
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2,
                    uQuantityId, dof, value);
                zeroFixedCads.Add(fixedCad);
            }

            FieldFixedCad fixedCadX;
            FieldFixedCad fixedCadY;
            {
                // DofIndex 0: X 1: Y
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge, DofIndex = (uint)0, Value = 0.0 },
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge, DofIndex = (uint)1, Value = 0.0 }
                };
                IList<FieldFixedCad> fixedCads = world.FieldFixedCads;
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    var fixedCad = new FieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, uQuantityId, data.DofIndex, data.Value);
                    fixedCads.Add(fixedCad);
                }
                fixedCadX = world.FieldFixedCads[0];
                fixedCadY = world.FieldFixedCads[1];
            }

            world.MakeElements();

            uint uValueId = 0;
            uint lValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                uint uDof = 2; // Vector2
                uint lDof = 1; // Scalar
                world.ClearFieldValue();
                uValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivationType.Value,
                    uQuantityId, uDof, false, FieldShowType.Real);
                lValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivationType.Value,
                    lQuantityId, lDof, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivationType.Value, false, world);
                // Lagrange未定乗数のサーモグラフィ表示
                //IFieldDrawer faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivationType.Value, false, world,
                //    lValueId, FieldDerivationType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(uValueId, FieldDerivationType.Value, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(uValueId, FieldDerivationType.Value, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 0.05;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                fixedCadX.DoubleValue = 0;
                fixedCadY.DoubleValue = Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Hyperelastic2DFEM(world);
                if (isMooney)
                {
                    // Mooney-Rivlin
                    {
                        //var solver = new IvyFEM.Linear.LapackEquationSolver();
                        //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                        //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                        //solver.IsOrderingToBandMatrix = true;
                        //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                        //FEM.Solver = solver;
                    }
                    {
                        //var solver = new IvyFEM.Linear.LisEquationSolver();
                        //solver.Method = IvyFEM.Linear.LisEquationSolverMethod.Default;
                        //FEM.Solver = solver;
                    }
                    {
                        var solver = new IvyFEM.Linear.IvyFEMEquationSolver();
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                        solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                        //solver.ConvRatioTolerance = 1.0e-14;
                        FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-10;
                        FEM.Solver = solver;
                    }
                }
                else
                {
                    // Ogden
                    {
                        var solver = new IvyFEM.Linear.LapackEquationSolver();
                        //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                        solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                        solver.IsOrderingToBandMatrix = true;
                        //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                        FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-10;
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
                        //solver.ConvRatioTolerance = 1.0e-14;
                        //FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-10;
                        //FEM.Solver = solver;
                    }
                }
                FEM.Solve();
                double[] U = FEM.U;

                world.UpdateFieldValueValuesFromNodeValues(uValueId, FieldDerivationType.Value, U);
                world.UpdateFieldValueValuesFromNodeValues(lValueId, FieldDerivationType.Value, U);
                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }

        public void HyperelasticTDProblem(MainWindow mainWindow, bool isMooney)
        {
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 0.0));
                pts.Add(new OpenTK.Vector2d(5.0, 1.0));
                pts.Add(new OpenTK.Vector2d(0.0, 1.0));
                var res = cad2D.AddPolygon(pts);
            }

            double eLen = 0.1;
            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint uQuantityId;
            uint lQuantityId;
            {
                uint uDof = 2; // Vector2
                uint lDof = 1; // Scalar
                uint uFEOrder = 1;
                uint lFEOrder = 1;
                uQuantityId = world.AddQuantity(uDof, uFEOrder);
                lQuantityId = world.AddQuantity(lDof, lFEOrder);
            }

            if (isMooney)
            {
                // Mooney-Rivlin
                world.ClearMaterial();
                uint maId = 0;
                var ma = new MooneyRivlinHyperelasticMaterial();
                ma.IsCompressible = false;
                //ma.IsCompressible = true;
                //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                ma.C1 = 200;
                ma.C2 = 200;
                ma.GravityX = 0;
                ma.GravityY = 0;
                ma.MassDensity = 1.0;
                maId = world.AddMaterial(ma);

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }
            else
            {
                // Odgen
                world.ClearMaterial();
                uint maId = 0;
                var ma = new OgdenHyperelasticMaterial();
                double[] alphas = { 1.3, 5.0, -2.0 };
                double[] mus = { 6300e3, 1.2e3, -10e3 };
                //double[] alphas = { 2.0, -2.0 };
                //double[] mus = { 400, -400 };
                System.Diagnostics.Debug.Assert(alphas.Length == mus.Length);
                ma.IsCompressible = false;
                //ma.IsCompressible = true;
                //ma.D1 = 1.0; // 非圧縮性のときは必要なし
                ma.SetAlphaMu(alphas.Length, alphas, mus);
                ma.GravityX = 0;
                ma.GravityY = 0;
                ma.MassDensity = 1.0;
                maId = world.AddMaterial(ma);

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            uint[] zeroEIds = { 4 };
            var zeroFixedCads = world.ZeroFieldFixedCads;
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                uint dof = 2; // Vector2
                double value = 0;
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2,
                    uQuantityId, dof, value);
                zeroFixedCads.Add(fixedCad);
            }

            FieldFixedCad fixedCadX;
            FieldFixedCad fixedCadY;
            {
                // DofIndex 0: X 1: Y
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge, DofIndex = (uint)0, Value = 0.0 },
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge, DofIndex = (uint)1, Value = 0.0 }
                };
                IList<FieldFixedCad> fixedCads = world.FieldFixedCads;
                fixedCads.Clear();
                foreach (var data in fixedCadDatas)
                {
                    var fixedCad = new FieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Vector2, uQuantityId, data.DofIndex, data.Value);
                    fixedCads.Add(fixedCad);
                }
                fixedCadX = world.FieldFixedCads[0];
                fixedCadY = world.FieldFixedCads[1];
            }

            world.MakeElements();

            uint uValueId = 0;
            uint prevUValueId = 0;
            uint lValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                uint uDof = 2; // Vector2
                uint lDof = 1; // Scalar
                world.ClearFieldValue();
                uValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivationType.Value | FieldDerivationType.Velocity | FieldDerivationType.Acceleration,
                    uQuantityId, uDof, false, FieldShowType.Real);
                prevUValueId = world.AddFieldValue(FieldValueType.Vector2,
                    FieldDerivationType.Value | FieldDerivationType.Velocity | FieldDerivationType.Acceleration,
                    uQuantityId, uDof, false, FieldShowType.Real);
                lValueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivationType.Value,
                    lQuantityId, lDof, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivationType.Value, false, world);
                // Lagrange未定乗数のサーモグラフィ表示
                //IFieldDrawer faceDrawer = new FaceFieldDrawer(uValueId, FieldDerivationType.Value, false, world,
                //    lValueId, FieldDerivationType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(uValueId, FieldDerivationType.Value, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                IFieldDrawer edgeDrawer2 = new EdgeFieldDrawer(uValueId, FieldDerivationType.Value, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = 0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 200; iTime++)
            {
                fixedCadX.DoubleValue = 0;
                fixedCadY.DoubleValue = Math.Sin(t * 2.0 * Math.PI * 0.1);

                var FEM = new Hyperelastic2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    uValueId, prevUValueId, lValueId);
                if (isMooney)
                {
                    // Mooney-Rivlin
                    {
                        //var solver = new IvyFEM.Linear.LapackEquationSolver();
                        //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                        //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                        //solver.IsOrderingToBandMatrix = true;
                        //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                        //FEM.Solver = solver;
                    }
                    {
                        //var solver = new IvyFEM.Linear.LisEquationSolver();
                        //solver.Method = IvyFEM.Linear.LisEquationSolverMethod.Default;
                        //FEM.Solver = solver;
                    }
                    {
                        var solver = new IvyFEM.Linear.IvyFEMEquationSolver();
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                        solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                        //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                        //solver.ConvRatioTolerance = 1.0e-14;
                        FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-10;
                        FEM.Solver = solver;
                    }
                }
                else
                {
                    // Ogden
                    {
                        var solver = new IvyFEM.Linear.LapackEquationSolver();
                        //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                        solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                        solver.IsOrderingToBandMatrix = true;
                        //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                        FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-10;
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
                        //solver.ConvRatioTolerance = 1.0e-14;
                        //FEM.ConvRatioToleranceForNewtonRaphson = 1.0e-10;
                        //FEM.Solver = solver;
                    }
                }
                FEM.Solve();
                //double[] U = FEM.U;

                FEM.UpdateFieldValues();

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
