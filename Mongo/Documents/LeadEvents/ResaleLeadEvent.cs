namespace states.Mongo.Documents.LeadEvents
{
    public sealed record ResaleLeadEvent : LeadEventBaseDocument
    {
        public decimal DepositAmount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
    }
}
