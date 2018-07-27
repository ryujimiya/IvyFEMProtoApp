using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM.Lapack
{
    struct Complex
    {
        private double r;
        private double i;

        public static Complex Zero => new Complex(0, 0);
        public static Complex ImaginaryOne => new Complex(0, 1);

        public Complex(double r, double i)
        {
            this.r = r;
            this.i = i;
        }

        public double Real
        {
            get { return r; }
            set { r = value; }
        }
        public double Imaginary
        {
            get { return i; }
            set { i = value; }
        }

        public double Magnitude
        {
            get { return Math.Sqrt(r * r + i * i); }
        }

        public double Phase
        {
            get { return Math.Atan2(r, i); }
        }

        public static System.Numerics.Complex ToDotNetComplex(Complex value)
        {
            return new System.Numerics.Complex(value.r, value.i);
        }

        public static explicit operator Complex(double value)
        {
            return new Complex(value, 0);
        }

        public static Complex operator +(Complex lhs, Complex rhs)
        {
            return new Complex(lhs.r + rhs.r, lhs.i + rhs.i);
        }

        public static Complex operator -(Complex lhs, Complex rhs)
        {
            return new Complex(lhs.r - rhs.r, lhs.i - rhs.i);
        }

        public static Complex operator *(Complex lhs, Complex rhs)
        {
            return new Complex(lhs.r * rhs.r - lhs.i * rhs.i, lhs.r * rhs.i + lhs.i * rhs.r);
        }

        public static Complex operator/(Complex lhs, Complex rhs)
        {
            double rhsSquare = rhs.r * rhs.r + rhs.i * rhs.i;
            return new Complex((lhs.r * rhs.r + lhs.i * rhs.i) / rhsSquare, (-lhs.r * rhs.i + lhs.i * rhs.r) / rhsSquare);
        }

        public static Complex Sqrt(Complex value)
        {
            var ret = System.Numerics.Complex.Sqrt(ToDotNetComplex(value));
            return new Complex(ret.Real, ret.Imaginary);
        }

        public string Dump()
        {
            string ret;
            string CRLF = System.Environment.NewLine;

            ret = Real + " + j " + Imaginary  + CRLF;
            return ret;
        }
    }
}
