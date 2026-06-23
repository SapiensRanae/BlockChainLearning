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
                    if (tx != null && !_blockchainService.PendingTransactions.Any(t => t.Id == tx.Id))
                    {
                        _blockchainService.AddTransactionToMemPool(tx);
                        Console.WriteLine($"Received new transaction {tx.Id}, propagating to peers.");
                        _p2PClient.BroadcastTransactionAsync(tx).Wait();
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
                            await _p2PClient.BroadcastChainAsync(_blockchainService.Chain);
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

                if (message.Type == "REQUEST_SPV_PROOF")
                {
                    var txId = message.Data;
                    var block = _blockchainService.Chain.FirstOrDefault(b => b.Transactions.Any(t => t.Id == txId));
                    if (block != null)
                    {
                        var proof = _hashingService.GetMerkleProof(block.Transactions, txId);
                        var spvData = new { TransactionId = txId, Proof = proof, ExpectedRoot = block.MerkleRoot, Transactions = block.Transactions };
                        var response = new NetworkMessage("SPV_RESULT", JsonSerializer.Serialize(spvData));
                        await writer.WriteLineAsync(JsonSerializer.Serialize(response));
                    }
                }

                if (message.Type == "SPV_RESULT")
                {
                    var data = JsonSerializer.Deserialize<JsonElement>(message.Data);
                    var txId = data.GetProperty("TransactionId").GetString();
                    var expectedRoot = data.GetProperty("ExpectedRoot").GetString();
                    var proof = JsonSerializer.Deserialize<List<string>>(data.GetProperty("Proof").GetRawText());
                    var txs = JsonSerializer.Deserialize<List<Transaction>>(data.GetProperty("Transactions").GetRawText());

                    bool rootExists = _blockchainService.Chain.Any(b => b.MerkleRoot == expectedRoot);
                    if (!rootExists)
                    {
                        Console.WriteLine("[SPV ALERT] Full node attempted to provide a fake Merkle root! Proof rejected.");
                        client.Close();
                        return;
                    }

                    bool isValid = _hashingService.VerifyMerkleProof(txId, proof, expectedRoot);
                    if (isValid)
                        Console.WriteLine($"[SPV] Transaction {txId} is VERIFIED in blockchain.");
                    else
                        Console.WriteLine($"[SPV] Transaction {txId} verification FAILED.");
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