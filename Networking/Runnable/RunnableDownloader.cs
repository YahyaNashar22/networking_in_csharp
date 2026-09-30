using Networking.Lib;

namespace Networking.Runnable

{
    internal class RunnableDownloader : IRunnable
    {
        public void Run()
        {
            Console.WriteLine("Enter url:");
            string? url = Console.ReadLine();

            Console.WriteLine("Enter filename:");
            string? file = Console.ReadLine();

            Console.WriteLine("Enter folder name:");
            string? folder = Console.ReadLine();

            Downloader downloader = new Downloader(url, file, folder);
            Task dlTask = downloader.DownloadWebPage();


            dlTask.GetAwaiter().GetResult();
        }

    }
}