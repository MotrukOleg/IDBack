using System;
using System.Threading.Tasks;
using WebApplication1.Services.PseudoGenService;
using Xunit;

namespace TestLabs.LabOneTests
{
    public class PrngTests
    {
        private readonly PseudoGenService _service;

        public PrngTests()
        {
            _service = new PseudoGenService(new Random(42), 1234);
        }
        

        [Fact]
        public async Task Generate_ReturnsCorrectSequenceLength()
        {
            // Arrange
            long a = 5, m = 17, n = 10, c = 3, x0 = 7;

            // Act
            var (seq, randomSeq) = await _service.Generate(a, m, n, c, x0);

            // Assert
            Assert.Equal(n, seq.Length);
            Assert.Equal(n, randomSeq.Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public async Task Generate_VariousSequenceLengths_ReturnsCorrectLength(long n)
        {
            // Arrange
            long a = 5, m = 17, c = 3, x0 = 7;

            // Act
            var (seq, randomSeq) = await _service.Generate(a, m, n, c, x0);

            // Assert
            Assert.Equal(n, seq.Length);
            Assert.Equal(n, randomSeq.Length);
        }

        [Theory]
        [InlineData(5, 17, 10, 3, 7)]
        [InlineData(3, 13, 20, 5, 2)]
        [InlineData(7, 19, 15, 2, 4)]
        public async Task Generate_VariousParameters_ProducesNonNullSequences(long a, long m, long n, long c, long x0)
        {
            // Act
            var (seq, randomSeq) = await _service.Generate(a, m, n, c, x0);

            // Assert
            Assert.NotNull(seq);
            Assert.NotNull(randomSeq);
            Assert.NotEmpty(seq);
            Assert.NotEmpty(randomSeq);
        }
        

        [Fact]
        public async Task GetPeriod_ReturnsZeroForEmptySequence()
        {
            // Arrange
            var emptySequence = Array.Empty<long>();

            // Act
            var period = await _service.GetPeriod(emptySequence);

            // Assert
            Assert.Equal(0, period);
        }

        [Fact]
        public async Task GetPeriod_ReturnsPeriodForRepeatingSequence()
        {
            // Arrange
            long[] seq = { 1, 2, 3, 1, 2, 3 };

            // Act
            var period = await _service.GetPeriod(seq);

            // Assert
            Assert.Equal(3, period);
        }

        [Fact]
        public async Task GetPeriod_ReturnsPeriodForSingleElement()
        {
            // Arrange
            long[] seq = { 5, 5, 5, 5 };

            // Act
            var period = await _service.GetPeriod(seq);

            // Assert
            Assert.Equal(1, period);
        }

        [Fact]
        public async Task GetPeriod_ReturnsPeriodForComplexPattern()
        {
            // Arrange
            long[] seq = { 1, 2, 3, 4, 1, 2, 3, 4 };

            // Act
            var period = await _service.GetPeriod(seq);

            // Assert
            Assert.Equal(4, period);
        }

        [Fact]
        public async Task GetPeriod_ReturnsPeriodForNonRepeatingSequence()
        {
            // Arrange
            long[] seq = { 1, 2, 3, 4, 5 };

            // Act
            var period = await _service.GetPeriod(seq);

            // Assert
            Assert.True(period >= seq.Length || period == 0);
        }

        [Fact]
        public async Task GetPeriod_WithSingleElement_ReturnsOne()
        {
            // Arrange
            long[] seq = { 42 };

            // Act
            var period = await _service.GetPeriod(seq);

            // Assert
            Assert.Equal(1, period);
        }
        
        [Fact]
        public async Task SequenceEstimation_ReturnsPositiveValue()
        {
            // Arrange
            long[] seq = { 2, 3, 4, 5, 6 };

            // Act
            var estimation = await _service.SequenceEstimation(seq);

            // Assert
            Assert.True(estimation > 0);
        }

        [Fact]
        public async Task SequenceEstimation_WithLargeNumbers_ReturnsPositiveValue()
        {
            // Arrange
            long[] seq = { 1000, 2000, 3000, 4000, 5000 };

            // Act
            var estimation = await _service.SequenceEstimation(seq);

            // Assert
            Assert.True(estimation > 0);
        }

        [Fact]
        public async Task SequenceEstimation_WithNegativeNumbers_HandlesCorrectly()
        {
            // Arrange
            long[] seq = { -5, -3, -1, 1, 3 };

            // Act
            var estimation = await _service.SequenceEstimation(seq);

            // Assert
            Assert.True(estimation >= 0);
        }

        [Fact]
        public async Task SequenceEstimation_WithZeros_HandlesCorrectly()
        {
            // Arrange
            long[] seq = { 0, 0, 0 };

            // Act
            var estimation = await _service.SequenceEstimation(seq);

            // Assert
            Assert.True(estimation >= 0);
        }

        [Theory]
        [InlineData(new long[] { 1, 2 })]
        [InlineData(new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
        public async Task SequenceEstimation_VariousLengths_ReturnsValidEstimation(long[] seq)
        {
            // Act
            var estimation = await _service.SequenceEstimation(seq);

            // Assert
            Assert.True(estimation >= 0);
        }
        
        [Fact]
        public void NextByte_ReturnsByte()
        {
            // Act
            var b = _service.NextByte();

            // Assert
            Assert.InRange(b, byte.MinValue, byte.MaxValue);
        }

        [Fact]
        public void NextByte_MultipleCallsReturnValidBytes()
        {
            // Act & Assert
            for (int i = 0; i < 100; i++)
            {
                var b = _service.NextByte();
                Assert.InRange(b, byte.MinValue, byte.MaxValue);
            }
        }

        [Fact]
        public void NextByte_ReturnsDifferentValues()
        {
            // Arrange
            var bytes = new byte[10];

            // Act
            for (int i = 0; i < 10; i++)
            {
                bytes[i] = _service.NextByte();
            }

            // Assert - At least some values should be different
            Assert.NotEqual(bytes[0], bytes[1]);
        }
        
        [Fact]
        public void NextBytes_ReturnsCorrectLength()
        {
            // Arrange
            int count = 16;

            // Act
            var bytes = _service.NextBytes(count);

            // Assert
            Assert.Equal(count, bytes.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(32)]
        [InlineData(256)]
        [InlineData(1024)]
        public void NextBytes_VariousLengths_ReturnsCorrectLength(int count)
        {
            // Act
            var bytes = _service.NextBytes(count);

            // Assert
            Assert.Equal(count, bytes.Length);
        }

        [Fact]
        public void NextBytes_ReturnsNonNullArray()
        {
            // Act
            var bytes = _service.NextBytes(16);

            // Assert
            Assert.NotNull(bytes);
        }

        [Fact]
        public void NextBytes_ReturnsDifferentSequences()
        {
            // Act
            var bytes1 = _service.NextBytes(16);
            var bytes2 = _service.NextBytes(16);

            // Assert
            Assert.NotEqual(bytes1, bytes2);
        }

        [Fact]
        public void NextBytes_AllValuesInValidRange()
        {
            // Act
            var bytes = _service.NextBytes(100);

            // Assert
            foreach (var b in bytes)
            {
                Assert.InRange(b, byte.MinValue, byte.MaxValue);
            }
        }
        
        [Fact]
        public void GeneratePseudoRandom_ReturnsCorrectLength()
        {
            // Arrange
            int count = 8;
            uint seed = 123;

            // Act
            var bytes = _service.GeneratePseudoRandom(count, seed);

            // Assert
            Assert.Equal(count, bytes.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(16)]
        [InlineData(64)]
        [InlineData(512)]
        public void GeneratePseudoRandom_VariousLengths_ReturnsCorrectLength(int count)
        {
            // Arrange
            uint seed = 42;

            // Act
            var bytes = _service.GeneratePseudoRandom(count, seed);

            // Assert
            Assert.Equal(count, bytes.Length);
        }

        [Fact]
        public void GeneratePseudoRandom_SameSeed_ProducesSameSequence()
        {
            // Arrange
            uint seed = 123;
            int count = 16;

            // Act
            var bytes1 = _service.GeneratePseudoRandom(count, seed);
            var bytes2 = _service.GeneratePseudoRandom(count, seed);

            // Assert
            Assert.Equal(bytes1, bytes2);
        }

        [Fact]
        public void GeneratePseudoRandom_DifferentSeeds_ProducesDifferentSequences()
        {
            // Arrange
            int count = 16;

            // Act
            var bytes1 = _service.GeneratePseudoRandom(count, 123);
            var bytes2 = _service.GeneratePseudoRandom(count, 456);

            // Assert
            Assert.NotEqual(bytes1, bytes2);
        }

        [Fact]
        public void GeneratePseudoRandom_ReturnsNonNullArray()
        {
            // Act
            var bytes = _service.GeneratePseudoRandom(8, 123);

            // Assert
            Assert.NotNull(bytes);
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(uint.MaxValue)]
        [InlineData(12345u)]
        public void GeneratePseudoRandom_VariousSeeds_ReturnsValidBytes(uint seed)
        {
            // Act
            var bytes = _service.GeneratePseudoRandom(16, seed);

            // Assert
            Assert.Equal(16, bytes.Length);
            foreach (var b in bytes)
            {
                Assert.InRange(b, byte.MinValue, byte.MaxValue);
            }
        }
    }
}