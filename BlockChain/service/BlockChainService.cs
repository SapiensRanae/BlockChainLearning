using BlockChain.models;

namespace BlockChain.service;

public class BlockChainService
{
    public List<Block> Chain { get; set; }
    private readonly HashingService _hashingService;
    private readonly MiningService _miningService;
    private int _halvingCounter = 5;
    public int Difficulty;
    public int Reward = 100;
    private const double TargetBlockTimeDuration = 1;
    private const int DifficultyAdjustmentInterval = 1;

    public BlockChainService(int difficulty = 1)
    {
        Chain = new List<Block>();
        _hashingService = new HashingService();
        _miningService = new MiningService(_hashingService);
        Difficulty = difficulty;
        CreateGenesisBlock();
    }

    private void CreateGenesisBlock()
    {
        var genesisBlock = new Block(0, DateTime.Parse("2024-06-01T00:00:00Z"), "0", Difficulty,
            new List<Transaction>());
        _miningService.Mine(genesisBlock, Difficulty);
        Chain.Add(genesisBlock);
    }

    public void PrintDifficultyHistory()
    {
        Console.WriteLine("Difficulty History:");
        foreach (var block in Chain)
        {
            Console.WriteLine($"Block #{block.Index}: Difficulty at Mining = {block.DifficultyAtMining}");
        }
    }

    private void UpdateReward(Block block)
    {
        if (block.Index % _halvingCounter == 0 && 0 < block.Index)
        {
            Reward = Math.Max(1, Reward / 2);

            // Console.WriteLine($"Reward halved to {Reward}");
        }
    }

    public void MineBlock(string minerAdress, List<Transaction> transactions)
    {
        foreach (var tx in transactions)
        {
            var isValid = TransactionService.ValidateTransaction(tx, this);
            if (!isValid.isValid)
            {
                throw new Exception($"Invalid transaction: {isValid.error}");
            }
        }

        var rewardTx = new Transaction("COINBASE", minerAdress, Reward);
        transactions.Add(rewardTx);
        var lastBlock = Chain.Last();
        var newBlock = new Block(lastBlock.Index + 1, DateTime.UtcNow, lastBlock.Hash, Difficulty, transactions);
        _miningService.Mine(newBlock, newBlock.DifficultyAtMining);
        Chain.Add(newBlock);
        UpdateReward(newBlock);
        if (newBlock.Index % DifficultyAdjustmentInterval == 0)
        {
            AdjustDifficulty();
        }
    }

    private void AdjustDifficulty()
    {
        var recentBlocks = Chain.Skip(Chain.Count - DifficultyAdjustmentInterval).Take(DifficultyAdjustmentInterval)
            .ToList();
        var totalMiningTime = recentBlocks.Sum(b => b.MiningDurationSec);
        var averageMiningTime = totalMiningTime / DifficultyAdjustmentInterval;

        if (averageMiningTime < TargetBlockTimeDuration / 5)
        {
            Difficulty += 2;
        }
        else if (averageMiningTime < TargetBlockTimeDuration)
        {
            Difficulty++;
        }
        else if (averageMiningTime / 5 > TargetBlockTimeDuration)
        {
            Difficulty = Math.Max(1, Difficulty - 2);
        }
        else if (averageMiningTime > TargetBlockTimeDuration)
        {
            Difficulty = Math.Max(1, Difficulty - 1);
        }

        Difficulty = Math.Max(1, Math.Min(Difficulty, 4));
        Console.WriteLine($"Adjusted difficulty to {Difficulty}");
    }

    public decimal GetBalance(string address)
    {
        decimal balance = 0;
        foreach (var block in Chain)
        {
            foreach (var tx in block.Transactions)
            {
                if (tx.From == address)
                {
                    balance -= tx.Amount;
                }
                else if (tx.To == address)
                {
                    balance += tx.Amount;
                }
            }
        }
        return balance;
    }


    
public List<string> AnalyzeChain()
    {
        var issues = new List<string>();

        for (int i = 0; i < Chain.Count; i++)
        {
            var currentBlock = Chain[i];
            var recalculatedHash = _hashingService.ComputeHash(currentBlock);

            if (currentBlock.Hash != recalculatedHash)
            {
                issues.Add($"Error in block #[{currentBlock.Index}]: Hash does not match block data (Data/Timestamp/Nonce changed).");
            }

            if (!IsHashMeetingDifficulty(currentBlock.Hash, currentBlock.DifficultyAtMining))
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

    private bool IsHashMeetingDifficulty(string hash, int difficulty)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        string prefix = new string('0', difficulty);
        return hash.StartsWith(prefix);
    }

    public bool IsValid()
    {
        return AnalyzeChain().Count == 0;
    }
}