using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using FluentValidation;

namespace UserManagement.API.Services
{
    public class ProfileService
    {
        private readonly ILogger<ProfileService> _logger;
        private readonly DbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IValidator<UserProfile> _validator;

        public ProfileService(
            ILogger<ProfileService> logger,
            DbContext dbContext,
            UserManager<IdentityUser> userManager,
            IMapper mapper,
            IValidator<UserProfile> validator)
        {
            _logger = logger;
            _dbContext = dbContext;
            _userManager = userManager;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<UserProfile> GetUserProfileAsync(Guid userId)
        {
            _logger.LogInformation("Retrieving profile for user: {UserId}", userId);
            return await _dbContext.Set<UserProfile>()
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<UserProfile> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var profile = _mapper.Map<UserProfile>(dto);
            profile.UserId = userId;
            
            var validationResult = await _validator.ValidateAsync(profile);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            _dbContext.Set<UserProfile>().Update(profile);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Profile updated for user: {UserId}", userId);
            return profile;
        }
    }

    public class UserProfile
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Bio { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Country { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        public string Bio { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Country { get; set; }
    }
}
