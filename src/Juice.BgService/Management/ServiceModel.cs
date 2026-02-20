namespace Juice.BgService.Management
{
    public class ServiceModel : IServiceModel
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = default!;
        public Dictionary<string, object?>? Options { get; set; }
        public string AssemblyQualifiedName { get; set; } = default!;
    }
}
