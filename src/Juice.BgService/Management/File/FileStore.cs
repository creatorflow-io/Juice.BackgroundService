using Microsoft.Extensions.Options;

namespace Juice.BgService.Management.File
{
    public class FileStore<TModel> : IServiceRepository<TModel>
        where TModel : class, IServiceModel
    {

        private IOptionsMonitor<FileStoreOptions<TModel>> _optionsMonitor;
        public FileStore(IOptionsMonitor<FileStoreOptions<TModel>> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
            _optionsMonitor.OnChange((options) =>
            {
                var handler = OnChanged;
                if (handler != null)
                {
                    handler.Invoke(this, default!);
                }
            });
        }

        public event EventHandler<EventArgs> OnChanged;

        public async Task<IEnumerable<TModel>> GetServicesModelAsync(CancellationToken token)
        {
            await Task.Yield();
            return _optionsMonitor.CurrentValue.Services;
        }
    }
}
