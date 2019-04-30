using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;

namespace IvyFEMProtoApp
{
    /// <summary>
    /// Materialのサンプル(SampleFEMで使用)
    /// </summary>
    class SampleFEMMaterial : Material
    {
        public double Alpha { get => Values[0]; set => Values[0] = value; }
        public double F { get => Values[1]; set => Values[1] = value; }

        public SampleFEMMaterial() : base()
        {
            int len = 2;
            Values = new double[len];
            for (int i = 0; i < len; i++)
            {
                Values[i] = 0.0;
            }
        }

        public SampleFEMMaterial(SampleFEMMaterial src) : base(src)
        {

        }

        public override void Copy(IObject src)
        {
            base.Copy(src);
        }
    }
}
