using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Lis
{
    class Constants
    {
        public const int LIS_COMM_WORLD = 0x1;
        public const int LIS_MATRIX_OPTION_LEN = 10;

        public const int LIS_OPTIONS_LEN = 27;
        public const int LIS_PARAMS_LEN = 15;

        public const int LIS_MATRIX_DECIDING_SIZE = -((int)MatrixType.LIS_MATRIX_RCO + 1);
        public const int LIS_MATRIX_NULL = -((int)MatrixType.LIS_MATRIX_RCO + 2);
        public const MatrixType LIS_MATRIX_DEFAULT = MatrixType.LIS_MATRIX_CSR;
        public const MatrixType LIS_MATRIX_POINT = MatrixType.LIS_MATRIX_CSR;
        public const MatrixType LIS_MATRIX_BLOCK = MatrixType.LIS_MATRIX_BSR;
    }

    enum SetValueFlag
    {
        LIS_INS_VALUE = 0,
        LIS_ADD_VALUE = 1,
        LIS_SUB_VALUE = 2
    }

    enum MatrixType
    {
        LIS_MATRIX_ASSEMBLING = 0,
        LIS_MATRIX_CSR = 1,
        LIS_MATRIX_CSC = 2,
        LIS_MATRIX_MSR = 3,
        LIS_MATRIX_DIA = 4,
        LIS_MATRIX_CDS = 4,
        LIS_MATRIX_ELL = 5,
        LIS_MATRIX_JAD = 6,
        LIS_MATRIX_BSR = 7,
        LIS_MATRIX_BSC = 8,
        LIS_MATRIX_VBR = 9,
        LIS_MATRIX_COO = 10,
        LIS_MATRIX_DENSE = 11,
        LIS_MATRIX_DNS = 11,
        LIS_MATRIX_RCO = 255,
        LIS_MATRIX_TJAD = 12,
        LIS_MATRIX_BJAD = 13,
        LIS_MATRIX_BCR = 14,
        LIS_MATRIX_CJAD = 15,
        LIS_MATRIX_PCSR = 16,
        LIS_MATRIX_LCSR = 17,
        LIS_MATRIX_LJAD = 18,
        LIS_MATRIX_LBSR = 19,
        LIS_MATRIX_CDIA = 20,
        LIS_MATRIX_MSC = 21
    }
}
