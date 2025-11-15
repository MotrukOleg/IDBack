using System;
using System.Text;
using Moq;
using WebApplication1.Dtos;
using WebApplication1.Interfaces.IMD5Service;
using WebApplication1.Interfaces.IPseudoGeneratorService;
using WebApplication1.Services.RC5Service;
using Xunit;
using Microsoft.Extensions.Logging;

public class Rc5Tests
{
    private Rc5Service CreateService()
    {
        var logger = new Mock<ILogger<Rc5Service>>();
        var md5 = new Mock<IMd5Service>();
        var pseudoGen = new Mock<IPseudoGeneratorService>();

        md5.Setup(m => m.ComputeMD5HashBytes(It.IsAny<byte[]>()))
            .Returns<byte[]>(input => {
                var hash = new byte[16];
                for (int i = 0; i < hash.Length && i < input.Length; i++)
                    hash[i] = input[i];
                return hash;
            });

        return new Rc5Service(logger.Object, md5.Object, pseudoGen.Object);
    }

    [Fact]
    public void DeriveKey_ReturnsCorrectLength()
    {
        var service = CreateService();
        var key64 = service.DeriveKey("pass", 64);
        var key128 = service.DeriveKey("pass", 128);
        var key256 = service.DeriveKey("pass", 256);

        Assert.Equal(8, key64.Length);
        Assert.Equal(16, key128.Length);
        Assert.Equal(32, key256.Length);
    }

    [Fact]
    public void EncryptText_And_DecryptText_RoundTrip()
    {
        var service = CreateService();
        string text = "Hello RC5!";
        string password = "testpass";

        var (encrypted, iv) = service.EncryptText(text, password, 128);
        Assert.NotNull(encrypted);
        Assert.NotNull(iv);

        string decrypted = service.DecryptText(encrypted, password, 128);
        Assert.Equal(text, decrypted);
    }


    [Fact]
    public void EncryptBytes_And_DecryptBytes_RoundTrip()
    {
        var service = CreateService();
        byte[] data = Encoding.UTF8.GetBytes("RC5 bytes test");
        string password = "bytespass";

        var encrypted = service.EncryptBytes(data, password, 128);
        Assert.NotNull(encrypted);

        var decrypted = service.DecryptBytes(encrypted, password, 128);
        Assert.Equal(data, decrypted);
    }

    [Fact]
    public void EncryptBytesWithMetadata_And_DecryptBytesWithMetadata_RoundTrip()
    {
        var service = CreateService();
        byte[] data = Encoding.UTF8.GetBytes("MetaDataTest");
        string password = "meta";
        string fileName = "file.txt";

        var encrypted = service.EncryptBytesWithMetadata(data, password, fileName, 128);
        Assert.NotNull(encrypted);

        var (decrypted, metadata) = service.DecryptBytesWithMetadata(encrypted, password, 128);
        Assert.Equal(data, decrypted);
        Assert.Equal("file", metadata.OriginalFileName);
        Assert.Equal(".txt", metadata.OriginalExtension);
        Assert.Equal(data.Length, metadata.OriginalSize);
        Assert.Equal(128, metadata.KeySize);
    }

    [Fact]
    public void EncryptText_ThrowsOnInvalidKeySize()
    {
        var service = CreateService();
        Assert.Throws<ArgumentException>(() => service.EncryptText("data", "pass", 99));
    }

    [Fact]
    public void DecryptText_ThrowsOnInvalidData()
    {
        var service = CreateService();
        Assert.Throws<ArgumentException>(() => service.DecryptText(new byte[2], "pass", 128));
    }
}
