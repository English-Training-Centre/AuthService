using FluentValidation;
using Libs.Core.Public.src.DTOs.Requests;

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