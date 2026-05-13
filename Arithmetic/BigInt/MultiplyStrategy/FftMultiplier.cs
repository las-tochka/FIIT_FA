using System.Numerics;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    private const int BASE = 10000;
    private const int BASE_DIGITS = 4;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsZero() || b.IsZero())
            return new BetterBigInteger(new uint[] { 0 });

        var aDigits = ToBase(a);
        var bDigits = ToBase(b);

        int n = 1;
        while (n < aDigits.Length + bDigits.Length)
            n <<= 1;

        Complex[] fa = new Complex[n];
        Complex[] fb = new Complex[n];

        for (int i = 0; i < aDigits.Length; i++)
            fa[i] = new Complex(aDigits[i], 0);

        for (int i = 0; i < bDigits.Length; i++)
            fb[i] = new Complex(bDigits[i], 0);

        FFT(fa, false);
        FFT(fb, false);

        for (int i = 0; i < n; i++)
            fa[i] *= fb[i];

        FFT(fa, true);

        long carry = 0;
        List<int> resultBase = new();

        for (int i = 0; i < n; i++)
        {
            long value = (long)Math.Round(fa[i].Real) + carry;

            resultBase.Add((int)(value % BASE));
            carry = value / BASE;
        }

        while (carry > 0)
        {
            resultBase.Add((int)(carry % BASE));
            carry /= BASE;
        }

        while (resultBase.Count > 1 && resultBase[^1] == 0)
            resultBase.RemoveAt(resultBase.Count - 1);

        return FromBase(
            resultBase,
            a.IsNegative ^ b.IsNegative
        );
    }

    private static void FFT(Complex[] a, bool invert)
    {
        int n = a.Length;

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;

            while ((j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }

            j ^= bit;

            if (i < j)
                (a[i], a[j]) = (a[j], a[i]);
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = 2 * Math.PI / len * (invert ? -1 : 1);

            Complex wlen = new Complex(
                Math.Cos(angle),
                Math.Sin(angle)
            );

            for (int i = 0; i < n; i += len)
            {
                Complex w = Complex.One;

                for (int j = 0; j < len / 2; j++)
                {
                    Complex u = a[i + j];
                    Complex v = a[i + j + len / 2] * w;

                    a[i + j] = u + v;
                    a[i + j + len / 2] = u - v;

                    w *= wlen;
                }
            }
        }

        if (invert)
        {
            for (int i = 0; i < n; i++)
                a[i] /= n;
        }
    }

    private static int[] ToBase(BetterBigInteger value)
    {
        string s = value.ToString();

        if (s[0] == '-')
            s = s[1..];

        List<int> result = new();

        for (int i = s.Length; i > 0; i -= BASE_DIGITS)
        {
            int start = Math.Max(0, i - BASE_DIGITS);
            int len = i - start;

            result.Add(int.Parse(s.Substring(start, len)));
        }

        return result.ToArray();
    }

    private static BetterBigInteger FromBase(
        List<int> digits,
        bool negative)
    {
        string result = digits[^1].ToString();

        for (int i = digits.Count - 2; i >= 0; i--)
            result += digits[i].ToString($"D{BASE_DIGITS}");

        if (negative && result != "0")
            result = "-" + result;

        return BetterBigInteger.Parse(result, 10);
    }
}