using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Config;
using Shoko.Abstractions.Video.Hashing;

namespace hashTest;

/// <summary>
/// Test hasher that should be faster
/// </summary>
public class HashProvider(ILogger<HashProvider> logger) : IHashProvider<HashProvider.TestHashConfiguration>
{
    /// <inheritdoc/>
    public string Name { get; } = "Test hasher";

    /// <inheritdoc/>
    public Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version!;
    /// <inheritdoc/>
    public string Description { get; } = """
        Test hasher that should be faster
    """;

    /// <inheritdoc/>
    public IReadOnlySet<string> AvailableHashTypes => new HashSet<string> { "ED2K", "CRC32", "MD5", "SHA1" };

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<HashDigest>> GetHashesForVideo(HashingRequest request, CancellationToken cancellationToken = default)
    {
        var (file, existingHashes, enabledHashTypes) = request;

        var ED2K = !existingHashes.Any(h => h.Type is "ED2K") && enabledHashTypes.Contains("ED2K");
        var CRC32 = !existingHashes.Any(h => h.Type is "CRC32") && enabledHashTypes.Contains("CRC32");
        var MD5 = !existingHashes.Any(h => h.Type is "MD5") && enabledHashTypes.Contains("MD5");
        var SHA1 = !existingHashes.Any(h => h.Type is "SHA1") && enabledHashTypes.Contains("SHA1");

        var hasher = new Hasher();
        var res = await Task.Run(()=>hasher.Hash(file.FullName, CRC32, SHA1, MD5));

        logger.LogInformation($"Hashed {file.FullName} in {res.Time}ms at {res.Speed}MB/s.");

        var calculatedHashes = new List<HashDigest>();
        if (ED2K && res.Ed2k?.Hash != null) {calculatedHashes.Add(new HashDigest { Type = "ED2K", Value = res.Ed2k.Hash });}
        if (CRC32 && res.Crc?.Hash != null) {calculatedHashes.Add(new HashDigest { Type = "CRC32", Value = res.Crc.Hash });}
        if (MD5 && res.Md5?.Hash != null) {calculatedHashes.Add(new HashDigest { Type = "MD5", Value = res.Md5.Hash });}
        if (SHA1 && res.Sha1?.Hash != null) {calculatedHashes.Add(new HashDigest { Type = "SHA1", Value = res.Sha1.Hash });}

        var hashes = calculatedHashes
            .Concat(existingHashes.Select(h => new HashDigest { Type = h.Type, Value = h.Value, Metadata = h.Metadata }))
            .DistinctBy(h => h.Type)
            .OrderBy(h => (h.Type, h.Value, h.Metadata))
            .ToList();
        return hashes;
    }

    [Display(Name = "Test Hasher")]
    public class TestHashConfiguration : IHashProviderConfiguration, INewtonsoftJsonConfiguration
    {
        /// <summary>
        /// meaningless option to test if webui is borked
        /// </summary>
        [Required]
        [Display(Name = "Meaningless option")]
        public bool IsWebuiBroken { get; set;}
    }
}