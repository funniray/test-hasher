using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashTest.rhash;

public class RhashMD4 : HashAlgorithm
{
    public static new RhashMD4 Create() => new();
    
    private IntPtr _ctx = IntPtr.Zero;

    private RhashMD4()
    {
        Initialize();
    }
    
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        Native.rhash_update(_ctx, array.AsSpan(ibStart, cbSize), (uint) cbSize);
    }

    protected override byte[] HashFinal()
    {
        Native.rhash_final(_ctx, IntPtr.Zero);
        Native.rhash_free(_ctx);
        
        Span<byte> dest = new Span<byte>(new byte[16]);
        Native.rhash_print(ref MemoryMarshal.GetReference(dest), _ctx, Native.RHashIds.RHASH_MD4,
            Native.RhashPrintSumFlags.RHPR_RAW);
        return dest.ToArray();
    }

    public override void Initialize()
    {
        _ctx = Native.rhash_init(Native.RHashIds.RHASH_MD4);
    }
}