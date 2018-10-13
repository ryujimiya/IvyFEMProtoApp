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
        private double WaveguideWidth = 0;
        private double InputWGLength = 0;

        public Problem()
        {
            WaveguideWidth = 1.0;
            InputWGLength = 1.0 * WaveguideWidth;
        }
    }
}
