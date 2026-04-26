using BlockChain.models;

namespace BlockChain.service;

public class BlockChainService
{
    public List<Block> Chain { get; set; }
    private HashingService hashingService;
    private MiningService miningService;
    public static int Difficulty = 1;
    private readonly double _targetBlockTimeDuration = 1;
    private readonly int _difficultyAdjustmentInterval = 1;
    
    public BlockChainService()
    {
        Chain = new List<Block>();
        hashingService = new HashingService();
        miningService = new MiningService(hashingService);
        CreateGenesisBlock();
    }

    private void CreateGenesisBlock()
    {
        var genesisBlock = new Block(0, DateTime.Parse("2024-06-01T00:00:00Z"), "0", "Genesis Block");
        miningService.Mine(genesisBlock, Difficulty);
        Chain.Add(genesisBlock);
    }
    
    public void AddBlock(string data)
    {
        var lastBlock = Chain.Last();
        var newBlock = new Block(lastBlock.Index + 1, DateTime.UtcNow, lastBlock.Hash, data);
        newBlock.Hash = hashingService.ComputeHash(newBlock);
        miningService.Mine(newBlock, Difficulty);
        Chain.Add(newBlock);
        if (newBlock.Index % _difficultyAdjustmentInterval == 0)
        {
            AdjustDifficulty();
        }
    }

    private void AdjustDifficulty()
    {
        var recentBlocks = Chain.Skip(Chain.Count - _difficultyAdjustmentInterval).Take(_difficultyAdjustmentInterval).ToList();
        var totalMiningTime = recentBlocks.Sum(b => b.MiningDurationSec);
        var averageMiningTime = totalMiningTime / _difficultyAdjustmentInterval;

        if (averageMiningTime < _targetBlockTimeDuration)
        {
            Difficulty++;
            Console.WriteLine($"Difficulty increased to {Difficulty} (average mining time: {averageMiningTime:F2}s)");
        }
        else if (averageMiningTime > _targetBlockTimeDuration )
        {
            Difficulty = Math.Max(1, Difficulty - 1);
            Console.WriteLine($"Difficulty decreased to {Difficulty} (average mining time: {averageMiningTime:F2}s)");
        }
    }

    public List<string> AnalyzeChain()
    {
        var issues = new List<string>();

        for (int i = 0; i < Chain.Count; i++)
        {
            var currentBlock = Chain[i];
            var recalculatedHash = hashingService.ComputeHash(currentBlock);

            if (currentBlock.Hash != recalculatedHash)
            {
                issues.Add($"Error in block #[{currentBlock.Index}]: Hash does not match block data (Data/Timestamp/Nonce changed).");
            }

            if (!IsHashMeetingDifficulty(currentBlock.Hash))
            {
                issues.Add($"Error in block #[{currentBlock.Index}]: Hash does not satisfy current difficulty.");
            }

            if (i > 0)
            {
                var previousBlock = Chain[i - 1];
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    issues.Add($"Error in block #[{currentBlock.Index}]: Chain broken (PreviousHash does not match previous block hash).");
                }
            }
        }

        return issues;
    }

    private bool IsHashMeetingDifficulty(string hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        string prefix = new string('0', Difficulty);
        return hash.StartsWith(prefix);
    }

    public bool IsValid()
    {
        return AnalyzeChain().Count == 0;
    }
}