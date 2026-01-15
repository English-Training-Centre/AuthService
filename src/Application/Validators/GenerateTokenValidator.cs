using AuthService.src.Application.DTOs.Commands;
using FluentValidation;

namespace AuthService.src.Application.Validators;

public sealed class GenerateTokenValidator : AbstractValidator<GenerateTokenRequest>
{
    public GenerateTokenValidator()
    {
        RuleFor(u => u.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.")
            .NotNull()
            .WithMessage("User ID is required.");

        RuleFor(u => u.FullName)
            .NotEmpty()
            .WithMessage("FullName is required.")
            .NotNull()
            .WithMessage("FullName is required.")
            .Length(2, 25)
            .WithMessage("FullName must be between 2 and 25 characters.");

        RuleFor(u => u.Username)
            .NotEmpty()
            .WithMessage("Username is required.")
            .NotNull()
            .WithMessage("Username is required.")
            .Length(3, 25)
            .WithMessage("Username must be between 3 and 25 characters.");

        RuleFor(u => u.Role)
            .NotEmpty()
            .WithMessage("Role is required.")
            .NotNull()
            .WithMessage("Role is required.");
    }
}