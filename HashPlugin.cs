using Shoko.Abstractions.Plugin;
using Shoko.Abstractions.Utilities;

namespace hashTest;

public class HashPlugin : IPlugin
{
    /// <inheritdoc/>
    public Guid ID => UuidUtility.GetV5(typeof(HashPlugin).FullName!);
    /// <inheritdoc/>
    public string Name { get; } = "Test Hash Plugin";
    /// <inheritdoc/>
    public string Description { get; } = "Provides an alternative hash implementation for ed2k, md5, sha1 and crc32";
}