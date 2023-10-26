namespace Juice.BgService.Extensions.Logging
{
    public class FileLoggerOptions
    {
        public string? Directory { get; set; }
        public int RetainPolicyFileCount { get; set; } = 50;
        public int MaxFileSize = 5 * 1024 * 1024;
        public TimeSpan BufferTime { get; set; } = TimeSpan.FromSeconds(5);
        public bool ForkJobLog { get; set; } = true;
    }
}
