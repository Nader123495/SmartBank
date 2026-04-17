// SmartBank.Application/Validators/ComplaintValidators.cs
using FluentValidation;
using SmartBank.Application.DTOs;

namespace SmartBank.Application.Validators
{
    public class CreateComplaintValidator : AbstractValidator<CreateComplaintDto>
    {
        private static readonly string[] ValidChannels = ["Agence", "Téléphone", "E-Banking", "Email", "Autre"];
        private static readonly string[] ValidPriorities = ["Faible", "Moyenne", "Haute", "Critique"];

        public CreateComplaintValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Le titre est obligatoire.")
                .MinimumLength(5).WithMessage("Le titre doit contenir au moins 5 caractères.")
                .MaximumLength(200);

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("La description est obligatoire.")
                .MinimumLength(20).WithMessage("La description doit contenir au moins 20 caractères.");

            RuleFor(x => x.ComplaintTypeId)
                .GreaterThan(0).WithMessage("Le type de réclamation est obligatoire.");

            RuleFor(x => x.Channel)
                .NotEmpty()
                .Must(c => ValidChannels.Contains(c)).WithMessage("Canal invalide.");

            RuleFor(x => x.Priority)
                .Must(p => ValidPriorities.Contains(p)).WithMessage("Priorité invalide.");

            RuleFor(x => x.ClientEmail)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.ClientEmail))
                .WithMessage("Email client invalide.");
        }
    }

    public class LoginValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        }
    }
}
