using FluentValidation;
using Libs.Core.Shared.src.DTOs.Requests;

namespace AuthService.src.Application.Validators;

public sealed class UserAuthValidator : AbstractValidator<UserAuthRequest>
{
    public UserAuthValidator()
    {
        RuleFor(u => u.Username)
            .NotEmpty()
            .Length(3, 25);

        RuleFor(u => u.Password)
            .NotEmpty()
            .MinimumLength(9);
    }
}