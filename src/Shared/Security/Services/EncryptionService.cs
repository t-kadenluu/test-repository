using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using AutoMapper;
using FluentValidation;
using System.Security.Cryptography;
using System.Text;

namespace Security.Services
{
    public class EncryptionService
    {
        private readonly ILogger<EncryptionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IValidator<EncryptionRequest> _validator;
        private readonly string _encryptionKey;

        public EncryptionService(
            ILogger<EncryptionService> logger,
            IConfiguration configuration,
            IMapper mapper,
            IValidator<EncryptionRequest> validator)
        {
            _logger = logger;
            _configuration = configuration;
            _mapper = mapper;
            _validator = validator;
            _encryptionKey = _configuration["Security:EncryptionKey"];
        }

        public async Task<string> EncryptAsync(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            _logger.LogDebug("Encrypting data");

            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                var result = Convert.ToBase64String(aes.IV.Concat(encryptedBytes).ToArray());
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data");
                throw;
            }
        }

        public async Task<string> DecryptAsync(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            _logger.LogDebug("Decrypting data");

            try
            {
                var fullCipher = Convert.FromBase64String(encryptedText);
                var iv = fullCipher.Take(16).ToArray();
                var cipher = fullCipher.Skip(16).ToArray();

                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data");
                throw;
            }
        }

        public string GenerateHash(string input, string salt = null)
        {
            _logger.LogDebug("Generating hash");

            if (string.IsNullOrEmpty(salt))
            {
                salt = Guid.NewGuid().ToString();
            }

            using var sha256 = SHA256.Create();
            var saltedInput = input + salt;
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedInput));
            
            return Convert.ToBase64String(hashBytes) + ":" + salt;
        }

        public bool VerifyHash(string input, string hash)
        {
            try
            {
                var parts = hash.Split(':');
                if (parts.Length != 2) return false;

                var originalHash = parts[0];
                var salt = parts[1];

                var newHash = GenerateHash(input, salt);
                return newHash.Split(':')[0] == originalHash;
            }
            catch
            {
                return false;
            }
        }
    }

    public class EncryptionRequest
    {
        public string Data { get; set; }
        public string Algorithm { get; set; }
    }
}
