using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class Material : IObject
    {
        public MaterialType MaterialType { get; protected set; } = MaterialType.NOT_SET; 
        public double[] Values { get; protected set; } = null;

        public Material()
        {

        }

        public Material(Material src)
        {
            Copy(src);
        }

        public void Copy(IObject src)
        {
            Material srcMa = src as Material;
            MaterialType = srcMa.MaterialType;
            Values = null;
            if (srcMa.Values != null)
            {
                Values = new double[srcMa.Values.Length];
                srcMa.Values.CopyTo(Values, 0);
            }
        }

    }
}
