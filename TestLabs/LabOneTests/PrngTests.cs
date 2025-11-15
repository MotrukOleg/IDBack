using System;
using System.Threading.Tasks;
using WebApplication1.Services.PseudoGenService;
using Xunit;

public class Prng
{
    [Fact]
    public async Task Generate_ReturnsCorrectSequenceLength()
    {
        var service = new PseudoGenService(new Random(42), 1234);
        long a = 5, m = 17, n = 10, c = 3, x0 = 7;

        var (seq, randomSeq) = await service.Generate(a, m, n, c, x0);

        Assert.Equal(n, seq.Length);
        Assert.Equal(n, randomSeq.Length);
    }

    [Fact]
    public async Task GetPeriod_ReturnsZeroForEmptySequence()
    {
        var service = new PseudoGenService(new Random(), 0);
        var period = await service.GetPeriod(Array.Empty<long>());
        Assert.Equal(0, period);
    }

    [Fact]
    public async Task GetPeriod_ReturnsPeriodForRepeatingSequence()
    {
        var service = new PseudoGenService(new Random(), 0);
        long[] seq = { 1, 2, 3, 1, 2, 3 };
        var period = await service.GetPeriod(seq);
        Assert.Equal(3, period);
    }

    [Fact]
    public async Task SequenceEstimation_ReturnsExpectedValue()
    {
        var service = new PseudoGenService(new Random(), 0);
        long[] seq = { 2, 3, 4, 5, 6 };
        var estimation = await service.SequenceEstimation(seq);
        Assert.True(estimation > 0);
    }

    [Fact]
    public void NextByte_ReturnsByte()
    {
        var service = new PseudoGenService(new Random(), 0);
        var b = service.NextByte();
        Assert.InRange(b, byte.MinValue, byte.MaxValue);
    }

    [Fact]
    public void NextBytes_ReturnsCorrectLength()
    {
        var service = new PseudoGenService(new Random(), 0);
        int count = 16;
        var bytes = service.NextBytes(count);
        Assert.Equal(count, bytes.Length);
    }

    [Fact]
    public void GeneratePseudoRandom_ReturnsCorrectLength()
    {
        var service = new PseudoGenService(new Random(), 0);
        int count = 8;
        uint seed = 123;
        var bytes = service.GeneratePseudoRandom(count, seed);
        Assert.Equal(count, bytes.Length);
    }
}
