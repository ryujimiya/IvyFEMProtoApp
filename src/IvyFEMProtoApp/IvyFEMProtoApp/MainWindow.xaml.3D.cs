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

            PlateKind plateKind = PlateKind.DKTPlate;
            Problem.LinearPlateProblem1(this, plateKind);
        }

        private void DKTPlateTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.DKTPlate;
            Problem.LinearPlateTDProblem1(this, plateKind);
        }

        private void DKTPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.DKTPlate;
            Problem.LinearPlateProblem2(this, plateKind);
        }

        private void DKTPlateTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.DKTPlate;
            Problem.LinearPlateTDProblem2(this, plateKind);
        }

        private void DKTPlateEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.DKTPlate;
            Problem.LinearPlateEigenProblem1(this, plateKind);
        }

        private void DKTPlateEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.DKTPlate;
            Problem.LinearPlateEigenProblem2(this, plateKind);
        }

        private void MindlinPlate1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MindlinPlate;
            Problem.LinearPlateProblem1(this, plateKind);
        }

        private void MindlinPlateTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MindlinPlate;
            Problem.LinearPlateTDProblem1(this, plateKind);
        }

        private void MindlinPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MindlinPlate;
            Problem.LinearPlateProblem2(this, plateKind);
        }

        private void MindlinPlateTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MindlinPlate;
            Problem.LinearPlateTDProblem2(this, plateKind);
        }

        private void MindlinPlateEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MindlinPlate;
            Problem.LinearPlateEigenProblem1(this, plateKind);
        }

        private void MindlinPlateEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MindlinPlate;
            Problem.LinearPlateEigenProblem2(this, plateKind);
        }

        private void MITCLinearPlate1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MITCLinearPlate;
            Problem.LinearPlateProblem1(this, plateKind);
        }

        private void MITCLinearPlateTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MITCLinearPlate;
            Problem.LinearPlateTDProblem1(this, plateKind);
        }

        private void MITCLinearPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MITCLinearPlate;
            Problem.LinearPlateProblem2(this, plateKind);
        }

        private void MITCLinearPlateTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MITCLinearPlate;
            Problem.LinearPlateTDProblem2(this, plateKind);
        }

        private void MITCLinearPlateEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MITCLinearPlate;
            Problem.LinearPlateEigenProblem1(this, plateKind);
        }

        private void MITCLinearPlateEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            PlateKind plateKind = PlateKind.MITCLinearPlate;
            Problem.LinearPlateEigenProblem2(this, plateKind);
        }
    }
}
