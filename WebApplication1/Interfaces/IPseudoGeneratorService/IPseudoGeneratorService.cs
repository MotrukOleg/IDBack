namespace WebApplication1.Interfaces.IPseudoGeneratorService
{
    public interface IPseudoGeneratorService
    {
        Task<(long[] seq, long[] randomSeq)> Generate(long a, long m, long n, long c, long x0);
        Task<long> GetPeriod(long[] seq);
        Task<double> SequenceEstimation(long[] seq);
        public byte NextByte();
        public byte[] NextBytes(int count);
    }
}
