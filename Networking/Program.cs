using Networking.Cmd;
using Networking.Runnable;

namespace Networking
{
    internal class Program
    {
        static void Main(string[] args)
        {
            App app = new App();
            IRunnable? runnable = null;
            int selectedApp = app.Start();

            switch (selectedApp)
            {
                case 1:
                    runnable = new RunnableDownloader();
                    break;
                default:
                    Console.WriteLine("No app has been selected");
                    break;
            }

            Console.WriteLine("============================");
            Console.WriteLine(runnable?.GetType().Name);
            Console.WriteLine("============================");
            runnable?.Run();
        }
    }
}
