namespace BlockChain.models;

public class Block
{
    public int Index { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string PreviousHash { get; set; }
    public string Hash { get; set; }
    public string MerkleRoot { get; set; }
   
    public List<Transaction> Transactions { get; set; }
    public int Nonce { get; set; }

    public double MiningDurationSec { get; set; } = 0;
    
    public int DifficultyAtMining { get; set; }
    
    public Block(int index, DateTime timestamp, string previousHash, int difficultyAtMining, List<Transaction> transactions)
    {
        Index = index;  
        Timestamp = timestamp;
        PreviousHash = previousHash;
        Transactions = transactions;
        DifficultyAtMining = difficultyAtMining;
        Hash = "";
    }
    
    

    public Block()
    {
        
    }
}