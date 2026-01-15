using AuthService.src.Application.DTOs.Commands;
using FluentValidation;

namespace AuthService.src.Application.Validators;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(u => u.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.")
            .NotNull()
            .WithMessage("User ID is required.");

        RuleFor(u => u.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh Token is required.")
            .NotNull()
            .WithMessage("Refresh Token is required.");
    }
}