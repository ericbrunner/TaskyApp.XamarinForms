using System;
using System.Net;
using System.Net.Sockets;

namespace TaskyListner
{
    class Program
    {

        public static TcpClient client;
        private static TcpListener server;
        private static string ipString;
        static void Main(string[] args)
        {

            try
            {

                ipString = "192.168.0.240";
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ipString), 1234);
                server = new TcpListener(ep);
                server.Start();

                Console.WriteLine(@"    
            ===================================================    
                   Started listening requests at: {0}:{1}    
            ===================================================",
                    ep.Address, ep.Port);

                // Buffer for reading data
                byte[] bytes = new byte[4096];

                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    client = server.AcceptTcpClient();
                    Console.WriteLine("Connected to client!" + " \n");

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        var data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine($"Received: {data}");

                        // Process the data sent by the client.
                        var serverResponse = "PONG";

                        byte[] serverResponseBytes = System.Text.Encoding.ASCII.GetBytes(serverResponse);

                        // Send back a response.
                        stream.Write(serverResponseBytes, 0, serverResponseBytes.Length);
                        Console.WriteLine($"Sent: {serverResponse}");
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
            finally
            {
                server.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();

        }
    }
}
