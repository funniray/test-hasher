using Force.Crc32;
using System.Buffers.Text;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HashTest {
    class Hasher {
        private static int ED2K_BLOCK_LENGTH = 1024 * 9500;

        private double[] timers = new double[3]{0,0,0};

        private void hashBlock(HashAlgorithm alg, byte[] stream, bool final, int timerIndex) {
            var timer = System.Diagnostics.Stopwatch.StartNew();

            if (final) {
                alg.TransformFinalBlock(stream, 0, stream.Length);
            } else {
                alg.TransformBlock(stream, 0, stream.Length, null, 0);
            }

            timers[timerIndex] += timer.ElapsedMilliseconds;
        }

        public async Task Hash(string filename, bool crcEnabled, bool sha1Enabled, bool md5Enabled) {
            timers = new double[3]{0,0,0};
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var url = new Uri(filename);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(url.UserInfo)));
            var result = await client.GetStreamAsync(url);
            // var stream = await result.Content.ReadAsStreamAsync();
            long bytesRead = 0;
            using (var reader = new BinaryReader(result)) {
                var tasks = new List<Task>();
                var hashes = new System.Collections.Concurrent.ConcurrentDictionary<int, byte[]>();
                var i = 0;

                using var crc = new Crc32Algorithm();
                using var sha1 = SHA1.Create();
                using var md5 = MD5.Create();

                Task crcTask = Task.Run(()=>{});
                Task sha1Task = Task.Run(()=>{});
                Task md5Task = Task.Run(()=>{});

                while (true)
                {
                    byte[] read = reader.ReadBytes(ED2K_BLOCK_LENGTH);
                    Task.WaitAll(new Task[3] { crcTask, sha1Task, md5Task });

                    if (read.Length <= 0) { break; }
                    var localIndex = i;
                    var isLast = read.Length < ED2K_BLOCK_LENGTH;
                    bytesRead += read.Length;

                    if (crcEnabled)
                        crcTask = Task.Run(() => { hashBlock(crc, read, isLast, 0); });
                    if (sha1Enabled)
                        sha1Task = Task.Run(() => { hashBlock(sha1, read, isLast, 1); });
                    if (md5Enabled)
                        md5Task = Task.Run(() => { hashBlock(md5, read, isLast, 2); });

                    tasks.Add(Task.Run(() =>
                    {
                        MD4 md4 = new MD4();
                        hashes.TryAdd(localIndex, md4.GetByteHashFromBytes(read));
                    }));

                    i++;
                }
                
                Task.WaitAll(tasks.ToArray());
                var sortedHashes = new SortedList<int, byte[]>(hashes);
                var digests = new byte[0];

                foreach (byte[] subhash in sortedHashes.Values) {
                    digests = digests.Concat(subhash).ToArray();
                }

                MD4 md4 = new MD4();
                string hash = md4.GetHexHashFromBytes(digests);

                long time = timer.ElapsedMilliseconds;

                // var file = new FileInfo(filename);

                HashOutput output = new();
                output.timers = new();

                if (crcEnabled) 
                {
                    output.crc32 = MD4.BytesToHex(crc.Hash, null);
                    output.timers.crc32 = timers[0];
                }
                if (sha1Enabled)
                {
                    output.sha1 = MD4.BytesToHex(sha1.Hash, null);
                    output.timers.sha1 = timers[1];
                }
                if (md5Enabled)
                {
                    output.md5 = MD4.BytesToHex(md5.Hash, null);
                    output.timers.md5 = timers[2];
                }

                output.ed2k = hash;
                output.speedMBps = Math.Round((bytesRead / (1024 * 1024)) / (time / 1000.0), 2);
                output.timeMs = time;

                Console.WriteLine(JsonSerializer.Serialize(output, HashOutputContex.Default.HashOutput));
            }
        }
    }
}