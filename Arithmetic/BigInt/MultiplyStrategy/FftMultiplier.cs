using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    private const int BITWISE_THRESHOLD = 64; // Порог для использования битового умножения
    
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null)
            throw new ArgumentNullException();
        
        if (a.IsZero() || b.IsZero())
            return new BetterBigInteger(new uint[] { 0 });
        
        var digitsA = a.GetDigits();
        var digitsB = b.GetDigits();
        
        // Для маленьких чисел используем простое умножение
        if (digitsA.Length * digitsB.Length < BITWISE_THRESHOLD)
        {
            return SimpleMultiply(a, b);
        }
        
        uint[] result = BitwiseMultiply(digitsA, digitsB);
        
        // Удаляем ведущие нули
        int lastIndex = result.Length - 1;
        while (lastIndex > 0 && result[lastIndex] == 0)
            lastIndex--;
        
        if (lastIndex + 1 < result.Length)
        {
            var trimmed = new uint[lastIndex + 1];
            for (int i = 0; i <= lastIndex; i++)
                trimmed[i] = result[i];
            result = trimmed;
        }
        
        return new BetterBigInteger(result, a.IsNegative ^ b.IsNegative);
    }
    
    private BetterBigInteger SimpleMultiply(BetterBigInteger a, BetterBigInteger b)
    {
        var simple = new SimpleMultiplier();
        return simple.Multiply(a, b);
    }
    
    private uint[] BitwiseMultiply(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        // Определяем максимальный размер в битах
        int maxBitsA = GetBitLength(a);
        int maxBitsB = GetBitLength(b);
        int resultBits = maxBitsA + maxBitsB;
        int resultWords = (resultBits + 31) / 32;
        
        uint[] result = new uint[resultWords];
        
        // Битовое умножение (алгоритм "умножение через сдвиги и сложения")
        for (int i = 0; i < maxBitsA; i++)
        {
            // Проверяем, установлен ли i-й бит в числе a
            if (GetBit(a, i))
            {
                // Добавляем b << i к результату
                AddShifted(result, b, i);
            }
        }
        
        return result;
    }
    
    private int GetBitLength(ReadOnlySpan<uint> digits)
    {
        if (digits.Length == 0)
            return 0;
        
        int lastWord = digits.Length - 1;
        uint word = digits[lastWord];
        
        int bits = lastWord * 32;
        while (word > 0)
        {
            bits++;
            word >>= 1;
        }
        
        return bits;
    }
    
    private bool GetBit(ReadOnlySpan<uint> digits, int bitIndex)
    {
        int wordIndex = bitIndex / 32;
        int bitOffset = bitIndex % 32;
        
        if (wordIndex >= digits.Length)
            return false;
        
        return ((digits[wordIndex] >> bitOffset) & 1) != 0;
    }
    
    private void AddShifted(uint[] result, ReadOnlySpan<uint> value, int shiftBits)
    {
        int shiftWords = shiftBits / 32;
        int shiftOffset = shiftBits % 32;
        
        ulong carry = 0;
        
        for (int i = 0; i < value.Length; i++)
        {
            int resultPos = i + shiftWords;
            
            // Сдвигаем текущее слово
            ulong shiftedValue = ((ulong)value[i] << shiftOffset) | carry;
            carry = (ulong)value[i] >> (32 - shiftOffset);
            
            // Добавляем к результату
            ulong sum = (ulong)result[resultPos] + (shiftedValue & 0xFFFFFFFF);
            result[resultPos] = (uint)sum;
            
            // Обрабатываем перенос
            if ((sum >> 32) > 0)
            {
                int carryPos = resultPos + 1;
                while (carryPos < result.Length && (sum >> 32) > 0)
                {
                    sum = (ulong)result[carryPos] + (sum >> 32);
                    result[carryPos] = (uint)sum;
                    carryPos++;
                }
            }
        }
        
        // Добавляем последний перенос
        if (carry > 0)
        {
            int lastPos = value.Length + shiftWords;
            if (lastPos < result.Length)
            {
                ulong sum = (ulong)result[lastPos] + carry;
                result[lastPos] = (uint)sum;
                
                int carryPos = lastPos + 1;
                while (carryPos < result.Length && (sum >> 32) > 0)
                {
                    sum = (ulong)result[carryPos] + (sum >> 32);
                    result[carryPos] = (uint)sum;
                    carryPos++;
                }
            }
        }
    }
}