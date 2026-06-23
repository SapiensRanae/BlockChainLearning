
using System.Diagnostics;
using BlockChain.models;
using BlockChain.service;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;


Console.WriteLine("Enter port: ");

var portInput = Console.ReadLine();
if (!int.TryParse(portInput, out var port))
{
    port = 8080;
    Console.WriteLine("Invalid port. Using default 8080.");
}
bool isSPV;
Console.WriteLine("1. Full Node \n 2. SPV Client");
var choiceInput = Console.ReadLine();
var choice = int.TryParse(choiceInput, out var parsedChoice) ? parsedChoice : 1;
switch (choice)
{
    case 1:
        isSPV = false;
        break;
    case 2:
        isSPV = true;
        break;
    default:
        Console.WriteLine("Invalid choice. Defaulting to Full Node.");
        isSPV = false;
        break;
}

var service = new ServiceCollection();
service.AddSingleton<BlockChainService>();
service.AddSingleton<HashingService>();

service.AddSingleton<BlockChain.service.P2P.P2PServer, BlockChain.service.P2P.P2PServer>();
service.AddSingleton<P2PClient>();
service.AddSingleton<DisplayService>();
service.AddSingleton<BlockchainExplorer>(sp =>
    new BlockchainExplorer(
        sp.GetRequiredService<BlockChainService>()
    )
);
service.AddSingleton<CryptoService, CryptoService>();


var provider = service.BuildServiceProvider();

var blockchainService = provider.GetRequiredService<BlockChainService>();
var explorer = provider.GetRequiredService<BlockchainExplorer>();
var blockchainService2 = provider.GetRequiredService<BlockChainService>();

var p2pServer = provider.GetRequiredService<BlockChain.service.P2P.P2PServer>();
var p2pClient = provider.GetRequiredService<P2PClient>();
var displayService = provider.GetRequiredService<DisplayService>();


var cryptoService = provider.GetRequiredService<CryptoService>();

var myWallet = new Wallet(cryptoService);
Console.WriteLine("Wallet Address: " + myWallet.publicKey);

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
        p2pClient.BroadcastChainAsync(blockchainService.Chain).Wait();
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





p2pServer.Start(port);
p2pClient.InitPeersAsync().Wait();

while (true)
{
    if (!isSPV)
    {


        
        Console.WriteLine("\n=== Blockchain Menu ===");
        Console.WriteLine($"Current Node Port: {port}");
        Console.WriteLine("1. Mine blocks");
        Console.WriteLine("2. Send transaction");
        Console.WriteLine("3. Show blockchain");
        Console.WriteLine("4. Check validity");
        Console.WriteLine("5. Connect to peer");
        Console.WriteLine("6. Show mempool");
        Console.WriteLine("7. Show balance");
        Console.WriteLine("8. SPV Transaction Verification");
        Console.WriteLine("9. Show all balances");
        Console.WriteLine("10. Create offline transaction (Cold Wallet)");
        Console.WriteLine("11. Broadcast transaction from file");
        Console.WriteLine("12. Mint own token");
        Console.WriteLine("13. View wallet history");
        Console.WriteLine("14. Find transaction by ID");
        Console.WriteLine("15. Save state (balances -> JSON)");
        Console.WriteLine("0. Exit");
        Console.Write("Enter command: ");

        var command = Console.ReadLine()?.Trim();

        switch (command)
        {

            case "1":
                blockchainService.MinePendingTransactions(myWallet.publicKey);
                p2pClient.BroadcastChainAsync(blockchainService.Chain).Wait();
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
                
                Console.Write("Enter token symbol (default MAIN): ");
                var tokenSymbol = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(tokenSymbol)) tokenSymbol = "MAIN";

                try
                {
                    var tx = TransactionService.CreateTransaction(myWallet.publicKey, recipientAddress, amount, fee,
                        myWallet.privateKey, tokenSymbol);
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
                Console.WriteLine(blockchainService.IsValid(blockchainService.Chain)
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

                if (string.IsNullOrWhiteSpace(peerAddress) || !peerAddress.Contains(':'))
                {
                    Console.WriteLine("Invalid peer address.");
                    break;
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
                var blockForSpv = blockchainService.Chain.LastOrDefault(b => b.Transactions.Count > 0);
                if (blockForSpv == null)
                {
                    Console.WriteLine("No blocks with transactions found.");
                    break;
                }
                var targetTx = blockForSpv.Transactions[0];
                var proof = provider.GetRequiredService<HashingService>().GetMerkleProof(blockForSpv.Transactions, targetTx.Id);
                var isValidSpv = provider.GetRequiredService<HashingService>().VerifyMerkleProof(
                    provider.GetRequiredService<HashingService>().ComputeHash(targetTx.ToRawString()), 
                    proof, 
                    blockForSpv.MerkleRoot);
                Console.WriteLine($"Transaction ID: {targetTx.Id}");
                Console.WriteLine($"Merkle Proof verification: {isValidSpv}");
                break;
            case "9":
                if (blockchainService.BalanceCash.TryGetValue(myWallet.publicKey, out var balances))
                {
                    Console.WriteLine($"Balances for {myWallet.publicKey}:");
                    foreach (var kvp in balances)
                    {
                        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    }
                }
                else
                {
                    Console.WriteLine("No balances found for this wallet.");
                }
                break;
            case "10":
                Console.Write("Enter recipient address: ");
                var coldRecipient = Console.ReadLine()?.Trim();
                Console.Write("Enter amount: ");
                if (!decimal.TryParse(Console.ReadLine(), out var coldAmount)) break;
                Console.Write("Enter fee: ");
                if (!decimal.TryParse(Console.ReadLine(), out var coldFee)) break;
                Console.Write("Enter token symbol (default MAIN): ");
                var coldToken = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(coldToken)) coldToken = "MAIN";
                Console.Write("Enter file path to save transaction: ");
                var coldPath = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(coldPath)) coldPath = Path.Combine(AppContext.BaseDirectory, "offline_transaction.json");;

                try
                {
                    var coldTx = new Transaction(myWallet.publicKey, coldRecipient, coldAmount, coldFee, 0, coldToken);
                    TransactionService.SignTransaction(coldTx, myWallet.privateKey);
                    File.WriteAllText(coldPath, JsonSerializer.Serialize(coldTx, new JsonSerializerOptions { WriteIndented = true }));
                    Console.WriteLine($"Offline transaction created and saved to {coldPath}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to create offline transaction: {e.Message}");
                }
                break;
            case "11":
                Console.Write("Enter transaction file path: ");
                var filePath = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(filePath)) break;
                try
                {
                    p2pClient.BroadcastTransactionFromFile(filePath);
                    Console.WriteLine("Transaction broadcasted successfully.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Broadcast failed: {e.Message}");
                }
                break;
            case "12":
                Console.Write("Enter token symbol to mint: ");
                var mintSymbol = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(mintSymbol)) break;
                Console.Write("Enter amount to mint: ");
                if (!decimal.TryParse(Console.ReadLine(), out var mintAmount)) break;

                try
                {
                    var mintTx = new Transaction("MINT", myWallet.publicKey, mintAmount, 0, 0, mintSymbol);
                    blockchainService.AddTransactionToMemPool(mintTx);
                    p2pClient.BroadcastTransactionAsync(mintTx).Wait();
                    Console.WriteLine($"Mint transaction for {mintAmount} {mintSymbol} added to mempool.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Minting failed: {e.Message}");
                }
                break;
            case "13":
                var history = explorer.GetTransactionHistory(myWallet.publicKey);
                Console.WriteLine($"Transaction history for {myWallet.publicKey}:");
                if (history.Count == 0)
                {
                    Console.WriteLine("No transactions found.");
                }
                else
                {
                    foreach (var hTx in history)
                    {
                        Console.WriteLine(hTx.ToString());
                    }
                }
                break;
            case "14":
                Console.Write("Enter transaction ID: ");
                var txId = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(txId)) break;

                var foundTx = explorer.FindTransactionById(txId);
                if (foundTx != null)
                {
                    var block = explorer.FindBlockByTransactionId(txId);
                    Console.WriteLine(block != null ? $"Found in block {block.Index}:" : "Found in mempool:");
                    Console.WriteLine(foundTx.ToString());
                }
                else
                {
                    Console.WriteLine("Transaction not found.");
                }
                break;
            case "15":
                blockchainService.SaveStateSnapshot();
                break;
            case "0":
                Console.WriteLine("Goodbye!");
                return;
            default:
                Console.WriteLine("Unknown command. Please choose a valid menu option.");
                break;
        }
    }
    else
    {
        Console.WriteLine("\n=== SPV Client Menu ===");
        Console.WriteLine("1. connect to peer");
        Console.WriteLine("2. create transaction");
        Console.WriteLine("3. ask for SPV proof");
        Console.WriteLine("0. exit");
        Console.Write("Enter command: ");

        var command = Console.ReadLine()?.Trim();

        switch (command)
        {
            case "1":
                Console.WriteLine("Enter peer address (ip:port): ");
                var peerAddress = Console.ReadLine();
                if (!string.IsNullOrEmpty(peerAddress) && peerAddress.Contains(':'))
                {
                    p2pClient.ConnectToPeer(peerAddress);
                    var parts = peerAddress.Split(':');
                    var resp = p2pClient.RequestChainAsync(parts[0], int.Parse(parts[1]), $"127.0.0.1:{port}").Result;
                    if (resp != null && resp.Type == "NEW_CHAIN")
                    {
                        var newChain = JsonSerializer.Deserialize<List<Block>>(resp.Data);
                        if (newChain != null)
                        {
                            blockchainService.ReplaceChain(newChain);
                            Console.WriteLine("Received chain headers (for SPV root validation).");
                        }
                    }
                }
                break;
            case "2":
                Console.Write("Enter recipient address: ");
                var recipientAddress = Console.ReadLine();
                Console.Write("Enter amount: ");
                if (!decimal.TryParse(Console.ReadLine(), out var amount)) break;
                Console.Write("Enter fee (default 1): ");
                if (!decimal.TryParse(Console.ReadLine(), out var fee)) fee = 1;
                
                try
                {
                    var tx = TransactionService.CreateTransaction(myWallet.publicKey, recipientAddress, amount, fee, myWallet.privateKey);
                    blockchainService.AddTransactionToMemPool(tx);
                    p2pClient.BroadcastTransactionAsync(tx).Wait();
                    Console.WriteLine("Transaction created and broadcasted.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Transaction failed: {e.Message}");
                }
                break;
            case "3":
                Console.Write("Enter transaction ID: ");
                var spvTxId = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(spvTxId))
                {
                    var msg = new NetworkMessage("REQUEST_SPV_PROOF", spvTxId);
                    p2pClient.BroadcastMessageAsync(msg).Wait();
                    Console.WriteLine("SPV proof request sent.");
                }
                break;
            case "0":
                Console.WriteLine("Goodbye!");
                return;
            default:
                Console.WriteLine("Unknown command.");
                break;
        }
    }

}
