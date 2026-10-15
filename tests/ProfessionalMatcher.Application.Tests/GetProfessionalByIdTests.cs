using ProfessionalMatcher.Application.Professionals;
using ProfessionalMatcher.Application.Professionals.Queries;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Tests;

public class GetProfessionalByIdTests
{
    private readonly IProfessionalRepository _repository = Substitute.For<IProfessionalRepository>();
    private readonly ISkillRepository _skills = Substitute.For<ISkillRepository>();
    private readonly IExperienceRepository _experiences = Substitute.For<IExperienceRepository>();
    private readonly IEducationRepository _education = Substitute.For<IEducationRepository>();

    private GetProfessionalById.Handler CreateHandler() =>
        new(_repository, new ProfessionalModelAssembler(_skills, _experiences, _education));

    [Fact]
    public async Task Handle_ProfessionalExists_ReturnsProfileWithCollections()
    {
        var professional = new Professional { FullName = "Jane Doe", Email = "jane@example.com" };
        _repository.GetById(professional.Id, Arg.Any<CancellationToken>()).Returns(professional);
        _skills.ListByProfessionalId(professional.Id, Arg.Any<CancellationToken>())
            .Returns([new Skill { ProfessionalId = professional.Id, Name = "C#", Category = "Backend", Level = 5 }]);
        _experiences.ListByProfessionalId(professional.Id, Arg.Any<CancellationToken>())
            .Returns([new Experience
            {
                ProfessionalId = professional.Id,
                Company = "Acme",
                Role = "Dev",
                StartDate = new DateOnly(2020, 1, 1),
            }]);
        _education.ListByProfessionalId(professional.Id, Arg.Any<CancellationToken>()).Returns([]);

        var model = await CreateHandler().Handle(new GetProfessionalById.Request { Id = professional.Id }, CancellationToken.None);

        model!.Email.Should().Be("jane@example.com");
        model.Headline.Should().BeEmpty("null text columns are exposed as empty strings");
        model.PhotoUrl.Should().BeNull();
        model.Skills.Should().ContainSingle().Which.Name.Should().Be("C#");
        model.Experiences.Should().ContainSingle().Which.StartDate.Should().Be("2020-01");
        model.Experiences[0].EndDate.Should().BeNull();
        model.Education.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProfessionalWithPhoto_ReturnsVersionedPhotoUrl()
    {
        var professional = Professional.Reconstitute(
            Guid.NewGuid(), "Jane Doe", "jane@example.com", null, null, null,
            photoUpdatedAt: DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000),
            createdAt: DateTimeOffset.UtcNow, updatedAt: DateTimeOffset.UtcNow);
        _repository.GetById(professional.Id, Arg.Any<CancellationToken>()).Returns(professional);
        _skills.ListByProfessionalId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _experiences.ListByProfessionalId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _education.ListByProfessionalId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);

        var model = await CreateHandler().Handle(new GetProfessionalById.Request { Id = professional.Id }, CancellationToken.None);

        model!.PhotoUrl.Should().Be($"/api/v1/professionals/{professional.Id}/photo?v=1700000000000");
    }

    [Fact]
    public async Task Handle_ProfessionalMissing_ReturnsNull()
    {
        _repository.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Professional?)null);

        var model = await CreateHandler().Handle(new GetProfessionalById.Request { Id = Guid.NewGuid() }, CancellationToken.None);

        model.Should().BeNull();
    }
}
