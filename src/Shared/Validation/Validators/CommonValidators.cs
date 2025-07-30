// Validation extensions and validators
using FluentValidation;

namespace Shared.Validation.Validators;

public class EmailValidator : AbstractValidator<string>
{
    public EmailValidator()
    {
        RuleFor(email => email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Please provide a valid email address.");
    }
}

public class PhoneNumberValidator : AbstractValidator<string>
{
    public PhoneNumberValidator()
    {
        RuleFor(phone => phone)
            .NotEmpty()
            .Matches(@"^\+?[\d\s\-\(\)]+$")
            .WithMessage("Please provide a valid phone number.");
    }
}

public class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator()
    {
        RuleFor(password => password)
            .NotEmpty()
            .MinimumLength(8)
            .Must(ContainSpecialCharacter)
            .WithMessage("Password must be at least 8 characters and contain special characters.");
    }
    
    private bool ContainSpecialCharacter(string password)
    {
        return password.Any(c => !char.IsLetterOrDigit(c));
    }
}
