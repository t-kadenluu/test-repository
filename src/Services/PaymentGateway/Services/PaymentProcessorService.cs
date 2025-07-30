using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Security.Cryptography;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace PaymentGateway.API.Services
{
    public class PaymentProcessorService
    {
        private readonly ILogger<PaymentProcessorService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly DbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IValidator<PaymentRequest> _paymentValidator;
        private readonly IEncryptionService _encryptionService;

        public PaymentProcessorService(
            ILogger<PaymentProcessorService> logger,
            HttpClient httpClient,
            IConfiguration configuration,
            DbContext dbContext,
            IMapper mapper,
            IValidator<PaymentRequest> paymentValidator,
            IEncryptionService encryptionService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
            _dbContext = dbContext;
            _mapper = mapper;
            _paymentValidator = paymentValidator;
            _encryptionService = encryptionService;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            _logger.LogInformation("Processing payment for amount: {Amount}, Currency: {Currency}", 
                request.Amount, request.Currency);

            try
            {
                // Validate payment request
                var validationResult = await _paymentValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        TransactionId = Guid.NewGuid().ToString()
                    };
                }

                // Encrypt sensitive payment data
                var encryptedCardNumber = await _encryptionService.EncryptAsync(request.CardNumber);
                var encryptedCvv = await _encryptionService.EncryptAsync(request.CVV);

                // Create payment transaction record
                var transaction = new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    Amount = request.Amount,
                    Currency = request.Currency,
                    EncryptedCardNumber = encryptedCardNumber,
                    CardHolderName = request.CardHolderName,
                    Status = TransactionStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    MerchantId = request.MerchantId
                };

                await _dbContext.Set<PaymentTransaction>().AddAsync(transaction);
                await _dbContext.SaveChangesAsync();

                // Process with external payment provider
                var providerResult = await ProcessWithProviderAsync(request, transaction.Id);

                // Update transaction status
                transaction.Status = providerResult.Success ? TransactionStatus.Completed : TransactionStatus.Failed;
                transaction.ProviderTransactionId = providerResult.ProviderTransactionId;
                transaction.ProviderResponse = JsonSerializer.Serialize(providerResult);
                transaction.CompletedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Payment processing completed. Success: {Success}, TransactionId: {TransactionId}", 
                    providerResult.Success, transaction.Id);

                return new PaymentResult
                {
                    Success = providerResult.Success,
                    TransactionId = transaction.Id.ToString(),
                    ProviderTransactionId = providerResult.ProviderTransactionId,
                    ErrorMessage = providerResult.ErrorMessage,
                    ProcessedAt = transaction.CompletedAt ?? DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for amount: {Amount}", request.Amount);
                
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment processing failed due to an internal error",
                    TransactionId = Guid.NewGuid().ToString()
                };
            }
        }

        private async Task<ProviderPaymentResult> ProcessWithProviderAsync(PaymentRequest request, Guid transactionId)
        {
            var providerEndpoint = _configuration["PaymentProvider:Endpoint"];
            var apiKey = _configuration["PaymentProvider:ApiKey"];

            var providerRequest = new
            {
                Amount = request.Amount,
                Currency = request.Currency,
                CardNumber = request.CardNumber,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                CVV = request.CVV,
                CardHolderName = request.CardHolderName,
                TransactionReference = transactionId.ToString()
            };

            var jsonContent = JsonSerializer.Serialize(providerRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.PostAsync(providerEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ProviderPaymentResult>(responseContent);
                return result;
            }

            return new ProviderPaymentResult
            {
                Success = false,
                ErrorMessage = $"Provider error: {response.StatusCode} - {responseContent}"
            };
        }

        public async Task<PaymentTransaction> GetTransactionAsync(Guid transactionId)
        {
            return await _dbContext.Set<PaymentTransaction>()
                .FirstOrDefaultAsync(t => t.Id == transactionId);
        }

        public async Task<List<PaymentTransaction>> GetTransactionsByMerchantAsync(string merchantId, DateTime? fromDate = null)
        {
            var query = _dbContext.Set<PaymentTransaction>()
                .Where(t => t.MerchantId == merchantId);

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDate.Value);
            }

            return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }
    }

    public interface IEncryptionService
    {
        Task<string> EncryptAsync(string plainText);
        Task<string> DecryptAsync(string encryptedText);
    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CardNumber { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string CVV { get; set; }
        public string CardHolderName { get; set; }
        public string MerchantId { get; set; }
        public string Description { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string ProviderTransactionId { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    public class ProviderPaymentResult
    {
        public bool Success { get; set; }
        public string ProviderTransactionId { get; set; }
        public string ErrorMessage { get; set; }
        public string AuthorizationCode { get; set; }
    }

    public class PaymentTransaction
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string EncryptedCardNumber { get; set; }
        public string CardHolderName { get; set; }
        public string MerchantId { get; set; }
        public TransactionStatus Status { get; set; }
        public string ProviderTransactionId { get; set; }
        public string ProviderResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled,
        Refunded
    }
}
