using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace hashTest.openssl;

public class SHA1 : HashAlgorithm
{
    public static new SHA1 Create() => new();
    
    public const int HashSizeInBits = 160;
    public const int HashSizeInBytes = HashSizeInBits / 8;

    private IntPtr _ctx = IntPtr.Zero;

    private SHA1()
    {
        Initialize();
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        CryptoEVP.EvpDigestUpdate(_ctx, array.AsSpan(ibStart, cbSize), (ulong) cbSize);
    }

    protected override byte[] HashFinal()
    {
        Span<byte> dest = new Span<byte>(new byte[HashSizeInBytes]);
        uint len = (uint) dest.Length;
        CryptoEVP.EvpDigestFinalEx(_ctx, ref MemoryMarshal.GetReference(dest), ref len);
        CryptoEVP.EvpMdCtxReset(_ctx);
        return dest.ToArray();
    }

    public override void Initialize()
    {
        var algo  = CryptoEVP.EvpGetDigestByName("SHA1");
        _ctx = CryptoEVP.EvpMdCtxNew();
        CryptoEVP.EvpDigestInitEx(_ctx, algo, IntPtr.Zero);
    }

    ~SHA1()
    {
        CryptoEVP.EvpMdCtxFree(_ctx);
    }
}