using BlockChain.service;

var displayService = new DisplayService();
var blockchainService = new BlockChainService();

for (int i = 1; i <= 3; i++)
{
    blockchainService.AddBlock($"Block {i} Data");
    blockchainService.AddBlock($"Block {i} Data");
    blockchainService.AddBlock($"Block {i} Data");
    Console.WriteLine($"Difficly: {blockchainService.Difficulty}");
    displayService.DisplayChain(blockchainService.Chain);
   
}

blockchainService.PrintDifficultyHistory();
