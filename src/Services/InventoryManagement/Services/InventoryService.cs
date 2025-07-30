using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using AutoMapper;
using FluentValidation;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace InventoryManagement.API.Services
{
    public class InventoryService
    {
        private readonly ILogger<InventoryService> _logger;
        private readonly DbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IValidator<Product> _productValidator;
        private readonly IValidator<StockMovement> _stockValidator;
        private readonly INotificationService _notificationService;

        public InventoryService(
            ILogger<InventoryService> logger,
            DbContext dbContext,
            IMapper mapper,
            IValidator<Product> productValidator,
            IValidator<StockMovement> stockValidator,
            INotificationService notificationService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _productValidator = productValidator;
            _stockValidator = stockValidator;
            _notificationService = notificationService;
        }

        public async Task<List<Product>> GetProductsAsync(ProductFilter filter = null)
        {
            _logger.LogInformation("Retrieving products with filter: {Filter}", JsonConvert.SerializeObject(filter));

            var query = _dbContext.Set<Product>().AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Category))
                {
                    query = query.Where(p => p.Category == filter.Category);
                }

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    query = query.Where(p => p.Name.Contains(filter.SearchTerm) || p.Description.Contains(filter.SearchTerm));
                }

                if (filter.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= filter.MinPrice.Value);
                }

                if (filter.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= filter.MaxPrice.Value);
                }

                if (filter.InStockOnly)
                {
                    query = query.Where(p => p.StockQuantity > 0);
                }
            }

            return await query.Include(p => p.StockMovements).ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(Guid productId)
        {
            _logger.LogDebug("Getting product by ID: {ProductId}", productId);
            
            return await _dbContext.Set<Product>()
                .Include(p => p.StockMovements)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<Product> CreateProductAsync(CreateProductDto dto)
        {
            var product = _mapper.Map<Product>(dto);
            product.Id = Guid.NewGuid();
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            var validationResult = await _productValidator.ValidateAsync(product);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            await _dbContext.Set<Product>().AddAsync(product);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);
            return product;
        }

        public async Task<bool> UpdateStockAsync(Guid productId, int quantity, StockMovementType movementType, string reason = null)
        {
            var product = await GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product not found for stock update: {ProductId}", productId);
                return false;
            }

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Quantity = quantity,
                MovementType = movementType,
                Reason = reason,
                CreatedAt = DateTime.UtcNow,
                PreviousQuantity = product.StockQuantity
            };

            var validationResult = await _stockValidator.ValidateAsync(movement);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid stock movement for product {ProductId}: {Errors}", 
                    productId, string.Join(", ", validationResult.Errors));
                return false;
            }

            // Update product stock
            switch (movementType)
            {
                case StockMovementType.StockIn:
                    product.StockQuantity += quantity;
                    break;
                case StockMovementType.StockOut:
                    if (product.StockQuantity < quantity)
                    {
                        _logger.LogWarning("Insufficient stock for product {ProductId}. Available: {Available}, Requested: {Requested}", 
                            productId, product.StockQuantity, quantity);
                        return false;
                    }
                    product.StockQuantity -= quantity;
                    break;
                case StockMovementType.Adjustment:
                    product.StockQuantity = quantity;
                    break;
            }

            movement.NewQuantity = product.StockQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _dbContext.Set<StockMovement>().AddAsync(movement);
            await _dbContext.SaveChangesAsync();

            // Check for low stock alerts
            if (product.StockQuantity <= product.LowStockThreshold)
            {
                await _notificationService.SendLowStockAlertAsync(product);
            }

            _logger.LogInformation("Stock updated for product {ProductId}: {MovementType} {Quantity}, New Stock: {NewStock}", 
                productId, movementType, quantity, product.StockQuantity);

            return true;
        }

        public async Task<List<Product>> GetLowStockProductsAsync()
        {
            return await _dbContext.Set<Product>()
                .Where(p => p.StockQuantity <= p.LowStockThreshold)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();
        }
    }

    public interface INotificationService
    {
        Task SendLowStockAlertAsync(Product product);
    }

    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SKU { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<StockMovement> StockMovements { get; set; } = new();
    }

    public class StockMovement
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public StockMovementType MovementType { get; set; }
        public string Reason { get; set; }
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum StockMovementType
    {
        StockIn,
        StockOut,
        Adjustment
    }

    public class CreateProductDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string SKU { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int InitialStock { get; set; }
        public int LowStockThreshold { get; set; }
    }

    public class ProductFilter
    {
        public string Category { get; set; }
        public string SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; }
    }
}
