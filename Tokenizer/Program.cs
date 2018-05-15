namespace Tokenizer
{
    using System;

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                PrintUsage();
                return 0;
            }

            var tokenizer = new Tokenizer(args[0], args[1], args[2]);
            
            return tokenizer.Run();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tTokenizer.exe template-file token-file output-file");
        }
    }
}
