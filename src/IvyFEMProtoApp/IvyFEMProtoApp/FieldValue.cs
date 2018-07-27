using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class FieldValue : IObject
    {
        public FieldType Type { get; set; } = FieldType.NO_VALUE;
        public FieldDerivationType DerivationType { get; set; } = 0;
        public FieldShowType ShowType { get; set; } = FieldShowType.SCALAR;
        public uint Dof { get; set; } = 1;
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
            ShowType = srcFV.ShowType;
            Values = srcFV.Values; // shallow copy
        }

        public uint GetCoordCount()
        {
            if (Values == null)
            {
                return 0;
            }
            return (uint)(Values.Length / Dof);
        }

        private double[] GetValues(FieldDerivationType dt)
        {
            double[] values = null;
            if (dt.HasFlag(FieldDerivationType.VALUE) && Values != null)
            {
                values = Values;
            }
            else if (dt.HasFlag(FieldDerivationType.VELOCITY) && VelocityValues != null)
            {
                values = VelocityValues;
            }
            else if (dt.HasFlag(FieldDerivationType.ACCELERATION))
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
                case FieldShowType.SCALAR:
                    value = values[coId * Dof + iDof];
                    break;
                case FieldShowType.ABS:
                    value = Math.Abs(values[coId * Dof + iDof]);
                    break;
                case FieldShowType.ZREAL:
                    value = values[coId * Dof];
                    break;
                case FieldShowType.ZIMAGINARIY:
                    value = values[coId * Dof + 1];
                    break;
                case FieldShowType.ZABS:
                    IvyFEM.Lapack.Complex cValue = new IvyFEM.Lapack.Complex(
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

            uint ptCnt = GetCoordCount();
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
