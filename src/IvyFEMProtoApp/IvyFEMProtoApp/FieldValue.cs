using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class FieldValue : IObject
    {
        public FieldValueType Type { get; set; } = FieldValueType.NoValue;
        public FieldDerivationType DerivationType { get; set; } = 0;
        public uint QuantityId { get; set; } = 0;
        public uint Dof { get; set; } = 1;
        public bool IsBubble { get; set; } = false;
        public FieldShowType ShowType { get; set; } = FieldShowType.Real;
        public double[] Values { get; set; } = null;
        public double[] VelocityValues { get; set; } = null;
        public double[] AccelerationValues { get; set; } = null;

        public FieldValue()
        {

        }

        public FieldValue(FieldValue src)
        {
            Copy(src);
        }

        public void Copy(IObject src)
        {
            FieldValue srcFV = src as FieldValue;
            Type = srcFV.Type;
            DerivationType = srcFV.DerivationType;
            QuantityId = srcFV.QuantityId;
            Dof = srcFV.Dof;
            IsBubble = srcFV.IsBubble;
            ShowType = srcFV.ShowType;
            CopyValues(srcFV);
        }

        public void CopyValues(FieldValue src)
        {
            Values = null;
            if (src.Values != null)
            {
                Values = new double[src.Values.Length];
                src.Values.CopyTo(Values, 0);
            }
            VelocityValues = null;
            if (src.VelocityValues != null)
            {
                VelocityValues = new double[src.VelocityValues.Length];
                src.VelocityValues.CopyTo(VelocityValues, 0);
            }
            AccelerationValues = null;
            if (src.AccelerationValues != null)
            {
                AccelerationValues = new double[src.AccelerationValues.Length];
                src.AccelerationValues.CopyTo(AccelerationValues, 0);
            }
        }

        public void AllocValues(uint dof, uint pointCnt)
        {
            Dof = dof;
            if (DerivationType.HasFlag(FieldDerivationType.Value))
            {
                Values = new double[pointCnt * Dof];
            }
            if (DerivationType.HasFlag(FieldDerivationType.Velocity))
            {
                VelocityValues = new double[pointCnt * Dof];
            }
            if (DerivationType.HasFlag(FieldDerivationType.Acceleration))
            {
                AccelerationValues = new double[pointCnt * Dof];
            }
        }

        public uint GetPointCount()
        {
            if (Values == null)
            {
                return 0;
            }
            return (uint)(Values.Length / Dof);
        }

        public double[] GetValues(FieldDerivationType dt)
        {
            double[] values = null;
            if (dt.HasFlag(FieldDerivationType.Value) && Values != null)
            {
                values = Values;
            }
            else if (dt.HasFlag(FieldDerivationType.Velocity) && VelocityValues != null)
            {
                values = VelocityValues;
            }
            else if (dt.HasFlag(FieldDerivationType.Acceleration) && AccelerationValues != null)
            {
                values = AccelerationValues;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return values;
        }

        public double[] GetValue(int coId, FieldDerivationType dt)
        {
            double[] values = GetValues(dt);
            double[] value = new double[Dof];
            for (int iDof = 0; iDof < Dof; iDof++)
            {
                value[iDof] = values[coId * Dof + iDof];
            }
            return value;
        }

        public double GetShowValue(int coId, int iDof, FieldDerivationType dt)
        {
            double[] values = GetValues(dt);
            double value = 0;
            switch(ShowType)
            {
                case FieldShowType.Real:
                    value = values[coId * Dof + iDof];
                    break;
                case FieldShowType.Abs:
                    value = Math.Abs(values[coId * Dof + iDof]);
                    break;
                case FieldShowType.ZReal:
                    value = values[coId * Dof];
                    break;
                case FieldShowType.ZImaginary:
                    value = values[coId * Dof + 1];
                    break;
                case FieldShowType.ZAbs:
                    System.Numerics.Complex cValue = new System.Numerics.Complex(
                        values[coId * Dof], Values[coId * Dof + 1]);
                    value = cValue.Magnitude;
                    break;
            }
            return value;
        }

        public void GetMinMaxShowValue(out double min, out double max, int iDof, FieldDerivationType dt)
        {
            min = Double.MaxValue;
            max = Double.MinValue;

            uint ptCnt = GetPointCount();
            for (int coId = 0; coId < ptCnt; coId++)
            {
                double value = GetShowValue(coId, iDof, dt);
                if (value < min)
                {
                    min = value;
                }
                if (value > max)
                {
                    max = value;
                }
            }
        }

    }
}
