using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpChatServer
{
    class TcpChatServer
    {
        // what listens in
        private TcpListener _listener;

        // types of clients connected
        private List<TcpClient> _viewers = new List<TcpClient>();
        private List<TcpClient> _messengers = new List<TcpClient>();

        // Names that are taken by other _messengers
        private Dictionary<TcpClient, string> _names = new Dictionary<TcpClient, string>();

        // Messages that need to be sent
        private Queue<string> _messageQueue = new Queue<string>();

        // Extra fun data
        public readonly string ChatName;
        public readonly int Port;
        public bool Running { get; private set; }

        // Buffer
        public readonly int BufferSize = 2 * 1024; // 2kb

        // Make a new TCP chat server, with our provided name
        public TcpChatServer(string chatName, int port)
        {
            // Set the basic data
            ChatName = chatName;
            Port = port;
            Running = false;

            // Make the listener listen for connections on any new network device
            _listener = new TcpListener(IPAddress.Any, Port);
        }

        // If the server is running, this will shut down the server
        public void ShutDown()
        {
            Running = false;
            Console.WriteLine("shutting down server");
        }

        // Start running the server. Will stop when `shutdown()` has been called
        public void Run()
        {
            // some info
            Console.WriteLine($"Starting the {ChatName} on port {Port}");
            Console.WriteLine("Press Ctrl-C to shut down the server at any time.");

            // Make the server run
            _listener.Start(); // No backlog
            Running = true;

            // Main server loop
            while (Running)
            {
                // Check for new clients
                if (_listener.Pending())
                {
                    _handleNewConnection();

                }
                // Do the rest
                _checkForDisconnects();
                _checkForNewMessages();
                _sendMessages();

                // use less cpu
                Thread.Sleep(10);

            }
            // Stop the server, and clean up any connected clients
            foreach (TcpClient v in _viewers)
            {
                _cleanupClient(v);
            }
            foreach (TcpClient m in _messengers)
            {
                _cleanupClient(m);
            }

            _listener.Stop();

            // Some info
            Console.WriteLine("server is shutdown");
        }

        private void _handleNewConnection()
        {
            // There is (at least) one, see what they want
            bool good = false;
            TcpClient newClient = _listener.AcceptTcpClient(); // Blocks
            NetworkStream networkStream = newClient.GetStream();

            // Modify the default buffer sizes
            newClient.SendBufferSize = BufferSize;
            newClient.ReceiveBufferSize = BufferSize;

            // print some info
            EndPoint? endPoint = newClient.Client.RemoteEndPoint;
            Console.WriteLine($"Handling a new client from ${endPoint}");

            // Let them identify themselves
            byte[] msgBuffer = new byte[BufferSize];
            int bytesRead = networkStream.Read(msgBuffer, 0, msgBuffer.Length);
            Console.WriteLine($"Got {bytesRead} bytes");

            if (bytesRead > 0)
            {
                string msg = Encoding.UTF8.GetString(msgBuffer, 0, bytesRead);

                if (msg == "viewer")
                {
                    // They just want to watch
                    good = true;
                    _viewers.Add(newClient);

                    Console.WriteLine($"{endPoint} is a viewer");

                    // send them a hello message
                    msg = String.Format($"Welcome to the {ChatName} chat server!");
                    msgBuffer = Encoding.UTF8.GetBytes(msg);
                    networkStream.Write(msgBuffer, 0, msgBuffer.Length); // Blocks
                }
                else if (msg.StartsWith("name:"))
                {
                    // Okay they might be a messenger
                    string name = msg.Substring(msg.IndexOf(':') + 1);

                    if ((name != string.Empty) && (!_names.ContainsValue(name)))
                    {
                        good = true;
                        _names.Add(newClient, name);
                        _messengers.Add(newClient);

                        Console.WriteLine($"{endPoint} is a messenger with the name: {name}");

                        // Tell the viewers we have a new messenger
                        _messageQueue.Enqueue(String.Format($"{name} has joined the chat"));
                    }
                }
                else
                {
                    // wasn't either a viewer or messenger, clean up anyways
                    Console.WriteLine($"wasn't able to identify {endPoint} as a viewer or a messenger");
                    _cleanupClient(newClient);
                }
            }

            // Do we really want them ?
            if (!good)
            {
                newClient.Close();
            }
        }


        // Sees if any of the clients have left the chat server
        private void _checkForDisconnects()
        {
            // check the viewers first
            foreach (TcpClient v in _viewers.ToArray())
            {
                if (_isDisconnected(v))
                {
                    Console.WriteLine($"Viewer {v.Client.RemoteEndPoint}");

                    // cleanup on our end
                    _viewers.Remove(v); // Remove from list
                    _cleanupClient(v);
                }
            }

            // check the messengers second
            foreach (TcpClient m in _messengers.ToArray())
            {
                if (_isDisconnected(m))
                {
                    // Get info about the messenger
                    string name = _names[m];

                    // Tell the viewers someone has left
                    Console.WriteLine($"messenger {name} has left the chat");
                    _messageQueue.Enqueue(String.Format($"{name} has left the chat"));

                    // clean up on our end
                    _messengers.Remove(m);
                    _names.Remove(m);
                    _cleanupClient(m);
                }
            }
        }


        // see iif any of our messengers have sent us a new message, put it in the queue
        private void _checkForNewMessages()
        {
            foreach (TcpClient m in _messengers)
            {
                int messageLength = m.Available;
                if (messageLength > 0)
                {
                    // there is one . . get it
                    byte[] msgBuffer = new byte[messageLength];
                    NetworkStream stream = m.GetStream();

                    int bytesRead = stream.Read(msgBuffer, 0, msgBuffer.Length);

                    // Attach a name to it and shove it into the queue
                    string message = Encoding.UTF8.GetString(msgBuffer, 0, bytesRead);
                    string msg = $"{_names[m]}: {message}";
                    _messageQueue.Enqueue(msg);
                }
            }

        }

        // clears out the message queue and sends it to all of the viewers
        private void _sendMessages()
        {
            foreach (string msg in _messageQueue)
            {
                // Encode the message
                byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);

                // send the message to each viewer
                foreach (TcpClient v in _viewers)
                {
                    v.GetStream().Write(msgBuffer, 0, msgBuffer.Length);
                }
            }

            // clear out the queue
            _messageQueue.Clear();
        }

        // checks if a socket has disconnected
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
                // we go a socket error, assume it's disconnected
                return true;
            }
        }

        // cleans up resources for a TcpClient
        private void _cleanupClient(TcpClient client)
        {
            client.GetStream().Close(); // close network stream
            client.Close(); // Close client
        }

    }

    class Program
    {
        public static TcpChatServer chat = null!;
        protected static void InterruptHandler(object? sender, ConsoleCancelEventArgs args)
        {
            chat.ShutDown();
            args.Cancel = true;
        }

        public static void Main(string[] args)
        {
            // Create the server
            string name = "Bad IRC";
            int port = 6000;
            chat = new TcpChatServer(name, port);

            // add a handler for a Ctrl-C press
            Console.CancelKeyPress += InterruptHandler;

            // run the chat server
            chat.Run();
        }
    }
}