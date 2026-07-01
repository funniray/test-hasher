using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace hashTest.openssl;

public class MD4 : HashAlgorithm
{
    public static new MD4 Create() => new();
    
    public const int HashSizeInBits = 128;
    public const int HashSizeInBytes = HashSizeInBits / 8;

    private IntPtr _ctx = IntPtr.Zero;

    private MD4()
    {
        CryptoEVP.OpenSslProviderLoad(IntPtr.Zero, "legacy");
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
        var algo  = CryptoEVP.EvpGetDigestByName("MD4");
        _ctx = CryptoEVP.EvpMdCtxNew();
        CryptoEVP.EvpDigestInitEx(_ctx, algo, IntPtr.Zero);
    }

    ~MD4()
    {
        CryptoEVP.EvpMdCtxFree(_ctx);
    }
}