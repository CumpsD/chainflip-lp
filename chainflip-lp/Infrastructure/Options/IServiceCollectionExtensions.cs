namespace ChainflipLp.Infrastructure.Options
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureAndValidate<T>(
            this IServiceCollection services,
            IConfiguration configuration)
            where T : class
            => services
                .AddOptions<T>()
                .Bind(configuration)
                .ValidateDataAnnotationsRecursively()
                .ValidateEagerly()
                .Services;
    }
}