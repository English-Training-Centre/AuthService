using AuthService.src.Application.DTOs.Commands;
using FluentValidation;

namespace AuthService.src.Application.Validators;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(u => u.UserId)
            .NotEmpty();

        RuleFor(u => u.RefreshToken)
            .NotEmpty();
    }
}