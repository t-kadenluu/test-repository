using Microsoft.AspNetCore.Mvc;
using OrderProcessing.API.Models;
using OrderProcessing.API.Services;

namespace OrderProcessing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        _logger.LogInformation("Getting all orders");
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        _logger.LogInformation("Getting order with id {OrderId}", id);
        var order = await _orderService.GetOrderByIdAsync(id);
        
        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByUser(int userId)
    {
        _logger.LogInformation("Getting orders for user {UserId}", userId);
        var orders = await _orderService.GetOrdersByUserIdAsync(userId);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request)
    {
        _logger.LogInformation("Creating new order for user {UserId}", request.UserId);
        var order = await _orderService.CreateOrderAsync(request);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusRequest request)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status}", id, request.Status);
        var updated = await _orderService.UpdateOrderStatusAsync(id, request.Status);
        
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }
}
