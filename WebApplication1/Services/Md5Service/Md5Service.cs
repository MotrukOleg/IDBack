using System.Text;
using WebApplication1.Interfaces.IMD5Service;

namespace WebApplication1.Services.MD5Service
{
    public class Md5Service : IMd5Service
    {
        private readonly uint[] T = new uint[64];
        private readonly int[] S = {
            7,12,17,22, 7,12,17,22, 7,12,17,22, 7,12,17,22,
            5,9,14,20, 5,9,14,20, 5,9,14,20, 5,9,14,20,
            4,11,16,23, 4,11,16,23, 4,11,16,23, 4,11,16,23,
            6,10,15,21, 6,10,15,21, 6,10,15,21, 6,10,15,21
        };

        private uint a0, b0, c0, d0;
        private ulong totalLength;
        private List<byte> buffer = new List<byte>();

        public Md5Service()
        {
            for (uint i = 0; i < 64; i++)
            {
                T[i] = (uint)Math.Floor(Math.Abs(Math.Sin(i + 1) * Math.Pow(2, 32)));
            }
            Initialize();
        }

        public void Initialize()
        {
            a0 = 0x67452301;
            b0 = 0xEFCDAB89;
            c0 = 0x98BADCFE;
            d0 = 0x10325476;
            totalLength = 0;
            buffer.Clear();
        }

        public void TransformBlock(byte[] data, int offset, int count)
        {
            totalLength += (ulong)count;

            int i = 0;
            if (buffer.Count > 0)
            {
                int needed = 64 - buffer.Count;
                int toCopy = Math.Min(needed, count);
                buffer.AddRange(data.Skip(offset).Take(toCopy));
                offset += toCopy;
                count -= toCopy;

                if (buffer.Count == 64)
                {
                    ProcessBlock(buffer.ToArray(), 0);
                    buffer.Clear();
                }
            }

            while (count >= 64)
            {
                ProcessBlock(data, offset);
                offset += 64;
                count -= 64;
            }

            if (count > 0)
            {
                buffer.AddRange(data.Skip(offset).Take(count));
            }
        }

        public string TransformFinalBlock()
        {
            byte[] padding = new byte[buffer.Count + 1 + ((56 - (int)((totalLength + 1) % 64) + 64) % 64) + 8];
            Array.Copy(buffer.ToArray(), 0, padding, 0, buffer.Count);
            padding[buffer.Count] = 0x80;

            ulong bitLength = totalLength * 8;
            Array.Copy(BitConverter.GetBytes(bitLength), 0, padding, padding.Length - 8, 8);

            for (int i = 0; i < padding.Length / 64; i++)
            {
                ProcessBlock(padding, i * 64);
            }

            byte[] hash = new byte[16];
            Array.Copy(BitConverter.GetBytes(a0), 0, hash, 0, 4);
            Array.Copy(BitConverter.GetBytes(b0), 0, hash, 4, 4);
            Array.Copy(BitConverter.GetBytes(c0), 0, hash, 8, 4);
            Array.Copy(BitConverter.GetBytes(d0), 0, hash, 12, 4);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        public async Task<string> ComputeMD5Hash(string? input)
        {
            byte[] message = Encoding.UTF8.GetBytes(input ?? string.Empty);

            Initialize();
            TransformBlock(message, 0, message.Length);
            string result = TransformFinalBlock();

            return await Task.FromResult(result);
        }

        public byte[] ComputeMD5HashBytes(byte[] input)
        {
            Initialize();
            TransformBlock(input, 0, input.Length);
            string hashHex = TransformFinalBlock();
            byte[] hashBytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                hashBytes[i] = Convert.ToByte(hashHex.Substring(i * 2, 2), 16);
            }
            return  hashBytes;
        }

        private void ProcessBlock(byte[] block, int offset)
        {
            uint[] M = new uint[16];
            for (int j = 0; j < 16; j++)
                M[j] = BitConverter.ToUInt32(block, offset + j * 4);

            uint A = a0, B = b0, C = c0, D = d0;

            for (int k = 0; k < 64; k++)
            {
                uint F, g;
                if (k < 16) { F = (B & C) | (~B & D); g = (uint)k; }
                else if (k < 32) { F = (D & B) | (~D & C); g = (uint)((5 * k + 1) % 16); }
                else if (k < 48) { F = B ^ C ^ D; g = (uint)((3 * k + 5) % 16); }
                else { F = C ^ (B | ~D); g = (uint)((7 * k) % 16); }

                F = F + A + T[k] + M[g];
                A = D;
                D = C;
                C = B;
                B = B + LeftRotate(F, S[k]);
            }

            a0 += A;
            b0 += B;
            c0 += C;
            d0 += D;
        }

        private uint LeftRotate(uint x, int c)
        {
            return (x << c) | (x >> (32 - c));
        }
    }
}
