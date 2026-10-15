using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Validators;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Updates the general data of a professional (name, e-mail, headline, location, summary).
/// </summary>
public class UpdateProfessionalDetails
{
    public class Handler(IProfessionalRepository repository, ProfessionalModelAssembler assembler)
    {
        /// <returns>The updated professional, or null if not found.</returns>
        public async Task<ProfessionalModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            var professional = await repository.GetById(request.Id, cancellationToken);
            if (professional is null) return null;

            request.Input.ApplyTo(professional);
            await repository.Replace(professional, cancellationToken);

            return await assembler.Assemble(professional, cancellationToken);
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid Id { get; set; }
        public ProfessionalDetailsInput Input { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Input).NotNull().SetValidator(new ProfessionalDetailsInputValidator());
    }
}
