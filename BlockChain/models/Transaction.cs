using BlockChain.models;
using BlockChain.service;

namespace BlockChain.models
{

    public class Transaction
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }

        public DateTime Timestamp { get; set; }
        
        public int minBlockHeight { get; set; }

        
        public byte[] Signature { get; set; }

        public Transaction(string from, string to, decimal amount, decimal fee = 0.01m, int minBlockHeight = 0)
        {
            Id = Guid.NewGuid().ToString();
            From = from;
            To = to;
            Amount = amount;
            Timestamp = DateTime.UtcNow;
            Fee = fee;
            this.minBlockHeight = minBlockHeight;
            
        }

        public string ToRawString()
        {
            return $"{Id}|{From}|{To}|{Amount}|{Timestamp}|{Fee}";
        }

        public override string ToString()
        {
            return $"Transaction ID: {Id} From: {From} To: {To} Amount: {Amount} Timestamp: {Timestamp} Signature: {Signature} Fee: {Fee}";
        }


    }

}

namespace BlockChain.service
    {

        public class TransactionService
        {
            private readonly CryptoService _cryptoService;
            private static BlockChainService _blockChainService;
            public TransactionService(BlockChainService blockChainService)
            {
                _cryptoService = new CryptoService();
                _blockChainService = blockChainService;
            }

            public static Transaction CreateTransaction(string from, string to, decimal amount, decimal fee, string privateKey)
            {
                var tx = new Transaction(from, to, amount, fee);
                SignTransaction(tx, privateKey);
                var validation = ValidateTransaction(tx);
                if (!validation.isValid)
                {
                    throw new Exception($"Invalid transaction: {validation.error}");
                }

                {
                    return tx;
                }
            }

            public static (bool isValid, string error) ValidateTransaction(Transaction tx)
            {
                if (tx.From == "COINBASE") return (true, null); // Coinbase transactions are always valid
                if (tx == null) return (false, "Transaction is null");
                if (string.IsNullOrEmpty(tx.From)) return (false, "From address is required");
                if (string.IsNullOrEmpty(tx.To)) return (false, "To address is required");
                if (tx.Amount <= 0) return (false, "Amount must be greater than 0");
                if (!CryptoService.VerifySignature( tx.ToRawString(), tx.Signature, tx.From)) return (false, "Invalid signature");
                if (tx.Timestamp < DateTime.UtcNow.AddMinutes(-1)) return (false, "Transaction is too old");
                if (tx.Timestamp > DateTime.UtcNow.AddMinutes(1)) return (false, "Transaction is too recent");
               //moved to mempool if (blockChainService.GetBalance(tx.From) < tx.Amount+tx.Fee) return (false, "Insufficient balance");
                if (tx.From == tx.To) return (false, "Transaction cannot send to itself");
                
                return (true, null);
            }

            public static void SignTransaction(Transaction tx, string privateKey)
            {
                var signature = CryptoService.Sign(tx.ToRawString(), privateKey);
                
                tx.Signature = signature; 
                
            }
        }

    }
