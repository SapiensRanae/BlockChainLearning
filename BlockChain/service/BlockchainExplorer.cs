using BlockChain.models;

namespace BlockChain.service;

public class BlockchainExplorer
{
    private readonly BlockChainService _blockChainService;
    public List<Block> Chain { get; }

    public BlockchainExplorer(BlockChainService blockChainService, List<Block> chain)
    {
        _blockChainService = blockChainService;
        Chain = chain;
    }

    public decimal GetTotalVolume()
    {
        return Chain.Sum(block => block.Transactions.Sum(tx => tx.Amount));
    }

    public Transaction? GetLargestTransaction()
    {
        return Chain.SelectMany(block => block.Transactions)
            .OrderByDescending(tx => tx.Amount)
            .FirstOrDefault();
    }

    public List<Transaction> GetTransactionHistory(string address)
    {
        return Chain.SelectMany(block => block.Transactions)
            .Where(tx => tx.From == address || tx.To == address)
            .OrderByDescending(tx => tx.Timestamp)
            .ToList();
    }

    public decimal GetTotalFeesEarned(string minerAddress)
    {
        return Chain
            .Where(block => block.Transactions.Count > 0)
            .Where(block => (block.Transactions[0].From == "COINBASE" || block.Transactions[0].From == "MINT") && block.Transactions[0].To == minerAddress)
            .Sum(block => block.Transactions.Where(tx => tx.From != "COINBASE" && tx.From != "MINT").Sum(tx => tx.Fee));
    }

    public Transaction? FindTransactionById(string txId)
    {
        return Chain.SelectMany(block => block.Transactions)
            .Concat(_blockChainService.PendingTransactions)
            .FirstOrDefault(tx => tx.Id == txId);
    }

    public Block? FindBlockByTransactionId(string txId)
    {
        return Chain.FirstOrDefault(block => block.Transactions.Any(tx => tx.Id == txId));
    }

    public (Block? block, Transaction? tx) FindTransactionLocation(string txId)
    {
        var tx = FindTransactionById(txId);
        if (tx == null)
        {
            return (null, null);
        }

        return (FindBlockByTransactionId(txId), tx);
    }

    public decimal GetTotalCoins()
    {
        return Chain.Sum(block => block.Transactions.Sum(tx => tx.Amount));
    }
}