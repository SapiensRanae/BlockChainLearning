using BlockChain.models;

namespace BlockChain.service;

public class BlockchainExplorer(List<Block> chain)
{
    public decimal getTotalVolume()
    {
        return chain.Sum(block => block.Transactions.Sum(tx => tx.Amount));
    }

    public Transaction? getLargestTransaction()
    {
        decimal maxAmount = 0;
        foreach (var tx in chain.SelectMany(block => block.Transactions))
        {
            if (tx.Amount > maxAmount)
            {
                maxAmount = tx.Amount;
            }
        }

        return chain.SelectMany(block => block.Transactions).FirstOrDefault(tx => tx.Amount == maxAmount);

    }
    
    public List<Transaction> getAddressHistory(string address)
    {
        List<Transaction> history = new List<Transaction>();
        foreach (var tx in chain.SelectMany(block => block.Transactions))
        {
            if (tx.From == address || tx.To == address)
            {
                history.Add(tx);
            }
                
        }
        return history;
    }
}