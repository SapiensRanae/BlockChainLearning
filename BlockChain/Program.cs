using BlockChain.models;
using BlockChain.service;

var displayService = new DisplayService();
var blockchainService = new BlockChainService();
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


var txAliceToBob = TransactionService.CreateTransaction(walletAlice.publicKey, walletBob.publicKey, 10, walletAlice.privateKey, blockchainService);
var txBobToAlice = TransactionService.CreateTransaction(walletBob.publicKey, walletAlice.publicKey, 5, walletBob.privateKey, blockchainService);

Console.WriteLine($"Alice's Balance: {blockchainService.GetBalance(walletAlice.publicKey)}");
Console.WriteLine($"Bob's Balance: {blockchainService.GetBalance(walletBob.publicKey)}");


blockchainService.MineBlock(walletAlice.publicKey, new List<Transaction> { txAliceToBob, txBobToAlice });

Console.WriteLine($"Alice's Balance: {blockchainService.GetBalance(walletAlice.publicKey)}");
Console.WriteLine($"Bob's Balance: {blockchainService.GetBalance(walletBob.publicKey)}");

// displayService.DisplayChain(blockchainService.Chain);

