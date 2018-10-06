using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    partial class Hyperelastic2DTDFEM : Hyperelastic2DBaseFEM
    {
        public double TimeStep { get; private set; } = 0;
        public double NewmarkBeta { get; private set; } = 1.0 / 4.0;
        public double NewmarkGamma { get; private set; } = 1.0 / 2.0;
        uint UValueId { get; set; } = 0;
        uint PrevUValueId { get; set; } = 0;
        uint LValueId { get; set; } = 0;

        public Hyperelastic2DTDFEM(FEWorld world, double timeStep,
            double newmarkBeta, double newmarkGamma,
            uint uValueId, uint prevUValueId,
            uint lValueId)
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

            TimeStep = timeStep;
            NewmarkBeta = newmarkBeta;
            NewmarkGamma = newmarkGamma;
            UValueId = uValueId;
            PrevUValueId = prevUValueId;
            LValueId = lValueId;
            SetupCalcABs();
        }

        protected void SetupCalcABs()
        {
            CalcElementABs.Clear();
            CalcElementABs.Add(CalcMooneyRivlinHyperelasticElementAB);
            CalcElementABs.Add(CalcOgdenRivlinHyperelasticElementAB);
            //CalcElementABs.Add(CalcOgdenOriginalRivlinIncompressibleHyperelasticElementAB);
        }

        public void UpdateFieldValues()
        {
            double dt = TimeStep;
            double beta = NewmarkBeta;
            double gamma = NewmarkGamma;

            var uFV = World.GetFieldValue(UValueId);
            var prevUFV = World.GetFieldValue(PrevUValueId);
            prevUFV.Copy(uFV);

            World.UpdateFieldValueValuesFromNodeValues(UValueId, FieldDerivationType.Value, U);
            World.UpdateFieldValueValuesFromNodeValues(LValueId, FieldDerivationType.Value, U);

            double[] u = uFV.GetDoubleValues(FieldDerivationType.Value);
            double[] velU = uFV.GetDoubleValues(FieldDerivationType.Velocity);
            double[] accU = uFV.GetDoubleValues(FieldDerivationType.Acceleration);
            double[] prevU = prevUFV.GetDoubleValues(FieldDerivationType.Value);
            double[] prevVelU = prevUFV.GetDoubleValues(FieldDerivationType.Velocity);
            double[] prevAccU = prevUFV.GetDoubleValues(FieldDerivationType.Acceleration);

            uint uCoCnt = uFV.GetPointCount();
            uint uQuantityId = uFV.QuantityId;
            int uDof = (int)uFV.Dof;
            System.Diagnostics.Debug.Assert(uCoCnt == World.GetCoordCount(uQuantityId));
            System.Diagnostics.Debug.Assert(uDof == 2);
            System.Diagnostics.Debug.Assert(u.Length == uCoCnt * uDof);
            for (int iPt = 0; iPt < uCoCnt; iPt++)
            {
                for (int iDof = 0; iDof < uDof; iDof++)
                {
                    int index = iPt * uDof + iDof;
                    velU[index] =
                        (gamma / (beta * dt)) * (u[index] - prevU[index]) +
                        (1.0 - gamma / beta) * prevVelU[index] +
                        dt * (1.0 - gamma / (2.0 * beta)) * prevAccU[index];
                    accU[index] =
                        (1.0 / (beta * dt * dt)) * (u[index] - prevU[index]) -
                        (1.0 / (beta * dt)) * prevVelU[index] -
                        (1.0 / (2.0 * beta) - 1.0) * prevAccU[index];
                }
            }
        }
    }
}
