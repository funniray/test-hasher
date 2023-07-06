namespace HashTest {
    class Program {
        static int Main(string[] args) {
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
            return 0;
        }
    }
}