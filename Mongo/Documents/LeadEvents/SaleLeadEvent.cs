namespace states.Mongo.Documents.LeadEvents
{
    public sealed record SaleLeadEvent : LeadEventBaseDocument
    {
        public decimal DepositAmount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
    }
}
