using FluentValidation;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Api.Validation;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Industry).MaximumLength(100);
        RuleFor(x => x.ContactName).MaximumLength(150);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.ContactPhone).MaximumLength(50);
    }
}

public class PatchCustomerRequestValidator : AbstractValidator<PatchCustomerRequest>
{
    public PatchCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Industry).MaximumLength(100);
        RuleFor(x => x.ContactName).MaximumLength(150);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.ContactPhone).MaximumLength(50);
    }
}

public class CreateSiteRequestValidator : AbstractValidator<CreateSiteRequest>
{
    public CreateSiteRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.SiteType).MaximumLength(100);
    }
}

public class PatchSiteRequestValidator : AbstractValidator<PatchSiteRequest>
{
    public PatchSiteRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300).When(x => x.Address is not null);
        RuleFor(x => x.SiteType).MaximumLength(100);
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Role).Must(role => role is "Manager" or "Technician");
        RuleFor(x => x.TechnicianId).NotEmpty().When(x => x.Role == "Technician");
        RuleFor(x => x.TechnicianId).Empty().When(x => x.Role == "Manager");
    }
}

public class PatchUserRequestValidator : AbstractValidator<PatchUserRequest>
{
    public PatchUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).When(x => x.FullName is not null);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200).When(x => x.Email is not null);
        RuleFor(x => x.Phone).MaximumLength(50);
    }
}

public class CreateTechnicianRequestValidator : AbstractValidator<CreateTechnicianRequest>
{
    public CreateTechnicianRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Availability).Must(IsAvailability).WithMessage("Use Available, OnJob or OffDuty.");
        RuleFor(x => x.Skills).Must(ValidSkills).WithMessage("Use at most 20 skills, each 1-100 characters and without '|'.");
    }

    internal static bool IsAvailability(string? value) =>
        Enum.TryParse<TechnicianAvailability>(value, true, out var availability) &&
        Enum.IsDefined(availability);

    internal static bool ValidSkills(List<string>? skills) =>
        skills is not null && skills.Count <= 20 &&
        skills.All(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 100 && !s.Contains('|'));
}

public class PatchTechnicianRequestValidator : AbstractValidator<PatchTechnicianRequest>
{
    public PatchTechnicianRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150).When(x => x.FullName is not null);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200).When(x => x.Email is not null);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Availability).Must(CreateTechnicianRequestValidator.IsAvailability)
            .When(x => x.Availability is not null);
        RuleFor(x => x.Skills).Must(CreateTechnicianRequestValidator.ValidSkills)
            .When(x => x.Skills is not null);
    }
}
