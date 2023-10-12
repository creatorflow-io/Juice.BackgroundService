namespace Juice.BgService.Management
{
    public interface IServiceManager : IManagedService
    {
        List<IManagedService> ManagedServices { get; }
    }
}
