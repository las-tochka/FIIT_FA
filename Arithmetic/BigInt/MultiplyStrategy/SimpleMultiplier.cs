using Arithmetic.BigInt.Interfaces;
using System;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null)
            throw new ArgumentNullException();
        
        if (a.IsZero() || b.IsZero())
            return new BetterBigInteger(new uint[] { 0 });

        ReadOnlySpan<uint> digitsA = a.GetDigits();
        ReadOnlySpan<uint> digitsB = b.GetDigits();
        
        if (digitsA.Length < digitsB.Length)
        {
            var temp = digitsA;
            digitsA = digitsB;
            digitsB = temp;
        }
        
        uint[] result = new uint[digitsA.Length + digitsB.Length];
        
        for (int i = 0; i < digitsB.Length; i++)
        {
            if (digitsB[i] == 0)
                continue;
            
            ulong carry = 0;

            for (int j = 0; j < digitsA.Length; j++)
            {
                ulong product = (ulong)digitsA[j] * digitsB[i] + result[i + j] + carry;
                result[i + j] = (uint)product;
                carry = product >> 32;
            }
            
            if (carry != 0)
            {
                result[i + digitsA.Length] += (uint)carry;
            }
        }
        
        int lastNonZero = result.Length - 1;
        while (lastNonZero > 0 && result[lastNonZero] == 0)
        {
            lastNonZero--;
        }
        
        if (lastNonZero + 1 != result.Length)
        {
            Array.Resize(ref result, lastNonZero + 1);
        }
        
        bool isNegative = a.IsNegative ^ b.IsNegative;
        
        return new BetterBigInteger(result, isNegative);
    }
}