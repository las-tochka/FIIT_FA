using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit; // 0 - положительное, 1 - отрицательное
    
    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;
    
    public bool IsNegative => _signBit == 1;
    private static readonly IMultiplier _simpleMultiplier = new SimpleMultiplier();
    private static readonly IMultiplier _karatsubaMultiplier = new KaratsubaMultiplier();
    private static readonly IMultiplier _fftMultiplier = new FftMultiplier();
    private static int _karatsubaThreshold = 256;
    private static int _fftThreshold = 2048;

    public static int KaratsubaThreshold
    {
        get => _karatsubaThreshold;
        set => _karatsubaThreshold = value > 0 ? value : 256;
    }
    
    public static int FftThreshold
    {
        get => _fftThreshold;
        set => _fftThreshold = value > 0 ? value : 2048;
    }
    

    private static IMultiplier _multiplier = _simpleMultiplier;
    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (digits == null) throw new ArgumentNullException(nameof(digits));

        InitializeFromDigits(digits, isNegative);
    }
    
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
    {
        if (digits == null) throw new ArgumentNullException(nameof(digits));

        var digitArray = digits.ToArray();
        InitializeFromDigits(digitArray, isNegative);
    }
    
    public static BetterBigInteger Parse(string value, int radix)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value));
        
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix));
        
        value = value.Trim();
        bool isNegative = false;
        
        if (value[0] == '-')
        {
            isNegative = true;
            value = value[1..];
        }
        else if (value[0] == '+')
        {
            value = value[1..];
        }
        
        if (string.IsNullOrEmpty(value))
            throw new FormatException("Invalid number format");
        
        BetterBigInteger result = new BetterBigInteger([0]);
        BetterBigInteger radixBigInt = new BetterBigInteger([(uint)radix]);
        
        for (int i = 0; i < value.Length; i++)
        {
            char c = char.ToUpper(value[i]);
            int digit;
            
            if (c >= '0' && c <= '9')
                digit = c - '0';
            else if (c >= 'A' && c <= 'Z')
                digit = c - 'A' + 10;
            else
                throw new FormatException($"Invalid character in number: {c}");
            
            if (digit >= radix)
                throw new FormatException($"Digit {digit} exceeds radix {radix}");
            
            result = result * radixBigInt + new BetterBigInteger([(uint)digit]);
        }
        
        if (isNegative && !result.isZero())
            result = -result;
        
        return result;
    }

    public BetterBigInteger(string value, int radix)
    {
        var parsed = Parse(value, radix);
        _signBit = parsed._signBit;
        _smallValue = parsed._smallValue;
        _data = parsed._data;
    }

    private void InitializeFromDigits(uint[] digits, bool isNegative)
    {
        if (digits == null) throw new ArgumentNullException(nameof(digits));
        
        int lastNonZero = digits.Length - 1;
        while (lastNonZero >= 0 && digits[lastNonZero] == 0)
            lastNonZero--;
        
        if (lastNonZero < 0)
        {
            _smallValue = 0;
            _data = null;
            _signBit = 0;
            return;
        }
        int length = lastNonZero + 1;
        if (length == 1)
        {
            _smallValue = digits[0];
            _data = null;
            _signBit = (isNegative && digits[0] != 0) ? 1 : 0;
            return;
        }
        
        _data = new uint[length];
        Array.Copy(digits, _data, length);
        _signBit = isNegative ? 1 : 0;
        _smallValue = 0;
    }

    private static int NormalizeLength(uint[] digits)
    {
        if (digits.Length == 0) return 0;
        int last = digits.Length - 1;
        while (last > 0 && digits[last] == 0)
            last--;
        return last;
    }
    
    
    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }

    private int GetLength() => _data?.Length ?? 1;

    private bool isZero()
    {
        return _data == null && _smallValue == 0;
    }


// ref - передача по ссылке ПОВТОРИТЬ
    private static uint AddCarry(
        uint a,
        uint b,
        ref uint carry)
    {
        ulong sum = (ulong)a + b + carry;

        carry = (uint)(sum >> 32);

        return (uint)sum;
    }

     private static uint SubBorrow(
        uint a,
        uint b,
        ref uint borrow)
    {
        ulong sub = (ulong)b + borrow;

        if (a >= sub)
        {
            borrow = 0;
            return (uint)(a - sub);
        }

        borrow = 1;
        return (uint)((1UL << 32) + a - sub);
    }

    private static int CompareMagnitude(
        BetterBigInteger a,
        BetterBigInteger b)
    {
        int lenA = a.GetLength();
        int lenB = b.GetLength();

        if (lenA > lenB)
            return 1;

        if (lenA < lenB)
            return -1;

        ReadOnlySpan<uint> ad = a.GetDigits();
        ReadOnlySpan<uint> bd = b.GetDigits();

        for (int i = lenA - 1; i >= 0; i--)
        {
            if (ad[i] > bd[i])
                return 1;

            if (ad[i] < bd[i])
                return -1;
        }

        return 0;
    }

    private int GetHighestUsedBit()
    {
        ReadOnlySpan<uint> digits = GetDigits();

        int highestWord = digits.Length - 1;

        uint word = digits[highestWord];

        int bit = 31;

        while (bit >= 0)
        {
            if (((word >> bit) & 1U) != 0)
                break;

            bit--;
        }

        return highestWord * 32 + bit;
    }

    private bool GetBit(int bitIndex)
    {
        if (bitIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));

        int wordIndex = bitIndex / 32;
        int bitOffset = bitIndex % 32;

        ReadOnlySpan<uint> digits = GetDigits();

        if (wordIndex >= digits.Length)
            return false;

        return ((digits[wordIndex] >> bitOffset) & 1U) != 0;
    }

    private void EnsureCapacity(int wordCount)
    {
        if (_data == null)
        {
            if (wordCount <= 1)
                return;

            uint[] newData = new uint[wordCount];
            newData[0] = _smallValue;

            _data = newData;
            _smallValue = 0;

            return;
        }

        if (_data.Length >= wordCount)
            return;

        Array.Resize(ref _data, wordCount);
    }

    private void SetBit(int bitIndex)
    {
        if (bitIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));

        int wordIndex = bitIndex / 32;
        int bitOffset = bitIndex % 32;

        EnsureCapacity(wordIndex + 1);

        if (_data == null)
        {
            _smallValue |= (1U << bitOffset);
            return;
        }

        _data[wordIndex] |= (1U << bitOffset);
    }

    private static uint[] AddMagnitude(
        ReadOnlySpan<uint> a,
        ReadOnlySpan<uint> b)
    {
        int max =
            Math.Max(a.Length, b.Length);

        uint[] result = new uint[max + 1];

        uint carry = 0;

        for (int i = 0; i < max; i++)
        {
            uint av =
                i < a.Length
                    ? a[i]
                    : 0;
            uint bv =
                i < b.Length
                    ? b[i]
                    : 0;

            result[i] =
                AddCarry(av, bv, ref carry);
        }
        result[max] = carry;

        return result;
    }

    private static uint[] SubtractMagnitude(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        uint[] result = new uint[a.Length];
        uint borrow = 0;
        
        for (int i = 0; i < a.Length; i++)
        {
            uint av = a[i];
            uint bv = i < b.Length ? b[i] : 0;
            
            ulong diff = (ulong)av - bv - borrow;
            result[i] = (uint)diff;
            borrow = (uint)((diff >> 32) & 1);
        }
        
        int last = result.Length - 1;
        while (last > 0 && result[last] == 0)
            last--;
        
        if (last + 1 != result.Length)
            Array.Resize(ref result, last + 1);
        
        return result;
    }
    
    public int CompareTo(IBigInteger? other)
    {
        if (other is null)
            return 1;
        
        if (other is not BetterBigInteger betterOther)
            throw new ArgumentException("Invalid type");
        
        if (IsNegative != betterOther.IsNegative)
            return IsNegative ? -1 : 1;
        
        if (isZero() && betterOther.isZero())
            return 0;
        
        int cmp = CompareMagnitude(this, betterOther);
        
        return IsNegative ? -cmp : cmp;
    }

    public bool Equals(IBigInteger? other)
    {
        if (other is null)
            return false;
        
        if (other is not BetterBigInteger betterOther)
            return false;
        
        if (isZero() && betterOther.isZero())
            return true;
        
        if (IsNegative != betterOther.IsNegative)
            return false;
        
        return CompareMagnitude(this, betterOther) == 0;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as IBigInteger);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + _signBit;
            
            if (_data != null)
            {
                for (int i = 0; i < _data.Length; i++)
                    hash = hash * 31 + _data[i].GetHashCode();
            }
            else
            {
                hash = hash * 31 + _smallValue.GetHashCode();
            }
            
            return hash;
        }
    }
    
    public static BetterBigInteger operator +(
        BetterBigInteger a,
        BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));
        if (a.IsNegative == b.IsNegative)
        {
            uint[] sum =
                AddMagnitude(
                    a.GetDigits(),
                    b.GetDigits());

            return new BetterBigInteger(
                sum,
                a.IsNegative);
        }

        int cmp = CompareMagnitude(a, b);
        if (cmp == 0)
            return new BetterBigInteger([0]);

        if (cmp > 0)
        {
            uint[] diff =
                SubtractMagnitude(
                    a.GetDigits(),
                    b.GetDigits());

            return new BetterBigInteger(
                diff,
                a.IsNegative);
        }
        else
        {
            uint[] diff =
                SubtractMagnitude(
                    b.GetDigits(),
                    a.GetDigits());

            return new BetterBigInteger(
                diff,
                b.IsNegative);
        }
    }

    public static BetterBigInteger operator -(
        BetterBigInteger a,
        BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));
        
        return a + (-b);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        if (a.isZero())
            return a;
        
        return new BetterBigInteger(
            a.GetDigits().ToArray(),
            !a.IsNegative);
    }
    private static uint[] DivideMagnitude(ReadOnlySpan<uint> dividend, ReadOnlySpan<uint> divisor, out uint[] remainder)
    {
        if (divisor.Length == 1)
            return DivideByUint(dividend, divisor[0], out remainder);
        
        int n = divisor.Length;
        int m = dividend.Length - n;
        
        uint[] quotient = new uint[m + 1];
        remainder = dividend.ToArray();
        
        uint d = (uint)((1UL << 32) / (divisor[n - 1] + 1));
        uint[] normalizedDivisor = MultiplyByUint(divisor, d);
        uint[] normalizedDividend = MultiplyByUint(dividend, d);
        
        if (normalizedDividend.Length == dividend.Length)
        {
            uint[] temp = new uint[normalizedDividend.Length + 1];
            Array.Copy(normalizedDividend, temp, normalizedDividend.Length);
            normalizedDividend = temp;
        }
        
        for (int j = m; j >= 0; j--)
        {
            ulong qhat = ((ulong)normalizedDividend[j + n] << 32) + normalizedDividend[j + n - 1];
            qhat /= normalizedDivisor[n - 1];
            
            if (qhat > 0xFFFFFFFF)
                qhat = 0xFFFFFFFF;
        
            ulong rhat = ((ulong)normalizedDividend[j + n] << 32) + normalizedDividend[j + n - 1] - qhat * normalizedDivisor[n - 1];
            
            while (qhat >= 0x100000000 || 
                qhat * normalizedDivisor[n - 2] > ((rhat << 32) + normalizedDividend[j + n - 2]))
            {
                qhat--;
                rhat += normalizedDivisor[n - 1];
                if (rhat >= 0x100000000)
                    break;
            }
            
            ulong borrow = 0;
            for (int i = 0; i < n; i++)
            {
                ulong product = qhat * normalizedDivisor[i];
                ulong diff = (ulong)normalizedDividend[j + i] - (product & 0xFFFFFFFF) - borrow;
                normalizedDividend[j + i] = (uint)diff;
                borrow = (product >> 32) + ((diff >> 32) & 1);
            }
            
            ulong finalDiff = (ulong)normalizedDividend[j + n] - borrow;
            normalizedDividend[j + n] = (uint)finalDiff;
            
            quotient[j] = (uint)qhat;
            
            if (finalDiff >> 32 != 0)
            {
                quotient[j]--;
                borrow = 0;
                for (int i = 0; i < n; i++)
                {
                    ulong sum = (ulong)normalizedDividend[j + i] + normalizedDivisor[i] + borrow;
                    normalizedDividend[j + i] = (uint)sum;
                    borrow = sum >> 32;
                }
                normalizedDividend[j + n] += (uint)borrow;
            }
        }
        
        remainder = DivideByUint(normalizedDividend, d, out _);
        
        int last = quotient.Length - 1;
        while (last > 0 && quotient[last] == 0)
            last--;
        
        if (last + 1 != quotient.Length)
            Array.Resize(ref quotient, last + 1);
        
        return quotient;
    }

    private static uint[] DivideByUint(ReadOnlySpan<uint> dividend, uint divisor, out uint[] remainder)
    {
        uint[] quotient = new uint[dividend.Length];
        ulong carry = 0;
        
        for (int i = dividend.Length - 1; i >= 0; i--)
        {
            ulong current = (carry << 32) + dividend[i];
            quotient[i] = (uint)(current / divisor);
            carry = current % divisor;
        }
        
        remainder = [(uint)carry];
        
        int last = quotient.Length - 1;
        while (last > 0 && quotient[last] == 0)
            last--;
        
        if (last + 1 != quotient.Length)
            Array.Resize(ref quotient, last + 1);
        
        return quotient;
    }

    private static uint[] MultiplyByUint(ReadOnlySpan<uint> digits, uint multiplier)
    {
        uint[] result = new uint[digits.Length + 1];
        ulong carry = 0;
        
        for (int i = 0; i < digits.Length; i++)
        {
            ulong product = (ulong)digits[i] * multiplier + carry;
            result[i] = (uint)product;
            carry = product >> 32;
        }
        
        result[digits.Length] = (uint)carry;
        
        int last = result.Length - 1;
        while (last > 0 && result[last] == 0)
            last--;
        
        if (last + 1 != result.Length)
            Array.Resize(ref result, last + 1);
        
        return result;
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));
        
        if (b.isZero())
            throw new DivideByZeroException();
        
        if (a.isZero())
            return new BetterBigInteger([0]);
        
        int cmp = CompareMagnitude(a, b);
        if (cmp < 0)
            return new BetterBigInteger([0]);
        
        if (cmp == 0)
            return new BetterBigInteger([1], a.IsNegative ^ b.IsNegative);
        
        uint[] quotient = DivideMagnitude(a.GetDigits(), b.GetDigits(), out _);
        
        return new BetterBigInteger(quotient, a.IsNegative ^ b.IsNegative);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));
        
        if (b.isZero())
            throw new DivideByZeroException();
        
        if (a.isZero())
            return new BetterBigInteger([0]);
        
        int cmp = CompareMagnitude(a, b);
        if (cmp < 0)
            return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);
        
        if (cmp == 0)
            return new BetterBigInteger([0]);
        
        DivideMagnitude(a.GetDigits(), b.GetDigits(), out uint[] remainder);
        
        return new BetterBigInteger(remainder, a.IsNegative);
    }

    internal static void SetMultiplicationThresholds(int karatsubaThreshold, int fftThreshold)
    {
        _karatsubaThreshold = karatsubaThreshold;
        _fftThreshold = fftThreshold;
    }
    
    internal static void SetMultiplicationStrategy(IMultiplier strategy)
    {
        _multiplier = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }
    
    public static void SetAutoMultiplicationStrategy()
    {
        _multiplier = null!;
    }
    
    private static IMultiplier SelectMultiplicationStrategy(int sizeA, int sizeB)
    {
        int maxSize = Math.Max(sizeA, sizeB);
        
        if (_multiplier != null)
            return _multiplier;
        
        if (maxSize >= _fftThreshold)
        {
            return _fftMultiplier;
        }
        else if (maxSize >= _karatsubaThreshold)
        {
            return _karatsubaMultiplier;
        }
        else
        {
            return _simpleMultiplier;
        }
    }

    public bool IsZero()
    {
        return isZero();
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null)
            throw new ArgumentNullException();
        
        if (a.IsZero() || b.IsZero())
            return new BetterBigInteger(new uint[] { 0 });
        
        // Автоматический выбор стратегии
        int sizeA = a.GetDigits().Length;
        int sizeB = b.GetDigits().Length;
        IMultiplier strategy = SelectMultiplicationStrategy(sizeA, sizeB);
        
        return strategy.Multiply(a, b);
    }

    private static (uint[] a, uint[] b) NormalizeLength(
        uint[] a,
        uint[] b)
    {
        int max = Math.Max(a.Length, b.Length);

        uint[] na = new uint[max];
        uint[] nb = new uint[max];

        Array.Copy(a, na, a.Length);
        Array.Copy(b, nb, b.Length);

        return (na, nb);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
{
    if (!a.IsNegative)
    {
        // Для положительных: ~x = -(x+1)
        var plusOne = a + new BetterBigInteger(new uint[] { 1 });
        return -plusOne;
    }
    else
    {
        // Для отрицательных: ~(-x) = x-1
        var abs = -a;
        return abs - new BetterBigInteger(new uint[] { 1 });
    }
}

public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
{
    // Создаём константы
    var ONE = new BetterBigInteger(new uint[] { 1 }, false);
    var MINUS_ONE = new BetterBigInteger(new uint[] { 1 }, true);
    
    // Оба положительные - простой случай
    if (!a.IsNegative && !b.IsNegative)
    {
        var digitsA = a.GetDigits().ToArray();
        var digitsB = b.GetDigits().ToArray();
        
        int maxLen = Math.Max(digitsA.Length, digitsB.Length);
        Array.Resize(ref digitsA, maxLen);
        Array.Resize(ref digitsB, maxLen);
        
        uint[] result = new uint[maxLen];
        for (int i = 0; i < maxLen; i++)
            result[i] = digitsA[i] & digitsB[i];
        
        // Удаляем ведущие нули
        int last = result.Length - 1;
        while (last > 0 && result[last] == 0)
            last--;
        
        if (last + 1 != result.Length)
            Array.Resize(ref result, last + 1);
        
        return new BetterBigInteger(result, false);
    }
    
    // Для всех случаев с отрицательными числами используем единый подход через дополнительный код
    // Определяем максимальную разрядность (в битах) для обоих чисел
    int maxBits = Math.Max(
        GetBitLength(a),
        GetBitLength(b)
    );
    // Добавляем знаковый бит
    maxBits += 1;
    // Округляем до ближайшего кратного 32 для удобства
    maxBits = ((maxBits + 31) / 32) * 32;
    
    // Преобразуем в дополнительный код
    var twosCompA = ToTwosComplement(a, maxBits);
    var twosCompB = ToTwosComplement(b, maxBits);
    
    // Выполняем AND
    var resultTwosComp = BitwiseAnd(twosCompA, twosCompB, maxBits);
    
    // Преобразуем обратно из дополнительного кода
    return FromTwosComplement(resultTwosComp, maxBits);
}

// Вспомогательный метод для получения битовой длины
private static int GetBitLength(BetterBigInteger value)
{
    if (value.IsNegative)
        value = -value;
    
    return value.GetHighestUsedBit() + 1;
}

// Преобразование в дополнительный код с фиксированной разрядностью
private static BetterBigInteger ToTwosComplement(BetterBigInteger value, int bits)
{
    if (!value.IsNegative)
    {
        // Положительное число - просто расширяем до нужной разрядности
        return ExtendToBits(value, bits, false);
    }
    
    // Отрицательное: 2^bits - |value|
    var absValue = -value;
    var twoPowBits = PowerOfTwo(bits);
    return twoPowBits - absValue;
}

// Преобразование из дополнительного кода
private static BetterBigInteger FromTwosComplement(BetterBigInteger value, int bits)
{
    var maxPositive = PowerOfTwo(bits - 1);
    
    if (value < maxPositive)
        return value;  // Положительное число
    
    // Отрицательное: value - 2^bits
    var twoPowBits = PowerOfTwo(bits);
    return value - twoPowBits;
}

// Расширение числа до определённого количества бит
private static BetterBigInteger ExtendToBits(BetterBigInteger value, int bits, bool isNegative)
{
    int words = (bits + 31) / 32;
    var digits = value.GetDigits().ToArray();
    
    if (digits.Length >= words)
        return value;
    
    Array.Resize(ref digits, words);
    
    // Если число отрицательное, заполняем старшие разряды единицами
    if (isNegative)
    {
        for (int i = digits.Length; i < words; i++)
            digits[i] = uint.MaxValue;
    }
    
    return new BetterBigInteger(digits, false);
}

// Создание числа 2^n
private static BetterBigInteger PowerOfTwo(int bits)
{
    int wordIndex = bits / 32;
    int bitOffset = bits % 32;
    uint[] digits = new uint[wordIndex + 1];
    digits[wordIndex] = 1U << bitOffset;
    return new BetterBigInteger(digits, false);
}

// Побитовое AND над числами в дополнительном коде
private static BetterBigInteger BitwiseAnd(BetterBigInteger a, BetterBigInteger b, int bits)
{
    int words = (bits + 31) / 32;
    var digitsA = a.GetDigits().ToArray();
    var digitsB = b.GetDigits().ToArray();
    
    // Расширяем до нужной длины
    Array.Resize(ref digitsA, words);
    Array.Resize(ref digitsB, words);
    
    uint[] result = new uint[words];
    for (int i = 0; i < words; i++)
        result[i] = digitsA[i] & digitsB[i];
    
    return new BetterBigInteger(result, false);
}

public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
{
    return ~(~a & ~b);
}

public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
{
    var absA = a.IsNegative ? ~a : a;
    var absB = b.IsNegative ? ~b : b;
    
    var listA = new List<uint>(absA.GetDigits().ToArray());
    var listB = new List<uint>(absB.GetDigits().ToArray());
    
    while (listA.Count < listB.Count) listA.Add(0);
    while (listB.Count < listA.Count) listB.Add(0);
    
    uint[] result = new uint[listA.Count];
    for (int i = 0; i < listA.Count; i++)
        result[i] = listA[i] ^ listB[i];
    
    int last = result.Length - 1;
    while (last > 0 && result[last] == 0)
        last--;
    
    if (last + 1 != result.Length)
        Array.Resize(ref result, last + 1);
    
    if (a.IsNegative != b.IsNegative)
    {
        var temp = new BetterBigInteger(result, false);
        return ~temp;
    }
    
    return new BetterBigInteger(result, false);
}
    
    public static BetterBigInteger operator <<(
        BetterBigInteger value,
        int shift)
    {
        if (shift < 0)
            throw new ArgumentOutOfRangeException(nameof(shift));

        if (shift == 0 || value.isZero())
            return value;

        ReadOnlySpan<uint> digits = value.GetDigits();

        int wordShift = shift / 32;
        int bitShift = shift % 32;
        uint[] result =
            new uint[digits.Length + wordShift + 1];

        ulong carry = 0;

        for (int i = 0; i < digits.Length; i++)
        {
            ulong current =
                ((ulong)digits[i] << bitShift) | carry;

            result[i + wordShift] = (uint)current;
            carry = current >> 32;
        }

        if (carry != 0)
            result[digits.Length + wordShift] = (uint)carry;

        return new BetterBigInteger(
            result,
            value.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger value, int shift)
    {
        if (shift < 0)
            throw new ArgumentOutOfRangeException(nameof(shift));
        
        if (shift == 0 || value.isZero())
            return value;
        
        // Используем деление с правильным округлением
        var divisor = new BetterBigInteger([1]);
        for (int i = 0; i < shift; i++)
            divisor = divisor << 1;
        
        if (!value.IsNegative)
            return value / divisor;
        
        // Для отрицательных чисел: деление с округлением вниз (к -∞)
        // В C# сдвиг для отрицательных округляет вниз
        var quotient = value / divisor;
        
        // Проверяем остаток
        var remainder = value % divisor;
        if (!remainder.isZero())
        {
            // Нужно округлить вниз (ещё на 1 в минус)
            quotient = quotient - new BetterBigInteger([1]);
        }
        
        return quotient;
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    public override string ToString()
    {
        if (isZero())
            return "0";
        
        return ToString(10);
    }

    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix));
        
        if (isZero())
            return "0";
        
        BetterBigInteger temp = this;
        bool negative = IsNegative;
        
        if (negative)
            temp = -temp;
        
        const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var result = new List<char>();
        
        var radixBigInt = new BetterBigInteger([(uint)radix]);
        
        while (!temp.isZero())
        {
            var remainder = temp % radixBigInt;
            uint digit = remainder.GetDigits()[0];
            result.Add(digits[(int)digit]);
            temp = temp / radixBigInt;
        }
        
        if (negative)
            result.Add('-');
        
        result.Reverse();
        return new string(result.ToArray());
    }
}