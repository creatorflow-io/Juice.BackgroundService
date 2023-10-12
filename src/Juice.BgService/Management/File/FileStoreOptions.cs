namespace Juice.BgService.Management.File
{
    public class FileStoreOptions<TModel>
        where TModel : class, IServiceModel
    {
        public TModel[] Services { get; set; }
    }
}
