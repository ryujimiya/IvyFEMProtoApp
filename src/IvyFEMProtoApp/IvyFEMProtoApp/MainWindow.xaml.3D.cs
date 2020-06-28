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
        private void Cad3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCad3D(this);
        }

        private void CoarseMesh3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCoarseMesh3D(this);
        }

        private void Mesh3DBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMesh3D(this);
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
    }
}
