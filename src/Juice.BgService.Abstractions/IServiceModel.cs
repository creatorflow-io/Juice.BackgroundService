namespace Juice.BgService
{
    public interface IServiceModel
    {
        Guid? Id { get; }
        string Name { get; }
        Dictionary<string, object?>? Options { get; }
        string AssemblyQualifiedName { get; }
    }
}
