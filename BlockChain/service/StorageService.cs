using BlockChain.models;

namespace BlockChain.service;

public class StorageService
{
    private const string fileName = "blockchain.json";
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, fileName);
    public void SaveBlockChain(List<Block> chain)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(chain);
        File.WriteAllText(FilePath, json);
    }
    
    public List<Block> LoadBlockChain()
    {
        if (!File.Exists(FilePath))
        {
            return new List<Block>();
        }
        var json = File.ReadAllText(FilePath);
        return System.Text.Json.JsonSerializer.Deserialize<List<Block>>(json);
    }
}