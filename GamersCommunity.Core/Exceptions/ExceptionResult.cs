namespace GamersCommunity.Core.Exceptions
{
    public class ExceptionResult
    {
        public string Message { get; set; } = "An unexpected error occurred.";
        public string? Exception { get; set; }
        public string? TraceId { get; set; }
    }
}
