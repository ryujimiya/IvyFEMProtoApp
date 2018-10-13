using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class Elastic2DDerivedBaseFEM : Elastic2DBaseFEM
    {
        // Linear / Saint Venant
        public static void SetStressValue(
            uint displacementValueId, uint stressValueId, uint equivStressValueId, FEWorld world)
        {
            System.Diagnostics.Debug.Assert(world.IsFieldValueId(displacementValueId));
            FieldValue uFV = world.GetFieldValue(displacementValueId);
            uint uQuantityId = uFV.QuantityId;

            FieldValue sigmaFV = null;
            if (stressValueId != 0)
            {
                System.Diagnostics.Debug.Assert(world.IsFieldValueId(stressValueId));
                sigmaFV = world.GetFieldValue(stressValueId);
                System.Diagnostics.Debug.Assert(sigmaFV.Type == FieldValueType.SymmetricTensor2);
                System.Diagnostics.Debug.Assert(sigmaFV.Dof == 3);
            }
            FieldValue eqSigmaFV = null;
            if (equivStressValueId != 0)
            {
                System.Diagnostics.Debug.Assert(world.IsFieldValueId(equivStressValueId));
                eqSigmaFV = world.GetFieldValue(equivStressValueId);
                System.Diagnostics.Debug.Assert(eqSigmaFV.Type == FieldValueType.Scalar);
                System.Diagnostics.Debug.Assert(eqSigmaFV.Dof == 1);
            }

            IList<uint> feIds = world.GetTriangleFEIds(uQuantityId);
            foreach (uint feId in feIds)
            {
                TriangleFE triFE = world.GetTriangleFE(uQuantityId, feId);
                int[] coIds = triFE.NodeCoordIds;
                Material ma = world.GetMaterial(triFE.MaterialId);
                double lambda = 0;
                double mu = 0;
                if (ma is LinearElasticMaterial)
                {
                    var ma1 = ma as LinearElasticMaterial;
                    lambda = ma1.LameLambda;
                    mu = ma1.LameMu;
                }
                else if (ma is SaintVenantHyperelasticMaterial)
                {
                    var ma1 = ma as SaintVenantHyperelasticMaterial;
                    lambda = ma1.LameLambda;
                    mu = ma1.LameMu;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                    throw new NotImplementedException();
                }

                var ip = triFE.GetIntegrationPoints(TriangleIntegrationPointCount.Point1);
                System.Diagnostics.Debug.Assert(ip.PointCount == 1);
                double[] L = ip.Ls[0];
                double[][] Nu = triFE.CalcNu(L);
                double[] Nx = Nu[0];
                double[] Ny = Nu[1];
                double[] uu = new double[4]; // 00, 10, 01, 11 (dux/dx duy/dx dux/dy duy/duy)
                for (int iNode = 0; iNode < coIds.Length; iNode++)
                {
                    int coId = coIds[iNode];
                    double[] u = uFV.GetDoubleValue(coId, FieldDerivationType.Value);
                    uu[0] += u[0] * Nx[iNode];
                    uu[1] += u[1] * Nx[iNode];
                    uu[2] += u[0] * Ny[iNode];
                    uu[3] += u[1] * Ny[iNode];
                }

                //ε strain
                double[] eps = new double[4];
                if (ma is LinearElasticMaterial)
                {
                    eps[0] = 0.5 * (uu[0] + uu[0]);
                    eps[1] = 0.5 * (uu[1] + uu[2]);
                    eps[2] = 0.5 * (uu[2] + uu[1]);
                    eps[3] = 0.5 * (uu[3] + uu[3]);
                }
                else if (ma is SaintVenantHyperelasticMaterial)
                {
                    eps[0] = 0.5 * (uu[0] + uu[0] + uu[0] * uu[0] + uu[1] * uu[1]);
                    eps[1] = 0.5 * (uu[1] + uu[2] + uu[2] * uu[0] + uu[1] * uu[3]);
                    eps[2] = 0.5 * (uu[2] + uu[1] + uu[0] * uu[2] + uu[3] * uu[1]);
                    eps[3] = 0.5 * (uu[3] + uu[3] + uu[2] * uu[2] + uu[3] * uu[3]);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                // σ stress
                double[] sigma = new double[4];
                sigma[0] = mu * eps[0] + lambda * (eps[0] + eps[3]);
                sigma[1] = mu * eps[1];
                sigma[2] = mu * eps[2];
                sigma[3] = mu * eps[3] + lambda * (eps[0] + eps[3]);

                double misesStress = Math.Sqrt(
                    0.5 * (sigma[0] - sigma[3]) * (sigma[0] - sigma[3]) +
                    0.5 * sigma[3] * sigma[3] +
                    0.5 * sigma[0] * sigma[0] +
                    3 * sigma[2] * sigma[2]);

                if (stressValueId != 0)
                {
                    double[] Sigma = sigmaFV.GetDoubleValues(FieldDerivationType.Value);
                    uint dof = sigmaFV.Dof;
                    Sigma[(feId - 1) * dof + 0] = sigma[0]; // σxx
                    Sigma[(feId - 1) * dof + 1] = sigma[3]; // σyy
                    Sigma[(feId - 1) * dof + 2] = sigma[2]; // τxy
                }
                if (equivStressValueId != 0)
                {
                    double[] EqSigma = eqSigmaFV.GetDoubleValues(FieldDerivationType.Value);
                    EqSigma[feId - 1] = misesStress;
                }
            }
        }
    }
}
