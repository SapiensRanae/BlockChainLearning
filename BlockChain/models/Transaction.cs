using BlockChain.models;

namespace BlockChain.models
{

    public class Transaction
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal Amount { get; set; }

        public DateTime Timestamp { get; set; }
        
        public byte[] Signature { get; set; }

        public Transaction(string from, string to, decimal amount)
        {
            Id = Guid.NewGuid().ToString();
            From = from;
            To = to;
            Amount = amount;
            Timestamp = DateTime.UtcNow;
        }

        public string ToRawString()
        {
            return $"{Id}|{From}|{To}|{Amount}|{Timestamp}";
        }

        public override string ToString()
        {
            return $"Transaction ID: {Id} From: {From} To: {To} Amount: {Amount} Timestamp: {Timestamp} Signature: {Signature}";
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

            public Transaction CreateTransaction(string from, string to, decimal amount, string privateKey)
            {
                var tx = new Transaction(from, to, amount);
                SignTransaction(tx, privateKey);
                var validation = ValidateTransaction(tx, _blockChainService);
                if (!validation.isValid)
                {
                    throw new Exception($"Invalid transaction: {validation.error}");
                }

                {
                    return tx;
                }
            }

            public static (bool isValid, string error) ValidateTransaction(Transaction tx, BlockChainService blockChainService )
            {
                if (tx == null) return (false, "Transaction is null");
                if (string.IsNullOrEmpty(tx.From)) return (false, "From address is required");
                if (string.IsNullOrEmpty(tx.To)) return (false, "To address is required");
                if (tx.Amount <= 0) return (false, "Amount must be greater than 0");
                if (!CryptoService.VerifySignature( tx.ToRawString(), tx.Signature, tx.From)) return (false, "Invalid signature");
                if (tx.Timestamp < DateTime.UtcNow.AddMinutes(-1)) return (false, "Transaction is too old");
                if (tx.Timestamp > DateTime.UtcNow.AddMinutes(1)) return (false, "Transaction is too recent");
                if (blockChainService.GetBalance(tx.From) < tx.Amount) return (false, "Insufficient balance");
                return (true, null);
            }

            public static void SignTransaction(Transaction tx, string privateKey)
            {
                var signature = CryptoService.Sign(tx.ToRawString(), privateKey);
                
                tx.Signature = signature; 
                
            }
        }

    }
