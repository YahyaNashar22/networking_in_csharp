namespace Networking.Cmd
{
    internal class App
    {
        string? input;
        int selectedApp;
        Dictionary<int, string> applications = new Dictionary<int, string>{
            {1, "Url Downloader"}
        };
        public int Start()
        {
            Console.WriteLine("Select the application you'd like to run:");

            foreach (KeyValuePair<int, string> app in applications)
            {
                Console.WriteLine($"{app.Key} - {app.Value}");
            }

            input = Console.ReadLine();

            if (int.TryParse(input, out int answer))
            {
                if (answer > 0 && answer <= applications.Count)
                {

                    selectedApp = answer;
                }
                else
                {
                    Console.WriteLine("please enter a valid number from the list");
                }
            }
            else
            {
                Console.WriteLine("please enter a valid number from the list");
            }

            return selectedApp;
        }
    }
}