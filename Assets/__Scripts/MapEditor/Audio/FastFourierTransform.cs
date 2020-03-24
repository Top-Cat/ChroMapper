using System;
using System.Numerics;

public class FastFourierTransform
{
    private static int BitReverse(int n, int bits)
    {
        int reversedN = n;
        int count = bits - 1;

        n >>= 1;
        while (n > 0)
        {
            reversedN = (reversedN << 1) | (n & 1);
            count--;
            n >>= 1;
        }

        return ((reversedN << count) & ((1 << bits) - 1));
    }

    /* Uses Cooley-Tukey iterative in-place algorithm with radix-2 DIT case
     * assumes no of points provided are a power of 2 */
    public static float[] FFT(float[] floats)
    {
        Complex[] buffer = new Complex[floats.Length];
        
        for (int i = 0; i < floats.Length; i++)
        {
            buffer[i] = new Complex(floats[i], 0);
        }

        int bits = (int)Math.Log(buffer.Length, 2);
        for (int j = 1; j < buffer.Length; j++)
        {
            int swapPos = BitReverse(j, bits);
            if (swapPos <= j)
            {
                continue;
            }
            var temp = buffer[j];
            buffer[j] = buffer[swapPos];
            buffer[swapPos] = temp;
        }

        // First the full length is used and 1011 value is swapped with 1101. Second if new swapPos is less than j
        // then it means that swap was happen when j was the swapPos.

        for (int N = 2; N <= buffer.Length; N <<= 1)
        {
            for (int i = 0; i < buffer.Length; i += N)
            {
                for (int k = 0; k < N / 2; k++)
                {

                    int evenIndex = i + k;
                    int oddIndex = i + k + (N / 2);
                    var even = buffer[evenIndex];
                    var odd = buffer[oddIndex];

                    double term = -2 * Math.PI * k / (double)N;
                    Complex exp = new Complex(Math.Cos(term), Math.Sin(term)) * odd;

                    buffer[evenIndex] = even + exp;
                    buffer[oddIndex] = even - exp;

                }
            }
        }

        for (int i = 0; i < buffer.Length; i += 1)
        {
            floats[i] = (float)Math.Sqrt(Math.Pow(buffer[i].Real, 2) + Math.Pow(buffer[i].Imaginary, 2));
        }

        return floats;
    }
}
