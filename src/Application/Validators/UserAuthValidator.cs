using AuthService.src.Application.DTOs.Commands;
using FluentValidation;

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