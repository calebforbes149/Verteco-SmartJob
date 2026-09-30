using FluentValidation;
using VertecoSmartJob.Contracts.Reference;

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
