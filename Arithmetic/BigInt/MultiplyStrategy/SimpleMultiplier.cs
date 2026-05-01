using Arithmetic.BigInt.Interfaces;
using System;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null)
            throw new ArgumentNullException();
        
        // Используем существующий метод isZero() (не IsZero)
        // Нужно сделать его internal или public в BetterBigInteger
        if (a.IsZero() || b.IsZero())
            return new BetterBigInteger(new uint[] { 0 });
        
        // Получаем цифры множителей
        ReadOnlySpan<uint> digitsA = a.GetDigits();
        ReadOnlySpan<uint> digitsB = b.GetDigits();
        
        // Для оптимизации: умножаем меньшее число на большее
        // Нельзя использовать кортежи с ReadOnlySpan, делаем вручную
        if (digitsA.Length < digitsB.Length)
        {
            // Меняем местами через временные переменные
            var temp = digitsA;
            digitsA = digitsB;
            digitsB = temp;
        }
        
        // Результат умножения
        uint[] result = new uint[digitsA.Length + digitsB.Length];
        
        // Основной цикл умножения "в столбик"
        for (int i = 0; i < digitsB.Length; i++)
        {
            // Если текущая цифра множителя равна 0, пропускаем итерацию
            if (digitsB[i] == 0)
                continue;
            
            ulong carry = 0;
            
            // Умножаем все цифры первого числа на текущую цифру второго
            for (int j = 0; j < digitsA.Length; j++)
            {
                ulong product = (ulong)digitsA[j] * digitsB[i] + result[i + j] + carry;
                result[i + j] = (uint)product;
                carry = product >> 32;
            }
            
            // Записываем остаток переноса
            if (carry != 0)
            {
                result[i + digitsA.Length] += (uint)carry;
            }
        }
        
        // Удаляем ведущие нули
        int lastNonZero = result.Length - 1;
        while (lastNonZero > 0 && result[lastNonZero] == 0)
        {
            lastNonZero--;
        }
        
        if (lastNonZero + 1 != result.Length)
        {
            Array.Resize(ref result, lastNonZero + 1);
        }
        
        // Определяем знак результата
        bool isNegative = a.IsNegative ^ b.IsNegative;
        
        return new BetterBigInteger(result, isNegative);
    }
}