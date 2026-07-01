using System.Runtime.InteropServices;

namespace hashTest.openssl;

internal partial class CryptoEVP
{
    [LibraryImport("libcrypto", EntryPoint = "EVP_get_digestbyname", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr EvpGetDigestByName(string algorithm);
    
    [LibraryImport("libcrypto", EntryPoint = "EVP_MD_CTX_new", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr EvpMdCtxNew();
    
    [LibraryImport("libcrypto", EntryPoint = "EVP_DigestInit_ex", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void EvpDigestInitEx(IntPtr ctx, IntPtr algorithm, IntPtr engine);
    
    [LibraryImport("libcrypto", EntryPoint = "EVP_DigestUpdate", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void EvpDigestUpdate(IntPtr ctx, ReadOnlySpan<byte> data, UInt64 size);
    
    [LibraryImport("libcrypto", EntryPoint = "EVP_DigestFinal_ex", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void EvpDigestFinalEx(IntPtr ctx, ref byte md, ref uint size);
    
    [LibraryImport("libcrypto", EntryPoint = "EVP_MD_CTX_reset", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void EvpMdCtxReset(IntPtr ctx);
    
    [LibraryImport("libcrypto", EntryPoint = "EVP_MD_CTX_free", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void EvpMdCtxFree(IntPtr ctx);
}