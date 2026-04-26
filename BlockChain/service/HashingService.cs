using System.Text;
using BlockChain.models;

namespace BlockChain.service;

public class HashingService
{
    public string ComputeHash(Block block)
    {
        string input = $"{block.Index}{block.Timestamp:O}{block.PreviousHash}{block.Data}{block.Nonce}";
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
} 