using WebApplication1.Interfaces.IPseudoGeneratorService;

namespace WebApplication1.Services.PseudoGenService
{
    public class PseudoGenService : IPseudoGeneratorService
    {
        public readonly static int MAX_BOUNDARY = 1337;
        public readonly static int MIN_BOUNDARY = 1;
        private long _period;
        private long _periodRandom;
        private double _cesaroRatio;
        private double _cesaroRandomRatio;
        private readonly Random _random;
        private ulong state = 0x123456789ABCDEF0;
        private const ulong A = 6364136223846793005UL;
        private const ulong C = 1442695040888963407UL;


        public PseudoGenService(Random random, ulong seed)
        {
            _random = random;
        }

        public async Task<(long[] seq , long[] randomSeq)> Generate(long a , long m , long n , long c , long x0)
        {
            if (MIN_BOUNDARY > n)
            {
                n = MIN_BOUNDARY;
            }
            else if (n > MAX_BOUNDARY)
            {
                n = MAX_BOUNDARY;
            }
            
            long[] _seq = new long[n];
            long[] _randomSequence = new long[n];
            
            _seq[0] = x0;

            for (int i = 0; i < n - 1; i++)
            {
                _seq[i + 1] = (a * _seq[i] + c) % m;
            }
            for (int i = 0; i < n; i++)
            {
                long randomNum = _random.NextInt64(1, Math.Max(m, 1000));
                _randomSequence[i] = randomNum;
            }

            return await Task.FromResult((_seq, _randomSequence));
        }

        public async Task<long> GetPeriod(long[] seq)
        {
            if (seq == null || seq.Length == 0) return 0;
            long per = 0;
            int n = seq.Length;

            var freq = new Dictionary<long, long>();
            long count = 0;

            for (int i = 0; i < n; i++)
            {
                if (freq.ContainsKey(seq[i]))
                {
                    per = count;
                    return per;
                }
                else
                {
                    freq[seq[i]] = 1;
                }

                count++;
            }

            per = count;
            return await Task.FromResult(per);
        }

        public async Task<double> SequenceEstimation(long[] seq)
        {
            if (seq == null || seq.Length < 2) ;

            long coprimeCount = 0;
            long totalPairs = seq.Length - 1;

            for (int i = 0; i < seq.Length - 1; i++)
            {
                if (Gcd(seq[i], seq[i + 1]) == 1) coprimeCount++;
            }


            double propability = (double)coprimeCount / totalPairs;
            return await Task.FromResult(Math.Sqrt(6 / propability));
        }

        public byte NextByte()
        {
            state = A * state + C;
            return (byte)(state >> 32);
        }

        public static byte[] GeneratePseudoRandom(int count, uint seed)
        {
            byte[] result = new byte[count];
            uint state = seed;

            for (int i = 0; i < count; i++)
            {
                state = (1103515245 * state + 12345) & 0x7FFFFFFF;
                result[i] = (byte)(state & 0xFF);
            }

            return result;
        }

        public byte[] NextBytes(int count)
        {
            byte[] result = new byte[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = NextByte();
            }
            return result;
        }

        private static long Gcd(long a , long b)
        {
            while (b != 0)
            {
                long temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
    }
}
