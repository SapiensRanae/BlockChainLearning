using System.Text;
using BlockChain.models;
using System.Text.Json;


namespace BlockChain.service;

public class BlockChainService
{
    public List<Block> Chain { get; set; }
    private readonly HashingService _hashingService;
    private readonly MiningService _miningService;
    private readonly StorageService _storageService;
    private int _halvingCounter = 5;
    public List<Transaction> PendingTransactions;
    public const int MaxTransactionPerBlock = 10;
    public const decimal BaseFee = 0.1m;
    public int Difficulty;
    public int Reward = 100;
    private readonly int _livenessSeconds = 60;
    private const double TargetBlockTimeDuration = 1;
    private const int DifficultyAdjustmentInterval = 1;
    public bool isSPV = false;
    private Dictionary<string, decimal> _balanceCashOld = new Dictionary<string, decimal>();
    
    public Dictionary<string, Dictionary<string, decimal>> BalanceCash = new Dictionary<string, Dictionary<string, decimal>>();

    public BlockChainService(int difficulty = 1, bool isSPV = false)
    {
        this.isSPV = isSPV;
        Chain = new List<Block>();
        _hashingService = new HashingService();
        _miningService = new MiningService(_hashingService);
        _storageService = new StorageService();
        Difficulty = difficulty;
        PendingTransactions = new List<Transaction>();
        CreateGenesisBlock();
        if (isSPV) return;
        var loadedChain = _storageService.LoadBlockChain();
        if (loadedChain.Count > 0 && loadedChain.Count > 0)
        {
            Chain = loadedChain;
        }
        
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
        if (PendingTransactions.Any(tx => tx.Id == transaction.Id))
        {
            return;
        }

        var transactionCount = 0;

        var isValid = TransactionService.ValidateTransaction(transaction);

        if (!isValid.isValid)
        {
            throw new Exception($"Transaction {transaction.Id} is invalid: {isValid}");
        }
        if (transaction.From != "COINBASE" && transaction.From != "MINT")
        {
            var balanceToken = GetBalance(transaction.From, transaction.TokenSymbol);
            var balanceMain = GetBalance(transaction.From, "MAIN");

            if (transaction.TokenSymbol == "MAIN")
            {
                if (balanceMain < transaction.Amount + transaction.Fee)
                {
                    throw new Exception($"Wallet {transaction.From} has insufficient balance for this transaction.");
                }
            }
            else
            {
                if (balanceToken < transaction.Amount)
                {
                    throw new Exception($"Wallet {transaction.From} has insufficient {transaction.TokenSymbol} balance.");
                }
                if (balanceMain < transaction.Fee)
                {
                    throw new Exception($"Wallet {transaction.From} has insufficient MAIN balance for fee.");
                }
            }

            if (transaction.Fee < BaseFee)
            {
                throw new Exception($"Transaction {transaction.Id} has insufficient fee. Minimum fee is {BaseFee}.");
            }
        }
        
        transactionCount = PendingTransactions.Count(tx => tx.From == transaction.From);

        if (transactionCount > 3)
        {
            InvalidOperationException ex = new InvalidOperationException($"Spam detected! Wallet {transaction.From} has too many pending transactions in the mempool.");
        }
        
        PendingTransactions.Add(transaction);
    }

    private void UpdateReward(Block block)
    {
        if (block.Index % _halvingCounter == 0 && 0 < block.Index)
        {
            Reward = Math.Max(1, Reward / 2);

        }
    }

    private int EvictStaleTransactions(int maxAgeSeconds)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-maxAgeSeconds);
        var transactionsToDelete = PendingTransactions.Where(tx => tx.Timestamp < cutoff).ToList();

        foreach (var transaction in transactionsToDelete)
        {
            PendingTransactions.Remove(transaction);
        }

        return transactionsToDelete.Count;
    }

    public void MinePendingTransactions(string minerAddress)
    {
        EvictStaleTransactions(_livenessSeconds);
        var transactionsToInclude = PendingTransactions
            .Where(tx => tx.minBlockHeight <= Chain.Count)
            .OrderByDescending(tx => tx.Fee - BaseFee)
            .ThenBy(tx => tx.Timestamp)
            .Take(MaxTransactionPerBlock)
            .ToList();

        var totalTips = transactionsToInclude.Sum(tx => tx.Fee - BaseFee);
        var totalReward = Reward + totalTips;

        var rewardingTransactions = new Transaction("COINBASE", minerAddress, totalReward, 0);
        
        transactionsToInclude.Insert(0,rewardingTransactions);
        
        var lastBlock = Chain.Last();
        var newBlock = new Block(lastBlock.Index + 1, DateTime.UtcNow, lastBlock.Hash, Difficulty, transactionsToInclude);
        newBlock.MerkleRoot = _hashingService.BuildMerkleTree(transactionsToInclude);
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
        
        _storageService.SaveBlockChain(Chain);
        
        
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

        Difficulty = Math.Max(1, Difficulty);
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
    
    
    public bool ValidateAndRebuildState()
    {
        BalanceCash.Clear();
        foreach (var block in Chain)
        {
            try
            {
                UpdateBalance(block);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
               return false;
            }

            
        }
        return true;
        
    }
    public decimal GetBalance(string address, string tokenSymbol = "MAIN")
    {
        if (BalanceCash.ContainsKey(address) && BalanceCash[address].ContainsKey(tokenSymbol))
        {
            return BalanceCash[address][tokenSymbol];
        }
        return 0;
    }


    private void UpdateBalance(Block block)
    {
        foreach (var tx in block.Transactions)
        {
            try
            {
                TransactionService.ValidateTransaction(tx);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            if (tx.From != "COINBASE" && tx.From != "MINT")
            {
                if (!BalanceCash.ContainsKey(tx.From))
                {
                    BalanceCash[tx.From] = new Dictionary<string, decimal>();
                }
                if (!BalanceCash[tx.From].ContainsKey(tx.TokenSymbol)) BalanceCash[tx.From][tx.TokenSymbol] = 0;
                if (!BalanceCash[tx.From].ContainsKey("MAIN")) BalanceCash[tx.From]["MAIN"] = 0;

                BalanceCash[tx.From][tx.TokenSymbol] -= tx.Amount;
                BalanceCash[tx.From]["MAIN"] -= tx.Fee;
            }

            if (!BalanceCash.ContainsKey(tx.To))
            {
                BalanceCash[tx.To] = new Dictionary<string, decimal>();
            }
            if (!BalanceCash[tx.To].ContainsKey(tx.TokenSymbol)) BalanceCash[tx.To][tx.TokenSymbol] = 0;
            
            BalanceCash[tx.To][tx.TokenSymbol] += tx.Amount;

            if (tx.From != "COINBASE" && tx.From != "MINT")
            {
                if (!BalanceCash[tx.To].ContainsKey("MAIN")) BalanceCash[tx.To]["MAIN"] = 0;
                BalanceCash[tx.To]["MAIN"] += tx.Fee;
            }
        }
    }


    
    public List<string> AnalyzeChain(List<Block> chain)
    {
        var issues = new List<string>();

        for (int i = 0; i < chain.Count; i++)
        {
            var currentBlock = chain[i];
            var prevBlock = i > 0 ? chain[i - 1] : null;
            var recalculatedHash = _hashingService.ComputeHash(currentBlock);

            if (currentBlock.Hash != recalculatedHash)
            {
                issues.Add($"Error in block #[{currentBlock.Index}]: Hash does not match block data (Data/Timestamp/Nonce changed).");
            }

            if (!IsHashMeetingDifficulty(currentBlock.Hash, currentBlock.DifficultyAtMining))
            {
                issues.Add($"Error in block #[{currentBlock.Index}]: Hash does not satisfy current difficulty.");
            }
            if (currentBlock.PreviousHash != prevBlock?.Hash)
            {
                issues.Add($"Error in block #[{currentBlock.Index}]: PreviousHash does not match hash of previous block.");
            }
            

            if (i > 0)
            {
                var previousBlock = chain[i - 1];
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    issues.Add($"Error in block #[{currentBlock.Index}]: Chain broken (PreviousHash does not match previous block hash).");
                }
            }

            foreach (var tx in currentBlock.Transactions)
            {
                if (tx.From == "COINBASE" || tx.From == "MINT")
                {
                    continue;
                }
                var validation = TransactionService.ValidateTransaction(tx);
                if(!validation.isValid)                {
                    issues.Add($"Error in block #[{currentBlock.Index}]: Invalid transaction {tx.Id} - {validation.error}");
                }
            }
        }

        return issues;
    }

    public void ReplaceChain(List<Block> newChain)
    {
        
      if (newChain.Count < Chain.Count) return;
      
      Chain = newChain;
      
      BalanceCash.Clear();
        foreach (var block in Chain)
        {
            UpdateBalance(block);
        }
        
        var minedTxId = Chain.SelectMany(tx => tx.Transactions).Select(tx => tx.Id).ToHashSet();
        var pendingTxId = PendingTransactions.Select(tx => tx.Id).ToHashSet();
        
        var txIdsToRemove = pendingTxId.Except(minedTxId).ToList();
        foreach (var txId in txIdsToRemove)
        {
            PendingTransactions.RemoveAll(tx => tx.Id == txId);
        }
        
        _storageService.SaveBlockChain(Chain);
        
    }

    private void DiffOldNewBalaneces()
    {
        foreach (var kv in _balanceCashOld)        {
            var oldBalance = kv.Value;
            var newBalance = GetBalance(kv.Key);
            if (oldBalance != newBalance)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Address {kv.Key} balance changed from {oldBalance} to {newBalance}");
                Console.ResetColor();
            }
        }
    }



    private bool IsHashMeetingDifficulty(string hash, int difficulty)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        string prefix = new string('0', difficulty);
        return hash.StartsWith(prefix);
    }

    public bool IsValid(List<Block> newChain)
    {
        var tempBalances = new Dictionary<string, decimal>();
        for (int i = 0; i < newChain.Count; i++)
        {
            var currentBlock = newChain[i];
            var prevBlock = i > 0 ? newChain[i - 1] : null;
            if (currentBlock.Hash != _hashingService.ComputeHash(currentBlock))
            {
                return false;
            }
            if (currentBlock.PreviousHash != prevBlock?.Hash)
            {
                return false;
            }
            if (!currentBlock.Hash.StartsWith(new string('0', currentBlock.DifficultyAtMining)))
            {
                return false;
            }

            foreach (var tx in currentBlock.Transactions)
            {
                var validationResult = TransactionService.ValidateTransaction(tx);
                if (!validationResult.isValid)
                {
                    return false;
                }
                if (tx.From != "COINBASE")
                {
                    decimal senderBalance = tempBalances.ContainsKey(tx.From) ? tempBalances[tx.From] : 0;
                    if (senderBalance < tx.Amount + tx.Fee)                    {
                        return false;
                    }
                    tempBalances[tx.From] = senderBalance - tx.Amount - tx.Fee;
                }
                if (!tempBalances.ContainsKey(tx.To))                {
                    tempBalances[tx.To] = 0;
                }
                if(!tempBalances.ContainsKey(tx.From))                {
                    tempBalances[tx.From] = 0;
                }
                tempBalances[tx.To] += tx.Amount + tx.Fee;
            }
        }
        return true;
    }

    public decimal GetTotalSupply()
    {
        return Chain.Sum(block => block.Transactions.Sum(tx => tx.Amount));
    }
    public decimal GetTotalBurned()
    {
        return Chain
            .SelectMany(block => block.Transactions)
            .Count(tx => tx.From != "COINBASE") * BaseFee;
    }
    
    public decimal GetActualTotalCoins()
    {
        return GetTotalSupply() - GetTotalBurned();
    }

    public AuditReport RunFullAudit(List<Block> chain)
    {
        var report = new AuditReport();
        for (int i = 0; i < chain.Count; i++)
        {
            if (i > 0)
            {
                var previousBlock = chain[i - 1];
                var currentBlock = chain[i];
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    report.IsChainValid = false;
                    report.CompromisedBlockIndexes.Add(currentBlock.Index);
                    report.ViolationDetails[currentBlock.Index] = new List<string>
                    {
                        $"Chain broken: PreviousHash does not match previous block hash."
                    };
                }
            }

            if (chain[i].MerkleRoot != _hashingService.BuildMerkleTree(chain[i].Transactions))
            {
                report.IsChainValid = false;
                report.CompromisedBlockIndexes.Add(chain[i].Index);
                if (!report.ViolationDetails.ContainsKey(chain[i].Index))
                {
                    report.ViolationDetails[chain[i].Index] = new List<string>();
                }

                report.ViolationDetails[chain[i].Index].Add("Merkle root does not match transactions.");
            }
            
            if (!IsHashMeetingDifficulty(chain[i].Hash, chain[i].DifficultyAtMining))
            {
                report.IsChainValid = false;
                report.CompromisedBlockIndexes.Add(chain[i].Index);
                if (!report.ViolationDetails.ContainsKey(chain[i].Index))
                {
                    report.ViolationDetails[chain[i].Index] = new List<string>();
                }

                report.ViolationDetails[chain[i].Index].Add(
                    $"Block hash does not satisfy difficulty {chain[i].DifficultyAtMining}."
                );
            }
        }
        return report;
    }

    public Block? FindAttackOrigin(AuditReport report, List<Block> chain)
    {

        if (report == null || report.CompromisedBlockIndexes == null || report.CompromisedBlockIndexes.Count == 0)
            return null;

        var compromisedOrdered = report.CompromisedBlockIndexes.Distinct().OrderBy(i => i);


        foreach (var idx in compromisedOrdered)
        {
            var block = chain.FirstOrDefault(b => b.Index == idx);
            if (block == null) continue;

            if (report.ViolationDetails.TryGetValue(idx, out var violations) && violations.Any(v =>
                    !v.Contains("PreviousHash", StringComparison.OrdinalIgnoreCase) &&
                    !v.Contains("Chain broken", StringComparison.OrdinalIgnoreCase)))
            {
                return block;
            }
        }
        
        foreach (var idx in compromisedOrdered)
        {
            var blk = chain.FirstOrDefault(b => b.Index == idx);
            if (blk != null) return blk;
        }

        return null;
    }

    public string GenerateForensicReport(AuditReport report, Block attacOrigin)
    {
        var output = new StringBuilder();
        output.AppendLine("=== FORENSIC AUDIT REPORT ===");
        if (report.IsChainValid)
        {
            output.AppendLine("Chain status: OK");
            output.AppendLine("No compromised blocks detected.");
        }
        else
        {
            output.AppendLine("Chain status: COMPROMISED");
            output.AppendLine($"Attack Origin: Block #{attacOrigin.Index} (Timestamp: {attacOrigin.Timestamp})");
            output.AppendLine($"Total Compromised Blocks: {report.CompromisedBlockIndexes.Count}");
            foreach (var blockIndex in report.CompromisedBlockIndexes)
            {
                output.AppendLine($"- Block #{blockIndex}:");
                if (report.ViolationDetails.TryGetValue(blockIndex, out var violations))
                {
                    foreach (var violation in violations)
                    {
                        output.AppendLine($"  - {violation}");
                    }
                }
            }

           
        }
        return output.ToString();
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
                BalanceCash = snapshot.Balances;
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

  
    private record StateSnapshot(DateTime Timestamp, int ChainLength, Dictionary<string, Dictionary<string, decimal>>? Balances,
        decimal TotalSupply, decimal TotalBurned, decimal ActualTotalCoins);

}