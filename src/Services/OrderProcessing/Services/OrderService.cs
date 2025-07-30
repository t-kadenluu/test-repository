using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using AutoMapper;
using FluentValidation;

namespace OrderProcessing.API.Services
{
    public class OrderService
    {
        private readonly ILogger<OrderService> _logger;
        private readonly DbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IValidator<Order> _orderValidator;
        private readonly IPaymentService _paymentService;

        public OrderService(
            ILogger<OrderService> logger,
            DbContext dbContext,
            IMapper mapper,
            IValidator<Order> orderValidator,
            IPaymentService paymentService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _orderValidator = orderValidator;
            _paymentService = paymentService;
        }

        public async Task<List<Order>> GetOrdersAsync(Guid customerId)
        {
            _logger.LogInformation("Retrieving orders for customer: {CustomerId}", customerId);
            
            return await _dbContext.Set<Order>()
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.OrderItems)
                .ToListAsync();
        }

        public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
        {
            var order = _mapper.Map<Order>(dto);
            order.Id = Guid.NewGuid();
            order.CreatedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Pending;

            var validationResult = await _orderValidator.ValidateAsync(order);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            await _dbContext.Set<Order>().AddAsync(order);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Order created: {OrderId}", order.Id);
            return order;
        }

        public async Task<bool> ProcessPaymentAsync(Guid orderId, PaymentDetails paymentDetails)
        {
            var order = await _dbContext.Set<Order>().FindAsync(orderId);
            if (order == null) return false;

            var paymentResult = await _paymentService.ProcessPaymentAsync(paymentDetails);
            if (paymentResult.Success)
            {
                order.Status = OrderStatus.Paid;
                order.PaidAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Payment processed for order: {OrderId}", orderId);
                return true;
            }

            return false;
        }
    }

    public class Order
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();
    }

    public class OrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Paid,
        Shipped,
        Delivered,
        Cancelled
    }

    public class CreateOrderDto
    {
        public Guid CustomerId { get; set; }
        public List<OrderItemDto> Items { get; set; }
    }

    public class OrderItemDto
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class PaymentDetails
    {
        public string CardNumber { get; set; }
        public string CardHolderName { get; set; }
        public string ExpiryDate { get; set; }
        public string CVV { get; set; }
        public decimal Amount { get; set; }
    }

    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentDetails details);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
