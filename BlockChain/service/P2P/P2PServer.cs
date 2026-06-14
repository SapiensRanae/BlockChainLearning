using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using BlockChain.models;
using BlockChain.service;

namespace BlockChain.service.P2P;

public class P2PServer
{
    private readonly BlockChainService _blockchainService;
    private readonly P2PClient _p2PClient;
    private readonly HashingService _hashingService;

    public P2PServer(BlockChainService blockchainService, P2PClient p2PClient, HashingService hashingService)
    {
        _blockchainService = blockchainService;
        _p2PClient = p2PClient;
        _hashingService = hashingService;
    }

    public void Start(int port)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Listening on port {port}");

        Task.Run(async () =>
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Peer connected");
                _ = HandleClientAsync(client);
            }
        });
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            await using var writer = new StreamWriter(stream) { AutoFlush = true };

            var jsonLine = await reader.ReadLineAsync();
            
            if (!string.IsNullOrEmpty(jsonLine))
            {
                var message = JsonSerializer.Deserialize<NetworkMessage>(jsonLine);
                if( message == null)
                {
                    Console.WriteLine("Received invalid message");
                    return;
                }

                if (message.Type == "NEW_TRANSACTION")
                {
                    var tx = JsonSerializer.Deserialize<Transaction>(message.Data);
                    if (tx != null && !_blockchainService.PendingTransactions.Contains(tx))
                    {
                        _blockchainService.AddTransactionToMemPool(tx);
                        Console.WriteLine($"Received new transaction {tx.Id}, propagating to peers.");
                    }
                }

                if (message.Type == "REQUEST_CHAIN")
                {
                    
                    var jsonChain = JsonSerializer.Serialize(_blockchainService.Chain);
                    var response = new NetworkMessage("NEW_CHAIN", jsonChain);
                    await writer.WriteLineAsync(JsonSerializer.Serialize(response));
                   
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        _p2PClient.ConnectToPeer(message.Data);
                    }
                }
                if (message.Type == "NEW_CHAIN")
                {
                    var newChain = JsonSerializer.Deserialize<List<Block>>(message.Data);
                    if (newChain != null)
                    {
                        var before = _blockchainService.Chain.Count;
                        _blockchainService.ReplaceChain(newChain);

                        if (_blockchainService.Chain.Count > before)
                        {
                            Console.WriteLine($"Adopted new chain (length { _blockchainService.Chain.Count }), propagating to peers.");
                            await _p2PClient.BrodcastChainAsync(_blockchainService.Chain);
                        }
                        else
                        {
                            if (newChain != _blockchainService.Chain)
                            {
                                Console.WriteLine("Received new chain but did not adopt it (not longer).");
                            }
                            
                        }
                    }
                }

                if (message.Type == "NEW_BLOCK")
                {
                    var newBlock = JsonSerializer.Deserialize<Block>(message.Data);
                    if (newBlock != null)
                    {
                        var lastBlock = _blockchainService.Chain.LastOrDefault();

                        if (lastBlock != null &&
                            lastBlock.Hash == newBlock.PreviousHash &&
                            _hashingService.ComputeHash(newBlock) == newBlock.Hash)
                        {
                            _blockchainService.Chain.Add(newBlock);
                            _blockchainService.ValidateAndRebuildState();

                            var includedTxIds = newBlock.Transactions
                                .Select(t => t.Id)
                                .ToHashSet();

                            _blockchainService.PendingTransactions.RemoveAll(
                                t => includedTxIds.Contains(t.Id));
                        }
                    }
                }

            }
        }

        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            client.Close();
        }
    }
}