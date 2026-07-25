using FluentValidation;

namespace BrandSignal.Application.Campaigns.Commands.CreateCampaign;

public class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(100).WithMessage("Company name must not exceed 100 characters.");

        RuleFor(x => x.TargetKeyword)
            .NotEmpty().WithMessage("Target keyword is required.")
            .MaximumLength(50).WithMessage("Target keyword must not exceed 50 characters.");
    }
}