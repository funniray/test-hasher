using System.Dynamic;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HashTest {
    public class HashOutput
    {
        public string? crc32 { get; set; }
        public string? sha1 { get; set; }
        public string? md5 { get; set; }
        public string? ed2k { get; set; }

        public HashTimers? timers { get; set; }
        public double? timeMs { get; set; }
        public double speedMBps { get; set; }
        public string? error { get; set; }
    }

    public class HashTimers
    {
        public double? crc32 { get; set; }
        public double? sha1 { get; set; }
        public double? md5 { get; set; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(HashOutput))]
    public partial class HashOutputContex : JsonSerializerContext { }

    class Program {
        static async Task<int> Main(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("Usage: testFile [--crc | -C] [--sha1 | -S] [--md5 | -M] [--all] file");
                return 1;
            }

            bool enableCRC = args.Contains("--crc") || args.Contains("-C");
            bool enableSHA = args.Contains("--sha1") || args.Contains("-S");
            bool enableMD5 = args.Contains("--md5") || args.Contains("-M");

            if (args.Contains("--all")) {
                enableCRC = true;
                enableSHA = true;
                enableMD5 = true;
            }

            var file = args[args.Length - 1];

            var hasher = new Hasher();
            try
            {
                await hasher.Hash(file, enableCRC, enableSHA, enableMD5);
            } catch (Exception e)
            {
                HashOutput output = new();
                output.error = e.ToString();
                Console.Error.WriteLine(JsonSerializer.Serialize(output, HashOutputContex.Default.HashOutput));
                return 1;
            }
            return 0;
        }
    }
}