using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TikBot.Services;

namespace TikBot.Jobs
{
    public class PerformanceMonitorJob : BackgroundService
    {
        private readonly PerformanceMonitorService _performanceMonitorService;

        public PerformanceMonitorJob(PerformanceMonitorService performanceMonitorService)
        {
            _performanceMonitorService = performanceMonitorService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow.AddHours(3.5); // Iran time (UTC+3:30)
                var today7Am = new DateTime(now.Year, now.Month, now.Day, 7, 0, 0);
                var today8Am = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
                var nextRun = now < today7Am ? today7Am : now < today8Am ? today8Am : today7Am.AddDays(1);

                var delay = nextRun - now;
                if (delay.TotalMilliseconds > 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                now = DateTime.UtcNow.AddHours(3.5);
                if (Math.Abs((now - today7Am).TotalMinutes) < 5)
                {
                    await _performanceMonitorService.CollectReportsAsync(stoppingToken);
                }
                else if (Math.Abs((now - today8Am).TotalMinutes) < 5)
                {
                    await _performanceMonitorService.SendReportsAsync(stoppingToken);
                }
            }
        }
    }
}
