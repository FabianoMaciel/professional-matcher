using System.Diagnostics.CodeAnalysis;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Queries;

/// <summary>
/// Retrieves a single professional, with skills, experiences and education, by id.
/// </summary>
public class GetProfessionalById
{
    public class Handler(IProfessionalRepository repository, ProfessionalModelAssembler assembler)
    {
        /// <returns>The professional, or null if not found.</returns>
        public async Task<ProfessionalModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            var professional = await repository.GetById(request.Id, cancellationToken);
            return professional is null ? null : await assembler.Assemble(professional, cancellationToken);
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid Id { get; set; }
    }
}
