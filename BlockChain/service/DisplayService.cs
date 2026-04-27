using BlockChain.models;

namespace BlockChain.service;

public class DisplayService
{
    public void DisplayChain(List<Block> chain)
    {
        foreach (var block in chain)
        {
            Console.WriteLine($"Index: {block.Index}");
            Console.WriteLine($"Timestamp: {block.Timestamp}");
       
            Console.WriteLine($"Hash: {block.Hash}");
            Console.WriteLine($"Previous Hash: {block.PreviousHash}");
            Console.WriteLine($"Nonce: {block.Nonce}");
            Console.WriteLine($"Mining Duration: {block.MiningDurationSec:F4}s");
            Console.WriteLine($"Difficulty: {block.DifficultyAtMining}");

            foreach (var tx in block.Transactions)
            {
               Console.WriteLine(tx.ToString()); 
            }

            Console.WriteLine(new string('-', 40));
        }
    }
}