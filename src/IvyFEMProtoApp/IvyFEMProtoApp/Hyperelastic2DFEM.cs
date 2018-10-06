using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    partial class Hyperelastic2DFEM : Hyperelastic2DBaseFEM
    {
        public Hyperelastic2DFEM(FEWorld world)
        {
            World = world;

            int quantityCnt = world.QuantityCount();
            QuantityIds = new uint[quantityCnt];
            Dofs = new int[quantityCnt];
            NodeCounts = new int[quantityCnt];
            for (uint quantityId = 0; quantityId < quantityCnt; quantityId++)
            {
                QuantityIds[quantityId] = quantityId;
                Dofs[quantityId] = (int)World.GetDof(quantityId);
                NodeCounts[quantityId] = (int)World.GetNodeCount(quantityId);
            }

            SetupCalcABs();
        }

        protected void SetupCalcABs()
        {
            CalcElementABs.Clear();
            CalcElementABs.Add(CalcMooneyRivlinHyperelasticElementAB);
            CalcElementABs.Add(CalcOgdenHyperelasticElementAB);
            //CalcElementABs.Add(CalcOgdenOriginalIncompressibleHyperelasticElementAB);
        }
    }
}
