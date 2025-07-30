using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;
using AutoMapper;
using FluentValidation;
using System.Linq;

namespace DataProcessingWorker.Processors
{
    public class FileDataProcessor : IDataProcessor
    {
        private readonly ILogger<FileDataProcessor> _logger;
        private readonly DbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IValidator<FileProcessingRequest> _validator;
        private readonly IFileService _fileService;

        public FileDataProcessor(
            ILogger<FileDataProcessor> logger,
            DbContext dbContext,
            IMapper mapper,
            IValidator<FileProcessingRequest> validator,
            IFileService fileService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _validator = validator;
            _fileService = fileService;
        }

        public async Task<List<DataProcessingRequest>> GetPendingRequestsAsync()
        {
            _logger.LogDebug("Getting pending file processing requests");
            
            return await _dbContext.Set<DataProcessingRequest>()
                .Where(r => r.Status == ProcessingStatus.Pending && r.DataType == "File")
                .OrderBy(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task<DataProcessingResult> ProcessDataAsync(DataProcessingRequest request)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting file processing for request: {RequestId}", request.Id);

            try
            {
                var fileRequest = _mapper.Map<FileProcessingRequest>(request);
                var validationResult = await _validator.ValidateAsync(fileRequest);
                
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }

                // Update status to in progress
                request.Status = ProcessingStatus.InProgress;
                await _dbContext.SaveChangesAsync();

                // Process the file
                var recordsProcessed = await ProcessFileAsync(fileRequest);

                // Update status to completed
                request.Status = ProcessingStatus.Completed;
                await _dbContext.SaveChangesAsync();

                var endTime = DateTime.UtcNow;
                var processingTime = endTime - startTime;

                _logger.LogInformation("Completed file processing for request: {RequestId}, Records: {Records}, Duration: {Duration}ms", 
                    request.Id, recordsProcessed, processingTime.TotalMilliseconds);

                return new DataProcessingResult
                {
                    RequestId = request.Id,
                    Success = true,
                    Message = "File processed successfully",
                    RecordsProcessed = recordsProcessed,
                    ProcessedAt = endTime,
                    ProcessingTime = processingTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process file for request: {RequestId}", request.Id);
                
                request.Status = ProcessingStatus.Failed;
                await _dbContext.SaveChangesAsync();

                return new DataProcessingResult
                {
                    RequestId = request.Id,
                    Success = false,
                    Message = ex.Message,
                    RecordsProcessed = 0,
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
        }

        public async Task MarkAsFailedAsync(Guid requestId, string errorMessage)
        {
            var request = await _dbContext.Set<DataProcessingRequest>().FindAsync(requestId);
            if (request != null)
            {
                request.Status = ProcessingStatus.Failed;
                await _dbContext.SaveChangesAsync();
                _logger.LogWarning("Marked request as failed: {RequestId}, Error: {Error}", requestId, errorMessage);
            }
        }

        private async Task<int> ProcessFileAsync(FileProcessingRequest request)
        {
            if (!await _fileService.ExistsAsync(request.SourcePath))
            {
                throw new FileNotFoundException($"Source file not found: {request.SourcePath}");
            }

            var fileContent = await _fileService.ReadAllTextAsync(request.SourcePath);
            var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            var processedData = new List<ProcessedRecord>();
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var record = JsonSerializer.Deserialize<ProcessedRecord>(line);
                if (record != null)
                {
                    record.ProcessedAt = DateTime.UtcNow;
                    processedData.Add(record);
                }
            }

            // Save processed data
            await _dbContext.Set<ProcessedRecord>().AddRangeAsync(processedData);
            await _dbContext.SaveChangesAsync();

            // Optionally move/copy processed file
            if (!string.IsNullOrEmpty(request.DestinationPath))
            {
                await _fileService.CopyAsync(request.SourcePath, request.DestinationPath);
            }

            return processedData.Count;
        }
    }

    public interface IFileService
    {
        Task<bool> ExistsAsync(string path);
        Task<string> ReadAllTextAsync(string path);
        Task CopyAsync(string sourcePath, string destinationPath);
    }

    public class FileProcessingRequest
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public string FileType { get; set; }
        public Dictionary<string, object> ProcessingOptions { get; set; }
    }

    public class ProcessedRecord
    {
        public Guid Id { get; set; }
        public string Data { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string Source { get; set; }
    }

    public enum ProcessingStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }

    public class DataProcessingRequest
    {
        public Guid Id { get; set; }
        public string DataType { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public ProcessingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
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
}
