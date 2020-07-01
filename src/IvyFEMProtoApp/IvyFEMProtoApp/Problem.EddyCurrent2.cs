using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void EddyCurrentTDProblem2(MainWindow mainWindow)
        {
            // ステーター
            double statorR1 = 4.0;
            double statorR2 = 8.0;
            double statorR3 = 10.0;
            double statorCoreTheta = 60.0 * Math.PI / 180.0;
            double statorTheta1 = 80.0 * Math.PI / 180.0;
            double statorTheta2 = 85.0 * Math.PI / 180.0;
            double statorGapTheta = statorCoreTheta - 2.0 * (Math.PI / 2.0 - statorTheta2);
            //
            double statorCore1X1 = statorR2 * Math.Cos(statorTheta1);
            double statorCore1Y1 = statorR2 * Math.Sin(statorTheta1);
            double statorCore1X2 = statorCore1X1;
            double statorCore1Y2 = statorR1 * Math.Sin(statorTheta2);
            double statorCore1X3 = -statorCore1X1;
            double statorCore1Y3 = statorCore1Y2;
            double statorCore1X4 = -statorCore1X1;
            double statorCore1Y4 = statorCore1Y1;
            //
            double statorCore2X1 = statorCore1X1 * Math.Cos(statorCoreTheta) - statorCore1Y1 * Math.Sin(statorCoreTheta);
            double statorCore2Y1 = statorCore1X1 * Math.Sin(statorCoreTheta) + statorCore1Y1 * Math.Cos(statorCoreTheta);
            double statorCore2X2 = statorCore1X2 * Math.Cos(statorCoreTheta) - statorCore1Y2 * Math.Sin(statorCoreTheta);
            double statorCore2Y2 = statorCore1X2 * Math.Sin(statorCoreTheta) + statorCore1Y2 * Math.Cos(statorCoreTheta);
            double statorCore2X3 = statorCore1X3 * Math.Cos(statorCoreTheta) - statorCore1Y3 * Math.Sin(statorCoreTheta);
            double statorCore2Y3 = statorCore1X3 * Math.Sin(statorCoreTheta) + statorCore1Y3 * Math.Cos(statorCoreTheta);
            double statorCore2X4 = statorCore1X4 * Math.Cos(statorCoreTheta) - statorCore1Y4 * Math.Sin(statorCoreTheta);
            double statorCore2Y4 = statorCore1X4 * Math.Sin(statorCoreTheta) + statorCore1Y4 * Math.Cos(statorCoreTheta);
            //
            double statorCore3X1 = statorCore2X1 * Math.Cos(statorCoreTheta) - statorCore2Y1 * Math.Sin(statorCoreTheta);
            double statorCore3Y1 = statorCore2X1 * Math.Sin(statorCoreTheta) + statorCore2Y1 * Math.Cos(statorCoreTheta);
            double statorCore3X2 = statorCore2X2 * Math.Cos(statorCoreTheta) - statorCore2Y2 * Math.Sin(statorCoreTheta);
            double statorCore3Y2 = statorCore2X2 * Math.Sin(statorCoreTheta) + statorCore2Y2 * Math.Cos(statorCoreTheta);
            double statorCore3X3 = statorCore2X3 * Math.Cos(statorCoreTheta) - statorCore2Y3 * Math.Sin(statorCoreTheta);
            double statorCore3Y3 = statorCore2X3 * Math.Sin(statorCoreTheta) + statorCore2Y3 * Math.Cos(statorCoreTheta);
            double statorCore3X4 = statorCore2X4 * Math.Cos(statorCoreTheta) - statorCore2Y4 * Math.Sin(statorCoreTheta);
            double statorCore3Y4 = statorCore2X4 * Math.Sin(statorCoreTheta) + statorCore2Y4 * Math.Cos(statorCoreTheta);
            //
            double statorCore4X1 = statorCore3X1 * Math.Cos(statorCoreTheta) - statorCore3Y1 * Math.Sin(statorCoreTheta);
            double statorCore4Y1 = statorCore3X1 * Math.Sin(statorCoreTheta) + statorCore3Y1 * Math.Cos(statorCoreTheta);
            double statorCore4X2 = statorCore3X2 * Math.Cos(statorCoreTheta) - statorCore3Y2 * Math.Sin(statorCoreTheta);
            double statorCore4Y2 = statorCore3X2 * Math.Sin(statorCoreTheta) + statorCore3Y2 * Math.Cos(statorCoreTheta);
            double statorCore4X3 = statorCore3X3 * Math.Cos(statorCoreTheta) - statorCore3Y3 * Math.Sin(statorCoreTheta);
            double statorCore4Y3 = statorCore3X3 * Math.Sin(statorCoreTheta) + statorCore3Y3 * Math.Cos(statorCoreTheta);
            double statorCore4X4 = statorCore3X4 * Math.Cos(statorCoreTheta) - statorCore3Y4 * Math.Sin(statorCoreTheta);
            double statorCore4Y4 = statorCore3X4 * Math.Sin(statorCoreTheta) + statorCore3Y4 * Math.Cos(statorCoreTheta);
            //
            double statorCore5X1 = statorCore4X1 * Math.Cos(statorCoreTheta) - statorCore4Y1 * Math.Sin(statorCoreTheta);
            double statorCore5Y1 = statorCore4X1 * Math.Sin(statorCoreTheta) + statorCore4Y1 * Math.Cos(statorCoreTheta);
            double statorCore5X2 = statorCore4X2 * Math.Cos(statorCoreTheta) - statorCore4Y2 * Math.Sin(statorCoreTheta);
            double statorCore5Y2 = statorCore4X2 * Math.Sin(statorCoreTheta) + statorCore4Y2 * Math.Cos(statorCoreTheta);
            double statorCore5X3 = statorCore4X3 * Math.Cos(statorCoreTheta) - statorCore4Y3 * Math.Sin(statorCoreTheta);
            double statorCore5Y3 = statorCore4X3 * Math.Sin(statorCoreTheta) + statorCore4Y3 * Math.Cos(statorCoreTheta);
            double statorCore5X4 = statorCore4X4 * Math.Cos(statorCoreTheta) - statorCore4Y4 * Math.Sin(statorCoreTheta);
            double statorCore5Y4 = statorCore4X4 * Math.Sin(statorCoreTheta) + statorCore4Y4 * Math.Cos(statorCoreTheta);
            //
            double statorCore6X1 = statorCore5X1 * Math.Cos(statorCoreTheta) - statorCore5Y1 * Math.Sin(statorCoreTheta);
            double statorCore6Y1 = statorCore5X1 * Math.Sin(statorCoreTheta) + statorCore5Y1 * Math.Cos(statorCoreTheta);
            double statorCore6X2 = statorCore5X2 * Math.Cos(statorCoreTheta) - statorCore5Y2 * Math.Sin(statorCoreTheta);
            double statorCore6Y2 = statorCore5X2 * Math.Sin(statorCoreTheta) + statorCore5Y2 * Math.Cos(statorCoreTheta);
            double statorCore6X3 = statorCore5X3 * Math.Cos(statorCoreTheta) - statorCore5Y3 * Math.Sin(statorCoreTheta);
            double statorCore6Y3 = statorCore5X3 * Math.Sin(statorCoreTheta) + statorCore5Y3 * Math.Cos(statorCoreTheta);
            double statorCore6X4 = statorCore5X4 * Math.Cos(statorCoreTheta) - statorCore5Y4 * Math.Sin(statorCoreTheta);
            double statorCore6Y4 = statorCore5X4 * Math.Sin(statorCoreTheta) + statorCore5Y4 * Math.Cos(statorCoreTheta);

            // ローター
            double rotorR1 = 2.0;
            double rotorR2 = 3.9;
            double rotorTheta1 = 50.0 * Math.PI / 180.0;
            double rotorTheta2 = 65.0 * Math.PI / 180.0;
            double rotorGapTheta = Math.PI / 2.0 - 2.0 * (Math.PI / 2.0 - rotorTheta1);
            double rotorCoreX = rotorR1 * Math.Cos(rotorTheta1);
            double rotorCoreY1 = rotorR1 * Math.Sin(rotorTheta1);
            double rotorCoreY2 = rotorR2 * Math.Sin(rotorTheta2);

            // コイル
            double coilW = 0.5;
            double coilH = 2.5;
            double coilGap = 0.1;
            // コア1のコイル
            //
            double coil11X1 = statorCore1X2 + coilGap + coilW;
            double coil11Y1 = statorCore1Y2;
            double coil11X2 = coil11X1;
            double coil11Y2 = coil11Y1 + coilH;
            double coil11X3 = coil11X1 - coilW;
            double coil11Y3 = coil11Y2;
            double coil11X4 = coil11X3;
            double coil11Y4 = coil11Y1;
            //
            double coil12X1 = statorCore1X3 - coilGap;
            double coil12Y1 = statorCore1Y3;
            double coil12X2 = coil12X1;
            double coil12Y2 = coil12Y1 + coilH;
            double coil12X3 = coil12X1 - coilW;
            double coil12Y3 = coil12Y2;
            double coil12X4 = coil12X3;
            double coil12Y4 = coil12Y1;
            // コア2のコイル
            //
            double coil21X1 = coil11X1 * Math.Cos(statorCoreTheta) - coil11Y1 * Math.Sin(statorCoreTheta);
            double coil21Y1 = coil11X1 * Math.Sin(statorCoreTheta) + coil11Y1 * Math.Cos(statorCoreTheta);
            double coil21X2 = coil11X2 * Math.Cos(statorCoreTheta) - coil11Y2 * Math.Sin(statorCoreTheta);
            double coil21Y2 = coil11X2 * Math.Sin(statorCoreTheta) + coil11Y2 * Math.Cos(statorCoreTheta);
            double coil21X3 = coil11X3 * Math.Cos(statorCoreTheta) - coil11Y3 * Math.Sin(statorCoreTheta);
            double coil21Y3 = coil11X3 * Math.Sin(statorCoreTheta) + coil11Y3 * Math.Cos(statorCoreTheta);
            double coil21X4 = coil11X4 * Math.Cos(statorCoreTheta) - coil11Y4 * Math.Sin(statorCoreTheta);
            double coil21Y4 = coil11X4 * Math.Sin(statorCoreTheta) + coil11Y4 * Math.Cos(statorCoreTheta);
            //
            double coil22X1 = coil12X1 * Math.Cos(statorCoreTheta) - coil12Y1 * Math.Sin(statorCoreTheta);
            double coil22Y1 = coil12X1 * Math.Sin(statorCoreTheta) + coil12Y1 * Math.Cos(statorCoreTheta);
            double coil22X2 = coil12X2 * Math.Cos(statorCoreTheta) - coil12Y2 * Math.Sin(statorCoreTheta);
            double coil22Y2 = coil12X2 * Math.Sin(statorCoreTheta) + coil12Y2 * Math.Cos(statorCoreTheta);
            double coil22X3 = coil12X3 * Math.Cos(statorCoreTheta) - coil12Y3 * Math.Sin(statorCoreTheta);
            double coil22Y3 = coil12X3 * Math.Sin(statorCoreTheta) + coil12Y3 * Math.Cos(statorCoreTheta);
            double coil22X4 = coil12X4 * Math.Cos(statorCoreTheta) - coil12Y4 * Math.Sin(statorCoreTheta);
            double coil22Y4 = coil12X4 * Math.Sin(statorCoreTheta) + coil12Y4 * Math.Cos(statorCoreTheta);
            // コア3のコイル
            //
            double coil31X1 = coil21X1 * Math.Cos(statorCoreTheta) - coil21Y1 * Math.Sin(statorCoreTheta);
            double coil31Y1 = coil21X1 * Math.Sin(statorCoreTheta) + coil21Y1 * Math.Cos(statorCoreTheta);
            double coil31X2 = coil21X2 * Math.Cos(statorCoreTheta) - coil21Y2 * Math.Sin(statorCoreTheta);
            double coil31Y2 = coil21X2 * Math.Sin(statorCoreTheta) + coil21Y2 * Math.Cos(statorCoreTheta);
            double coil31X3 = coil21X3 * Math.Cos(statorCoreTheta) - coil21Y3 * Math.Sin(statorCoreTheta);
            double coil31Y3 = coil21X3 * Math.Sin(statorCoreTheta) + coil21Y3 * Math.Cos(statorCoreTheta);
            double coil31X4 = coil21X4 * Math.Cos(statorCoreTheta) - coil21Y4 * Math.Sin(statorCoreTheta);
            double coil31Y4 = coil21X4 * Math.Sin(statorCoreTheta) + coil21Y4 * Math.Cos(statorCoreTheta);
            //
            double coil32X1 = coil22X1 * Math.Cos(statorCoreTheta) - coil22Y1 * Math.Sin(statorCoreTheta);
            double coil32Y1 = coil22X1 * Math.Sin(statorCoreTheta) + coil22Y1 * Math.Cos(statorCoreTheta);
            double coil32X2 = coil22X2 * Math.Cos(statorCoreTheta) - coil22Y2 * Math.Sin(statorCoreTheta);
            double coil32Y2 = coil22X2 * Math.Sin(statorCoreTheta) + coil22Y2 * Math.Cos(statorCoreTheta);
            double coil32X3 = coil22X3 * Math.Cos(statorCoreTheta) - coil22Y3 * Math.Sin(statorCoreTheta);
            double coil32Y3 = coil22X3 * Math.Sin(statorCoreTheta) + coil22Y3 * Math.Cos(statorCoreTheta);
            double coil32X4 = coil22X4 * Math.Cos(statorCoreTheta) - coil22Y4 * Math.Sin(statorCoreTheta);
            double coil32Y4 = coil22X4 * Math.Sin(statorCoreTheta) + coil22Y4 * Math.Cos(statorCoreTheta);
            // コア4のコイル
            //
            double coil41X1 = coil31X1 * Math.Cos(statorCoreTheta) - coil31Y1 * Math.Sin(statorCoreTheta);
            double coil41Y1 = coil31X1 * Math.Sin(statorCoreTheta) + coil31Y1 * Math.Cos(statorCoreTheta);
            double coil41X2 = coil31X2 * Math.Cos(statorCoreTheta) - coil31Y2 * Math.Sin(statorCoreTheta);
            double coil41Y2 = coil31X2 * Math.Sin(statorCoreTheta) + coil31Y2 * Math.Cos(statorCoreTheta);
            double coil41X3 = coil31X3 * Math.Cos(statorCoreTheta) - coil31Y3 * Math.Sin(statorCoreTheta);
            double coil41Y3 = coil31X3 * Math.Sin(statorCoreTheta) + coil31Y3 * Math.Cos(statorCoreTheta);
            double coil41X4 = coil31X4 * Math.Cos(statorCoreTheta) - coil31Y4 * Math.Sin(statorCoreTheta);
            double coil41Y4 = coil31X4 * Math.Sin(statorCoreTheta) + coil31Y4 * Math.Cos(statorCoreTheta);
            //
            double coil42X1 = coil32X1 * Math.Cos(statorCoreTheta) - coil32Y1 * Math.Sin(statorCoreTheta);
            double coil42Y1 = coil32X1 * Math.Sin(statorCoreTheta) + coil32Y1 * Math.Cos(statorCoreTheta);
            double coil42X2 = coil32X2 * Math.Cos(statorCoreTheta) - coil32Y2 * Math.Sin(statorCoreTheta);
            double coil42Y2 = coil32X2 * Math.Sin(statorCoreTheta) + coil32Y2 * Math.Cos(statorCoreTheta);
            double coil42X3 = coil32X3 * Math.Cos(statorCoreTheta) - coil32Y3 * Math.Sin(statorCoreTheta);
            double coil42Y3 = coil32X3 * Math.Sin(statorCoreTheta) + coil32Y3 * Math.Cos(statorCoreTheta);
            double coil42X4 = coil32X4 * Math.Cos(statorCoreTheta) - coil32Y4 * Math.Sin(statorCoreTheta);
            double coil42Y4 = coil32X4 * Math.Sin(statorCoreTheta) + coil32Y4 * Math.Cos(statorCoreTheta);
            // コア5のコイル
            //
            double coil51X1 = coil41X1 * Math.Cos(statorCoreTheta) - coil41Y1 * Math.Sin(statorCoreTheta);
            double coil51Y1 = coil41X1 * Math.Sin(statorCoreTheta) + coil41Y1 * Math.Cos(statorCoreTheta);
            double coil51X2 = coil41X2 * Math.Cos(statorCoreTheta) - coil41Y2 * Math.Sin(statorCoreTheta);
            double coil51Y2 = coil41X2 * Math.Sin(statorCoreTheta) + coil41Y2 * Math.Cos(statorCoreTheta);
            double coil51X3 = coil41X3 * Math.Cos(statorCoreTheta) - coil41Y3 * Math.Sin(statorCoreTheta);
            double coil51Y3 = coil41X3 * Math.Sin(statorCoreTheta) + coil41Y3 * Math.Cos(statorCoreTheta);
            double coil51X4 = coil41X4 * Math.Cos(statorCoreTheta) - coil41Y4 * Math.Sin(statorCoreTheta);
            double coil51Y4 = coil41X4 * Math.Sin(statorCoreTheta) + coil41Y4 * Math.Cos(statorCoreTheta);
            //
            double coil52X1 = coil42X1 * Math.Cos(statorCoreTheta) - coil42Y1 * Math.Sin(statorCoreTheta);
            double coil52Y1 = coil42X1 * Math.Sin(statorCoreTheta) + coil42Y1 * Math.Cos(statorCoreTheta);
            double coil52X2 = coil42X2 * Math.Cos(statorCoreTheta) - coil42Y2 * Math.Sin(statorCoreTheta);
            double coil52Y2 = coil42X2 * Math.Sin(statorCoreTheta) + coil42Y2 * Math.Cos(statorCoreTheta);
            double coil52X3 = coil42X3 * Math.Cos(statorCoreTheta) - coil42Y3 * Math.Sin(statorCoreTheta);
            double coil52Y3 = coil42X3 * Math.Sin(statorCoreTheta) + coil42Y3 * Math.Cos(statorCoreTheta);
            double coil52X4 = coil42X4 * Math.Cos(statorCoreTheta) - coil42Y4 * Math.Sin(statorCoreTheta);
            double coil52Y4 = coil42X4 * Math.Sin(statorCoreTheta) + coil42Y4 * Math.Cos(statorCoreTheta);
            // コア6のコイル
            //
            double coil61X1 = coil51X1 * Math.Cos(statorCoreTheta) - coil51Y1 * Math.Sin(statorCoreTheta);
            double coil61Y1 = coil51X1 * Math.Sin(statorCoreTheta) + coil51Y1 * Math.Cos(statorCoreTheta);
            double coil61X2 = coil51X2 * Math.Cos(statorCoreTheta) - coil51Y2 * Math.Sin(statorCoreTheta);
            double coil61Y2 = coil51X2 * Math.Sin(statorCoreTheta) + coil51Y2 * Math.Cos(statorCoreTheta);
            double coil61X3 = coil51X3 * Math.Cos(statorCoreTheta) - coil51Y3 * Math.Sin(statorCoreTheta);
            double coil61Y3 = coil51X3 * Math.Sin(statorCoreTheta) + coil51Y3 * Math.Cos(statorCoreTheta);
            double coil61X4 = coil51X4 * Math.Cos(statorCoreTheta) - coil51Y4 * Math.Sin(statorCoreTheta);
            double coil61Y4 = coil51X4 * Math.Sin(statorCoreTheta) + coil51Y4 * Math.Cos(statorCoreTheta);
            //
            double coil62X1 = coil52X1 * Math.Cos(statorCoreTheta) - coil52Y1 * Math.Sin(statorCoreTheta);
            double coil62Y1 = coil52X1 * Math.Sin(statorCoreTheta) + coil52Y1 * Math.Cos(statorCoreTheta);
            double coil62X2 = coil52X2 * Math.Cos(statorCoreTheta) - coil52Y2 * Math.Sin(statorCoreTheta);
            double coil62Y2 = coil52X2 * Math.Sin(statorCoreTheta) + coil52Y2 * Math.Cos(statorCoreTheta);
            double coil62X3 = coil52X3 * Math.Cos(statorCoreTheta) - coil52Y3 * Math.Sin(statorCoreTheta);
            double coil62Y3 = coil52X3 * Math.Sin(statorCoreTheta) + coil52Y3 * Math.Cos(statorCoreTheta);
            double coil62X4 = coil52X4 * Math.Cos(statorCoreTheta) - coil52Y4 * Math.Sin(statorCoreTheta);
            double coil62Y4 = coil52X4 * Math.Sin(statorCoreTheta) + coil52Y4 * Math.Cos(statorCoreTheta);

            // ステーター
            // 比透磁率
            double statorMu = 1000.0;
            // 導電率
            double statorSigma = 1.39e+6;
            // 印加電圧の勾配
            double statorGradPhi = 0.0;

            // ローター
            // 比透磁率
            double rotorMu = 1000.0;
            // 導電率
            double rotorSigma = 1.39e+6;
            // 印加電圧の勾配
            double rotorGradPhi = 0.0;

            // コイル1,4
            // 比透磁率
            double coil14Mu = 1.0;
            // 導電率
            double coil14Sigma = 59.0e+6;
            // 印加電圧の勾配
            double coil14GradPhi = -1.0e+2;
            // コイル3,6
            // 比透磁率
            double coil36Mu = 1.0;
            // 導電率
            double coil36Sigma = 59.0e+6;
            // 印加電圧の勾配
            double coil36GradPhi = -1.0e+2;
            // コイル2,5
            // 比透磁率
            double coil25Mu = 1.0;
            // 導電率
            double coil25Sigma = 59.0e+6;
            // 印加電圧の勾配
            double coil25GradPhi = -1.0e+2;

            // 周波数
            double freq = 50.0;

            double eLen = rotorR1 * 0.2;

            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> statorR3Pts = new List<OpenTK.Vector2d>();
                statorR3Pts.Add(new OpenTK.Vector2d(0.0, statorR3));
                statorR3Pts.Add(new OpenTK.Vector2d(-statorR3, 0.0));
                statorR3Pts.Add(new OpenTK.Vector2d(0.0, -statorR3));
                statorR3Pts.Add(new OpenTK.Vector2d(statorR3, 0.0));
                uint statorR3LId = cad.AddPolygon(statorR3Pts).AddLId;
                uint[] statorR3EIds = { 1, 2, 3, 4 };
                foreach (uint eId in statorR3EIds)
                {
                    cad.SetCurveArc(eId, false, 1.0 / 2.0);
                }

                IList<OpenTK.Vector2d> statorR2Pts = new List<OpenTK.Vector2d>();
                //
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore1X1, statorCore1Y1));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore1X2, statorCore1Y2));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore1X3, statorCore1Y3));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore1X4, statorCore1Y4));
                //
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore2X1, statorCore2Y1));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore2X2, statorCore2Y2));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore2X3, statorCore2Y3));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore2X4, statorCore2Y4));
                //
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore3X1, statorCore3Y1));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore3X2, statorCore3Y2));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore3X3, statorCore3Y3));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore3X4, statorCore3Y4));
                //
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore4X1, statorCore4Y1));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore4X2, statorCore4Y2));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore4X3, statorCore4Y3));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore4X4, statorCore4Y4));
                //
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore5X1, statorCore5Y1));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore5X2, statorCore5Y2));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore5X3, statorCore5Y3));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore5X4, statorCore5Y4));
                //
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore6X1, statorCore6Y1));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore6X2, statorCore6Y2));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore6X3, statorCore6Y3));
                statorR2Pts.Add(new OpenTK.Vector2d(statorCore6X4, statorCore6Y4));
                //
                uint _statorR2LId = cad.AddPolygon(statorR2Pts, statorR3LId).AddLId;

                uint[] _statorR2EIds = { 8, 12, 16, 20, 24, 28 };
                foreach (uint eId in _statorR2EIds)
                {
                    cad.SetCurveArc(eId, false, 1.0 / (2.0 * Math.Tan(statorGapTheta / 2.0)));
                }

                uint[] _statorR1EIds = { 6, 10, 14, 18, 22, 26 };
                foreach (uint eId in _statorR1EIds)
                {
                    cad.SetCurveArc(eId, false, 1.0 / (2.0 * Math.Tan(Math.PI / 2.0 - statorTheta1)));
                }

                IList<OpenTK.Vector2d> rotorPts = new List<OpenTK.Vector2d>();
                //
                rotorPts.Add(new OpenTK.Vector2d(rotorCoreX, rotorCoreY1));
                rotorPts.Add(new OpenTK.Vector2d(rotorCoreX, rotorCoreY2));
                rotorPts.Add(new OpenTK.Vector2d(-rotorCoreX, rotorCoreY2));
                rotorPts.Add(new OpenTK.Vector2d(-rotorCoreX, rotorCoreY1));
                //
                rotorPts.Add(new OpenTK.Vector2d(-rotorCoreY1, rotorCoreX));
                rotorPts.Add(new OpenTK.Vector2d(-rotorCoreY2, rotorCoreX));
                rotorPts.Add(new OpenTK.Vector2d(-rotorCoreY2, -rotorCoreX));
                rotorPts.Add(new OpenTK.Vector2d(-rotorCoreY1, -rotorCoreX));
                //
                rotorPts.Add(new OpenTK.Vector2d(-rotorCoreX, -rotorCoreY1));
                rotorPts.Add(new OpenTK.Vector2d(-rotorCoreX, -rotorCoreY2));
                rotorPts.Add(new OpenTK.Vector2d(rotorCoreX, -rotorCoreY2));
                rotorPts.Add(new OpenTK.Vector2d(rotorCoreX, -rotorCoreY1));
                //
                rotorPts.Add(new OpenTK.Vector2d(rotorCoreY1, -rotorCoreX));
                rotorPts.Add(new OpenTK.Vector2d(rotorCoreY2, -rotorCoreX));
                rotorPts.Add(new OpenTK.Vector2d(rotorCoreY2, rotorCoreX));
                rotorPts.Add(new OpenTK.Vector2d(rotorCoreY1, rotorCoreX));
                //
                uint _rotorLId = cad.AddPolygon(rotorPts, _statorR2LId).AddLId;

                uint[] rotorR2EIds = { 30, 34, 38, 42 };
                foreach (uint eId in rotorR2EIds)
                {
                    cad.SetCurveArc(eId, false, 1.0 / (2.0 * Math.Tan(Math.PI / 2.0 - rotorTheta2)));
                }

                uint[] _rotorR1EIds = { 32, 36, 40, 44 };
                foreach (uint eId in _rotorR1EIds)
                {
                    cad.SetCurveArc(eId, false, 1.0 / (2.0 * Math.Tan(rotorGapTheta / 2.0)));
                }

                //
                IList<OpenTK.Vector2d> coil11Pts = new List<OpenTK.Vector2d>();
                coil11Pts.Add(new OpenTK.Vector2d(coil11X1, coil11Y1));
                coil11Pts.Add(new OpenTK.Vector2d(coil11X2, coil11Y2));
                coil11Pts.Add(new OpenTK.Vector2d(coil11X3, coil11Y3));
                coil11Pts.Add(new OpenTK.Vector2d(coil11X4, coil11Y4));
                uint _coil11LId = cad.AddPolygon(coil11Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil12Pts = new List<OpenTK.Vector2d>();
                coil12Pts.Add(new OpenTK.Vector2d(coil12X1, coil12Y1));
                coil12Pts.Add(new OpenTK.Vector2d(coil12X2, coil12Y2));
                coil12Pts.Add(new OpenTK.Vector2d(coil12X3, coil12Y3));
                coil12Pts.Add(new OpenTK.Vector2d(coil12X4, coil12Y4));
                uint _coil12LId = cad.AddPolygon(coil12Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil21Pts = new List<OpenTK.Vector2d>();
                coil21Pts.Add(new OpenTK.Vector2d(coil21X1, coil21Y1));
                coil21Pts.Add(new OpenTK.Vector2d(coil21X2, coil21Y2));
                coil21Pts.Add(new OpenTK.Vector2d(coil21X3, coil21Y3));
                coil21Pts.Add(new OpenTK.Vector2d(coil21X4, coil21Y4));
                uint _coil21LId = cad.AddPolygon(coil21Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil22Pts = new List<OpenTK.Vector2d>();
                coil22Pts.Add(new OpenTK.Vector2d(coil22X1, coil22Y1));
                coil22Pts.Add(new OpenTK.Vector2d(coil22X2, coil22Y2));
                coil22Pts.Add(new OpenTK.Vector2d(coil22X3, coil22Y3));
                coil22Pts.Add(new OpenTK.Vector2d(coil22X4, coil22Y4));
                uint _coil22LId = cad.AddPolygon(coil22Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil31Pts = new List<OpenTK.Vector2d>();
                coil31Pts.Add(new OpenTK.Vector2d(coil31X1, coil31Y1));
                coil31Pts.Add(new OpenTK.Vector2d(coil31X2, coil31Y2));
                coil31Pts.Add(new OpenTK.Vector2d(coil31X3, coil31Y3));
                coil31Pts.Add(new OpenTK.Vector2d(coil31X4, coil31Y4));
                uint _coil31LId = cad.AddPolygon(coil31Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil32Pts = new List<OpenTK.Vector2d>();
                coil32Pts.Add(new OpenTK.Vector2d(coil32X1, coil32Y1));
                coil32Pts.Add(new OpenTK.Vector2d(coil32X2, coil32Y2));
                coil32Pts.Add(new OpenTK.Vector2d(coil32X3, coil32Y3));
                coil32Pts.Add(new OpenTK.Vector2d(coil32X4, coil32Y4));
                uint _coil32LId = cad.AddPolygon(coil32Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil41Pts = new List<OpenTK.Vector2d>();
                coil41Pts.Add(new OpenTK.Vector2d(coil41X1, coil41Y1));
                coil41Pts.Add(new OpenTK.Vector2d(coil41X2, coil41Y2));
                coil41Pts.Add(new OpenTK.Vector2d(coil41X3, coil41Y3));
                coil41Pts.Add(new OpenTK.Vector2d(coil41X4, coil41Y4));
                uint _coil41LId = cad.AddPolygon(coil41Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil42Pts = new List<OpenTK.Vector2d>();
                coil42Pts.Add(new OpenTK.Vector2d(coil42X1, coil42Y1));
                coil42Pts.Add(new OpenTK.Vector2d(coil42X2, coil42Y2));
                coil42Pts.Add(new OpenTK.Vector2d(coil42X3, coil42Y3));
                coil42Pts.Add(new OpenTK.Vector2d(coil42X4, coil42Y4));
                uint _coil42LId = cad.AddPolygon(coil42Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil51Pts = new List<OpenTK.Vector2d>();
                coil51Pts.Add(new OpenTK.Vector2d(coil51X1, coil51Y1));
                coil51Pts.Add(new OpenTK.Vector2d(coil51X2, coil51Y2));
                coil51Pts.Add(new OpenTK.Vector2d(coil51X3, coil51Y3));
                coil51Pts.Add(new OpenTK.Vector2d(coil51X4, coil51Y4));
                uint _coil51LId = cad.AddPolygon(coil51Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil52Pts = new List<OpenTK.Vector2d>();
                coil52Pts.Add(new OpenTK.Vector2d(coil52X1, coil52Y1));
                coil52Pts.Add(new OpenTK.Vector2d(coil52X2, coil52Y2));
                coil52Pts.Add(new OpenTK.Vector2d(coil52X3, coil52Y3));
                coil52Pts.Add(new OpenTK.Vector2d(coil52X4, coil52Y4));
                uint _coil52LId = cad.AddPolygon(coil52Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil61Pts = new List<OpenTK.Vector2d>();
                coil61Pts.Add(new OpenTK.Vector2d(coil61X1, coil61Y1));
                coil61Pts.Add(new OpenTK.Vector2d(coil61X2, coil61Y2));
                coil61Pts.Add(new OpenTK.Vector2d(coil61X3, coil61Y3));
                coil61Pts.Add(new OpenTK.Vector2d(coil61X4, coil61Y4));
                uint _coil61LId = cad.AddPolygon(coil61Pts, _statorR2LId).AddLId;
                //
                IList<OpenTK.Vector2d> coil62Pts = new List<OpenTK.Vector2d>();
                coil62Pts.Add(new OpenTK.Vector2d(coil62X1, coil62Y1));
                coil62Pts.Add(new OpenTK.Vector2d(coil62X2, coil62Y2));
                coil62Pts.Add(new OpenTK.Vector2d(coil62X3, coil62Y3));
                coil62Pts.Add(new OpenTK.Vector2d(coil62X4, coil62Y4));
                uint _coil62LId = cad.AddPolygon(coil62Pts, _statorR2LId).AddLId;
            }

            // check
            uint statorLId = 1;
            uint airLId = 2;
            uint rotorLId = 3;
            uint[] coil14LIds = { 4, 5, 10, 11 };
            uint[] coil36LIds = { 8, 9, 14, 15 };
            uint[] coil25LIds = { 6, 7, 12, 13 };
            {
                double[] statorColor = { 0.5, 0.5, 1.0 };
                cad.SetLoopColor(statorLId, statorColor);

                double[] airColor = { 1.0, 1.0, 1.0 };
                cad.SetLoopColor(airLId, airColor);

                double[] rotorColor = { 0.7, 0.7, 1.0 };
                cad.SetLoopColor(rotorLId, rotorColor);

                double[] coil14Color = { 1.0, 0.5, 0.5 };
                foreach (uint lId in coil14LIds)
                {
                    cad.SetLoopColor(lId, coil14Color);
                }

                double[] coil36Color = { 1.0, 0.3, 0.3 };
                foreach (uint lId in coil36LIds)
                {
                    cad.SetLoopColor(lId, coil36Color);
                }

                double[] coil25Color = { 1.0, 0.2, 0.2 };
                foreach (uint lId in coil25LIds)
                {
                    cad.SetLoopColor(lId, coil25Color);
                }
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad2DDrawer(cad);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            Mesher2D mesher = new Mesher2D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            var meshDrawer = new Mesher2DDrawer(mesher);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 1; // スカラー
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint statorMaId;
            uint airMaId;
            uint rotorMaId;
            uint coil141MaId;
            uint coil142MaId;
            uint coil361MaId;
            uint coil362MaId;
            uint coil251MaId;
            uint coil252MaId;
            {
                world.ClearMaterial();

                EddyCurrentMaterial statorMa = new EddyCurrentMaterial
                {
                    Mu = statorMu,
                    Sigma = statorSigma,
                    GradPhi = statorGradPhi
                };
                EddyCurrentMaterial airMa = new EddyCurrentMaterial
                {
                    Mu = 1.0,
                    Sigma = 0.0,
                    GradPhi = 0.0
                };
                EddyCurrentMaterial rotorMa = new EddyCurrentMaterial
                {
                    Mu = rotorMu,
                    Sigma = rotorSigma,
                    GradPhi = rotorGradPhi
                };
                EddyCurrentMaterial coil141Ma = new EddyCurrentMaterial
                {
                    Mu = coil14Mu,
                    Sigma = coil14Sigma,
                    GradPhi = coil14GradPhi
                };
                EddyCurrentMaterial coil142Ma = new EddyCurrentMaterial
                {
                    Mu = coil14Mu,
                    Sigma = coil14Sigma,
                    GradPhi = -coil14GradPhi
                };
                EddyCurrentMaterial coil361Ma = new EddyCurrentMaterial
                {
                    Mu = coil36Mu,
                    Sigma = coil36Sigma,
                    GradPhi = coil36GradPhi
                };
                EddyCurrentMaterial coil362Ma = new EddyCurrentMaterial
                {
                    Mu = coil36Mu,
                    Sigma = coil36Sigma,
                    GradPhi = -coil36GradPhi
                };
                EddyCurrentMaterial coil251Ma = new EddyCurrentMaterial
                {
                    Mu = coil25Mu,
                    Sigma = coil25Sigma,
                    GradPhi = coil25GradPhi
                };
                EddyCurrentMaterial coil252Ma = new EddyCurrentMaterial
                {
                    Mu = coil25Mu,
                    Sigma = coil25Sigma,
                    GradPhi = -coil25GradPhi
                };
                statorMaId = world.AddMaterial(statorMa);
                airMaId = world.AddMaterial(airMa);
                rotorMaId = world.AddMaterial(rotorMa);
                coil141MaId = world.AddMaterial(coil141Ma);
                coil142MaId = world.AddMaterial(coil142Ma);
                coil361MaId = world.AddMaterial(coil361Ma);
                coil362MaId = world.AddMaterial(coil362Ma);
                coil251MaId = world.AddMaterial(coil251Ma);
                coil252MaId = world.AddMaterial(coil252Ma);

                world.SetCadLoopMaterial(statorLId, statorMaId);
                world.SetCadLoopMaterial(airLId, airMaId);
                world.SetCadLoopMaterial(rotorLId, rotorMaId);
                //
                world.SetCadLoopMaterial(coil14LIds[0], coil141MaId);
                world.SetCadLoopMaterial(coil14LIds[1], coil142MaId);
                world.SetCadLoopMaterial(coil14LIds[2], coil142MaId);
                world.SetCadLoopMaterial(coil14LIds[3], coil141MaId);
                //
                world.SetCadLoopMaterial(coil36LIds[0], coil361MaId);
                world.SetCadLoopMaterial(coil36LIds[1], coil362MaId);
                world.SetCadLoopMaterial(coil36LIds[2], coil361MaId);
                world.SetCadLoopMaterial(coil36LIds[3], coil362MaId);
                //
                world.SetCadLoopMaterial(coil25LIds[0], coil251MaId);
                world.SetCadLoopMaterial(coil25LIds[1], coil252MaId);
                world.SetCadLoopMaterial(coil25LIds[2], coil251MaId);
                world.SetCadLoopMaterial(coil25LIds[3], coil252MaId);
            }

            {
                // ステーターの外側境界はA=0
                var fixedCadDatas = new[]
                {
                    new { CadId = (uint)1, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)2, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)3, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } },
                    new { CadId = (uint)4, CadElemType = CadElementType.Edge,
                        FixedDofIndexs = new List<uint> { 0 }, Values = new List<double> { 0.0 } }
                };
                var fixedCads = world.GetFieldFixedCads(quantityId);
                foreach (var data in fixedCadDatas)
                {
                    // Scalar
                    var fixedCad = new ConstFieldFixedCad(data.CadId, data.CadElemType,
                        FieldValueType.Scalar, data.FixedDofIndexs, data.Values);
                    fixedCads.Add(fixedCad);
                }
            }

            world.MakeElements();

            uint vecValueId = 0;
            uint valueId = 0;
            uint prevValueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // スカラー
                valueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);
                prevValueId = world.AddFieldValue(FieldValueType.Scalar,
                    FieldDerivativeType.Value | FieldDerivativeType.Velocity | FieldDerivativeType.Acceleration,
                    quantityId, false, FieldShowType.Real);

                // Vector2
                vecValueId = world.AddFieldValue(FieldValueType.Vector2, FieldDerivativeType.Value,
                    quantityId, true, FieldShowType.Real);

                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                var vectorDrawer = new VectorFieldDrawer(
                    vecValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            double t = 0;
            double dt = (1.0 / freq) * 0.1;//0.05;
            double newmarkBeta = 1.0 / 4.0;
            double newmarkGamma = 1.0 / 2.0;
            for (int iTime = 0; iTime <= 1000; iTime++)
            {
                var coil141Ma = world.GetMaterial(coil141MaId) as EddyCurrentMaterial;
                var coil142Ma = world.GetMaterial(coil142MaId) as EddyCurrentMaterial;
                var coil361Ma = world.GetMaterial(coil361MaId) as EddyCurrentMaterial;
                var coil362Ma = world.GetMaterial(coil362MaId) as EddyCurrentMaterial;
                var coil251Ma = world.GetMaterial(coil251MaId) as EddyCurrentMaterial;
                var coil252Ma = world.GetMaterial(coil252MaId) as EddyCurrentMaterial;
                double omega = 2.0 * Math.PI * freq;
                coil141Ma.GradPhi = coil14GradPhi * Math.Sqrt(2.0) * Math.Sin(omega * t);
                coil142Ma.GradPhi = -coil141Ma.GradPhi;
                coil361Ma.GradPhi = coil36GradPhi * Math.Sqrt(2.0) * Math.Sin(omega * t - 120.0 * Math.PI / 180.0);
                coil362Ma.GradPhi = -coil361Ma.GradPhi;
                coil251Ma.GradPhi = coil25GradPhi * Math.Sqrt(2.0) * Math.Sin(omega * t - 240.0 * Math.PI / 180.0);
                coil252Ma.GradPhi = -coil251Ma.GradPhi;

                var FEM = new EddyCurrent2DTDFEM(world, dt,
                    newmarkBeta, newmarkGamma,
                    valueId, prevValueId);
                {
                    //var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    //solver.IsOrderingToBandMatrix = true;
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
                    solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    FEM.Solver = solver;
                }
                FEM.Solve();
                FEM.UpdateFieldValuesTimeDomain();
                double[] nodeA = FEM.U;
                double[] coordB = FEM.CoordB;

                // 磁束密度B
                int bDof = 2; // x,y成分
                int coCnt = coordB.Length / bDof;
                // Bを表示用にスケーリングする
                {
                    double maxValue = 0;
                    int cnt = coordB.Length;
                    // coordBはx,y成分の順に並んでいる
                    foreach (double value in coordB)
                    {
                        double abs = Math.Abs(value);
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.5 * statorR3;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            coordB[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                world.UpdateBubbleFieldValueValuesFromCoordValues(vecValueId, FieldDerivativeType.Value, coordB);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
                t += dt;
            }
        }
    }
}
