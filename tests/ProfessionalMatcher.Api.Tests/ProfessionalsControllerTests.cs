using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProfessionalMatcher.Api.Tests;

/// <remarks>
/// Only requests rejected before reaching the database are exercised here; the rest of the pipeline
/// is covered by the Application tests and by running the API against PostgreSQL.
/// </remarks>
public class ProfessionalsControllerTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid Id = Guid.NewGuid();

    [Fact]
    public async Task GetProfessionalById_InvalidGuid_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/professionals/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateDetails_InvalidBody_ReturnsValidationProblemWithOneMessagePerError()
    {
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/professionals/{Id}", new { fullName = "", email = "nope" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await ReadErrors(response));
        errors.Should().ContainKey("input.fullName").WhoseValue.Should().HaveCount(1);
        errors.Should().ContainKey("input.email").WhoseValue.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateSkill_InvalidLevel_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/professionals/{Id}/skills",
            new { name = "C#", category = "Backend", yearsOfExperience = 3, level = 9, isPrimary = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadErrors(response)).Should().ContainKey("input.level");
    }

    [Fact]
    public async Task CreateExperience_EndBeforeStart_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/professionals/{Id}/experiences",
            new { company = "Acme", role = "Dev", startDate = "2020-05", endDate = "2019-01" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadErrors(response)).Should().ContainKey("input.endDate");
    }

    [Fact]
    public async Task CreateEducation_MissingBody_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/professionals/{Id}/education", new StringContent("", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportCv_FileThatIsNotAPdf_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent("just text"u8.ToArray()) { Headers = { ContentType = new("application/pdf") } }, "file", "cv.pdf" },
        };

        var response = await client.PostAsync($"/api/v1/professionals/{Id}/cv-import", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadErrors(response)).Should().ContainKey("content");
    }

    [Fact]
    public async Task ImportCv_WithoutFile_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/professionals/{Id}/cv-import", new MultipartFormDataContent());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<Dictionary<string, string[]>> ReadErrors(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("errors").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(e => e.GetString()!).ToArray());
    }
}
