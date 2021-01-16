using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
//using OpenTK; // System.Numericsと衝突するので注意
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using IvyFEM;

namespace IvyFEMProtoApp
{
    partial class MainWindow
    {
        private void Cad3DPlateOnlyBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCad3DPlateOnly(this);
        }

        private void CoarseMesh3DPlateOnlyBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCoarseMesh3DPlateOnly(this);
        }

        private void Mesh3DPlateOnlyBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMesh3DPlateOnly(this);
        }

        private void Cad3D1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCad3D1(this);
        }

        private void CoarseMesh3D1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCoarseMesh3D1(this);
        }

        private void Mesh3D1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMesh3D1(this);
        }

        private void Cad3D2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCad3D2(this);
        }

        private void CoarseMesh3D2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCoarseMesh3D2(this);
        }

        private void Mesh3D2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMesh3D2(this);
        }

        private void Cad3D3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCad3D3(this);
        }

        private void CoarseMesh3D3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCoarseMesh3D3(this);
        }

        private void Mesh3D3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMesh3D3(this);
        }

        private void Cad3D4Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCad3D4(this);
        }

        private void CoarseMesh3D4Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCoarseMesh3D4(this);
        }

        private void Mesh3D4Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMesh3D4(this);
        }

        private void Cad3D5Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCad3D5(this);
        }

        private void CoarseMesh3D5Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCoarseMesh3D5(this);
        }

        private void Mesh3D5Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMesh3D5(this);
        }

        private void ElasticLinear3D1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticLinearStVenant3DProblem1(this, isStVenant);
        }

        private void ElasticLinear3DTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticLinearStVenant3DTDProblem1(this, isStVenant);
        }

        private void ElasticLinear3D2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticLinearStVenant3DProblem2(this, isStVenant);
        }

        private void ElasticLinear3DTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticLinearStVenant3DTDProblem2(this, isStVenant);
        }

        private void ElasticLinearEigen3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticLinearStVenantEigen3DProblem(this, isStVenant);
        }

        private void StVenantHyperelastic3D1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticLinearStVenant3DProblem1(this, isStVenant);
        }

        private void StVenantHyperelastic3DTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticLinearStVenant3DTDProblem1(this, isStVenant);
        }

        private void StVenantHyperelastic3D2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticLinearStVenant3DProblem2(this, isStVenant);
        }

        private void StVenantHyperelastic3DTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticLinearStVenant3DTDProblem2(this, isStVenant);
        }

        private void StVenantHyperelasticEigen3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticLinearStVenantEigen3DProblem(this, isStVenant);
        }

        private void MooneyRivlinHyperelastic3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.Hyperelastic3DProblem(this, isMooney);
        }

        private void MooneyRivlinHyperelastic3DTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.Hyperelastic3DTDProblem(this, isMooney);
        }

        private void OgdenHyperelastic3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.Hyperelastic3DProblem(this, isMooney);
        }

        private void OgdenHyperelastic3DTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.Hyperelastic3DTDProblem(this, isMooney);
        }

        private void ElasticMultipointConstraint3DTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticMultipointConstraint3DTDProblem(this, isStVenant);
        }

        private void StVenantHyperelasticMultipointConstraint3DTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticMultipointConstraint3DTDProblem(this, isStVenant);
        }

        private void MooneyRivlinHyperelasticMultipointConstraint3DTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticMultipointConstraint3DTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticMultipointConstraint3DTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.HyperelasticMultipointConstraint3DTDProblem(this, isMooney);
        }

        private void ElasticContact3DTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticContact3DTD1Problem(this, isStVenant);
        }

        private void StVenantHyperelasticContact3DTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticContact3DTD1Problem(this, isStVenant);
        }

        private void MooneyRivlinHyperelasticContact3DTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticContact3DTD1Problem(this, isMooney);
        }

        private void OgdenHyperelasticContact3DTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticContact3DTD1Problem(this, isMooney);
        }

        private void ElasticContact3DTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticContact3DTD2Problem(this, isStVenant);
        }

        private void StVenantHyperelasticContact3DTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticContact3DTD2Problem(this, isStVenant);
        }

        private void MooneyRivlinHyperelasticContact3DTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticContact3DTD2Problem(this, isMooney);
        }

        private void OgdenHyperelasticContact3DTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticContact3DTD2Problem(this, isMooney);
        }

        private void DKTPlate1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.LinearPlateProblem1(this, isMindlin);
        }

        private void DKTPlateTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.LinearPlateTDProblem1(this, isMindlin);
        }

        private void DKTPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.LinearPlateProblem2(this, isMindlin);
        }

        private void DKTPlateTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.LinearPlateTDProblem2(this, isMindlin);
        }

        private void DKTPlateEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.LinearPlateEigenProblem1(this, isMindlin);
        }

        private void DKTPlateEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.LinearPlateEigenProblem2(this, isMindlin);
        }

        private void MindlinPlate1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.LinearPlateProblem1(this, isMindlin);
        }

        private void MindlinPlateTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.LinearPlateTDProblem1(this, isMindlin);
        }

        private void MindlinPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.LinearPlateProblem2(this, isMindlin);
        }

        private void MindlinPlateTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.LinearPlateTDProblem2(this, isMindlin);
        }

        private void MindlinPlateEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.LinearPlateEigenProblem1(this, isMindlin);
        }

        private void MindlinPlateEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.LinearPlateEigenProblem2(this, isMindlin);
        }

        private void MITCLinearPlate1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MITCLinearPlateProblem1(this);
        }

        private void MITCLinearPlateTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MITCLinearPlateTDProblem1(this);
        }

        private void MITCLinearPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MITCLinearPlateProblem2(this);
        }

        private void MITCLinearPlateTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MITCLinearPlateTDProblem2(this);
        }

        private void MITCLinearPlateEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MITCLinearPlateEigenProblem1(this);
        }

        private void MITCLinearPlateEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MITCLinearPlateEigenProblem2(this);
        }

        private void MITCStVenantPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MITCStVenantPlateProblem2(this);
        }

        private void MITCStVenantPlateTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MITCStVenantPlateTDProblem2(this);
        }

        private void MITCStVenantThicknessStretchPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MITCStVenantThicknessStretchPlateProblem2(this);
        }

        private void MITCMooneyRivlinPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MITCMooneyRivlinPlateProblem2(this);
        }

        private void Poisson3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.Poisson3DProblem(this);
        }

        private void Diffusion3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.Diffusion3DProblem(this);
        }

        private void Diffusion3DTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.Diffusion3DTDProblem(this);
        }

        private void AdvectionDiffusion3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.AdvectionDiffusion3DProblem(this);
        }

        private void AdvectionDiffusion3DTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.AdvectionDiffusion3DTDProblem(this);
        }

        private void Helmholtz3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.Helmholtz3DProblem(this);
        }
    }
}
