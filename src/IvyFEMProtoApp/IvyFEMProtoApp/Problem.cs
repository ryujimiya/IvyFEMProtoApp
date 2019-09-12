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
        private ChartWindow ChartWindow = null;

        public Problem()
        {

        }

        private void ChartWindow_Closed(object sender, EventArgs e)
        {
            ChartWindow = null;
        }
    }
}
