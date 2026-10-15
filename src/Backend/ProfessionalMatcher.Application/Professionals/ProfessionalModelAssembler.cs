using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals;

/// <summary>Builds the full professional profile (details plus skills, experiences and education).</summary>
public class ProfessionalModelAssembler(
    ISkillRepository skills,
    IExperienceRepository experiences,
    IEducationRepository education)
{
    public async Task<ProfessionalModel> Assemble(Professional professional, CancellationToken cancellationToken)
    {
        var skillList = await skills.ListByProfessionalId(professional.Id, cancellationToken);
        var experienceList = await experiences.ListByProfessionalId(professional.Id, cancellationToken);
        var educationList = await education.ListByProfessionalId(professional.Id, cancellationToken);

        return ProfessionalModel.From(professional, skillList, experienceList, educationList);
    }
}
