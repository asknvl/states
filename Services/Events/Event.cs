namespace states.Services.Events
{
    public abstract record Event<TPayload>
    {
        public Guid Id { get; init; }
        public string Type { get; init; }
        public TPayload Payload { get; init; }
        public DateTime OccuredAt { get; init; }        

        protected Event(string eventType, TPayload payload)
        {
            Id = Guid.CreateVersion7();
            Type = eventType;
            Payload = payload;            
            OccuredAt = DateTime.UtcNow;            
        }

        protected Event(string eventType)
        {
            Id = Guid.CreateVersion7();
            Type = eventType;            
            OccuredAt = DateTime.UtcNow;
        }
    }
}
