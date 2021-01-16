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
        public void ElasticLinearStVenantEigen3DProblem(MainWindow mainWindow, bool isStVenant)
        {
            /////////////////////
            Dimension = 3; // 3次元
            var camera3D = mainWindow.Camera as Camera3D;
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 70.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            /////////////////////

            double beamLen = 1.0;
            double b = 0.1 * beamLen;
            double h = 0.25 * b;
            // 規格化周波数 fn = b/λ
            Func<double, double> toNormalizedFreq = freq => b * freq / Constants.C0;
            double eLen = 0.04 * beamLen;
            double E = 169.0e+9;
            double rho = 2300.0;
            double nu = 0.262;
            IList<uint> shellLIds1;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, b, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, b, h));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, h));
                pts.Add(new OpenTK.Vector3d(beamLen, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(beamLen, b, 0.0));
                pts.Add(new OpenTK.Vector3d(beamLen, b, h));
                pts.Add(new OpenTK.Vector3d(beamLen, 0.0, h));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }

            {
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(shellLIds1, holes1, insideVIds1);
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad3DDrawer(cad);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            Mesher3D mesher = new Mesher3D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 3; // Vector3
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }
            world.TetIntegrationPointCount = TetrahedronIntegrationPointCount.Point4;

            {
                world.ClearMaterial();
                uint maId1 = 0;
                if (isStVenant)
                {
                    var ma1 = new StVenantHyperelasticMaterial();
                    ma1.Young = E;
                    ma1.Poisson = nu;
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.GravityZ = 0;
                    ma1.MassDensity = rho;
                    maId1 = world.AddMaterial(ma1);
                }
                else
                {
                    var ma1 = new LinearElasticMaterial();
                    ma1.Young = E;
                    ma1.Poisson = nu;
                    ma1.GravityX = 0;
                    ma1.GravityY = 0;
                    ma1.GravityZ = 0;
                    ma1.MassDensity = rho;
                    maId1 = world.AddMaterial(ma1);
                }

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);

                uint[] lIds = { 1, 2, 3, 4, 5, 6 };
                foreach (uint lId in lIds)
                {
                    world.SetCadLoopMaterial(lId, maId1);
                }
            }

            uint[] zeroLIds = { 1, 6 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint lId in zeroLIds)
            {
                // Vector2
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.Vector3);
                zeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            uint valueId = 0;
            FaceFieldDrawer faceDrawer;
            EdgeFieldDrawer edgeDrawer;
            //EdgeFieldDrawer edgeDrawer2;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // Vector3
                valueId = world.AddFieldValue(FieldValueType.Vector3, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, false, world);
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, false, true, world);
                //edgeDrawer2 = new EdgeFieldDrawer(
                //    valueId, FieldDerivativeType.Value, true, true, world);
            }
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(faceDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                //fieldDrawerArray.Add(edgeDrawer2);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }

            {
                var FEM = new Elastic3DEigenFEM(world);
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
