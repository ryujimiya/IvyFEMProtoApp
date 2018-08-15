using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class ElasticMaterial : ElasticBaseMaterial
    {
        public ElasticMaterial() : base()
        {
            MaterialType = MaterialType.ELASTIC;
        }
    }
}
