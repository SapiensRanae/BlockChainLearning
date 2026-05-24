
using BlockChain.models;
using BlockChain.service;
using Microsoft.Extensions.DependencyInjection;


var service = new ServiceCollection();
service.AddSingleton<BlockChain.service.BlockChainService>();
service.AddSingleton<BlockChain.service.P2PServer, BlockChain.service.P2PServer>();
service.AddSingleton<BlockChain.service.P2PClient, BlockChain.service.P2PClient>();
service.AddSingleton<BlockChain.service.DisplayService>();
service.AddSingleton<BlockChain.service.BlockchainExplorer>();
service.AddSingleton<BlockChain.service.CryptoService, BlockChain.service.CryptoService>();


var provider = service.BuildServiceProvider();

var blockchainService = provider.GetRequiredService<BlockChain.service.BlockChainService>();
var p2pServer = provider.GetRequiredService<BlockChain.service.P2PServer>();
var p2pClient = provider.GetRequiredService<BlockChain.service.P2PClient>();
var displayService = provider.GetRequiredService<BlockChain.service.DisplayService>();


var cryptoService = provider.GetRequiredService<BlockChain.service.CryptoService>();

var myWallet = new Wallet(cryptoService);
Console.WriteLine("Wallet Address: " + myWallet.publicKey);
Console.WriteLine("Enter port: ");
int port = int.Parse(Console.ReadLine() ?? "8080");

p2pServer.Start(port);


while (true)
{
    Console.WriteLine("Enter command: ");
    Console.WriteLine("1. mine");
    Console.WriteLine("2. send");
    Console.WriteLine("3. display");
    Console.WriteLine("4. connect");
    Console.WriteLine("5. show mempool");
    Console.WriteLine("6. show balance");
    Console.WriteLine("7. exit");
    
    var command = Console.ReadLine();

    switch (command)
    {
        
        case "1":
            blockchainService.MinePendingTransactions(myWallet.publicKey);
            break;
        case "2":
            Console.WriteLine("Enter recipient address: ");
            var recipientAddress = Console.ReadLine();
            Console.WriteLine("Enter amount: ");
            var amount = decimal.Parse(Console.ReadLine() ?? "0");
            Console.WriteLine("Enter fee (Min 1) : ");
            var fee = decimal.Parse(Console.ReadLine() ?? "1");
            if (fee < 1)            {
                fee = 1;
                Console.WriteLine("Fee set to 1");
            }
            try
            {
                var tx = TransactionService.CreateTransaction( myWallet.publicKey, recipientAddress, amount, fee, myWallet.privateKey);
                blockchainService.AddTransactionToMemPool(tx);
                p2pClient.BroadcastTransactionAsync(tx).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            break;
        case "3":
            displayService.DisplayChain(blockchainService.Chain);
            break;
        case "4":
            Console.WriteLine("Enter peer address (ip:port): ");
            var peerAddress = Console.ReadLine();
            if (!string.IsNullOrEmpty(peerAddress))
            {
                p2pClient.ConnectToPeer(peerAddress);
            }
            break;
        case "5":
            Console.WriteLine("Pending Transactions in Mempool:");
            foreach (var tx in blockchainService.PendingTransactions)
            {
                Console.WriteLine(tx.ToString());
            }
            break;
        case "6":
            Console.WriteLine("Balance: " + blockchainService.GetBalance(myWallet.publicKey));
            break;
    }
    
}
