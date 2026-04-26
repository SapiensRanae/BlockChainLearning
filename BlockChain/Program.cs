using BlockChain.service;

var displayService = new DisplayService();
var blockchainService = new BlockChainService();

for (int i = 1; i <= 10; i++)
{
    blockchainService.AddBlock($"Block {i} Data");
    blockchainService.AddBlock($"Block {i} Data");
    blockchainService.AddBlock($"Block {i} Data");
    Console.WriteLine($"Difficly: {BlockChainService.Difficulty}");
    displayService.DisplayChain(blockchainService.Chain);
   
}

