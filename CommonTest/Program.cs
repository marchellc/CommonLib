using CommonLib;

using System.Threading.Tasks;

namespace CommonTest
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            CommonLibrary.Initialize(args);

            await Task.Delay(-1);
        }
    }
}