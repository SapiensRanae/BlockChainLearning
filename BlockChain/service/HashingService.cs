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
        if (transactions == null || transactions.Count == 0)
            return string.Empty;
        var hashes = transactions.Select(tx => ComputeHash(tx.ToRawString())).ToList();
        while (hashes.Count > 1)
        {
            var tempList = new List<string>();
            for (int i = 0; i < hashes.Count; i += 2)
            {
                if (i + 1 < hashes.Count)
                    tempList.Add(ComputeHash(hashes[i] + hashes[i + 1]));
                else
                    tempList.Add(hashes[i]);
            }
            hashes = tempList;
        }
        return hashes[0];
    }

    public List<string> GetMerkleProof(List<Transaction> transactions, string targetTxId)
    {
        var hashes = transactions.Select(tx => ComputeHash(tx.ToRawString())).ToList();
        var proof = new List<string>();
        int index = transactions.FindIndex(tx => tx.Id == targetTxId);
        if (index == -1) return proof;

        while (hashes.Count > 1)
        {
            var tempList = new List<string>();
            for (int i = 0; i < hashes.Count; i += 2)
            {
                if (i + 1 < hashes.Count)
                {
                    if (i == index) proof.Add(hashes[i + 1]);
                    else if (i + 1 == index) proof.Add(hashes[i]);
                    tempList.Add(ComputeHash(hashes[i] + hashes[i + 1]));
                }
                else
                {
                    tempList.Add(hashes[i]);
                }
            }
            index /= 2;
            hashes = tempList;
        }
        return proof;
    }

    public bool VerifyMerkleProof(string txHash, List<string> proof, string expectedMerkleRoot)
    {
        string currentHash = txHash;
        foreach (var p in proof)
        {
            currentHash = ComputeHash(currentHash + p);
        }
        return currentHash == expectedMerkleRoot;
    }
}