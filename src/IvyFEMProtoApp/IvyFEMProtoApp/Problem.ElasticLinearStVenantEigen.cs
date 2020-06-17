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
        public void ElasticLinearStVenantEigenProblem(MainWindow mainWindow, bool isStVenant)
        {
            double beamLen = 1.0;
            double b = 0.2 * beamLen;
            double h = 0.25 * b;
            // 規格化周波数 fn = b/λ
            Func<double, double> toNormalizedFreq = freq => b * freq / Constants.C0;
            double eLen = 0.2 * beamLen;
            if (isStVenant)
            {
                //eLen = 0.2 * beamLen;
                eLen = 0.04 * beamLen;
            }
            else
            {
                //eLen = 0.2 * beamLen; //!!まだ収束してない ただ大回転になるため線形弾性体では限界がある
                eLen = 0.04 * beamLen;
            }
            double E = 169.0e+9;
            double rho = 2300.0;
            double nu = 0.262;
            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(beamLen, 0.0));
                pts.Add(new OpenTK.Vector2d(beamLen, h));
                pts.Add(new OpenTK.Vector2d(0.0, h));
                uint lId1 = cad.AddPolygon(pts).AddLId;
            }

            Mesher2D mesher = new Mesher2D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 2; // Vector2
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }
            world.TriIntegrationPointCount = TriangleIntegrationPointCount.Point3;

            {
                world.ClearMaterial();
                uint maId = 0;
                if (isStVenant)
                {
                    var ma = new StVenantHyperelasticMaterial();
                    ma.Young = E;
                    ma.Poisson = nu;
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = rho;
                    maId = world.AddMaterial(ma);
                }
                else
                {
                    var ma = new LinearElasticMaterial();
                    ma.Young = E;
                    ma.Poisson = nu;
                    ma.GravityX = 0;
                    ma.GravityY = 0;
                    ma.MassDensity = rho;
                    maId = world.AddMaterial(ma);
                }

                uint lId = 1;
                world.SetCadLoopMaterial(lId, maId);
            }

            uint[] zeroEIds = { 2, 4 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Vector2);
                zeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector2
                valueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                fieldDrawerArray.Add(edgeDrawer);
                var edgeDrawer2 = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, true, world);
                fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            {
                var FEM = new Elastic2DEigenFEM(world);
                FEM.Solve();
                System.Numerics.Complex[] freqZs = FEM.FrequencyZs;
                System.Numerics.Complex[][] eVecZs = FEM.EVecZs;

                double freq;
                double[] eVec;
                {
                    System.Numerics.Complex freqZ;
                    System.Numerics.Complex[] eVecZ;
                    {
                        int iMode = 0;
                        freqZ = freqZs[iMode];
                        eVecZ = eVecZs[iMode];
                        System.Diagnostics.Debug.Assert(Math.Abs(freqZ.Imaginary) < 1.0e-12);
                        double fn = toNormalizedFreq(freqZ.Real);
                        System.Diagnostics.Debug.WriteLine("iMode = {0} b/λ = {1}", iMode, fn);
                    }
                    freq = freqZ.Real;
                    eVec = new double[eVecZ.Length];
                    for (int i = 0; i < eVecZ.Length; i++)
                    {
                        eVec[i] = eVecZ[i].Real;
                    }
                }
                double[] U = eVec;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, U);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                string resStr = "";
                string CR = System.Environment.NewLine;
                resStr += "normalized free frequency (b/λ)" + CR;
                resStr += "--------------------------------" + CR;
                for (int i = 0; i < freqZs.Length; i++)
                {
                    double fn = toNormalizedFreq(freqZs[i].Real);
                    resStr += string.Format("{0}: {1}", i + 1, fn) + CR;
                }
                if (AlertWindow1 != null)
                {
                    AlertWindow1.Close();
                }
                AlertWindow1 = new AlertWindow();
                AlertWindow1.Owner = mainWindow;
                AlertWindow1.Closing += AlertWindow1_Closing;
                AlertWindow1.Left = mainWindow.Left + mainWindow.Width;
                AlertWindow1.Top = mainWindow.Top;
                AlertWindow1.TextBox1.Text = resStr;
                AlertWindow1.Show();
            }
        }
    }
}
