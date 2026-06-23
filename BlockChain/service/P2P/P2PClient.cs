using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using BlockChain.models;

namespace BlockChain.service;

public class P2PClient
{
    private readonly BlockChainService _blockChainService;
    private readonly List<string> _peers = new List<string>(); 
    private List<string> _peersToRemove = new List<string>(); 
    private const string PeersFile = "peers.json";

    private readonly Dictionary<string, DateTime> _recentChainBroadcasts = new Dictionary<string, DateTime>();

    private readonly Dictionary<string, DateTime> _recentTransactions = new Dictionary<string, DateTime>();

    public P2PClient(BlockChainService blockChainService)
    {
        _blockChainService = blockChainService;
    }
    public void ConnectToPeer(string peerAddress)
    {
        if (!_peers.Contains(peerAddress))
        {
            _peers.Add(peerAddress);
            SavePeers();
            Console.WriteLine($"Connected to peer: {peerAddress}");
        }   
    }

    private void SavePeers()
    {
        File.WriteAllText(PeersFile, JsonSerializer.Serialize(_peers));
    }

    private void LoadPeers()
    {
        if (File.Exists(PeersFile))
        {
            var loaded = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(PeersFile));
            if (loaded != null)
            {
                foreach (var peer in loaded)
                {
                    if (!_peers.Contains(peer)) _peers.Add(peer);
                }
            }
        }
    }

    public async Task InitPeersAsync()
    {
        LoadPeers();
        if (_peers.Count == 0) return;

        var tasks = _peers.ToList().Select(async peer =>
        {
            try
            {
                var parts = peer.Split(':');
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(parts[0], int.Parse(parts[1]));
                if (await Task.WhenAny(connectTask, Task.Delay(1000)) == connectTask)
                {
                    await connectTask;
                    Console.WriteLine($"Restored connection to peer: {peer}");
                }
            }
            catch
            {
            }
        });
        await Task.WhenAll(tasks);
    }
    

    public async Task BroadcastTransactionAsync(Transaction transaction)
    {
        if (_recentTransactions.ContainsKey(transaction.Id) && (DateTime.UtcNow - _recentTransactions[transaction.Id]).TotalMinutes < 5)
        {
            return;
        }
        _recentTransactions[transaction.Id] = DateTime.UtcNow;

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
            _peersToRemove.AddRange(_peers); 
        }
        if (_peersToRemove.Count > 0)
        {
            foreach (var peer in _peersToRemove)
            {
                _peers.Remove(peer);
                Console.WriteLine($"Removed unreachable peer: {peer}");
            }
            _peersToRemove.Clear();
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
    
    public async Task BroadcastChainAsync(List<Block> chain)
    {
        var message = new NetworkMessage("NEW_CHAIN", JsonSerializer.Serialize(chain));
        await BroadcastMessageAsync(message);
    }
    
    public async Task BroadcastMessageAsync(NetworkMessage message)
    {
        var jsonMessage = JsonSerializer.Serialize(message);
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
                await writer.WriteLineAsync(jsonMessage);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            _peersToRemove.AddRange(_peers); 
        }
        
        
        if (_peersToRemove.Count > 0)
        {
            foreach (var peer in _peersToRemove)
            {
                _peers.Remove(peer);
                Console.WriteLine($"Removed unreachable peer: {peer}");
            }
            _peersToRemove.Clear();
        }
        
        
    }
    
    public void BroadcastTransactionFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new Exception("Transaction file not found.");
        }

        var transaction = JsonSerializer.Deserialize<Transaction>(File.ReadAllText(filePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (transaction == null)
        {
            throw new Exception("Invalid transaction file.");
        }

        if (!CryptoService.VerifySignature(transaction.ToRawString(), transaction.Signature, transaction.From))
        {
            throw new Exception("Invalid transaction signature.");
        }

        if (transaction.From != "COINBASE" && transaction.From != "MINT" && _blockChainService.GetBalance(transaction.From) < transaction.Amount + transaction.Fee)
        {
            throw new Exception("Insufficient balance.");
        }

        _blockChainService.AddTransactionToMemPool(transaction);
        BroadcastTransactionAsync(transaction).GetAwaiter().GetResult();
    }
    
    public void DisconnectFromPeer(string peerAddress)
    {
        _peers.Remove(peerAddress);
        Console.WriteLine($"Disconnected from peer: {peerAddress}");
    }
    
}