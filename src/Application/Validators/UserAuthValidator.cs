using AuthService.src.Application.DTOs.Commands;
using FluentValidation;

namespace AuthService.src.Application.Validators;

public sealed class UserAuthValidator : AbstractValidator<UserAuthRequest>
{
    public UserAuthValidator()
    {
        RuleFor(u => u.Username)
            .NotEmpty()
            .WithMessage("Username is required.")
            .NotNull()
            .WithMessage("Username is required.")
            .Length(3, 25)
            .WithMessage("Username must be between 3 and 25 characters.");

        RuleFor(u => u.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .NotNull()
            .WithMessage("Passowrd is required.")
            .MinimumLength(9)
            .WithMessage("Password must be at least 9 characters");
    }
}