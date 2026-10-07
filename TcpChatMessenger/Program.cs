using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpChatMessenger
{
    class TcpChatMessenger
    {
        // Connection objects
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; set; }

        // Buffer & messaging
        public readonly int BufferSize = 2 * 1024; // 2kb
        private NetworkStream? _msgStream = null;

        // Personal data
        public readonly string Name;

        public TcpChatMessenger(string serverAddress, int port, string name)
        {
            // Create a non-connected TcpClient
            _client = new TcpClient(); // other constructors will start a connection
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;

            // Set the other things
            ServerAddress = serverAddress;
            Port = port;
            Name = name;
        }

        public void Connect()
        {
            // Try to connect
            _client.Connect(ServerAddress, Port); // Will resolve DNS
            EndPoint? endPoint = _client.Client.RemoteEndPoint;

            // Make sure we're connected
            if (_client.Connected)
            {
                // Got int!
                Console.WriteLine($"Connected to the server at {endPoint}");

                // Tell them that we're a messenger
                _msgStream = _client.GetStream();
                byte[] msgBuffer = Encoding.UTF8.GetBytes(String.Format($"name: {Name}"));
                _msgStream.Write(msgBuffer, 0, msgBuffer.Length); // Blocks

                // if we're still connected after sending our name, that means the server accepts us
                if (!_isDisconnected(_client))
                {
                    Running = true;
                }
                else
                {
                    // Name was probably taken...
                    _cleanupNetworkResources();
                    Console.WriteLine($"The server rejected us; {Name} is probably in use.");
                }
            }
            else
            {
                _cleanupNetworkResources();
                Console.WriteLine($"Wasn't able to connect to the server at {endPoint}");
            }
        }

        public void SendMessages()
        {
            bool wasRunning = Running;

            while (Running)
            {
                // Poll for user input
                Console.Write($"{Name}> ");
                string? msg = Console.ReadLine();

                // Quit or send a message
                if ((msg?.ToLower() == "quit") || (msg?.ToLower() == "exit"))
                {
                    // User wants to quit
                    Console.WriteLine("Disconnecting...");
                    Running = false;
                }
                else if (msg != string.Empty && msg is not null)
                {
                    // Send the message
                    byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);
                    _msgStream?.Write(msgBuffer, 0, msgBuffer.Length); // Blocks
                }

                // use less CPU
                Thread.Sleep(10);

                // Check the server didn't disconnect us
                if (_isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server has disconnected from us.\n:[");
                }
            }

            _cleanupNetworkResources();
            if (wasRunning)
            {
                Console.WriteLine("Disconnected.");
            }
        }

        // Cleans any leftover network resources
        private void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }

        // Checks if a socket has disconnected
        private static bool _isDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch (SocketException se)
            {
                // we got a socket error, assume it's disconnected
                Console.WriteLine(se);
                return true;
            }
        }

        public static void Main(string[] args)
        {
            // Get a name
            Console.WriteLine("Enter a name to use: ");
            string? name = Console.ReadLine();

            // Setup the Messenger
            string host = "localhost"; // args[0].Trim();
            int port = 6000;           // int.Parse(args[1].Trim());

            TcpChatMessenger messenger = new TcpChatMessenger(host, port, name ?? "Default Messenger");

            // connect and send messages
            messenger.Connect();
            messenger.SendMessages();
        }
    }
}
