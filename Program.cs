namespace HashTest {
    class Program {
        static void Main(string[] args) {
            var hasher = new Hasher();

            hasher.Hash(args[0]);
        }
    }
}