namespace Juice.BgService.Tests.Shared
{
    public class CustomServiceModel : IServiceModel
    {
        public Guid? Id { get; set; }

        public string Name { get; set; }

        public Dictionary<string, object?> Options { get; set; }

        public string AssemblyQualifiedName { get; set; }

        public string Custom { get; set; }
    }
}
