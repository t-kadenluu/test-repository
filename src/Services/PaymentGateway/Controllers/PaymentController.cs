using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using AutoMapper;
using FluentValidation;

namespace PaymentGateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaymentProcessorService _paymentService;
        private readonly IMapper _mapper;
        private readonly IValidator<ProcessPaymentRequest> _validator;

        public PaymentController(
            ILogger<PaymentController> logger,
            IPaymentProcessorService paymentService,
            IMapper mapper,
            IValidator<ProcessPaymentRequest> validator)
        {
            _logger = logger;
            _paymentService = paymentService;
            _mapper = mapper;
            _validator = validator;
        }

        [HttpPost("process")]
        [SwaggerOperation(Summary = "Process payment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaymentResponseDto>> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var paymentRequest = _mapper.Map<PaymentRequestDto>(request);
            var result = await _paymentService.ProcessPaymentAsync(paymentRequest);
            var response = _mapper.Map<PaymentResponseDto>(result);

            if (result.Success)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }

        [HttpGet("transaction/{transactionId}")]
        [SwaggerOperation(Summary = "Get transaction details")]
        public async Task<ActionResult<TransactionDto>> GetTransaction(Guid transactionId)
        {
            var transaction = await _paymentService.GetTransactionAsync(transactionId);
            if (transaction == null)
            {
                return NotFound();
            }

            var dto = _mapper.Map<TransactionDto>(transaction);
            return Ok(dto);
        }
    }

    public interface IPaymentProcessorService
    {
        Task<PaymentResultDto> ProcessPaymentAsync(PaymentRequestDto request);
        Task<TransactionDto> GetTransactionAsync(Guid transactionId);
    }

    public class ProcessPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CardNumber { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string CVV { get; set; }
        public string CardHolderName { get; set; }
    }

    public class PaymentRequestDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CardNumber { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string CVV { get; set; }
        public string CardHolderName { get; set; }
        public string MerchantId { get; set; }
    }

    public class PaymentResponseDto
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    public class TransactionDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
