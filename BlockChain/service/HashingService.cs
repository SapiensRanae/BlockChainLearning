using System.Text;
using BlockChain.models;

namespace BlockChain.service;

public class HashingService
{
    public string ComputeHash(Block block)
    {   
        var txData = string.Concat(block.Transactions.Select(tx => tx.ToRawString()).ToArray());
        string input = $"{block.Index}{block.Timestamp:O}{block.MerkleRoot}{block.PreviousHash}{block.Nonce}{block.DifficultyAtMining}";
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }

    public string ComputeHash(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes;
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
            hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }
    
    public string BuildMerkleTree(List<Transaction> transactions)
    {
        if (transactions.Count == 0 || transactions == null)
            return string.Empty;
        var hashAllTransactions = transactions.Select(tx => tx.ToRawString()).ToList();
        while (hashAllTransactions.Count > 1)
        {
            var tempList = new List<string>();
            for (int i = 0; i < hashAllTransactions.Count; i += 2)
            {
                if (i + 1 < hashAllTransactions.Count)
                {
                    string combinedHash = hashAllTransactions[i] + hashAllTransactions[i + 1];
                    tempList.Add(ComputeHash(combinedHash));
                }
                else
                {
                    tempList.Add(hashAllTransactions[i]);
                }
            }
            hashAllTransactions = tempList;
        }
        return hashAllTransactions[0];
    }
} 