using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using System;

namespace ZudamalMavjiSomonServices
{
    public class Worker : BackgroundService
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(Worker));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.Info("Start:");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Service.Start();
                }
                catch(Exception ex)
                {
                    _log.Error(ex);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
