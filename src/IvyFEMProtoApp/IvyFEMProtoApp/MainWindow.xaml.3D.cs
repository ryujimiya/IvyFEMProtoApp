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

            Problem.DKTPlateProblem1(this);
        }

        private void DKTPlateTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DKTPlateTDProblem1(this);
        }

        private void DKTPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DKTPlateProblem2(this);
        }

        private void DKTPlateTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DKTPlateTDProblem2(this);
        }

        private void DKTPlateEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DKTPlateEigenProblem1(this);
        }

        private void DKTPlateEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DKTPlateEigenProblem2(this);
        }
    }
}
