using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Lapack
{
    /// <summary>
    /// 行列クラス(double)
    ///    1次元配列として行列データを保持します。
    ///    1次元配列は、clapackの配列数値格納順序と同じ（行データを先に格納する: Column Major Order)
    ///    既存のdouble[,]からの置き換えポイント
    ///       double[,] --> DoubleMatrix
    ///       GetLength(0) --> RowSize
    ///       GetLength(1) --> ColumnSize
    /// </summary>
    public class DoubleMatrix
    {
        public double[] Buffer { get; protected set; } = null;
        public int RowSize { get; protected set; } = 0;
        public int ColumnSize = 0;

        public DoubleMatrix()
        {
            Clear();
        }

        public DoubleMatrix(int rowSize, int columnSize)
        {
            Resize(rowSize, columnSize);
        }

        public DoubleMatrix(double[] buffer, int rowSize, int columnSize, bool alloc = true)
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

        public DoubleMatrix(DoubleMatrix src)
        {
            Copy(src);
        }

        public void Resize(int rowSize, int columnSize)
        {
            Buffer = new double[rowSize * columnSize];
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

            ret += "DoubleMatrix" + CRLF;
            ret += "ColumnSize = " + ColumnSize + CRLF;
            ret += "RowSize = " + RowSize + CRLF;
            for (int col = 0; col < ColumnSize; col++)
            {
                for (int row = 0; row < RowSize; row++)
                {
                    ret += "[" + row + ", " + col + "] = " + this[row, col] + CRLF; 
                }
            }
            return ret;
        }

        public int BufferIndex(int row, int col)
        {
            System.Diagnostics.Debug.Assert(row >= 0 && row < RowSize && col >= 0 && col < ColumnSize);
            return (row + col * RowSize);
        }

        public double this[int row, int col]
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

        public void Copy(DoubleMatrix src)
        {
            Copy(src.Buffer, src.RowSize, src.ColumnSize);
        }

        public void Copy(double[] buffer, int rowSize, int columnSize)
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
                Buffer[i] = 0.0;
            }
        }

        public void Identity()
        {
            Zero();
            for (int i = 0; i < RowSize; ++i)
            {
                this[i, i] = 1;
            }
        }

        public void Transpose()
        {
            DoubleMatrix t = new DoubleMatrix(ColumnSize, RowSize);

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

        public static DoubleMatrix Inverse(DoubleMatrix A)
        {
            System.Diagnostics.Debug.Assert(A.RowSize == A.ColumnSize);
            int n = A.RowSize;
            DoubleMatrix workA = new DoubleMatrix(A);
            DoubleMatrix workB = new DoubleMatrix(n, n);
            workB.Identity(); // 単位行列
            double[] a = workA.Buffer;
            double[] b = workB.Buffer;
            // [A][X] = [B]
            //  [B]の内容が書き換えられるので、matXを新たに生成せず、Bを出力に指定している
            int xRow = 0;
            int xCol = 0;
            IvyFEM.Lapack.Functions.dgesv(out b, out xRow, out xCol, a, n, n, b, n, n);

            bool alloc = false; // 指定したバッファを使用する
            DoubleMatrix X = new DoubleMatrix(b, xRow, xCol, alloc);
            return X;
        }

        public void Inverse()
        {
            DoubleMatrix ret = Inverse(this);
            Buffer = ret.Buffer;
            RowSize = ret.RowSize;
            ColumnSize = ret.ColumnSize;
        }

        public static DoubleMatrix operator *(DoubleMatrix A, DoubleMatrix B)
        {
            double[] c;
            int cRow;
            int cCol;
            IvyFEM.Lapack.Functions.dgemmAB(out c, out cRow, out cCol,
                A.Buffer, A.RowSize, A.ColumnSize,
                B.Buffer, B.RowSize, B.ColumnSize);

            bool alloc = false;
            DoubleMatrix C = new DoubleMatrix(c, cRow, cCol, alloc);
            return C;
        }
    }
}
