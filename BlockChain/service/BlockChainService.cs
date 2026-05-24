using BlockChain.models;
using System.Text.Json;
using System.IO;

namespace BlockChain.service;

public class BlockChainService
{
    public List<Block> Chain { get; set; }
    private readonly HashingService _hashingService;
    private readonly MiningService _miningService;
    private int _halvingCounter = 5;
    public List<Transaction> PendingTransactions;
    public const int maxTransactionPerBlock = 10;
    public const decimal baseFee = 0.1m;
    public int Difficulty;
    public int Reward = 100;
    private readonly int livenessSeconds = 60;
    private const double TargetBlockTimeDuration = 1;
    private const int DifficultyAdjustmentInterval = 1;
    
    public Dictionary<string, decimal> BalanceCash = new Dictionary<string, decimal>();

    public BlockChainService(int difficulty = 1)
    {
        Chain = new List<Block>();
        _hashingService = new HashingService();
        _miningService = new MiningService(_hashingService);
        Difficulty = difficulty;
        PendingTransactions = new List<Transaction>();
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
    
    public void AddTransactionToMemPool(Transaction transaction)
    {
        var transactionCount = 0;
        
        var isValid = TransactionService.ValidateTransaction(transaction);

        if (!isValid.isValid)
        {
            throw new Exception($"Transaction {transaction.Id} is invalid: {isValid}");
        }
        if (transaction.From != "COINBASE")
        {
            var balance = GetBalance(transaction.From);
            // if (balance < transaction.Amount + transaction.Fee)
            // {
            //     throw new Exception($"Wallet {transaction.From} has insufficient balance for this transaction.");
            // }
            if (transaction.Fee < baseFee)
            {
                throw new Exception($"Transaction {transaction.Id} has insufficient fee. Minimum fee is {baseFee}.");
            }
        }
        
        transactionCount = PendingTransactions.Count(tx => tx.From == transaction.From);

        if (transactionCount > 3)
        {
            throw new Exception($"Wallet {transaction.From} has too many pending transactions in the mempool.");
        }
        
        PendingTransactions.Add(transaction);
    }

    private void UpdateReward(Block block)
    {
        if (block.Index % _halvingCounter == 0 && 0 < block.Index)
        {
            Reward = Math.Max(1, Reward / 2);

            // Console.WriteLine($"Reward halved to {Reward}");
        }
    }

    public void MinePendingTransactions(string minerAdress)
    {
        var transactionsToInclude = PendingTransactions.OrderByDescending(tx => tx.Fee - baseFee).ThenBy(tx => tx.Timestamp).Take(maxTransactionPerBlock).ToList();
        var transactionsToDelete = transactionsToInclude.Where(tx => tx.Timestamp < DateTime.UtcNow.AddSeconds(-livenessSeconds)).ToList();
        
        foreach (var transaction in transactionsToDelete)        {
            PendingTransactions.Remove(transaction);
            transactionsToInclude.Remove(transaction);
            Console.WriteLine($"Transaction {transaction.Id} removed from mempool due to inactivity.");
        }

        

        var totalTips = transactionsToInclude.Sum(tx => tx.Fee - baseFee);
        var totalReward = Reward + totalTips;
        
        
        var rewardingTransactions = new Transaction("COINBASE", minerAdress, totalReward, 0);
        
        transactionsToInclude.Insert(0,rewardingTransactions);
        
        var lastBlock = Chain.Last();
        var newBlock = new Block(lastBlock.Index + 1, DateTime.UtcNow, lastBlock.Hash, Difficulty, transactionsToInclude);
        _miningService.Mine(newBlock, Difficulty);
        Chain.Add(newBlock);
        UpdateBalance(newBlock);
        UpdateReward(newBlock);
        
        foreach (var transaction in transactionsToInclude)
        {
            PendingTransactions.Remove(transaction);
        }
        
        if (Chain.Count % DifficultyAdjustmentInterval == 0)
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

        Difficulty = Math.Max(1, Math.Min(Difficulty, 1));
        Console.WriteLine($"Adjusted difficulty to {Difficulty}");
    }

    public decimal GetBalanceOld(string address)
    {
        var balance = 0m;
        foreach (var block in Chain)
        {
            foreach (var tx in block.Transactions)
            {
                if (tx.From == address)
                {
                    balance -= tx.Amount;
                }

                if (tx.To == address)
                {
                    balance += tx.Amount;
                }
            }
        }

        return balance;
    }
    
    public void RebuildState()
    {
        BalanceCash.Clear();
        foreach (var block in Chain)
        {
            UpdateBalance(block);
        }
    }
    public decimal GetBalance(string address)
    {
        if (BalanceCash.ContainsKey(address))
        {
            return BalanceCash[address];
        }
        return 0;
    }


    private void UpdateBalance(Block block)
    {
        foreach (var tx in block.Transactions)
        {
            if (tx.From != "COINBASE")
            {
                if (!BalanceCash.ContainsKey(tx.From))
                {
                    BalanceCash[tx.From] = 0;
                }
                BalanceCash[tx.From] -= tx.Amount + tx.Fee;
            }

            if (!BalanceCash.ContainsKey(tx.To))
            {
                BalanceCash[tx.To] = 0;
            }
            BalanceCash[tx.To] += tx.Amount + tx.Fee;
        }
    }


    
    public List<string> AnalyzeChain(List<Block> chain)
    {
        var issues = new List<string>();

        for (int i = 0; i < chain.Count; i++)
        {
            var currentBlock = chain[i];
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
                var previousBlock = chain[i - 1];
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    issues.Add($"Error in block #[{currentBlock.Index}]: Chain broken (PreviousHash does not match previous block hash).");
                }
            }
        }

        return issues;
    }

    public void ReplaceChain(List<Block> newChain)
    {
        if (newChain.Count <= Chain.Count) return;
        var issues = AnalyzeChain(newChain);
        
        if(newChain.Sum(block => block.DifficultyAtMining) <= Chain.Sum(block => block.DifficultyAtMining))
        {
            issues.Add("The new chain does not have more cumulative difficulty than the current chain.");
        }
        if (issues.Count > 0)
        {
            throw new Exception("The new chain is invalid: " + string.Join(", ", issues));
        }
        Chain = newChain;
        BalanceCash.Clear();
        foreach (var block in Chain)
        {
            UpdateBalance(block);
        }

        var mixedTxId = Chain.SelectMany(block => block.Transactions).Select(tx => tx.Id).ToHashSet();
        
    }

    private bool IsHashMeetingDifficulty(string hash, int difficulty)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        string prefix = new string('0', difficulty);
        return hash.StartsWith(prefix);
    }

    public bool IsValid()
    {
        return AnalyzeChain(Chain).Count == 0;
        
    }

    public decimal GetTotalSupply()
    {
        return Chain.Sum(block => block.Transactions.Sum(tx => tx.Amount));
    }
    public decimal GetTotalBurned()
    {
        return Chain
            .SelectMany(block => block.Transactions)
            .Count(tx => tx.From != "COINBASE") * baseFee;
    }
    
    public decimal GetActualTotalCoins()
    {
        return GetTotalSupply() - GetTotalBurned();
    }

    public void SaveStateSnapshot(string filePath = "savestate.json")
    {
        try
        {
            var state = new
            {
                Timestamp = DateTime.UtcNow,
                ChainLength = Chain.Count,
                Balances = BalanceCash,
                TotalSupply = GetTotalSupply(),
                TotalBurned = GetTotalBurned(),
                ActualTotalCoins = GetActualTotalCoins()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(state, options);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"Saved state snapshot to '{filePath}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save state snapshot: {ex.Message}");
        }
    }
    

    public bool LoadStateSnapshot(string filePath = "savestate.json")
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"State snapshot file not found: {filePath}");
                return false;
            }

            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var snapshot = JsonSerializer.Deserialize<StateSnapshot>(json, options);
            if (snapshot == null)
            {
                Console.WriteLine("Failed to parse state snapshot (null).");
                return false;
            }

            BalanceCash.Clear();
            if (snapshot.Balances != null)
            {
                foreach (var kv in snapshot.Balances)
                {
                    BalanceCash[kv.Key] = kv.Value;
                }
            }

            Console.WriteLine($"Loaded state snapshot from '{filePath}'. Balances restored: {BalanceCash.Count} entries.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load state snapshot: {ex.Message}");
            return false;
        }
    }

  
    private record StateSnapshot(DateTime Timestamp, int ChainLength, Dictionary<string, decimal>? Balances,
        decimal TotalSupply, decimal TotalBurned, decimal ActualTotalCoins);

}