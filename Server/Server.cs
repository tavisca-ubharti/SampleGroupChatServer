using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class Server
    {
        private static Dictionary<Socket, string> _connectionList = new Dictionary<Socket, string>();

        public static void StartServer()
        {

            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHost.AddressList[1];
            Console.WriteLine("Server Ip Address : " + ipAddress);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 8080);
            Socket connectionListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                connectionListener.Bind(localEndPoint);
                connectionListener.Listen(100);
                Console.WriteLine("Waiting connection ... ");
                while (true)
                {
                    var clientHandler = connectionListener.Accept();
                    var userName = string.Empty;
                    while (userName.Trim().Equals(string.Empty))
                    {
                        if (_connectionList.ContainsKey(clientHandler))
                            break;
                        clientHandler.Send(Encoding.ASCII.GetBytes("Enter your name"));
                        var message = new byte[1024];
                        var numByte = clientHandler.Receive(message);
                        userName = Encoding.ASCII.GetString(message, 0, numByte);
                        if (!userName.Trim().Equals(string.Empty))
                        {
                            _connectionList[clientHandler] = userName;
                            clientHandler.Send(Encoding.ASCII.GetBytes("You have logged in..."));
                            Console.WriteLine(userName + " join the chat");
                            BroadcastMessage(clientHandler, userName + " join the chat");
                        }

                    }
                    Task.Factory.StartNew(() => RecieveMessage(clientHandler));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void RecieveMessage(Socket clientSocket)
        {
            if (clientSocket == null)
                return;
            var messageRecieved = new Byte[1024];
            var messageToBeBroadcast = string.Empty;
            while (true)
            {
                int numByte = clientSocket.Receive(messageRecieved);
                messageToBeBroadcast = string.Format("{0} : {1}", _connectionList[clientSocket], Encoding.ASCII.GetString(messageRecieved, 0, numByte));
                BroadcastMessage(clientSocket, messageToBeBroadcast);
                if (Encoding.ASCII.GetString(messageRecieved, 0, numByte).Equals("bye"))
                {
                    clientSocket.Send(Encoding.ASCII.GetBytes("bye"));
                    messageToBeBroadcast = string.Format("{0} left the chat", _connectionList[clientSocket]);
                    Console.WriteLine("{0} left the chat", _connectionList[clientSocket]);
                    BroadcastMessage(clientSocket, messageToBeBroadcast);
                    _connectionList.Remove(clientSocket);
                    break;
                }
            }
        }

        private static void BroadcastMessage(Socket clientSocket, string messageToBeBroadcast)
        {
            var message = Encoding.ASCII.GetBytes(messageToBeBroadcast);
            Socket removeSocket = null;
            foreach (var client in _connectionList.Keys)
            {
                if (client != clientSocket)
                {
                    try
                    {
                        client.Send(message);
                    }
                    catch (Exception)
                    {
                        removeSocket = client;
                    }
                }
            }
            if (removeSocket != null)
            {
                messageToBeBroadcast = string.Format("{0} left the chat", _connectionList[removeSocket]);
                _connectionList.Remove(removeSocket);
                removeSocket = null;
                BroadcastMessage(clientSocket, messageToBeBroadcast);
            }
        }
    }
}
