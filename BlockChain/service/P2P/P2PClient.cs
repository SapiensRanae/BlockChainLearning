using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using BlockChain.models;

namespace BlockChain.service;

public class P2PClient
{
    private readonly List<string> _peers = new List<string>(); // peerAddress -> lastSeen
    // cache recent chain broadcasts (signature -> timestamp) to avoid ping-pong
    private readonly Dictionary<string, DateTime> _recentChainBroadcasts = new Dictionary<string, DateTime>();
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
        var message = new NetworkMessage("NEW_TRANSACTION", jsonTransaction);
        jsonTransaction = JsonSerializer.Serialize(message);

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

    public async Task<NetworkMessage?> RequestChainAsync(string ip, int port, string? selfAddress = null)
    {
        try
        {
            var message = new NetworkMessage("REQUEST_CHAIN", selfAddress ?? "");
            var jsonMessage = JsonSerializer.Serialize(message);

            using var client = new TcpClient();
            await client.ConnectAsync(ip, port);

            await using var stream = client.GetStream();
        
            using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true) { AutoFlush = true })
            {
                await writer.WriteLineAsync(jsonMessage);
                await writer.FlushAsync();
            }

         
            using var reader = new StreamReader(stream);
            var responseLine = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(responseLine)) return null;
            var response = JsonSerializer.Deserialize<NetworkMessage>(responseLine);
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to request chain from {ip}:{port}: {e.Message}");
            return null;
        }
    }
    
    public async Task BrodcastChainAsync(List<Block> chain)
    {
    
        var last = chain.LastOrDefault();
        var signature = last != null ? last.Hash + ":" + chain.Count : "empty";

        // avoid broadcasting the same chain multiple times in short interval
        if (_recentChainBroadcasts.TryGetValue(signature, out var when))
        {
            if ((DateTime.UtcNow - when).TotalSeconds < 5)
            {
                
                return;
            }
        }

        var jsonChain = JsonSerializer.Serialize(chain);
        var message = new NetworkMessage("NEW_CHAIN", jsonChain);
        var jsonMessage = JsonSerializer.Serialize(message);

        try
        {
            foreach (var peer in _peers)
            {
                var parts = peer.Split(':');
                var ipAddress = parts[0];
                var port = int.Parse(parts[1]);
                
                using var client = new TcpClient();
                await client.ConnectAsync(ipAddress, port);
                
                await using var stream = client.GetStream();
                await using var writer = new StreamWriter(stream) { AutoFlush = true };
                await writer.WriteLineAsync(jsonMessage);
            }
            // record timestamp of this broadcast signature
            _recentChainBroadcasts[signature] = DateTime.UtcNow;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    public void DisconnectFromPeer(string peerAddress)
    {
        _peers.Remove(peerAddress);
        Console.WriteLine($"Disconnected from peer: {peerAddress}");
    }
    
}