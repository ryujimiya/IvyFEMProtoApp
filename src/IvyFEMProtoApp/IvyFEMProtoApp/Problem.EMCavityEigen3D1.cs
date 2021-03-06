﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void EMCavityEigen3DProblem1(MainWindow mainWindow, uint feOrder)
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

            double WA = 1.0;
            Func<double, double> toNormalizedFreq = freq =>
            {
                double omega = 2.0 * Math.PI * freq;
                double k0 = omega / Constants.C0;
                double normalizedFreq = k0 * WA;
                return normalizedFreq;
            };

            bool isSymmetricBandSolver = true;
            if (feOrder == 1)
            {
                isSymmetricBandSolver = true;
            }
            else if (feOrder == 2)
            {
                // 対称バンド行列版だと失敗するので一般版を使う
                isSymmetricBandSolver = false;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }

            IList<uint> shellLIds1;
            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(WA, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(WA, WA, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, WA, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, WA));
                pts.Add(new OpenTK.Vector3d(WA, 0.0, WA));
                pts.Add(new OpenTK.Vector3d(WA, WA, WA));
                pts.Add(new OpenTK.Vector3d(0.0, WA, WA));
                var res = cad.AddCube(pts);
                shellLIds1 = res.AddLIds;
            }
            {
                IList<uint> lIds1 = new List<uint>();
                {
                    foreach (uint lId in shellLIds1)
                    {
                        lIds1.Add(lId);
                    }
                }
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(lIds1, holes1, insideVIds1);
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

            double eLen = 0.0;
            if (feOrder == 1)
            {
                //eLen = 0.05;
                //eLen = 0.20;
                //eLen = 0.10;
                eLen = 0.20;
            }
            else if (feOrder == 2)
            {
                eLen = 0.40;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            Mesher3D mesher = new Mesher3D(cad, eLen);

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof1 = 1; // スカラー
                uint feOrder1 = feOrder;
                quantityId = world.AddQuantity(dof1, feOrder1, FiniteElementType.Edge);
            }

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
                uint maId1 = world.AddMaterial(vacuumMa);

                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId1);
            }

            uint[] zeroLIds = { 1, 2, 3, 4, 5, 6 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint lId in zeroLIds)
            {
                // 複素数(辺方向成分)
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            uint valueId = 0;
            uint vecValueId = 0;
            VectorFieldDrawer vectorDrawer;
            EdgeFieldDrawer edgeDrawer;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数(辺方向成分)
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                // Vector3
                vecValueId = world.AddFieldValue(FieldValueType.ZVector3, FieldDerivativeType.Value,
                    quantityId, true, FieldShowType.ZReal);
                vectorDrawer = new VectorFieldDrawer(
                    vecValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
            }
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(vectorDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }

            {
                var FEM = new EMCavity3DEigenFEM(world);
                FEM.IsSymmetricBandSolver = isSymmetricBandSolver;
                FEM.Solve();
                System.Numerics.Complex[] freqZs = FEM.Frequencys;
                System.Numerics.Complex[][] eVecZs = FEM.EVecs;
                System.Numerics.Complex[][] coordExyzVecZs = FEM.CoordExyzEVecs;

                //-----------------------------------------------
                // 零固有値を除外する
                int modeCnt = freqZs.Length;
                var freqZs2 = new List<System.Numerics.Complex>();
                var eVecZs2 = new List<System.Numerics.Complex[]>();
                var coordExyzEVecZs2 = new List<System.Numerics.Complex[]>();
                for (int iMode = 0; iMode < modeCnt; iMode++)
                {
                    double freq1 = freqZs[iMode].Real;
                    double nf = toNormalizedFreq(freq1);
                    //if (nf < 1.0e-6)
                    if (nf < 1.0e-4)
                    {
                        continue;
                    }
                    freqZs2.Add(freqZs[iMode]);
                    eVecZs2.Add(eVecZs[iMode]);
                    coordExyzEVecZs2.Add(coordExyzVecZs[iMode]);
                }
                freqZs = freqZs2.ToArray();
                eVecZs = eVecZs2.ToArray();
                coordExyzVecZs = coordExyzEVecZs2.ToArray();
                //-----------------------------------------------

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
                        System.Diagnostics.Debug.WriteLine("iMode = {0} k0a = {1}", iMode, fn);
                    }
                    freq = freqZ.Real;
                    eVec = new double[eVecZ.Length];
                    for (int i = 0; i < eVecZ.Length; i++)
                    {
                        eVec[i] = eVecZ[i].Real;
                    }
                }
                System.Numerics.Complex[] eigenCoordExyz;
                {
                    int iMode = 0;
                    int dof = 3; // x,y,z成分
                    int coCnt = coordExyzVecZs[iMode].Length / dof;
                    eigenCoordExyz = new System.Numerics.Complex[coCnt * dof];
                    coordExyzVecZs[iMode].CopyTo(eigenCoordExyz, 0);
                }
                // Exyzを表示用にスケーリングする
                {
                    double maxValue = 0;
                    int cnt = eigenCoordExyz.Length;
                    foreach (System.Numerics.Complex value in eigenCoordExyz)
                    {
                        double abs = value.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.2;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            eigenCoordExyz[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                world.UpdateBubbleFieldValueValuesFromCoordValues(vecValueId, FieldDerivativeType.Value, eigenCoordExyz);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                string resStr = "";
                string CR = System.Environment.NewLine;
                resStr += "normalized free frequency k0a)" + CR;
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
