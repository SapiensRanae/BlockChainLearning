using System.Diagnostics;
using BlockChain.models;

namespace BlockChain.service;

public class MiningService
{
    private readonly HashingService _hashingService;

    public MiningService(HashingService hashingService)
    {
        _hashingService = hashingService;
    }

    public long Mine(Block block, int difficulty)
    {
        var target = new String ('0', difficulty);
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
           
            block.Hash = _hashingService.ComputeHash(block);

            if (block.Hash.StartsWith(target))
            {
                Console.WriteLine($"Block mined: {block.Hash}");
                stopwatch.Stop();
                block.MiningDurationSec = stopwatch.Elapsed.TotalSeconds;
                return block.Nonce;
            }
            block.Nonce++;
        }
    }
}
