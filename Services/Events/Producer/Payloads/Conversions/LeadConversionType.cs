namespace states.Services.Events.Producer.Payloads.Conversions
{
    /// <summary>
    /// Вид конверсии лида — дискриминатор внутри payload события lead.conversion
    /// (в JSON едет строкой через JsonStringEnumConverter продюсера).
    /// </summary>
    public enum LeadConversionType
    {
        Contact,
        Registration,
        Sale,
        Resale
    }
}
