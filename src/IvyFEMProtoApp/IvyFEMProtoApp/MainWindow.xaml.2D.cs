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
        private void CadBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCad(this);
        }

        private void CoarseMeshBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeCoarseMesh(this);
        }

        private void MeshBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMesh(this);
        }

        private void MeshHollowLoopBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.MakeMeshHollowLoop(this);
        }

        private void DrawStringBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DrawStringTest(Camera, GLControl);
        }

        private void LapackBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.InterseMatrixExample();
            Problem.LinearEquationExample();
            Problem.EigenValueExample();
        }

        private void LisBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.LisExample();
        }

        private void FFTBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FFTExample(this);
        }

        private void ExampleFEMBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ExampleFEMProblem(this);
        }

        private void CadEditBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            var cadEditWindow = new CadEditWindow();
            cadEditWindow.Owner = this;
            cadEditWindow.ShowDialog();
        }

        private void ElasticLinear1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticLinearStVenantProblem1(this, false, isStVenant);
            Problem.ElasticLinearStVenantProblem1(this, true, isStVenant);
        }

        private void ElasticLinearTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticLinearStVenantTDProblem1(this, isStVenant);
        }

        private void ElasticLinear2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticLinearStVenantProblem2(this, isStVenant);
        }

        private void ElasticLinearTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticLinearStVenantTDProblem2(this, isStVenant);
        }

        private void ElasticLinearEigenBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticLinearStVenantEigenProblem(this, isStVenant);
        }

        private void StVenantHyperelastic1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticLinearStVenantProblem1(this, false, isStVenant);
        }

        private void StVenantHyperelasticTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticLinearStVenantTDProblem1(this, isStVenant);
        }

        private void StVenantHyperelastic2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticLinearStVenantProblem2(this, isStVenant);
        }

        private void StVenantHyperelasticTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticLinearStVenantTDProblem2(this, isStVenant);
        }

        private void StVenantHyperelasticEigenBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticLinearStVenantEigenProblem(this, isStVenant);
        }

        private void MooneyRivlinHyperelasticBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticProblem(this, isMooney);
        }

        private void MooneyRivlinHyperelasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.HyperelasticProblem(this, isMooney);
        }

        private void OgdenHyperelasticTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.HyperelasticTDProblem(this, isMooney);
        }

        private void ElasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticMultipointConstraintTDProblem(this, isStVenant);
        }

        private void StVenantHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticMultipointConstraintTDProblem(this, isStVenant);
        }

        private void MooneyRivlinHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticMultipointConstraintTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticMultipointConstraintTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = false; // Ogden
            Problem.HyperelasticMultipointConstraintTDProblem(this, isMooney);
        }

        private void ElasticContactTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticContactTD1Problem(this, isStVenant);
        }

        private void StVenantHyperelasticContactTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticContactTD1Problem(this, isStVenant);
        }

        private void MooneyRivlinHyperelasticContactTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticContactTD1Problem(this, isMooney);
        }

        private void OgdenHyperelasticContactTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticContactTD1Problem(this, isMooney);
        }

        private void ElasticContactTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticContactTD2Problem(this, isStVenant);
        }

        private void StVenantHyperelasticContactTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticContactTD2Problem(this, isStVenant);
        }

        private void MooneyRivlinHyperelasticContactTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticContactTD2Problem(this, isMooney);
        }

        private void OgdenHyperelasticContactTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticContactTD2Problem(this, isMooney);
        }

        private void ElasticTwoBodyContactBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticTwoBodyContactProblem(this, isStVenant);
        }

        private void ElasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = false;
            Problem.ElasticTwoBodyContactTDProblem(this, isStVenant);
        }

        private void StVenantHyperelasticTwoBodyContactBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticTwoBodyContactProblem(this, isStVenant);
        }

        private void StVenantHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isStVenant = true;
            Problem.ElasticTwoBodyContactTDProblem(this, isStVenant);
        }

        private void MooneyRivlinHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isMooney = true;
            Problem.HyperelasticTwoBodyContactTDProblem(this, isMooney);
        }

        private void OgdenHyperelasticTwoBodyContactTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            // Ogden
            bool isMooney = false;
            Problem.HyperelasticTwoBodyContactTDProblem(this, isMooney);
        }

        private void Truss1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TrussProblem1(this);
        }

        private void TrussTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TrussTDProblem1(this);
        }

        private void Truss2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TrussProblem2(this);
        }

        private void TrussTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TrussTDProblem2(this);
        }

        private void Beam1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamProblem1(this, isTimoshenko);
        }

        private void BeamTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamTDProblem1(this, isTimoshenko);
        }

        private void BeamEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamEigenProblem1(this, isTimoshenko);
        }

        private void Beam2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamProblem2(this, isTimoshenko);
        }

        private void BeamTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamTDProblem2(this, isTimoshenko);
        }

        private void Beam3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamProblem3(this, isTimoshenko);
        }

        private void BeamTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.BeamTDProblem3(this, isTimoshenko);
        }

        private void Frame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameProblem0(this, isTimoshenko);
        }

        private void FrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameTDProblem0(this, isTimoshenko);
        }

        private void FrameEigen0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameEigenProblem0(this, isTimoshenko);
        }

        private void Frame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameProblem1(this, isTimoshenko);
        }

        private void FrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameTDProblem1(this, isTimoshenko);
        }

        private void Frame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameProblem2(this, isTimoshenko);
        }

        private void FrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameTDProblem2(this, isTimoshenko);
        }

        private void Frame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameProblem3(this, isTimoshenko);
        }

        private void FrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.FrameTDProblem3(this, isTimoshenko);
        }

        private void TimoshenkoBeam1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamProblem1(this, isTimoshenko);
        }

        private void TimoshenkoBeamTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamTDProblem1(this, isTimoshenko);
        }

        private void TimoshenkoBeamEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamEigenProblem1(this, isTimoshenko);
        }

        private void TimoshenkoBeam2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamProblem2(this, isTimoshenko);
        }

        private void TimoshenkoBeamTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamTDProblem2(this, isTimoshenko);
        }

        private void TimoshenkoBeam3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamProblem3(this, isTimoshenko);
        }

        private void TimoshenkoBeamTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.BeamTDProblem3(this, isTimoshenko);
        }

        private void TimoshenkoFrame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameProblem0(this, isTimoshenko);
        }

        private void TimoshenkoFrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameTDProblem0(this, isTimoshenko);
        }

        private void TimoshenkoFrameEigen0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameEigenProblem0(this, isTimoshenko);
        }

        private void TimoshenkoFrame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameProblem1(this, isTimoshenko);
        }

        private void TimoshenkoFrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameTDProblem1(this, isTimoshenko);
        }

        private void TimoshenkoFrame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameProblem2(this, isTimoshenko);
        }

        private void TimoshenkoFrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameTDProblem2(this, isTimoshenko);
        }

        private void TimoshenkoFrame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameProblem3(this, isTimoshenko);
        }

        private void TimoshenkoFrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.FrameTDProblem3(this, isTimoshenko);
        }

        private void CorotationalFrame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameProblem0(this, isTimoshenko);
        }

        private void CorotationalFrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameTDProblem0(this, isTimoshenko);
        }

        private void CorotationalFrame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameProblem1(this, isTimoshenko);
        }

        private void CorotationalFrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameTDProblem1(this, isTimoshenko);
        }

        private void CorotationalFrame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameProblem2(this, isTimoshenko);
        }

        private void CorotationalFrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameTDProblem2(this, isTimoshenko);
        }

        private void CorotationalFrame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameProblem3(this, isTimoshenko);
        }

        private void CorotationalFrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = false;
            Problem.CorotationalFrameTDProblem3(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameProblem0(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameTDProblem0(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameProblem1(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameTDProblem1(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameProblem2(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameTDProblem2(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameProblem3(this, isTimoshenko);
        }

        private void TimoshenkoCorotationalFrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            bool isTimoshenko = true;
            Problem.CorotationalFrameTDProblem3(this, isTimoshenko);
        }

        private void FieldConsistentTLFrame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameProblem0(this);
        }

        private void FieldConsistentTLFrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameTDProblem0(this);
        }

        private void FieldConsistentTLFrame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameProblem1(this);
        }

        private void FieldConsistentTLFrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameTDProblem1(this);
        }

        private void FieldConsistentTLFrame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameProblem2(this);
        }

        private void FieldConsistentTLFrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameTDProblem2(this);
        }

        private void FieldConsistentTLFrame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLFrameProblem3(this);
        }

        private void FieldConsistentTLFrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.FieldConsistentTLTDFrameProblem3(this);
        }

        private void TimoshenkoTLFrame0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameProblem0(this);
        }

        private void TimoshenkoTLFrameTD0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameTDProblem0(this);
        }

        private void TimoshenkoTLFrame1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameProblem1(this);
        }

        private void TimoshenkoTLFrameTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameTDProblem1(this);
        }

        private void TimoshenkoTLFrame2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameProblem2(this);
        }

        private void TimoshenkoTLFrameTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameTDProblem2(this);
        }

        private void TimoshenkoTLFrame3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameProblem3(this);
        }

        private void TimoshenkoTLFrameTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.TimoshenkoTLFrameTDProblem3(this);
        }

        private void ElasticLambWaveguide0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticLambWaveguideProblem0(this);
        }

        private void ElasticLambWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticLambWaveguideProblem1(this);
        }

        private void ElasticLambWaveguideFirstOrderABC0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticLambWaveguideFirstOrderABCProblem0(this);
        }

        private void ElasticLambWaveguideFirstOrderABC1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticLambWaveguideFirstOrderABCProblem1(this);
        }

        private void ElasticLambWaveguidePML0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticLambWaveguidePMLProblem0(this);
        }

        private void ElasticLambWaveguidePML1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticLambWaveguidePMLProblem1(this);
        }

        private void ElasticSHWaveguide0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticSHWaveguideProblem0(this);
        }

        /*
        private void ElasticSHWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticSHWaveguideProblem1(this);
        }
        */

        private void ElasticSHWaveguide2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticSHWaveguideProblem2(this);
        }

        private void ElasticSHWaveguideFirstOrderABC0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticSHWaveguideFirstOrderABCProblem0(this);
        }

        private void ElasticSHWaveguideFirstOrderABC2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticSHWaveguideFirstOrderABCProblem2(this);
        }

        private void ElasticSHWaveguidePML0Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticSHWaveguidePMLProblem0(this);
        }

        private void ElasticSHWaveguidePML2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.ElasticSHWaveguidePMLProblem2(this);
        }

        private void ElasticLambWaveguidePMLTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.ElasticLambWaveguidePMLTDProblem1_0(this);
            Problem.ElasticLambWaveguidePMLTDProblem1(this);
        }

        private void HPlaneWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideProblem1_0(this);
            Problem.HWaveguideProblem1(this);
        }

        private void HPlaneWaveguide2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideProblem2_0(this);
            Problem.HWaveguideProblem2(this);
        }

        private void EPlaneWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.EWaveguideProblem1_0(this);
            Problem.EWaveguideProblem1(this);
        }

        private void HPlaneWaveguideHigherOrderABC1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCProblem1_0(this);
            Problem.HWaveguideHigherOrderABCProblem1(this);
        }

        private void HPlaneWaveguideHigherOrderABC2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCProblem2_0(this);
            Problem.HWaveguideHigherOrderABCProblem2(this);
        }

        private void HPlaneWaveguideHigherOrderABC3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCProblem3_0(this);
            Problem.HWaveguideHigherOrderABCProblem3(this);
        }

        private void HPlaneWaveguideHigherOrderABCTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCTDProblem1_0(this);
            Problem.HWaveguideHigherOrderABCTDProblem1(this);
        }

        private void HPlaneWaveguideHigherOrderABCTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCTDProblem2_0(this);
            Problem.HWaveguideHigherOrderABCTDProblem2(this);
        }

        private void HPlaneWaveguideHigherOrderABCTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideHigherOrderABCTDProblem3_0(this);
            Problem.HWaveguideHigherOrderABCTDProblem3(this);
        }

        private void HPlaneWaveguideFirstOrderABCTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguideFirstOrderABCTDProblem3_0(this);
            Problem.HWaveguideFirstOrderABCTDProblem3(this);
        }

        private void HPlaneWaveguidePML1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLProblem1_0(this);
            Problem.HWaveguidePMLProblem1(this);
        }

        private void HPlaneWaveguidePML2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLProblem2_0(this);
            Problem.HWaveguidePMLProblem2(this);
        }

        private void HPlaneWaveguidePML3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLProblem3_0(this);
            Problem.HWaveguidePMLProblem3(this);
        }

        private void HPlaneWaveguidePMLTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLTDProblem1_0(this);
            Problem.HWaveguidePMLTDProblem1(this);
        }

        private void HPlaneWaveguidePMLTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLTDProblem2_0(this);
            Problem.HWaveguidePMLTDProblem2(this);
        }

        private void HPlaneWaveguidePMLTD3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.HWaveguidePMLTDProblem3_0(this);
            Problem.HWaveguidePMLTDProblem3(this);
        }

        private void Waveguide2DEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 1;
            Problem.Waveguide2DEigenProblem1(this, false, feOrder);
            WPFUtils.DoEvents(20 * 1000);
            Problem.Waveguide2DEigenProblem1(this, true, feOrder);
        }

        private void Waveguide2DEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 1;
            Problem.Waveguide2DEigenProblem2(this, feOrder);
        }

        private void Waveguide2DEigen3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 1;
            Problem.Waveguide2DEigenProblem3(this, feOrder);
        }

        private void Waveguide2DEigen2ndOrder1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 2;
            Problem.Waveguide2DEigenProblem1(this, false, feOrder);
            WPFUtils.DoEvents(20 * 1000);
            Problem.Waveguide2DEigenProblem1(this, true, feOrder);
        }

        private void Waveguide2DEigen2ndOrder2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 2;
            Problem.Waveguide2DEigenProblem2(this, feOrder);
        }

        private void Waveguide2DEigen2ndOrder3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 2;
            Problem.Waveguide2DEigenProblem3(this, feOrder);
        }

        private void Waveguide2DEigenOpen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 1;
            Problem.Waveguide2DEigenOpenProblem2(this, feOrder);
        }

        private void Waveguide2DEigenOpen3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 1;
            Problem.Waveguide2DEigenOpenProblem3(this, feOrder);
        }

        private void Waveguide2DEigenOpen2ndOrder2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 2;
            Problem.Waveguide2DEigenOpenProblem2(this, feOrder);
        }

        private void Waveguide2DEigenOpen2ndOrder3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            uint feOrder = 2;
            Problem.Waveguide2DEigenOpenProblem3(this, feOrder);
        }

        private void SquareLatticePCWaveguideEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PCWaveguideEigenSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguideEigen1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PCWaveguideEigenTriangleLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguideEigen2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PCWaveguideEigenTriangleLatticeProblem2(this);
        }

        private void SquareLatticePBG1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PBGSquareLatticeProblem1(this);
        }

        private void TriangleLatticePBG1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PBGTriangleLatticeProblem1(this);
        }

        private void TriangleLatticePBG2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PBGTriangleLatticeProblem2(this);
        }

        private void SquareLatticePCWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguideSquareLatticeProblem1_0(this);
            Problem.PCWaveguideSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguide1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguideTriangleLatticeProblem1_0(this);
            Problem.PCWaveguideTriangleLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguide2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguideTriangleLatticeProblem2_0(this);
            Problem.PCWaveguideTriangleLatticeProblem2(this);
        }

        private void TriangleLatticePCWaveguide3Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PCWaveguideTriangleLatticeProblem3(this);
        }

        private void SquareLatticePCWaveguidePBC1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePBCSquareLatticeProblem1_0(this);
            Problem.PCWaveguidePBCSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguidePBC1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePBCTriangleLatticeProblem1_0(this);
            Problem.PCWaveguidePBCTriangleLatticeProblem1(this);
        }

        private void SquareLatticePCWaveguideModalABCZTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguideModalABCZTDSquareLatticeProblem1_0(this);
            Problem.PCWaveguideModalABCZTDSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguideModalABCZTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguideModalABCZTDTriangleLatticeProblem1_0(this);
            Problem.PCWaveguideModalABCZTDTriangleLatticeProblem1(this);
        }

        private void SquareLatticePCWaveguidePML1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePMLSquareLatticeProblem1_0(this);
            Problem.PCWaveguidePMLSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguidePML1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePMLTriangleLatticeProblem1_0(this);
            Problem.PCWaveguidePMLTriangleLatticeProblem1(this);
        }

        private void SquareLatticePCWaveguidePMLTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePMLTDSquareLatticeProblem1_0(this);
            Problem.PCWaveguidePMLTDSquareLatticeProblem1(this);
        }

        private void TriangleLatticePCWaveguidePMLTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PCWaveguidePMLTDTriangleLatticeProblem1_0(this);
            Problem.PCWaveguidePMLTDTriangleLatticeProblem1(this);
        }

        private void EddyCurrentTD1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.EddyCurrentTDProblem1(this);
        }

        private void EddyCurrentTD2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.EddyCurrentTDProblem2(this);
        }

        // mu = 0.02, 0.002 FluidEquationType.StdGNavierStokes
        // mu = 0.0002 FluidEquationType.SUPGNavierStokes
        // mu = 0.00002 Not converge
        private void StdGFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidProblem1(this, fluidEquationType);
        }

        private void StdGFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidTDProblem1(this, fluidEquationType);
        }

        private void StdGFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidProblem2(this, fluidEquationType);
        }

        private void StdGFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGNavierStokes;
            Problem.FluidTDProblem2(this, fluidEquationType);
        }

        private void SUPGFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidProblem1(this, fluidEquationType);
        }

        private void SUPGFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidTDProblem1(this, fluidEquationType);
        }

        private void SUPGFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidProblem2(this, fluidEquationType);
        }

        private void SUPGFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGNavierStokes;
            Problem.FluidTDProblem2(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PressurePoissonFluidProblem1(this);

            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidProblem1(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PressurePoissonFluidTDProblem1(this);

            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidTDProblem1(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PressurePoissonFluidProblem2(this);

            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidProblem2(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PressurePoissonFluidTDProblem2(this);

            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidTDProblem2(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid1RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //Problem.PressurePoissonFluidRKTDProblem1(this); // NG

            FluidEquationType fluidEquationType = FluidEquationType.StdGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidRKTDProblem1(this, fluidEquationType);
        }

        private void StdGPressurePoissonFluid2RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            MessageBox.Show("FIX ME: You won't get results.");
            //Problem.PressurePoissonFluidRKTDProblem2(this); // NG
            Problem.PressurePoissonWithBellFluidRKTDProblem2(this); // NG
        }

        private void SUPGPressurePoissonFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //MessageBox.Show("FIX ME: now, conv ratio = 10^-4");
            MessageBox.Show("FIX ME: now, conv ratio = 10^-3");
            FluidEquationType fluidEquationType = FluidEquationType.SUPGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidProblem1(this, fluidEquationType);
        }

        private void SUPGPressurePoissonFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //これは10^-4のまま
            MessageBox.Show("FIX ME: now, conv ratio = 10^-4");
            FluidEquationType fluidEquationType = FluidEquationType.SUPGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidTDProblem1(this, fluidEquationType);
        }

        private void SUPGPressurePoissonFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //MessageBox.Show("FIX ME: now, conv ratio = 10^-4");
            MessageBox.Show("FIX ME: now, conv ratio = 10^-3");
            FluidEquationType fluidEquationType = FluidEquationType.SUPGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidProblem2(this, fluidEquationType);
        }

        private void SUPGPressurePoissonFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            //MessageBox.Show("FIX ME: now, conv ratio = 10^-4");
            MessageBox.Show("FIX ME: now, conv ratio = 10^-3");
            FluidEquationType fluidEquationType = FluidEquationType.SUPGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidTDProblem2(this, fluidEquationType);
        }

        private void SUPGPressurePoissonFluid1RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            MessageBox.Show("FIXME: Not work! diverge!");
            FluidEquationType fluidEquationType = FluidEquationType.SUPGPressurePoissonWithBell;
            Problem.PressurePoissonWithBellFluidRKTDProblem1(this, fluidEquationType);
        }

        private void StdGVorticityFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidProblem1(this, fluidEquationType);
        }

        private void StdGVorticityFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidTDProblem1(this, fluidEquationType);
        }

        private void StdGVorticityFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidProblem2(this, fluidEquationType);
        }

        private void StdGVorticityFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.StdGVorticity;
            Problem.VorticityFluidTDProblem2(this, fluidEquationType);
        }

        private void SUPGVorticityFluid1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidProblem1(this, fluidEquationType);
        }

        private void SUPGVorticityFluid1TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidTDProblem1(this, fluidEquationType);
        }

        private void SUPGVorticityFluid2Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidProblem2(this, fluidEquationType);
        }

        private void SUPGVorticityFluid2TDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            FluidEquationType fluidEquationType = FluidEquationType.SUPGVorticity;
            Problem.VorticityFluidTDProblem2(this, fluidEquationType);
        }

        private void StdGVorticityFluid1RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.VorticityFluidRKTDProblem1(this);
        }

        private void StdGVorticityFluid2RKTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.VorticityFluidRKTDProblem2(this);
        }

        private void PoissonBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.PoissonProblem(this);
        }

        private void DiffusionBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DiffusionProblem(this);
        }

        private void DiffusionTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.DiffusionTDProblem(this);
        }

        private void AdvectionDiffusionBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.AdvectionDiffusionProblem(this);
        }

        private void AdvectionDiffusionTDBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.AdvectionDiffusionTDProblem(this);
        }

        private void HelmholtzBtn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.HelmholtzProblem(this);
        }

        private void Optimize1Btn_Click(object sender, RoutedEventArgs e)
        {
            InitProblem(e.Source as MenuItem);

            Problem.Optimize1Problem(this);
        }
    }
}
