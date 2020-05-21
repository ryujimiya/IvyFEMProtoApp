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
            Problem.DKTMindlinPlateProblem1(this, isMindlin);
        }

        private void DKTPlateTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.DKTMindlinPlateTDProblem1(this, isMindlin);
        }

        private void DKTPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.DKTMindlinPlateProblem2(this, isMindlin);
        }

        private void DKTPlateTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.DKTMindlinPlateTDProblem2(this, isMindlin);
        }

        private void DKTPlateEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.DKTMindlinPlateEigenProblem1(this, isMindlin);
        }

        private void DKTPlateEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = false;
            Problem.DKTMindlinPlateEigenProblem2(this, isMindlin);
        }

        private void MindlinPlate1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.DKTMindlinPlateProblem1(this, isMindlin);
        }

        private void MindlinPlateTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.DKTMindlinPlateTDProblem1(this, isMindlin);
        }

        private void MindlinPlate2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.DKTMindlinPlateProblem2(this, isMindlin);
        }

        private void MindlinPlateTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.DKTMindlinPlateTDProblem2(this, isMindlin);
        }

        private void MindlinPlateEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.DKTMindlinPlateEigenProblem1(this, isMindlin);
        }

        private void MindlinPlateEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMindlin = true;
            Problem.DKTMindlinPlateEigenProblem2(this, isMindlin);
        }

    }
}
