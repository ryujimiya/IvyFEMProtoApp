using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Lapack
{
    class ComplexMatrix
    {
        public System.Numerics.Complex[] Buffer { get; protected set; } = null;
        public int RowSize { get; protected set; } = 0;
        public int ColumnSize = 0;

        public ComplexMatrix()
        {
            Clear();
        }

        public ComplexMatrix(int rowSize, int columnSize)
        {
            Resize(rowSize, columnSize);
        }

        public ComplexMatrix(System.Numerics.Complex[] buffer, int rowSize, int columnSize, bool alloc = true)
        {
            if (alloc)
            {
                Copy(buffer, rowSize, columnSize);
            }
            else
            {
                System.Diagnostics.Debug.Assert(buffer.Length == rowSize * columnSize);
                Buffer = buffer;
                RowSize = rowSize;
                ColumnSize = columnSize;
            }
        }

        public ComplexMatrix(ComplexMatrix src)
        {
            Copy(src);
        }

        public static explicit operator ComplexMatrix(DoubleMatrix doubleM)
        {
            ComplexMatrix m = new ComplexMatrix(doubleM.RowSize, doubleM.ColumnSize);
            for (int i = 0; i < m.Buffer.Length; i++)
            {
                m.Buffer[i] = (System.Numerics.Complex)doubleM.Buffer[i];
            }
            return m;
        }

        public void Resize(int rowSize, int columnSize)
        {
            Buffer = new System.Numerics.Complex[rowSize * columnSize];
            RowSize = rowSize;
            ColumnSize = columnSize;
        }

        public void Clear()
        {
            Buffer = null;
            RowSize = 0;
            ColumnSize = 0;
        }

        public string Dump()
        {
            string ret = "";
            string CRLF = System.Environment.NewLine;

            ret += "ComplexMatrix" + CRLF;
            ret += "ColumnSize = " + ColumnSize + CRLF;
            ret += "RowSize = " + RowSize + CRLF;
            for (int col = 0; col < ColumnSize; col++)
            {
                for (int row = 0; row < RowSize; row++)
                {
                    ret += "[" + row + ", " + col + "] = " + this[row, col].ToString() + CRLF;
                }
            }
            return ret;
        }

        public int BufferIndex(int row, int col)
        {
            System.Diagnostics.Debug.Assert(row >= 0 && row < RowSize && col >= 0 && col < ColumnSize);
            return (row + col * RowSize);
        }

        public System.Numerics.Complex this[int row, int col]
        {
            get
            {
                if (row < 0 || RowSize <= row || col < 0 || ColumnSize <= col)
                {
                    throw new IndexOutOfRangeException();
                }
                return Buffer[row + col * RowSize];
            }
            set
            {
                if (row < 0 || RowSize <= row || col < 0 || ColumnSize <= col)
                {
                    throw new IndexOutOfRangeException();
                }
                Buffer[row + col * RowSize] = value;
            }
        }

        public void Copy(ComplexMatrix src)
        {
            Copy(src.Buffer, src.RowSize, src.ColumnSize);
        }

        public void Copy(System.Numerics.Complex[] buffer, int rowSize, int columnSize)
        {
            System.Diagnostics.Debug.Assert(buffer.Length == rowSize * columnSize);
            if (buffer.Length != rowSize * columnSize)
            {
                return;
            }

            // バッファ確保
            if (RowSize == rowSize && ColumnSize == columnSize)
            {
                // 何もしない
            }
            else if (Buffer != null && Buffer.Length == rowSize * columnSize)
            {
                RowSize = rowSize;
                ColumnSize = columnSize;
            }
            else
            {
                Resize(rowSize, columnSize);
            }

            // コピー
            buffer.CopyTo(Buffer, 0);
        }

        public void Zero()
        {
            int size = Buffer.Length;
            for (int i = 0; i < size; ++i)
            {
                Buffer[i] = (System.Numerics.Complex)0;
            }
        }

        public void Identity()
        {
            Zero();
            for (int i = 0; i < RowSize; ++i)
            {
                this[i, i] = (System.Numerics.Complex)1;
            }
        }

        public void Transpose()
        {
            ComplexMatrix t = new ComplexMatrix(ColumnSize, RowSize);

            for (int row = 0; row < RowSize; row++)
            {
                for (int col = 0; col < ColumnSize; col++)
                {
                    t[col, row] = this[row, col];
                }
            }

            Clear();
            Buffer = t.Buffer;
            RowSize = t.RowSize;
            ColumnSize = t.ColumnSize;
        }

        public static ComplexMatrix Conjugate(ComplexMatrix A)
        {
            ComplexMatrix X = new ComplexMatrix(A.Buffer, A.RowSize, A.ColumnSize);
            IvyFEM.Lapack.Functions.zlacgv(X.Buffer);
            return X;
        }

        public void Conjugate()
        {
            ComplexMatrix ret = Conjugate(this);
            Buffer = ret.Buffer;
            RowSize = ret.RowSize;
            ColumnSize = ret.ColumnSize;
        }

        public static ComplexMatrix Inverse(ComplexMatrix A)
        {
            System.Diagnostics.Debug.Assert(A.RowSize == A.ColumnSize);
            int n = A.RowSize;
            ComplexMatrix workA = new ComplexMatrix(A);
            ComplexMatrix workB = new ComplexMatrix(n, n);
            workB.Identity(); // 単位行列
            System.Numerics.Complex[] a = workA.Buffer;
            System.Numerics.Complex[] b = workB.Buffer;
            // [A][X] = [B]
            //  [B]の内容が書き換えられるので、matXを新たに生成せず、Bを出力に指定している
            int xRow = 0;
            int xCol = 0;
            Lapack.Functions.zgesv(out b, out xRow, out xCol, a, n, n, b, n, n);

            bool alloc = false; // 指定したバッファを使用する
            ComplexMatrix X = new ComplexMatrix(b, xRow, xCol, alloc);
            return X;
        }

        public void Inverse()
        {
            ComplexMatrix ret = Inverse(this);
            Buffer = ret.Buffer;
            RowSize = ret.RowSize;
            ColumnSize = ret.ColumnSize;
        }

        public static ComplexMatrix operator *(ComplexMatrix A, ComplexMatrix B)
        {
            System.Numerics.Complex[] c;
            int cRow;
            int cCol;
            Lapack.Functions.zgemmAB(out c, out cRow, out cCol,
                A.Buffer, A.RowSize, A.ColumnSize,
                B.Buffer, B.RowSize, B.ColumnSize);

            bool alloc = false;
            ComplexMatrix C = new ComplexMatrix(c, cRow, cCol, alloc);
            return C;
        }
    }
}
