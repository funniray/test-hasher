using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Hashing;
using System.Text;
using hashTest.openssl;

namespace HashTest {

    public class HashStructure {
        public string? Hash {get;set;}
        public double Time {get;set;}

        public HashStructure(string hash, double time) {
            this.Hash = hash;
            this.Time = time;
        }
    }
    public class ResponseStructure {
        public HashStructure? Crc {get; set;}
        public HashStructure? Sha1 {get; set;}
        public HashStructure? Md5 {get; set;}
        public HashStructure? Ed2k {get; set;}
        public double Time {get;set;}
        public double Speed {get;set;}

        public ResponseStructure(HashStructure? crc, HashStructure? sha1, HashStructure? Md5, HashStructure? Ed2k, double speed, double time) {
            this.Crc = crc;
            this.Sha1 = sha1;
            this.Md5 = Md5;
            this.Ed2k = Ed2k;
            this.Speed = speed;
            this.Time = time;
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(ResponseStructure))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }

    class Hasher {
        private static int ED2K_BLOCK_LENGTH = 1024 * 9500;

        private double[] timers = new double[3]{0,0,0};
        private double ed2ktime = 0;

        private void hashBlock(HashAlgorithm alg, byte[] stream, bool final, int timerIndex) {
            var timer = System.Diagnostics.Stopwatch.StartNew();

            if (final) {
                alg.TransformFinalBlock(stream, 0, stream.Length);
            } else {
                alg.TransformBlock(stream, 0, stream.Length, null, 0);
            }

            timers[timerIndex] += timer.ElapsedMilliseconds;
        }
        
        private void hashBlock(NonCryptographicHashAlgorithm alg, byte[] stream, int timerIndex) {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            
            alg.Append(stream);

            timers[timerIndex] += timer.ElapsedMilliseconds;
        }

        private byte[] getCrc32(Crc32 crc)
        {
            byte[] o = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(o, crc.GetCurrentHashAsUInt32());
            return o;
        }
        
        public static string BytesToHex(byte[] a, int? len)
        {
            string temp = BitConverter.ToString(a);

            // We need to remove the dashes that come from the BitConverter
            var sb = new StringBuilder((len.GetValueOrDefault(a.Length) - 2)/2); // This should be the final size

            for (int i = 0; i < temp.Length; i++)
                if (temp[i] != '-')
                    sb.Append(temp[i]);

            return sb.ToString();
        }


        public void Hash(string url, bool crcEnabled, bool sha1Enabled, bool md5Enabled) {
            timers = new double[3]{0,0,0};
            long bytesRead = 0;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            using (var stream = File.Open(url, FileMode.Open)) {
                using (var reader = new BinaryReader(stream)) {
                    var tasks = new List<Task>();
                    var hashes = new System.Collections.Concurrent.ConcurrentDictionary<int, byte[]>();
                    var i = 0;

                    var crc = new Crc32();
                    using var sha1 = SHA1.Create();
                    using var md5 = MD5.Create();

                    Task crcTask = Task.Run(()=>{});
                    Task sha1Task = Task.Run(()=>{});
                    Task md5Task = Task.Run(()=>{});
                    
                    while (true) {
                        byte[] read = reader.ReadBytes(ED2K_BLOCK_LENGTH);
                        Task.WaitAll(new []{crcTask, sha1Task, md5Task});

                        if (read.Length <= 0) {break;}
                        var localIndex = i;
                        var isLast = read.Length < ED2K_BLOCK_LENGTH;
                        bytesRead+=read.Length;
                    
                        if (crcEnabled)
                            crcTask = Task.Run(()=>{hashBlock(crc, read, 0);});
                        if (sha1Enabled)
                            sha1Task = Task.Run(()=>{hashBlock(sha1, read, isLast, 1);});
                        if (md5Enabled)
                            md5Task = Task.Run(()=>{hashBlock(md5, read, isLast, 2);});

                        tasks.Add(Task.Run(()=>
                        {
                            var time = System.Diagnostics.Stopwatch.StartNew();
                            var md4 = MD4.Create();
                            hashes.TryAdd(localIndex, md4.ComputeHash(read));
                            time.Stop();
                            ed2ktime += time.ElapsedMilliseconds;
                        }));

                        i++;
                    }
                    Task.WaitAll(tasks.ToArray());
                    var sortedHashes = new SortedList<int, byte[]>(hashes);
                    var digests = new byte[0];

                    foreach (byte[] subhash in sortedHashes.Values) {
                        digests = digests.Concat(subhash).ToArray();
                    }

                    MD4 md4 = MD4.Create();
                    string hash = BytesToHex(md4.ComputeHash(digests), null);

                    long time = timer.ElapsedMilliseconds;

                    ResponseStructure response = new(
                        crcEnabled ? new(BytesToHex(getCrc32(crc), null), timers[0]) : null,
                        sha1Enabled&&sha1.Hash!=null ? new(BytesToHex(sha1.Hash, null), timers[1]) : null,
                        md5Enabled&&md5.Hash!=null ? new(BytesToHex(md5.Hash, null), timers[2]) : null,
                        new(hash, ed2ktime),
                        Math.Round((bytesRead/(1024*1024))/(time/1000.0), 2),
                        timer.ElapsedMilliseconds
                    );

                    Console.WriteLine(JsonSerializer.Serialize(response, SourceGenerationContext.Default.ResponseStructure));

                    // if (crcEnabled)
                    //     Console.WriteLine("CRC32: {0} (took {1}ms)", BytesToHex(crc.Hash, null), timers[0]);
                    // if (sha1Enabled)
                    //     Console.WriteLine("SHA1: {0} (took {1}ms)", BytesToHex(sha1.Hash, null), timers[1]);
                    // if (md5Enabled)
                    //     Console.WriteLine("MD5: {0} (took {1}ms)", BytesToHex(md5.Hash, null), timers[2]);
                    // Console.WriteLine("ED2K: {0}", hash);

                    // Console.WriteLine("Overall speed: {0}MB/s in {1}ms", Math.Round((bytesRead/(1024*1024))/(time/1000.0), 2), time);
                }
            }
        }
    }
}