
using System.Diagnostics;
using BlockChain.models;
using BlockChain.service;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;


var service = new ServiceCollection();
service.AddSingleton<BlockChain.service.BlockChainService>();

service.AddSingleton<BlockChain.service.P2P.P2PServer, BlockChain.service.P2P.P2PServer>();
service.AddSingleton<BlockChain.service.P2PClient, BlockChain.service.P2PClient>();
service.AddSingleton<BlockChain.service.DisplayService>();
service.AddSingleton<BlockChain.service.BlockchainExplorer>(sp => 
    new BlockChain.service.BlockchainExplorer(
        sp.GetRequiredService<BlockChain.service.BlockChainService>(),
        sp.GetRequiredService<BlockChain.service.BlockChainService>().Chain
    )
);
service.AddSingleton<BlockChain.service.CryptoService, BlockChain.service.CryptoService>();


var provider = service.BuildServiceProvider();

var blockchainService = provider.GetRequiredService<BlockChain.service.BlockChainService>();
var explorer = provider.GetRequiredService<BlockChain.service.BlockchainExplorer>();
var blockchainService2 = provider.GetRequiredService<BlockChain.service.BlockChainService>();

var p2pServer = provider.GetRequiredService<BlockChain.service.P2P.P2PServer>();
var p2pClient = provider.GetRequiredService<BlockChain.service.P2PClient>();
var displayService = provider.GetRequiredService<BlockChain.service.DisplayService>();


var cryptoService = provider.GetRequiredService<BlockChain.service.CryptoService>();

var myWallet = new Wallet(cryptoService);
Console.WriteLine("Wallet Address: " + myWallet.publicKey);
Console.WriteLine("Enter port: ");

var portInput = Console.ReadLine();
if (!int.TryParse(portInput, out var port))
{
    port = 8080;
    Console.WriteLine("Invalid port. Using default 8080.");
}

p2pServer.Start(port);

void SimulateNewCain()
{

    var secondWallet = new Wallet(cryptoService);


    blockchainService2.MinePendingTransactions(secondWallet.publicKey);
    blockchainService2.MinePendingTransactions(secondWallet.publicKey);
    blockchainService2.MinePendingTransactions(secondWallet.publicKey);
    blockchainService2.MinePendingTransactions(secondWallet.publicKey);
    blockchainService2.MinePendingTransactions(secondWallet.publicKey);
    blockchainService2.MinePendingTransactions(secondWallet.publicKey);
    blockchainService2.MinePendingTransactions(secondWallet.publicKey);

    var newChain = new List<Block>(blockchainService2.Chain);

    Console.WriteLine("Simulating new chain with one additional block...");
    blockchainService.ReplaceChain(newChain);

    try
    {
        p2pClient.BrodcastChainAsync(blockchainService.Chain).Wait();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to broadcast chain: {ex.Message}");
    }
}

void TestReport()
{
   
    var bcs = new BlockChain.service.BlockChainService();
    var testerWallet = new Wallet(cryptoService);

    for (int i = 0; i < 5; i++) bcs.MinePendingTransactions(testerWallet.publicKey);

    var b = bcs.Chain.FirstOrDefault(x => x.Index == 2);
   b.MerkleRoot = "HACKED";

   var audit = bcs.RunFullAudit(bcs.Chain);

    Console.WriteLine(bcs.GenerateForensicReport(audit, bcs.FindAttackOrigin(audit, bcs.Chain)));
}

void Benchmark(){
    var walletAlice = new Wallet(new CryptoService());

    for(int i = 0; i < 100000; i++)

    {
        blockchainService.MinePendingTransactions(walletAlice.publicKey);
    }

    var stopwatch = Stopwatch.StartNew();
    blockchainService.GetBalanceOld(walletAlice.publicKey);
    stopwatch.Stop();
    Console.WriteLine($"Old: {stopwatch.Elapsed}");
    var stopwatch2 = Stopwatch.StartNew();
    blockchainService.GetBalance(walletAlice.publicKey);
    stopwatch2.Stop();
    Console.WriteLine($"New: {stopwatch2.Elapsed}");
}

while (true)
{
   
    Console.WriteLine("\n=== Blockchain Menu ===");
    Console.WriteLine("1. mine");
    Console.WriteLine("2. send");
    Console.WriteLine("3. show blockchain");
    Console.WriteLine("4. check validity");
    Console.WriteLine("5. connect");
    Console.WriteLine("6. show mempool");
    Console.WriteLine("7. show balance");
    Console.WriteLine("8. save state (balances -> JSON)");
    Console.WriteLine("9. simulate new chain");
    Console.WriteLine("10. benchmark");
    Console.WriteLine("11. test report");
    Console.WriteLine("12. find transaction by hash");
    Console.WriteLine("0. exit");
    Console.Write("Enter command: ");
    
    var command = Console.ReadLine()?.Trim();

    switch (command)
    {
        
        case "1":
            blockchainService.MinePendingTransactions(myWallet.publicKey);
            p2pClient.BrodcastChainAsync(blockchainService.Chain).Wait();
            break;
        case "2":
            Console.Write("Enter recipient address: ");
            var recipientAddress = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(recipientAddress))
            {
                Console.WriteLine("Recipient address cannot be empty.");
                break;
            }

            Console.Write("Enter amount: ");
            if (!decimal.TryParse(Console.ReadLine(), out var amount) || amount <= 0)
            {
                Console.WriteLine("Invalid amount. Please enter a positive number.");
                break;
            }

            Console.Write("Enter fee (Min 1): ");
            if (!decimal.TryParse(Console.ReadLine(), out var fee))
            {
                Console.WriteLine("Invalid fee. Fee set to 1.");
                fee = 1;
            }

            if (fee < 1)
            {
                fee = 1;
                Console.WriteLine("Fee set to 1");
            }

            try
            {
                var tx = TransactionService.CreateTransaction(myWallet.publicKey, recipientAddress, amount, fee,
                    myWallet.privateKey);
                blockchainService.AddTransactionToMemPool(tx);
                p2pClient.BroadcastTransactionAsync(tx).Wait();
                Console.WriteLine("Transaction added to mempool and broadcasted.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Transaction failed: {e.Message}");
            }
            
            break;
        case "3":
            displayService.PrintChain(blockchainService.Chain);
            break;
        case "4":
            Console.WriteLine(blockchainService.IsValid()
                ? "Blockchain is valid."
                : "Blockchain is INVALID.");
            break;
        case "5":
            Console.WriteLine("Enter peer address (ip:port): ");
            var peerAddress = Console.ReadLine();
            if (!string.IsNullOrEmpty(peerAddress))
            {
                        p2pClient.ConnectToPeer(peerAddress);
            }
            var parts = peerAddress.Split(':');
            var resp = p2pClient.RequestChainAsync(parts[0], int.Parse(parts[1]), $"127.0.0.1:{port}").Result;
            if (resp != null && resp.Type == "NEW_CHAIN")
            {
                var newChain = JsonSerializer.Deserialize<List<Block>>(resp.Data);
                if (newChain != null)
                {
                    blockchainService.ReplaceChain(newChain);
                    Console.WriteLine("Replaced local chain with peer chain.");
                }
            }
            break;
        case "6":
            Console.WriteLine("Pending Transactions in Mempool:");
            foreach (var tx in blockchainService.PendingTransactions)
            {
                Console.WriteLine(tx.ToString());
            }
            break;
        case "7":
            Console.WriteLine("Balance: " + blockchainService.GetBalance(myWallet.publicKey));
            break;
        case "8":
            // Save state (balances and some metadata) to JSON file in current directory
            blockchainService.SaveStateSnapshot();
            break;
        case "9":
            SimulateNewCain();
            break;
        case "10":
            Benchmark();
            break;
        case "11":
            TestReport();
            break;
        case "12":
            Console.Write("Enter transaction hash: ");
            var txHash = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(txHash))            {
                Console.WriteLine("Transaction hash cannot be empty.");
                break;
            }
            var txLocation = explorer.FindTransactionLocation(txHash);
            if (txLocation.tx != null)
            {
                Console.WriteLine(txLocation.block is not null ? $"Transaction found in block index {txLocation.block.Index}:" : $"Transaction found in mempool:");
                Console.WriteLine($"From: {txLocation.tx.From}");
                Console.WriteLine($"To: {txLocation.tx.To}");
                Console.WriteLine($"Amount: {txLocation.tx.Amount}");
                Console.WriteLine($"Fee: {txLocation.tx.Fee}");
                Console.WriteLine($"Timestamp: {txLocation.tx.Timestamp}");
                
            }
            else
            {
                Console.WriteLine("Transaction not found in blockchain or mempool.");
            }
            break;
        case "0":
            Console.WriteLine("Goodbye!");
            return;
        default:
            Console.WriteLine("Unknown command. Please choose a valid menu option.");
            break;
    }
    
}
