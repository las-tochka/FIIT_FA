using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private const int BASE_CASE_THRESHOLD = 32;
    
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null)
            throw new System.ArgumentNullException();
        
        if (a.IsZero() || b.IsZero())
            return new BetterBigInteger(new uint[] { 0 });
        
        System.ReadOnlySpan<uint> digitsA = a.GetDigits();
        System.ReadOnlySpan<uint> digitsB = b.GetDigits();
        
        uint[] result = KaratsubaMultiply(digitsA, digitsB);
        
        int lastIndex = result.Length - 1;
        while (lastIndex > 0 && result[lastIndex] == 0)
            lastIndex--;
        
        if (lastIndex + 1 < result.Length)
            System.Array.Resize(ref result, lastIndex + 1);
        
        return new BetterBigInteger(result, a.IsNegative ^ b.IsNegative);
    }
    
    private uint[] KaratsubaMultiply(System.ReadOnlySpan<uint> a, System.ReadOnlySpan<uint> b)
    {
        if (a.Length <= BASE_CASE_THRESHOLD || b.Length <= BASE_CASE_THRESHOLD)
        {
            return SimpleMultiply(a, b);
        }
        
        int n = a.Length > b.Length ? a.Length : b.Length;
        int half = n >> 1; // Деление на 2 через битовый сдвиг
        
        System.ReadOnlySpan<uint> aLow = GetLowPart(a, half);
        System.ReadOnlySpan<uint> aHigh = GetHighPart(a, half);
        System.ReadOnlySpan<uint> bLow = GetLowPart(b, half);
        System.ReadOnlySpan<uint> bHigh = GetHighPart(b, half);
        
        uint[] z0 = KaratsubaMultiply(aLow, bLow);
        uint[] z2 = KaratsubaMultiply(aHigh, bHigh);
        
        uint[] sumA = AddMagnitude(aLow, aHigh);
        uint[] sumB = AddMagnitude(bLow, bHigh);
        uint[] z1 = KaratsubaMultiply(sumA, sumB);
        
        z1 = SubtractMagnitude(z1, z0);
        z1 = SubtractMagnitude(z1, z2);
        
        return CombineResults(z2, z1, z0, half);
    }
    
    private uint[] SimpleMultiply(System.ReadOnlySpan<uint> a, System.ReadOnlySpan<uint> b)
    {
        if (a.Length < b.Length)
        {
            System.ReadOnlySpan<uint> temp = a;
            a = b;
            b = temp;
        }
        
        uint[] result = new uint[a.Length + b.Length];
        
        for (int i = 0; i < b.Length; i++)
        {
            if (b[i] == 0)
                continue;
            
            ulong carry = 0;
            
            for (int j = 0; j < a.Length; j++)
            {
                ulong product = (ulong)a[j] * b[i] + result[i + j] + carry;
                result[i + j] = (uint)product;
                carry = product >> 32;
            }
            
            if (carry != 0)
            {
                result[i + a.Length] += (uint)carry;
            }
        }
        
        int lastIndex = result.Length - 1;
        while (lastIndex > 0 && result[lastIndex] == 0)
            lastIndex--;
        
        if (lastIndex + 1 < result.Length)
            System.Array.Resize(ref result, lastIndex + 1);
        
        return result;
    }
    
    private System.ReadOnlySpan<uint> GetLowPart(System.ReadOnlySpan<uint> digits, int half)
    {
        int lowLen = half < digits.Length ? half : digits.Length;
        return digits.Slice(0, lowLen);
    }
    
    private System.ReadOnlySpan<uint> GetHighPart(System.ReadOnlySpan<uint> digits, int half)
    {
        if (digits.Length <= half)
            return System.ReadOnlySpan<uint>.Empty;
        
        return digits.Slice(half, digits.Length - half);
    }
    
    private uint[] AddMagnitude(System.ReadOnlySpan<uint> a, System.ReadOnlySpan<uint> b)
    {
        int maxLen = a.Length > b.Length ? a.Length : b.Length;
        uint[] result = new uint[maxLen + 1];
        ulong carry = 0;
        
        for (int i = 0; i < maxLen; i++)
        {
            ulong av = i < a.Length ? a[i] : 0;
            ulong bv = i < b.Length ? b[i] : 0;
            ulong sum = av + bv + carry;
            result[i] = (uint)sum;
            carry = sum >> 32;
        }
        
        if (carry > 0)
            result[maxLen] = (uint)carry;
        
        int last = result.Length - 1;
        while (last > 0 && result[last] == 0)
            last--;
        
        if (last + 1 != result.Length)
            System.Array.Resize(ref result, last + 1);
        
        return result;
    }
    
    private uint[] SubtractMagnitude(uint[] a, uint[] b)
    {
        uint[] result = new uint[a.Length];
        ulong borrow = 0;
        
        for (int i = 0; i < a.Length; i++)
        {
            ulong av = a[i];
            ulong bv = i < b.Length ? b[i] : 0;
            ulong diff = av - bv - borrow;
            result[i] = (uint)diff;
            borrow = (diff >> 32) & 1;
        }
        
        int last = result.Length - 1;
        while (last > 0 && result[last] == 0)
            last--;
        
        if (last + 1 != result.Length)
            System.Array.Resize(ref result, last + 1);
        
        return result;
    }
    
    private uint[] CombineResults(uint[] high, uint[] mid, uint[] low, int shift)
    {
        int shiftWords = shift;
        int doubleShiftWords = shift << 1; // Умножение на 2 через битовый сдвиг
        
        int highLen = high.Length + doubleShiftWords;
        int midLen = mid.Length + shiftWords;
        int lowLen = low.Length;
        
        int resultLen = highLen;
        if (midLen > resultLen) resultLen = midLen;
        if (lowLen > resultLen) resultLen = lowLen;
        
        uint[] result = new uint[resultLen];
        
        // Добавляем low часть
        for (int i = 0; i < low.Length; i++)
            result[i] = low[i];
        
        // Добавляем mid часть со сдвигом
        for (int i = 0; i < mid.Length; i++)
        {
            int pos = i + shiftWords;
            ulong sum = (ulong)result[pos] + mid[i];
            result[pos] = (uint)sum;
            
            if ((sum >> 32) > 0)
            {
                int carryPos = pos + 1;
                while (carryPos < result.Length)
                {
                    sum = (ulong)result[carryPos] + 1;
                    result[carryPos] = (uint)sum;
                    if ((sum >> 32) == 0) break;
                    carryPos++;
                }
            }
        }
        
        // Добавляем high часть со сдвигом
        for (int i = 0; i < high.Length; i++)
        {
            int pos = i + doubleShiftWords;
            if (pos >= result.Length)
            {
                System.Array.Resize(ref result, pos + 1);
            }
            
            ulong sum = (ulong)result[pos] + high[i];
            result[pos] = (uint)sum;
            
            if ((sum >> 32) > 0)
            {
                int carryPos = pos + 1;
                while (carryPos < result.Length)
                {
                    sum = (ulong)result[carryPos] + 1;
                    result[carryPos] = (uint)sum;
                    if ((sum >> 32) == 0) break;
                    carryPos++;
                }
                if (carryPos >= result.Length && (sum >> 32) > 0)
                {
                    System.Array.Resize(ref result, result.Length + 1);
                    result[result.Length - 1] = 1;
                }
            }
        }
        
        return result;
    }
}