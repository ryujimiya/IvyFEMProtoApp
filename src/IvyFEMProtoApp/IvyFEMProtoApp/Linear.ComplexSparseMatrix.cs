using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Linear
{
    class ComplexSparseMatrix
    {
        public int RowLength { get; protected set; } = 0;
        public int ColumnLength { get; protected set; } = 0;
        public Dictionary<int, System.Numerics.Complex>[] IndexsValues { get; protected set; } = null;

        public ComplexSparseMatrix()
        {
            Clear();
        }

        public ComplexSparseMatrix(int rowLength, int columnLength)
        {
            Resize(rowLength, columnLength);
        }

        public ComplexSparseMatrix(ComplexSparseMatrix src)
        {
            Copy(src);
        }

        public void Resize(int rowLength, int columnLength)
        {
            IndexsValues = new Dictionary<int, System.Numerics.Complex>[columnLength];
            for (int col = 0; col < IndexsValues.Length; col++)
            {
                IndexsValues[col] = new Dictionary<int, System.Numerics.Complex>();
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

        public System.Numerics.Complex this[int row, int col]
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
                System.Numerics.Complex value = 0;
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
                    if (value.Magnitude >= Constants.PrecisionLowerLimit)
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
                    if (value.Magnitude >= Constants.PrecisionLowerLimit)
                    {
                        rowValues.Add(row, value);
                    }
                }
            }
        }

        public void Copy(ComplexSparseMatrix src)
        {
            Resize(src.RowLength, src.ColumnLength);
            for (int col = 0; col < src.IndexsValues.Length; col++)
            {
                var srcRowValues = src.IndexsValues[col];
                foreach (KeyValuePair<int, System.Numerics.Complex> srcIndexValue in srcRowValues)
                {
                    int row = srcIndexValue.Key;
                    System.Numerics.Complex value = srcIndexValue.Value;
                    IndexsValues[col].Add(row, value);
                }
            }
        }

        public static System.Numerics.Complex[] operator *(ComplexSparseMatrix A, System.Numerics.Complex[] b)
        {
            System.Numerics.Complex[] c = new System.Numerics.Complex[A.RowLength];
            for (int col = 0; col < A.ColumnLength; col++)
            {
                var rowValues = A.IndexsValues[col];
                foreach (var indexValue in rowValues)
                {
                    int row = indexValue.Key;
                    System.Numerics.Complex value = indexValue.Value;
                    c[row] += value * b[col];
                }
            }
            return c;
        }
    }
}
