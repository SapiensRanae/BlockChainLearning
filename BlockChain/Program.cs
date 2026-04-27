using BlockChain.models;
using BlockChain.service;

var displayService = new DisplayService();
var blockchainService = new BlockChainService();

blockchainService.AddBlock(new List<Transaction>{TransactionService.CreateTransaction("Alice", "Bob", 10)});
blockchainService.AddBlock(new List<Transaction>{TransactionService.CreateTransaction("Alice", "Bob", 10)});
blockchainService.AddBlock(new List<Transaction>{TransactionService.CreateTransaction("Alice", "Bob", 10), TransactionService.CreateTransaction("Alice", "Bob", 10),TransactionService.CreateTransaction("Alice", "Bob", 10)} );
blockchainService.AddBlock(new List<Transaction>{TransactionService.CreateTransaction("Alice", "Bob", 10)});
displayService.DisplayChain(blockchainService.Chain);
