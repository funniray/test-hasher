using Force.Crc32;
using System.Security.Cryptography;

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

        public void Hash(string filename) {
            timers = new double[3]{0,0,0};
            var timer = System.Diagnostics.Stopwatch.StartNew();
            using (var stream = File.Open(filename, FileMode.Open)) {
                using (var reader = new BinaryReader(stream)) {
                    var tasks = new List<Task>();
                    var hashes = new System.Collections.Concurrent.ConcurrentDictionary<int, byte[]>();
                    var i = 0;

                    using var crc = new Crc32Algorithm();
                    using var sha1 = SHA1.Create();
                    using var md5 = MD5.Create();

                    Task? crcTask = null;
                    Task? sha1Task = null;
                    Task? md5Task = null;
                    
                    while (true) {
                        byte[] read = reader.ReadBytes(ED2K_BLOCK_LENGTH);
                        if (crcTask != null && sha1Task != null && md5Task != null) {
                            Task.WaitAll(new Task[3]{crcTask, sha1Task, md5Task});
                        }

                        if (read.Length <= 0) {break;}
                        var localIndex = i;
                        var isLast = read.Length < ED2K_BLOCK_LENGTH;
                    
                        crcTask = Task.Run(()=>{hashBlock(crc, read, isLast, 0);});
                        sha1Task = Task.Run(()=>{hashBlock(sha1, read, isLast, 1);});
                        md5Task = Task.Run(()=>{hashBlock(md5, read, isLast, 2);});

                        tasks.Add(Task.Run(()=>{
                            MD4 md4 = new MD4();
                            hashes.TryAdd(localIndex, md4.GetByteHashFromBytes(read));
                        }));

                        i++;
                    }

                    Console.WriteLine("Finished reading file in " + timer.ElapsedMilliseconds + "ms");
                    Task.WaitAll(tasks.ToArray());
                    var sortedHashes = new SortedList<int, byte[]>(hashes);
                    var digests = new byte[0];

                    foreach (byte[] subhash in sortedHashes.Values) {
                        digests = digests.Concat(subhash).ToArray();
                    }

                    MD4 md4 = new MD4();
                    string hash = md4.GetHexHashFromBytes(digests);

                    long time = timer.ElapsedMilliseconds;

                    var file = new FileInfo(filename);

                    Console.WriteLine("CRC32: {0} (took {1}ms)", MD4.BytesToHex(crc.Hash, null), timers[0]);
                    Console.WriteLine("SHA1: {0} (took {1}ms)", MD4.BytesToHex(sha1.Hash, null), timers[1]);
                    Console.WriteLine("MD5: {0} (took {1}ms)", MD4.BytesToHex(md5.Hash, null), timers[2]);
                    Console.WriteLine("ED2K: {0}", hash);

                    Console.WriteLine("Overall speed: {0}MB/s in {1}ms", (file.Length/(1024*1024))/(time/1000), time);
                }
            }
        }
    }
}