namespace states.Mongo.Documents.LeadEvents
{
    public sealed record RegistrationLeadEvent : LeadEventBaseDocument
    {
        public string CurrencyCode { get; set; } = string.Empty;
    }
}
