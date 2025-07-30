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

namespace OrderProcessing.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderManagementController : ControllerBase
    {
        private readonly ILogger<OrderManagementController> _logger;
        private readonly IOrderManagementService _orderService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateOrderRequest> _createOrderValidator;
        private readonly IValidator<UpdateOrderRequest> _updateOrderValidator;

        public OrderManagementController(
            ILogger<OrderManagementController> logger,
            IOrderManagementService orderService,
            IMapper mapper,
            IValidator<CreateOrderRequest> createOrderValidator,
            IValidator<UpdateOrderRequest> updateOrderValidator)
        {
            _logger = logger;
            _orderService = orderService;
            _mapper = mapper;
            _createOrderValidator = createOrderValidator;
            _updateOrderValidator = updateOrderValidator;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get orders for current user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<OrderResponseDto>>> GetOrders()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return BadRequest("Invalid user ID");
            }

            _logger.LogInformation("Getting orders for user: {UserId}", userId);
            var orders = await _orderService.GetOrdersAsync(userId);
            var orderDtos = _mapper.Map<List<OrderResponseDto>>(orders);
            
            return Ok(orderDtos);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Create new order")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var validationResult = await _createOrderValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return BadRequest("Invalid user ID");
            }

            var createOrderDto = _mapper.Map<CreateOrderRequestDto>(request);
            createOrderDto.CustomerId = userId;

            var order = await _orderService.CreateOrderAsync(createOrderDto);
            var orderDto = _mapper.Map<OrderResponseDto>(order);

            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, orderDto);
        }
    }

    public interface IOrderManagementService
    {
        Task<List<OrderResponseDto>> GetOrdersAsync(Guid customerId);
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderRequestDto dto);
    }

    public class CreateOrderRequest
    {
        public List<OrderItemRequest> Items { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class UpdateOrderRequest
    {
        public string ShippingAddress { get; set; }
        public string Status { get; set; }
    }

    public class OrderItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponseDto> Items { get; set; }
    }

    public class CreateOrderRequestDto
    {
        public Guid CustomerId { get; set; }
        public List<OrderItemResponseDto> Items { get; set; }
        public string ShippingAddress { get; set; }
    }

    public class OrderItemResponseDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
