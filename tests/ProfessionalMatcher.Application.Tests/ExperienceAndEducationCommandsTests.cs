using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Commands;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Tests;

public class ExperienceAndEducationCommandsTests
{
    private readonly IProfessionalRepository _professionals = Substitute.For<IProfessionalRepository>();
    private readonly IExperienceRepository _experiences = Substitute.For<IExperienceRepository>();
    private readonly IEducationRepository _education = Substitute.For<IEducationRepository>();
    private readonly Guid _professionalId = Guid.NewGuid();

    [Fact]
    public async Task CreateExperience_ParsesMonthsAndCleansTechnologies()
    {
        _professionals.GetById(_professionalId, Arg.Any<CancellationToken>())
            .Returns(new Professional { FullName = "Jane", Email = "jane@example.com" });
        var handler = new CreateExperience.Handler(_professionals, _experiences);
        var input = new ExperienceInput
        {
            Company = "Acme",
            Role = "Dev",
            StartDate = "2020-01",
            EndDate = "2022-06",
            Technologies = [" .NET ", "", "Azure"],
        };

        var model = await handler.Handle(
            new CreateExperience.Request { ProfessionalId = _professionalId, Input = input }, CancellationToken.None);

        model!.StartDate.Should().Be("2020-01");
        model.EndDate.Should().Be("2022-06");
        model.Technologies.Should().Equal(".NET", "Azure");
        await _experiences.Received(1).Add(
            Arg.Is<Experience>(e => e.ProfessionalId == _professionalId && e.StartDate == new DateOnly(2020, 1, 1)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateExperience_EmptyEndDate_MarksAsCurrent()
    {
        var experience = new Experience
        {
            ProfessionalId = _professionalId,
            Company = "Acme",
            Role = "Dev",
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = new DateOnly(2021, 1, 1),
        };
        _experiences.GetById(experience.Id, Arg.Any<CancellationToken>()).Returns(experience);
        var handler = new UpdateExperience.Handler(_experiences);

        var model = await handler.Handle(
            new UpdateExperience.Request
            {
                ProfessionalId = _professionalId,
                ExperienceId = experience.Id,
                Input = new ExperienceInput { Company = "Acme", Role = "Lead", StartDate = "2020-01", EndDate = "" },
            },
            CancellationToken.None);

        model!.EndDate.Should().BeNull();
        model.Role.Should().Be("Lead");
    }

    [Fact]
    public async Task DeleteExperience_ExperienceOfAnotherProfessional_ReturnsFalse()
    {
        var experience = new Experience { ProfessionalId = Guid.NewGuid(), Company = "Acme", Role = "Dev" };
        _experiences.GetById(experience.Id, Arg.Any<CancellationToken>()).Returns(experience);
        var handler = new DeleteExperience.Handler(_experiences);

        var deleted = await handler.Handle(
            new DeleteExperience.Request { ProfessionalId = _professionalId, ExperienceId = experience.Id },
            CancellationToken.None);

        deleted.Should().BeFalse();
        await _experiences.DidNotReceive().Delete(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateEducation_ProfessionalMissing_ReturnsNull()
    {
        _professionals.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Professional?)null);
        var handler = new CreateEducation.Handler(_professionals, _education);

        var model = await handler.Handle(
            new CreateEducation.Request
            {
                ProfessionalId = _professionalId,
                Input = new EducationInput { Institution = "Univ", Degree = "BSc", StartYear = 2005 },
            },
            CancellationToken.None);

        model.Should().BeNull();
    }

    [Fact]
    public async Task UpdateEducation_OwnEntry_ReplacesIt()
    {
        var entry = new Education { ProfessionalId = _professionalId, Institution = "Old", Degree = "BSc", StartYear = 2000 };
        _education.GetById(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        var handler = new UpdateEducation.Handler(_education);

        var model = await handler.Handle(
            new UpdateEducation.Request
            {
                ProfessionalId = _professionalId,
                EducationId = entry.Id,
                Input = new EducationInput { Institution = "Univ X", Degree = "BSc", StartYear = 2005, EndYear = 2009 },
            },
            CancellationToken.None);

        model!.Institution.Should().Be("Univ X");
        model.EndYear.Should().Be(2009);
        await _education.Received(1).Replace(entry, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteEducation_OwnEntry_ReturnsTrue()
    {
        var entry = new Education { ProfessionalId = _professionalId, Institution = "Univ", Degree = "BSc" };
        _education.GetById(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        var handler = new DeleteEducation.Handler(_education);

        var deleted = await handler.Handle(
            new DeleteEducation.Request { ProfessionalId = _professionalId, EducationId = entry.Id },
            CancellationToken.None);

        deleted.Should().BeTrue();
        await _education.Received(1).Delete(entry.Id, Arg.Any<CancellationToken>());
    }
}
