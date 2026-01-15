using AuthService.src.Application.DTOs.Commands;
using FluentValidation;

namespace AuthService.src.Application.Validators;

public sealed class GenerateTokenValidator : AbstractValidator<GenerateTokenRequest>
{
    public GenerateTokenValidator()
    {
        RuleFor(u => u.UserId)
            .NotEmpty();

        RuleFor(u => u.FullName)
            .NotEmpty()
            .Length(2, 25);

        RuleFor(u => u.Username)
            .NotEmpty()
            .Length(3, 25);

        RuleFor(u => u.Role)
            .NotEmpty();
    }
}