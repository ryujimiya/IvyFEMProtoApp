using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Linear
{
    class Constants
    {
        public const double ConvRatioTolerance = 1.0e-6;
    }

    enum LapackEquationSolverMethod
    {
        Default,
        Dense,
        Band,
        PositiveDefiniteBand
    }

    enum LisEquationSolverMethod
    {
        Default,
        CG,
        BiCG,
        CGS,
        BiCGSTAB,
        BiCGSTABl,
        GPBiCG,
        TFQMR,
        Orthominm,
        GMRESm,
        Jacobi,
        GaussSeidel,
        SOR,
        BiCGSafe,
        CR,
        BiCR,
        CRS,
        BiCRSTAB,
        GPBiCR,
        BiCRSafe,
        FGMRESm,
        IDRs,
        IDR1,
        MINRES,
        COCG,
        COCR
    }

    enum IvyFEMEquationSolverMethod
    {
        Default,
        CG,
        COCG
    }
}
