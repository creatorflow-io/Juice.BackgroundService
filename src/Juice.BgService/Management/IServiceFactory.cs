namespace Juice.BgService.Management
{
    public interface IServiceFactory
    {
        //IManagedService? CreateService(Type type);
        IManagedService? CreateService<TModel>(string typeAssemblyQualifiedName)
            where TModel : class, IServiceModel;
        bool IsServiceExists(string typeAssemblyQualifiedName);
    }
}
