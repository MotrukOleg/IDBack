using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using WebApplication1.Services.MD5Service;
using Xunit;

namespace TestLabs.LabTwoTests
{
    public class MD5Tests
    {
        private readonly Md5Service _md5Service;

        public MD5Tests()
        {
            _md5Service = new Md5Service();
        }

        [Theory]
        [InlineData("", "D41D8CD98F00B204E9800998ECF8427E")]
        [InlineData(" ", "7215EE9C7D9DC229D2921A40E899EC5F")]
        [InlineData("a", "0CC175B9C0F1B6A831C399E269772661")]
        [InlineData("abc", "900150983CD24FB0D6963F7D28E17F72")]
        [InlineData("message digest", "F96B697D7CB7938D525A2F31AAF161D0")]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "C3FCD3D76192E4007DFB496CCA67E13B")]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", "D174AB98D277D9F5A5611C2C9F419D9F")]
        [InlineData("12345678901234567890123456789012345678901234567890123456789012345678901234567890", "57EDF4A22BE3C955AC49DA2E2107B67A")]
        public async Task ComputeMD5Hash_ValidInput_ReturnsCorrectHash(string input, string expectedHash)
        {
            // Act
            string actualHash = await _md5Service.ComputeMD5Hash(input);

            // Assert
            Assert.Equal(expectedHash.ToLower(), actualHash.ToLower());
        }

        [Fact]
        public async Task ComputeMD5Hash_EmptyString_ReturnsEmptyStringHash()
        {
            // Arrange
            string input = "";
            string expectedHash = "D41D8CD98F00B204E9800998ECF8427E";

            // Act
            string actualHash = await _md5Service.ComputeMD5Hash(input);

            // Assert
            Assert.Equal(expectedHash.ToLower(), actualHash.ToLower());
        }

        [Fact]
        public async Task ComputeMD5Hash_MultipleCallsSameInput_ReturnsSameHash()
        {
            // Arrange
            string input = "test input";

            // Act
            string hash1 = await _md5Service.ComputeMD5Hash(input);
            string hash2 = await _md5Service.ComputeMD5Hash(input);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Theory]
        [InlineData("c:/Users/Олег/Downloads/V18I03A177_F68SN-WO6LUbGk5Qy50HB.pdf", "ab2fdb97bc5936cb16e0968d6bd01a9c")]
        [InlineData("c:/Users/Олег/Downloads/your-muse-orchestral-hip-hop-background-music-for-video-short-1-403076.mp3", "3bd722157f3aeea284c15c0fa686e023")]
        [InlineData("c:/Users/Олег/Downloads/IMG20230928214421 (1).jpg", "c36be8822be51a1a697cdcdba380f620")]
        public async Task ComputeMD5FileHash_ValidInput_ReturnsCorrectHash(string filePath, string expectedHash)
        {
            // Arrange
            using var stream = File.OpenRead(filePath);
            byte[] buffer = new byte[81920];
            int bytesRead;

            // Act
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                _md5Service.TransformBlock(buffer, 0, bytesRead);
            }

            string actualHash = _md5Service.TransformFinalBlock();

            // Assert
            Assert.Equal(expectedHash.ToLower(), actualHash.ToLower());
        }

        [Fact]
        public async Task ComputeMD5FileHash_ValidFile_ProducesConsistentHash()
        {
            // Arrange
            string filePath = "c:/Users/Олег/Downloads/V18I03A177_F68SN-WO6LUbGk5Qy50HB.pdf";
            string expectedHash = "ab2fdb97bc5936cb16e0968d6bd01a9c";

            // Act - First hash
            using var stream1 = File.OpenRead(filePath);
            byte[] buffer1 = new byte[81920];
            int bytesRead1;
            var service1 = new Md5Service();

            while ((bytesRead1 = await stream1.ReadAsync(buffer1, 0, buffer1.Length)) > 0)
            {
                service1.TransformBlock(buffer1, 0, bytesRead1);
            }

            string hash1 = service1.TransformFinalBlock();

            // Act - Second hash
            using var stream2 = File.OpenRead(filePath);
            byte[] buffer2 = new byte[81920];
            int bytesRead2;
            var service2 = new Md5Service();

            while ((bytesRead2 = await stream2.ReadAsync(buffer2, 0, buffer2.Length)) > 0)
            {
                service2.TransformBlock(buffer2, 0, bytesRead2);
            }

            string hash2 = service2.TransformFinalBlock();

            // Assert
            Assert.Equal(hash1.ToLower(), hash2.ToLower());
        }

        [Fact]
        public async Task ComputeMD5FileHash_DifferentFiles_ProducesDifferentHashes()
        {
            // Arrange
            string filePath1 = "c:/Users/Олег/Downloads/V18I03A177_F68SN-WO6LUbGk5Qy50HB.pdf";
            string filePath2 = "c:/Users/Олег/Downloads/your-muse-orchestral-hip-hop-background-music-for-video-short-1-403076.mp3";

            // Act - First file hash
            using var stream1 = File.OpenRead(filePath1);
            byte[] buffer1 = new byte[81920];
            int bytesRead1;
            var service1 = new Md5Service();

            while ((bytesRead1 = await stream1.ReadAsync(buffer1, 0, buffer1.Length)) > 0)
            {
                service1.TransformBlock(buffer1, 0, bytesRead1);
            }

            string hash1 = service1.TransformFinalBlock();

            // Act - Second file hash
            using var stream2 = File.OpenRead(filePath2);
            byte[] buffer2 = new byte[81920];
            int bytesRead2;
            var service2 = new Md5Service();

            while ((bytesRead2 = await stream2.ReadAsync(buffer2, 0, buffer2.Length)) > 0)
            {
                service2.TransformBlock(buffer2, 0, bytesRead2);
            }

            string hash2 = service2.TransformFinalBlock();

            // Assert
            Assert.NotEqual(hash1.ToLower(), hash2.ToLower());
        }
    }
}