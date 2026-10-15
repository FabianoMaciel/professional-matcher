using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals;
using ProfessionalMatcher.Application.Professionals.Commands;
using ProfessionalMatcher.Application.Professionals.Queries;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Tests;

public class ProfileDetailsAndPhotoTests
{
    private readonly IProfessionalRepository _professionals = Substitute.For<IProfessionalRepository>();
    private readonly IProfessionalPhotoRepository _photos = Substitute.For<IProfessionalPhotoRepository>();

    [Fact]
    public async Task UpdateDetails_ProfessionalExists_PersistsAndReturnsProfile()
    {
        var professional = new Professional { FullName = "Old", Email = "old@example.com" };
        _professionals.GetById(professional.Id, Arg.Any<CancellationToken>()).Returns(professional);
        var skills = Substitute.For<ISkillRepository>();
        var experiences = Substitute.For<IExperienceRepository>();
        var education = Substitute.For<IEducationRepository>();
        skills.ListByProfessionalId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        experiences.ListByProfessionalId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        education.ListByProfessionalId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = new UpdateProfessionalDetails.Handler(
            _professionals, new ProfessionalModelAssembler(skills, experiences, education));

        var model = await handler.Handle(
            new UpdateProfessionalDetails.Request
            {
                Id = professional.Id,
                Input = new ProfessionalDetailsInput
                {
                    FullName = " Jane Doe ",
                    Email = "jane@example.com",
                    Headline = "Engineer",
                    Location = "Brasil",
                    Summary = "Hi",
                },
            },
            CancellationToken.None);

        model!.FullName.Should().Be("Jane Doe");
        model.Location.Should().Be("Brasil");
        await _professionals.Received(1).Replace(professional, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDetails_ProfessionalMissing_ReturnsNull()
    {
        _professionals.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Professional?)null);
        var handler = new UpdateProfessionalDetails.Handler(
            _professionals,
            new ProfessionalModelAssembler(
                Substitute.For<ISkillRepository>(),
                Substitute.For<IExperienceRepository>(),
                Substitute.For<IEducationRepository>()));

        var model = await handler.Handle(
            new UpdateProfessionalDetails.Request { Id = Guid.NewGuid(), Input = new ProfessionalDetailsInput() },
            CancellationToken.None);

        model.Should().BeNull();
    }

    [Fact]
    public async Task UploadPhoto_ProfessionalExists_SavesPhotoAndReturnsUrl()
    {
        var professionalId = Guid.NewGuid();
        _professionals.GetById(professionalId, Arg.Any<CancellationToken>())
            .Returns(new Professional { FullName = "Jane", Email = "jane@example.com" });
        var handler = new UploadProfessionalPhoto.Handler(_professionals, _photos);

        var result = await handler.Handle(
            new UploadProfessionalPhoto.Request
            {
                ProfessionalId = professionalId,
                ContentType = "image/png",
                Content = [1, 2, 3],
            },
            CancellationToken.None);

        result!.PhotoUrl.Should().StartWith($"/api/v1/professionals/{professionalId}/photo?v=");
        await _photos.Received(1).Save(
            Arg.Is<ProfessionalPhoto>(p => p.ProfessionalId == professionalId && p.Content.Length == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadPhoto_ProfessionalMissing_ReturnsNull()
    {
        _professionals.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Professional?)null);
        var handler = new UploadProfessionalPhoto.Handler(_professionals, _photos);

        var result = await handler.Handle(
            new UploadProfessionalPhoto.Request { ProfessionalId = Guid.NewGuid(), ContentType = "image/png", Content = [1] },
            CancellationToken.None);

        result.Should().BeNull();
        await _photos.DidNotReceive().Save(Arg.Any<ProfessionalPhoto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPhoto_WithoutPhoto_ReturnsNull()
    {
        _photos.Get(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ProfessionalPhoto?)null);
        var handler = new GetProfessionalPhoto.Handler(_photos);

        var result = await handler.Handle(
            new GetProfessionalPhoto.Request { ProfessionalId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPhoto_WithPhoto_ReturnsContentAndType()
    {
        var id = Guid.NewGuid();
        _photos.Get(id, Arg.Any<CancellationToken>())
            .Returns(new ProfessionalPhoto { ProfessionalId = id, ContentType = "image/webp", Content = [9, 9] });
        var handler = new GetProfessionalPhoto.Handler(_photos);

        var result = await handler.Handle(new GetProfessionalPhoto.Request { ProfessionalId = id }, CancellationToken.None);

        result!.ContentType.Should().Be("image/webp");
        result.Content.Should().Equal(9, 9);
    }
}
