using Juice.Plugins.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Juice.BgService.Management
{
    public class ServiceFactory
    {
        private IServiceProvider _serviceProvider;
        private IPluginsManager? _pluginsManager;
        private ILogger _logger;
        public ServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _pluginsManager = serviceProvider.GetService<IPluginsManager>();
            _logger = serviceProvider.GetRequiredService<ILogger<ServiceFactory>>();
        }

        public IManagedService? CreateService<TModel>(TModel serviceModel)
            where TModel : class, IServiceModel
        {
            var typeAssemblyQualifiedName = serviceModel.AssemblyQualifiedName;
            try
            {
                var type = Type.GetType(typeAssemblyQualifiedName);
                if (type != null && type.IsAssignableTo(typeof(IManagedService)))
                {
                    _logger.LogInformation($"Found {type.Name} in app services");
                    var result = TryCreateFromTypedFactory<TModel>(type, _serviceProvider);
                    if (result != null)
                    {
                        return result;
                    }
                    return CreateService<TModel>(type, _serviceProvider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create service {typeAssemblyQualifiedName}");
            }

            if (_pluginsManager?.Plugins?.Any() ?? false)
            {
                _logger.LogInformation($"Found {_pluginsManager.Plugins.Count()} plugins");
            }
            else
            {
                _logger.LogInformation($"No plugins found");
            }

            return _pluginsManager?.Plugins?
                .Where(p => p.IsLoaded)
                .Select(p =>
                {
                    var t = p.GetType(typeAssemblyQualifiedName);
                    if (t != null && t.IsAssignableTo(typeof(IManagedService)))
                    {
                        var result = TryCreateFromTypedFactory<TModel>(t, p.ServiceProvider!);
                        if (result != null)
                        {
                            _logger.LogInformation($"Found {t.Name} via typed factory in plugin {p.Name}");
                            return result;
                        }
                        var r = CreateService<TModel>(t, p.ServiceProvider!);
                        if (r != null)
                        {
                            _logger.LogInformation($"Found {t.Name} in plugin {p.Name}");
                        }
                        return r;
                    }
                    return default;
                })
                .FirstOrDefault(s => s != default);
        }

        public bool IsServiceExists(IServiceModel serviceModel)
        {
            var typeAssemblyQualifiedName = serviceModel.AssemblyQualifiedName;
            try
            {
                var type = Type.GetType(typeAssemblyQualifiedName);
                if (type != null)
                {
                    if (type.IsAssignableTo(typeof(IManagedService)))
                    {
                        _logger.LogInformation($"Found {type.Name} in app services");
                        return true;
                    }
                    if (_serviceProvider.GetService(typeof(IServiceFactory<>).MakeGenericType(type)) != null)
                    {
                        _logger.LogInformation($"Found typed factory for {type.Name} in app services");
                        return true;
                    }
                }
            }
            catch (Exception)
            { }

            if (_pluginsManager?.Plugins?.Any() ?? false)
            {
                _logger.LogInformation($"Found {_pluginsManager.Plugins.Count()} plugins");
            }
            else
            {
                _logger.LogInformation($"No plugins found");
            }

            return _pluginsManager?.Plugins?
                .Any(p =>
                {
                    var t = p.GetType(typeAssemblyQualifiedName);
                    if (t != null)
                    {
                        _logger.LogInformation($"Found {t!.Name} in plugin {p.Name}");
                    }
                    if (t != null && t.IsAssignableTo(typeof(IManagedService)))
                    {
                        _logger.LogInformation($"Found {t!.Name} of type IManagedService in plugin {p.Name}");
                        return true;
                    }
                    if (t != null && p.ServiceProvider?.GetService(typeof(IServiceFactory<>).MakeGenericType(t)) != null)
                    {
                        _logger.LogInformation($"Found typed factory for {t!.Name} in plugin {p.Name}");
                        return true;
                    }
                    return false;
                }) ?? false;
        }

        private IManagedService? TryCreateFromTypedFactory<TModel>(Type type, IServiceProvider serviceProvider)
            where TModel : class, IServiceModel
        {
            try
            {
                var factoryType = typeof(IServiceFactory<>).MakeGenericType(type);
                var factory = serviceProvider.GetService(factoryType);
                if (factory == null)
                {
                    return null;
                }
                var createMethod = factoryType.GetMethod(nameof(IServiceFactory<IManagedService>.CreateService))!
                    .MakeGenericMethod(typeof(TModel));
                var result = (IManagedService?)createMethod.Invoke(factory, null);
                if (result == null)
                {
                    _logger.LogDebug($"Typed factory for {type.Name} returned null, falling back to default creation");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Typed factory for {type.Name} threw an exception, falling back to default creation");
                return null;
            }
        }

        private static IManagedService? CreateService<TModel>(Type type, IServiceProvider serviceProvider)
            where TModel : class, IServiceModel
        {
            if (type.IsAssignableTo(typeof(IManagedService)))
            {
                var serviceArgs = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length)
                               .First().GetParameters()
                               .Select(p => p.ParameterType.IsAssignableFrom(typeof(IServiceProvider))
                                   ? serviceProvider : serviceProvider.GetRequiredService(p.ParameterType))
                               .ToArray();

                if (type.ContainsGenericParameters)
                {
                    return (IManagedService?)Activator.CreateInstance(type.MakeGenericType(typeof(TModel)), serviceArgs);
                }
                return (IManagedService?)Activator.CreateInstance(type, serviceArgs);
            }
            return default;
        }
    }
}
