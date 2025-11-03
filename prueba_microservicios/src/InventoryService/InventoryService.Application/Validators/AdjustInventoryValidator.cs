using FluentValidation;
using InventoryService.Application.DTOs;

namespace InventoryService.Application.Validators;

public class AdjustInventoryValidator : AbstractValidator<AdjustInventoryDto>
{
    public AdjustInventoryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required");

        RuleFor(x => x.QuantityChange)
            .GreaterThan(0).WithMessage("QuantityChange must be greater than 0");

        RuleFor(x => x.MovementType)
            .NotEmpty().WithMessage("MovementType is required")
            .Must(x => x == "In" || x == "Out")
            .WithMessage("MovementType must be 'In' or 'Out'");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}

