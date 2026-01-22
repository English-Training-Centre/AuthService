using AuthService.src.Application.DTOs.Requests;
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