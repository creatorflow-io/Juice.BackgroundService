namespace Juice.BgService
{
    /// <summary>
    /// Type-specific factory for creating instances of <typeparamref name="T"/>.
    /// Register an implementation in the DI container to override the default
    /// reflection-based instantiation in <c>ServiceFactory</c>
    /// for the service type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Declared in <c>Juice.BgService.Abstractions</c> so background service
    /// implementation projects can implement this interface without depending on
    /// the management layer (<c>Juice.BgService</c>).
    /// <para>
    /// When <see cref="CreateService{TModel}"/> returns <c>null</c>, the caller
    /// falls back to the default reflection-based creation path.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">
    /// The concrete <see cref="IManagedService"/> type this factory handles.
    /// </typeparam>
    public interface IServiceFactory<T>
        where T : class, IManagedService
    {
        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> for use with model
        /// type <typeparamref name="TModel"/>.
        /// </summary>
        /// <typeparam name="TModel">
        /// The service model type used to configure the created service.
        /// </typeparam>
        /// <returns>
        /// A new <see cref="IManagedService"/> instance, or <c>null</c> to
        /// signal that this factory opts out and the default creation path
        /// should be used instead.
        /// </returns>
        IManagedService? CreateService<TModel>()
            where TModel : class, IServiceModel;
    }
}
