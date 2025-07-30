using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Collections.Generic;
using AutoMapper;
using FluentValidation;

namespace DataProcessingWorker.Services
{
    public class DataProcessingBackgroundService : BackgroundService
    {
        private readonly ILogger<DataProcessingBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly Timer _timer;

        public DataProcessingBackgroundService(
            ILogger<DataProcessingBackgroundService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Processing Worker started at: {Time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dataProcessor = scope.ServiceProvider.GetRequiredService<IDataProcessor>();
                    var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
                    var validator = scope.ServiceProvider.GetRequiredService<IValidator<DataProcessingRequest>>();

                    await ProcessPendingDataAsync(dataProcessor, mapper, validator);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing data processing");
                }

                var delayMinutes = _configuration.GetValue<int>("DataProcessing:DelayMinutes", 5);
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }
        }

        private async Task ProcessPendingDataAsync(IDataProcessor processor, IMapper mapper, IValidator<DataProcessingRequest> validator)
        {
            _logger.LogInformation("Starting data processing cycle at: {Time}", DateTimeOffset.Now);

            var pendingRequests = await processor.GetPendingRequestsAsync();
            
            foreach (var request in pendingRequests)
            {
                try
                {
                    var validationResult = await validator.ValidateAsync(request);
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Invalid data processing request: {RequestId}, Errors: {Errors}", 
                            request.Id, string.Join(", ", validationResult.Errors));
                        continue;
                    }

                    var result = await processor.ProcessDataAsync(request);
                    var resultDto = mapper.Map<DataProcessingResultDto>(result);
                    
                    _logger.LogInformation("Successfully processed data request: {RequestId}", request.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process data request: {RequestId}", request.Id);
                    await processor.MarkAsFailedAsync(request.Id, ex.Message);
                }
            }

            _logger.LogInformation("Completed data processing cycle at: {Time}", DateTimeOffset.Now);
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Processing Worker is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }

    public interface IDataProcessor
    {
        Task<List<DataProcessingRequest>> GetPendingRequestsAsync();
        Task<DataProcessingResult> ProcessDataAsync(DataProcessingRequest request);
        Task MarkAsFailedAsync(Guid requestId, string errorMessage);
    }

    public class DataProcessingRequest
    {
        public Guid Id { get; set; }
        public string DataType { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public DateTime CreatedAt { get; set; }
        public ProcessingStatus Status { get; set; }
    }

    public class DataProcessingResult
    {
        public Guid RequestId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public int RecordsProcessed { get; set; }
        public DateTime ProcessedAt { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    public class DataProcessingResultDto
    {
        public Guid RequestId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public int RecordsProcessed { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    public enum ProcessingStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
}
