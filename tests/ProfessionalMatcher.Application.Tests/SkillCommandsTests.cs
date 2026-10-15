using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Commands;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Tests;

public class SkillCommandsTests
{
    private readonly IProfessionalRepository _professionals = Substitute.For<IProfessionalRepository>();
    private readonly ISkillRepository _skills = Substitute.For<ISkillRepository>();
    private readonly Guid _professionalId = Guid.NewGuid();

    private static SkillInput Input(string name = "  C#  ") =>
        new() { Name = name, Category = "Backend", YearsOfExperience = 15, Level = 5, IsPrimary = true };

    [Fact]
    public async Task Create_ProfessionalExists_AddsTrimmedSkill()
    {
        _professionals.GetById(_professionalId, Arg.Any<CancellationToken>())
            .Returns(new Professional { FullName = "Jane", Email = "jane@example.com" });
        var handler = new CreateSkill.Handler(_professionals, _skills);

        var model = await handler.Handle(
            new CreateSkill.Request { ProfessionalId = _professionalId, Input = Input() }, CancellationToken.None);

        model!.Name.Should().Be("C#");
        await _skills.Received(1).Add(
            Arg.Is<Skill>(s => s.ProfessionalId == _professionalId && s.Level == 5 && s.IsPrimary),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ProfessionalMissing_ReturnsNullAndAddsNothing()
    {
        _professionals.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Professional?)null);
        var handler = new CreateSkill.Handler(_professionals, _skills);

        var model = await handler.Handle(
            new CreateSkill.Request { ProfessionalId = _professionalId, Input = Input() }, CancellationToken.None);

        model.Should().BeNull();
        await _skills.DidNotReceive().Add(Arg.Any<Skill>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_SkillOfAnotherProfessional_ReturnsNullAndReplacesNothing()
    {
        var skill = new Skill { ProfessionalId = Guid.NewGuid(), Name = "Java", Category = "Backend" };
        _skills.GetById(skill.Id, Arg.Any<CancellationToken>()).Returns(skill);
        var handler = new UpdateSkill.Handler(_skills);

        var model = await handler.Handle(
            new UpdateSkill.Request { ProfessionalId = _professionalId, SkillId = skill.Id, Input = Input() },
            CancellationToken.None);

        model.Should().BeNull();
        await _skills.DidNotReceive().Replace(Arg.Any<Skill>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_OwnSkill_AppliesInputAndReplaces()
    {
        var skill = new Skill { ProfessionalId = _professionalId, Name = "Java", Category = "Backend" };
        _skills.GetById(skill.Id, Arg.Any<CancellationToken>()).Returns(skill);
        var handler = new UpdateSkill.Handler(_skills);

        var model = await handler.Handle(
            new UpdateSkill.Request { ProfessionalId = _professionalId, SkillId = skill.Id, Input = Input("C#") },
            CancellationToken.None);

        model!.Name.Should().Be("C#");
        await _skills.Received(1).Replace(skill, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_OwnSkill_DeletesAndReturnsTrue()
    {
        var skill = new Skill { ProfessionalId = _professionalId, Name = "C#", Category = "Backend" };
        _skills.GetById(skill.Id, Arg.Any<CancellationToken>()).Returns(skill);
        var handler = new DeleteSkill.Handler(_skills);

        var deleted = await handler.Handle(
            new DeleteSkill.Request { ProfessionalId = _professionalId, SkillId = skill.Id }, CancellationToken.None);

        deleted.Should().BeTrue();
        await _skills.Received(1).Delete(skill.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_SkillOfAnotherProfessional_ReturnsFalse()
    {
        var skill = new Skill { ProfessionalId = Guid.NewGuid(), Name = "C#", Category = "Backend" };
        _skills.GetById(skill.Id, Arg.Any<CancellationToken>()).Returns(skill);
        var handler = new DeleteSkill.Handler(_skills);

        var deleted = await handler.Handle(
            new DeleteSkill.Request { ProfessionalId = _professionalId, SkillId = skill.Id }, CancellationToken.None);

        deleted.Should().BeFalse();
        await _skills.DidNotReceive().Delete(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
