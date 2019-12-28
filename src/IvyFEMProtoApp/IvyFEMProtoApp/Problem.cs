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
        private ChartWindow ChartWindow1 = null;
        private ChartWindow ChartWindow2 = null;
        private ChartWindow ChartWindow3 = null;

        public Problem()
        {

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
    }
}
