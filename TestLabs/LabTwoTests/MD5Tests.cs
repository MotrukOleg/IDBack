using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using WebApplication1.Services.MD5Service;

namespace TestLabs.LabTwoTests
{
    public class MD5Tests
    {
        [Theory]
        [InlineData("", "D41D8CD98F00B204E9800998ECF8427E")]
        [InlineData(" " , "7215ee9c7d9dc229d2921a40e899ec5f")]
        [InlineData("a", "0CC175B9C0F1B6A831C399E269772661")]
        [InlineData("abc", "900150983CD24FB0D6963F7D28E17F72")]
        [InlineData("message digest", "F96B697D7CB7938D525A2F31AAF161D0")]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "C3FCD3D76192E4007DFB496CCA67E13B")]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", "D174AB98D277D9F5A5611C2C9F419D9F")]
        [InlineData("12345678901234567890123456789012345678901234567890123456789012345678901234567890", "57EDF4A22BE3C955AC49DA2E2107B67A")]
        public async Task ComputeMD5Hash_ValidInput_ReturnsCorrectHash(string input, string expectedHash)
        {
            var md5Service = new Md5Service();

            string actualHash = await md5Service.ComputeMD5Hash(input);

            Assert.Equal(expectedHash.ToLower(), actualHash.ToLower());
        }

        [Theory]
        [InlineData("c:/Users/Олег/Downloads/V18I03A177_F68SN-WO6LUbGk5Qy50HB.pdf" , "ab2fdb97bc5936cb16e0968d6bd01a9c")]
        [InlineData("c:/Users/Олег/Downloads/your-muse-orchestral-hip-hop-background-music-for-video-short-1-403076.mp3" , "3bd722157f3aeea284c15c0fa686e023")]
        [InlineData("c:/Users/Олег/Downloads/IMG20230928214421 (1).jpg" , "c36be8822be51a1a697cdcdba380f620")]
        public async Task ComputeMD5FileHash_ValidInput_ReturnsCorrectHash(string filePath , string expectedHash)
        {

            var md5Service = new Md5Service();

            using var stream = File.OpenRead(filePath);
            var file = new FormFile(stream, 0, stream.Length, "file", Path.GetFileName(filePath));
            byte[] buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                md5Service.TransformBlock(buffer, 0, bytesRead);
            }

            string hash = md5Service.TransformFinalBlock();


            Assert.Equal(expectedHash.ToLower(), hash.ToLower());
        }

    }
}