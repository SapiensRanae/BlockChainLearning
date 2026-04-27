using BlockChain.models;
using BlockChain.service;

var displayService = new DisplayService();
var blockchainService = new BlockChainService();
BlockchainExplorer explorer = new BlockchainExplorer(blockchainService.Chain);

blockchainService.AddBlock(new List<Transaction>{TransactionService.CreateTransaction("Alice", "Bob", 10)});
blockchainService.AddBlock(new List<Transaction>{TransactionService.CreateTransaction("Max", "Bob", 10)});
blockchainService.AddBlock(new List<Transaction>{TransactionService.CreateTransaction("Alice", "Bob", 10), TransactionService.CreateTransaction("Alice", "Bob", 10),TransactionService.CreateTransaction("Alice", "Bob", 10)} );
blockchainService.AddBlock(new List<Transaction>{TransactionService.CreateTransaction("Alice", "Max", 10)});


displayService.DisplayChain(blockchainService.Chain);

Console.WriteLine(explorer.getTotalVolume());
Console.WriteLine(explorer.getLargestTransaction());

foreach (var tx in explorer.getAddressHistory("Alice"))
{
    Console.WriteLine(tx);
}

Console.WriteLine(explorer.FindTransactionLocation(blockchainService.Chain.Last().Transactions.Last().Id));

