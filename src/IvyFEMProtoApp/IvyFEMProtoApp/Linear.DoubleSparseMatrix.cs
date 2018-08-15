using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Linear
{
    class DoubleSparseMatrix
    {
        public int RowLength { get; protected set; } = 0;
        public int ColumnLength { get; protected set; } = 0;
        public Dictionary<int, double>[] IndexsValues { get; protected set; } = null;

        public DoubleSparseMatrix()
        {
            Clear();
        }

        public DoubleSparseMatrix(int rowLength, int columnLength)
        {
            Resize(rowLength, columnLength);
        }

        public DoubleSparseMatrix(DoubleSparseMatrix src)
        {
            Copy(src);
        }

        public void Resize(int rowLength, int columnLength)
        {
            IndexsValues = new Dictionary<int, double>[columnLength];
            for (int col = 0; col < IndexsValues.Length; col++)
            {
                IndexsValues[col] = new Dictionary<int, double>();
            }
            RowLength = rowLength;
            ColumnLength = columnLength;
        }

        public void Clear()
        {
            IndexsValues = null;
            RowLength = 0;
            ColumnLength = 0;
        }

        public double this[int row, int col]
        {
            get
            {
                if (row < 0 || RowLength <= row || col < 0 || ColumnLength <= col)
                {
                    throw new IndexOutOfRangeException();
                }
                if (IndexsValues == null)
                {
                    throw new InvalidOperationException();
                }
                double value = 0;
                var rowValues = IndexsValues[col];
                if (rowValues.ContainsKey(row))
                {
                    value = rowValues[row];
                }
                return value;
            }
            set
            {
                if (row < 0 || RowLength <= row || col < 0 || ColumnLength <= col)
                {
                    throw new IndexOutOfRangeException();
                }
                if (IndexsValues == null)
                {
                    throw new InvalidOperationException();
                }
                var rowValues = IndexsValues[col];
                if (rowValues.ContainsKey(row))
                {
                    if (Math.Abs(value) >= Constants.PrecisionLowerLimit)
                    {
                        rowValues[row] = value;
                    }
                    else
                    {
                        rowValues.Remove(row);
                    }
                }
                else
                {
                    if (Math.Abs(value) >= Constants.PrecisionLowerLimit)
                    {
                        rowValues.Add(row, value);
                    }
                }
            }
        }

        public void Copy(DoubleSparseMatrix src)
        {
            Resize(src.RowLength, src.ColumnLength);
            for (int col = 0; col < src.IndexsValues.Length; col++)
            {
                var srcRowValues = src.IndexsValues[col];
                foreach (KeyValuePair<int, double> srcIndexValue in srcRowValues)
                {
                    int row = srcIndexValue.Key;
                    double value = srcIndexValue.Value;
                    IndexsValues[col].Add(row, value);
                }
            }
        }

        public static double[] operator *(DoubleSparseMatrix A, double[] b)
        {
            double[] c = new double[A.RowLength];
            for (int col  = 0; col < A.ColumnLength; col++)
            {
                var rowValues = A.IndexsValues[col];
                foreach (var indexValue in rowValues)
                {
                    int row = indexValue.Key;
                    double value = indexValue.Value;
                    c[row] += value * b[col];
                }
            }
            return c;
        }
    }
}
