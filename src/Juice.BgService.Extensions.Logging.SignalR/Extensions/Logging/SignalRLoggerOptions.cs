namespace Juice.BgService.Extensions.Logging
{
    public class SignalRLoggerOptions : FileLoggerOptions
    {
        public string HubUrl { get; set; }
        public string? JoinGroupMethod { get; set; }
        public string? LogMethod { get; set; }
        public string? StateMethod { get; set; }
        public bool IncludeScopes { get; set; } = true;
        public bool Disabled { get; set; }
    }
}
