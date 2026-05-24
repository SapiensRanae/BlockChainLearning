using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using BlockChain.models;

namespace BlockChain.service;

public class P2PClient
{
    private readonly List<string> _peers = new List<string>(); // peerAddress -> lastSeen
    public void ConnectToPeer(string peerAddress)
    {
        if (!_peers.Contains(peerAddress))
        {
            _peers.Add(peerAddress);
            Console.WriteLine($"Connected to peer: {peerAddress}");
        }   
    }

    public async Task BroadcastTransactionAsync(Transaction transaction)
    {
        var jsonTransaction = JsonSerializer.Serialize(transaction);

        try
        {
            foreach (var peer in _peers)
            {
                var parts = peer.Split(':');
                var ipAddress = parts[0];
                var port = int.Parse(parts[1]);
                
                var client = new TcpClient();
                await client.ConnectAsync(ipAddress, port);
                
                await using var stream = client.GetStream();
                await using var writer = new StreamWriter(stream) { AutoFlush = true };
                await writer.WriteLineAsync(jsonTransaction);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}