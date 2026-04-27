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
            return $"Transaction ID: {Id} From: {From} To: {To} Amount: {Amount} Timestamp: {Timestamp}";
        }


    }

}

namespace BlockChain.service
    {

        public class TransactionService
        {
            public TransactionService()
            {

            }

            public static Transaction CreateTransaction(string from, string to, decimal amount)
            {
                var tx = new Transaction(from, to, amount);
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
                if (tx == null) return (false, "Transaction is null");
                if (string.IsNullOrEmpty(tx.From)) return (false, "From address is required");
                if (string.IsNullOrEmpty(tx.To)) return (false, "To address is required");
                if (tx.Amount <= 0) return (false, "Amount must be greater than 0");
                return (true, null);
            }
        }

    }
