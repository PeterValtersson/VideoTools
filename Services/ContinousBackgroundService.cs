using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoTools.Services
{
    public interface IContinuousWorkIteration
    {
        public Task Start(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        Task Run(CancellationToken stoppingToken);
    }
    public class ContinuousBackgroundOptions
    {
        public int cycleTime { get; set; }
    }
    public class ContinuousBackgroundService<TIteration>(TIteration iteration, IOptions<ContinuousBackgroundOptions> _options)
        : BackgroundService
        where TIteration : IContinuousWorkIteration
    {
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await iteration.Start(cancellationToken).ConfigureAwait(false);
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await iteration.StopAsync(cancellationToken).ConfigureAwait(false);
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = Task.Delay(_options.Value.cycleTime, stoppingToken);
                await iteration.Run(stoppingToken).ConfigureAwait(false);
                await delay;
            }
        }
    }

    public static partial class Registration
    {
        public static IServiceCollection AddContinuousBackgroundService<TIteration>(this IServiceCollection services)
            where TIteration : class, IContinuousWorkIteration
        {
            services.AddSingleton<TIteration>();
            services.AddHostedService<ContinuousBackgroundService<TIteration>>();
            return services;
        }
    }
}
