namespace DTOs
{
    public class FailedEventMessage
    {
        public string OriginalMessage { get; set; } = default!;
        public string? Error { get; set; }
    }
}
