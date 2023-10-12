namespace Juice.BgService.Management
{
    public interface IServiceRepository<TModel>
        where TModel : class, IServiceModel
    {
        event EventHandler<EventArgs> OnChanged;
        Task<IEnumerable<TModel>> GetServicesModelAsync(CancellationToken token);

    }
}
