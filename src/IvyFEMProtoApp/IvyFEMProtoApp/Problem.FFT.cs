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
        public void FFTExample(MainWindow mainWindow)
        {
            int n = 8;
            double[] times = new double[n];
            double[] datas = new double[n];
            for (int i = 0; i < n; i++)
            {
                times[i] = i;
                datas[i] = i < 4 ? 1.0 : 0.0;
            }

            string CRLF = System.Environment.NewLine;
            string ret =
                "FFT Example" + CRLF +
                "---------------------------------" + CRLF +
                "datas" + CRLF;
            for (int i = 0; i < n; i++)
            {
                ret += string.Format("{0}", datas[i]) + CRLF;
            }
            ret += "---------------------------------" + CRLF;

            double[] freqs;
            System.Numerics.Complex[] freqDomainDatas;
            IvyFEM.FFT.Functions.DoFFT(times, datas, out freqs, out freqDomainDatas);
            ret +=
                "Result" + CRLF +
                "---------------------------------" + CRLF +
                "frequency domain data" + CRLF;
            for (int i = 0; i < n; i++)
            {
                ret += string.Format("{0}", freqDomainDatas[i]) + CRLF;
            }
            ret += "---------------------------------" + CRLF;

            System.Diagnostics.Debug.WriteLine(ret);
            AlertWindow.ShowDialog(ret);
        }
    }
}
