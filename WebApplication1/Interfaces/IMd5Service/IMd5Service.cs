namespace WebApplication1.Interfaces.IMD5Service
{
    public interface IMd5Service
    {
        void TransformBlock(byte[] data, int offset, int count);
        string TransformFinalBlock();
        Task<string> ComputeMD5Hash(string? input);
        byte[] ComputeMD5HashBytes(byte[] input);
    }
}
