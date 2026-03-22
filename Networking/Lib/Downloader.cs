using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

namespace Networking.Lib
{
    internal class Downloader
    {
        private string urlToDownload;
        private string fileName;
        private string downloadFolder;
        public Downloader(string? url, string? filename, string? folder)
        {
            urlToDownload = string.IsNullOrWhiteSpace(url) ? "https://16bpp.net/" : url;
            fileName = string.IsNullOrWhiteSpace(filename) ? "index.html" : filename;
            downloadFolder = string.IsNullOrWhiteSpace(folder) ? "downloads" : folder;

            Directory.CreateDirectory(downloadFolder);
        }
        public async Task DownloadWebPage()
        {
            Console.WriteLine("Starting download . . .");

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage res = await httpClient.GetAsync(urlToDownload);

                    // if we get a 200 response, then save it
                    if (res.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Got it . . .");

                        // Get the data
                        byte[] data = await res.Content.ReadAsByteArrayAsync();

                        // Save it to file
                        string filePath = Path.Combine(".", downloadFolder, fileName);
                        FileStream fStream = File.Create(filePath);
                        await fStream.WriteAsync(data, 0, data.Length);
                        fStream.Close();

                        Console.WriteLine("Done!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong!");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
