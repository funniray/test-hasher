using HashTest.rhash;

namespace HashTest {
    class Program {
        static int Main(string[] args) {
            Native.rhash_library_init();
            
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
            hasher.Hash(file, enableCRC, enableSHA, enableMD5);
            // Console.WriteLine("wtf");
            // for (int i = 1; i<512; i*=2) {
            //     Hasher2.Hash(file, i);
            // }
            return 0;
        }
    }
}