namespace BlockChain.models;

public class Block
{
    public int Index { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string PreviousHash { get; set; }
    public string Hash { get; set; }
    public string Data { get; set; }
    public int Nonce { get; set; }

    public double MiningDurationSec { get; set; } = 0;
    
    public Block(int index, DateTime timestamp, string previousHash, string data)
    {
        Index = index;  
        Timestamp = timestamp;
        PreviousHash = previousHash;
        Data = data;
        Hash = "";
    }

    public Block()
    {
        
    }
}