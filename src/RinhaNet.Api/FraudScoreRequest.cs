namespace RinhaNet.Api;

public class FraudScoreRequest
{
    public string Id { get; set; }
    public Transaction Transaction { get; set; }
    public Customer Customer { get; set; }
    public Merchant Merchant { get; set; }
    public Terminal Terminal { get; set; }
    public LastTransaction Last_transaction { get; set; }
}

public class Transaction
{
    public float Amount { get; set; }
    public int Installments { get; set; }
    public DateTimeOffset Requested_at { get; set; }
}

public class Customer
{
    public float Avg_amount { get; set; }
    public int Tx_count_24h { get; set; }
    public string[] Known_merchants { get; set; }
}

public class Merchant
{
    public string Id { get; set; }
    public string Mcc { get; set; }
    public float Avg_amount { get; set; }
}

public class Terminal
{
    public bool Is_online { get; set; }
    public bool Card_present { get; set; }
    public float Km_from_home { get; set; }
}

public class LastTransaction
{
    public DateTimeOffset Timestamp { get; set; }
    public float Km_from_current { get; set; }
}