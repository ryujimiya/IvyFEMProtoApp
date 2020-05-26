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
    public enum PlateKind
    {
        DKTPlate,
        MindlinPlate,
        MITCLinearPlate
    }

    partial class Problem
    {
        public int Dimension { get; set; } = 2;

        private ChartWindow ChartWindow1 = null;
        private ChartWindow ChartWindow2 = null;
        private ChartWindow ChartWindow3 = null;
        private AlertWindow AlertWindow1 = null;

        public Problem()
        {

        }

        public void Init(MainWindow mainWindow)
        {
            if (ChartWindow1 != null)
            {
                ChartWindow1.Close();
            }
            if (ChartWindow2 != null)
            {
                ChartWindow2.Close();
            }
            if (ChartWindow3 != null)
            {
                ChartWindow3.Close();
            }
            if (AlertWindow1 != null)
            {
                AlertWindow1.Close();
            }
            Dimension = 2;
            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            fieldDrawerArray.Clear();
        }

        private void ChartWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ChartWindow1 = null;
        }

        private void ChartWindow2_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ChartWindow2 = null;
        }

        private void ChartWindow3_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ChartWindow3 = null;
        }

        private void AlertWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AlertWindow1 = null;
        }
    }
}
