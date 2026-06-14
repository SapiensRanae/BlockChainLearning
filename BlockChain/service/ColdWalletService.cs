using System.Text.Json;
using BlockChain.models;

namespace BlockChain.service;

public class ColdWalletService
{

    public Transaction GenerateOfflineTransaction(string from, string to, decimal amount, decimal fee, string privateKey, string filePath)
    {
        var transaction = new Transaction(from, to, amount, fee);
        TransactionService.SignTransaction(transaction, privateKey);
        File.WriteAllText(filePath, JsonSerializer.Serialize(transaction, new JsonSerializerOptions { WriteIndented = true }));
        return transaction;
    }


}