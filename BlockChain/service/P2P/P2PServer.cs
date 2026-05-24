using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using BlockChain.models;

namespace BlockChain.service;

public class P2PServer
{
    private readonly BlockChainService _blockchainService;

    public P2PServer(BlockChainService blockchainService)
    {
        _blockchainService = blockchainService;
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
            var jsonLine = await reader.ReadLineAsync();
            if (!string.IsNullOrEmpty(jsonLine))
            {
                var tx = JsonSerializer.Deserialize<Transaction>(jsonLine);
                if (tx != null && _blockchainService.PendingTransactions.Contains(tx) == false)
                {
                    Console.WriteLine($"Received transaction from peer: {tx.Id}");
                    _blockchainService.AddTransactionToMemPool(tx);
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