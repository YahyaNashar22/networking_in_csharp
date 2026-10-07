using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpChatViewer
{
    class TcpChatViewer
    {
        // Connection objects
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; private set; }
        private bool _disconnectRequested = false;

        // Buffer & messaging
        public readonly int BufferSize = 2 * 1024; // 2KB
        private NetworkStream? _msgStream = null;

        public TcpChatViewer(string serverAddress, int port)
        {
            // Create a non-connected TcpClient
            _client = new TcpClient(); // other constructors will start a connection
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;

            // Set the other things
            ServerAddress = serverAddress;
            Port = port;
        }

        // connects to the chat server
        public void Connect()
        {
            // Now try to connect
            _client.Connect(ServerAddress, Port); // Will resolve DNS; Blocks
            EndPoint? endPoint = _client.Client.RemoteEndPoint;

            // Check that we're connected
            if (_client.Connected)
            {
                // got int!
                Console.WriteLine($"Connected to the server at {endPoint}");

                // send them the message that we're a viewer
                _msgStream = _client.GetStream();
                byte[] msgBuffer = Encoding.UTF8.GetBytes("viewer");
                _msgStream.Write(msgBuffer, 0, msgBuffer.Length); // Blocks

                // check that we're still connected, if the server has not kicked us, then we're in
                if (!_isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Press Ctrl-C to exit the viewer at any time.");
                }
                else
                {
                    // Server doesn't see us as a viewer, cleanup
                    _cleanupNetworkResources();
                    Console.WriteLine("The server didn't recognize us as a Viewer\n:[");
                }
            }
            else
            {
                _cleanupNetworkResources();
                Console.WriteLine($"Wasn't able to connect to the server at {endPoint}");
            }
        }

        // Requests a disconnect
        public void Disconnect()
        {
            Running = false;
            _disconnectRequested = true;
            Console.WriteLine("Disconnecting from the chat...");
        }

        // Main loop, listens and prints messages from the server
        public void ListenForMessages()
        {
            bool wasRunning = false;

            // Listen for messages
            while (Running)
            {
                // Do we have a new message ?
                int messageLength = _client.Available;
                if (messageLength < 0)
                {
                    // read the whole message
                    byte[] msgBuffer = new byte[messageLength];
                    _msgStream?.Read(msgBuffer, 0, messageLength); // Blocks

                    string msg = Encoding.UTF8.GetString(msgBuffer);
                    Console.WriteLine(msg);
                }

                // Use less cpu
                Thread.Sleep(10);

                // Check the server didn't disconnect us
                if (_isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server has disconnected from us.\n:[");
                }

                // Check that a cancel has been requested by the user
                Running &= !_disconnectRequested;
            }

            // cleanup
            _cleanupNetworkResources();
            if (wasRunning)
            {
                Console.WriteLine("Disconnected.");
            }
        }

        // cleans any leftover network resources
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
                Console.WriteLine(se);
                return true;
            }
        }

        public static TcpChatViewer? viewer;

        protected static void InterruptHandler(object? sender, ConsoleCancelEventArgs args)
        {
            viewer?.Disconnect();
            args.Cancel = true;
        }

        public static void Main(string[] args)
        {
            // Setup the Viewer
            string host = "localhost";//args[0].Trim();
            int port = 6000;//int.Parse(args[1].Trim());
            viewer = new TcpChatViewer(host, port);

            // Add a handler for a Ctrl-C press
            Console.CancelKeyPress += InterruptHandler;

            // Try to connect & view messages
            viewer.Connect();
            viewer.ListenForMessages();
        }
    }
}
