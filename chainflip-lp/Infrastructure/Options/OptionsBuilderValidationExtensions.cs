namespace ChainflipLp.Infrastructure.Options
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Extension methods for adding configuration related options services to the DI container via <see cref="OptionsBuilder{TOptions}"/>.
    /// </summary>
    public static class OptionsBuilderValidationExtensions
    {
        /// <summary>
        /// Register this options instance for validation of its DataAnnotations.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        public static OptionsBuilder<TOptions> ValidateDataAnnotationsRecursively<TOptions>(this OptionsBuilder<TOptions> optionsBuilder)
            where TOptions : class
        {
            if (optionsBuilder == null)
                throw new ArgumentNullException(nameof(optionsBuilder));

            optionsBuilder
                .Services
                .AddSingleton<IValidateOptions<TOptions>>(new DataAnnotationsValidateRecursiveOptions<TOptions>(optionsBuilder.Name));

            return optionsBuilder;
        }

        /// <summary>
        /// Validates this options instance at startup.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        public static OptionsBuilder<TOptions> ValidateEagerly<TOptions>(this OptionsBuilder<TOptions> optionsBuilder)
            where TOptions : class
        {
            if (optionsBuilder == null)
                throw new ArgumentNullException(nameof(optionsBuilder));

            var tempProvider = optionsBuilder.Services.BuildServiceProvider();
            var tempOptions = tempProvider.GetRequiredService<IOptions<TOptions>>();

            try
            {
                // Trigger for validating options.
                _ = tempOptions.Value;
            }
            catch (OptionsValidationException ex)
            {
                throw new ValidationException($"{typeof(TOptions).Name} failed validation.", ex);
            }

            return optionsBuilder;
        }
    }
}