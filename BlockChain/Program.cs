using BlockChain.models;
using BlockChain.service;

var displayService = new DisplayService();
var blockchainService = new BlockChainService();
var transactionService = new TransactionService(blockchainService);

BlockchainExplorer explorer = new BlockchainExplorer(blockchainService.Chain);

var walletAlice = new Wallet(new CryptoService());
var walletBob = new Wallet(new CryptoService());

blockchainService.MineBlock(walletAlice.publicKey, new List<Transaction>());
blockchainService.MineBlock(walletAlice.publicKey, new List<Transaction>());

blockchainService.MineBlock(walletBob.publicKey, new List<Transaction>());
blockchainService.MineBlock(walletBob.publicKey, new List<Transaction>());
blockchainService.MineBlock(walletBob.publicKey, new List<Transaction>());
blockchainService.MineBlock(walletBob.publicKey, new List<Transaction>());
blockchainService.MineBlock(walletBob.publicKey, new List<Transaction>());
blockchainService.MineBlock(walletBob.publicKey, new List<Transaction>());


var txAliceToBob = transactionService.CreateTransaction(walletAlice.publicKey, walletBob.publicKey, 10, walletAlice.privateKey);
var txBobToAlice = transactionService.CreateTransaction(walletBob.publicKey, walletAlice.publicKey, 5, walletBob.privateKey);

Console.WriteLine($"Alice's Balance: {blockchainService.GetBalance(walletAlice.publicKey)}");
Console.WriteLine($"Bob's Balance: {blockchainService.GetBalance(walletBob.publicKey)}");


blockchainService.MineBlock(walletAlice.publicKey, new List<Transaction> { txAliceToBob, txBobToAlice });

Console.WriteLine($"Alice's Balance: {blockchainService.GetBalance(walletAlice.publicKey)}");
Console.WriteLine($"Bob's Balance: {blockchainService.GetBalance(walletBob.publicKey)}");

Console.WriteLine(explorer.GetTotalCoins());

// displayService.DisplayChain(blockchainService.Chain);

