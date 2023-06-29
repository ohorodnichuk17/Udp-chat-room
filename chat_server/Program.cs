using Common;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        new ChatServer().Start();
    }
}

public class ChatServer
{
    const string IP = "127.0.0.1";
    const int PORT = 3737;

    private TcpListener server = new(new IPEndPoint(IPAddress.Parse(IP), PORT));
    private HashSet<TcpClient> members = new();

    public void Start()
    {
        server.Start();

        while (true)
        {
            // waiting for the client connction...
            TcpClient client = server.AcceptTcpClient();

            Console.WriteLine($"{DateTime.Now.ToShortTimeString()}: {"..."} from {client.Client.LocalEndPoint}");
            
            Task.Run(() => ListenClient(client));
        }
    }

    public void ListenClient(TcpClient client)
    {
        bool isActive = true;

        while (isActive)
        {
            var stream = client.GetStream();

            var formatter = new BinaryFormatter();
            var request = (ClientCommand)formatter.Deserialize(stream);

            Console.WriteLine($"Client request: {request.Type} | {request.Text ?? "..."}");

            switch (request.Type)
            {
                case CommandType.Join:
                    members.Add(client);
                    break;
                case CommandType.Leave:
                    members.Remove(client);
                    isActive = false;
                    break;
                case CommandType.Message:
                    foreach (var member in members)
                    {
                        request.UserName = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                        request.Timestamp = DateTime.Now;
                        SendCommand(request, member);
                    }
                    break;
                case CommandType.Like:
                case CommandType.Dislike:
                    foreach (var member in members)
                        SendCommand(request, member);
                    break;
            }
        }

        client.Close();
    }

    public void SendCommand(ClientCommand command, TcpClient client)
    {
        NetworkStream ns = client.GetStream();
        var formatter = new BinaryFormatter();
        formatter.Serialize(ns, command);
        ns.Flush();
    }
}