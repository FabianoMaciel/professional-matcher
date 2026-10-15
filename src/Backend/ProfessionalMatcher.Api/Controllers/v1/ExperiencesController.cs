using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Commands;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Api.Controllers.v1;

/// <summary>
/// Provides API endpoints for the work experiences of a professional.
/// </summary>
[Route("api/v{version:apiVersion}/professionals/{professionalId:guid}/experiences")]
[Produces(MediaTypeNames.Application.Json)]
[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous] // TODO: replace with the real authorization policy once auth is decided.
public class ExperiencesController(IBusProvider mediator) : ControllerBase
{
    /// <summary>
    /// Adds a work experience to a professional.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ExperienceModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExperienceModel>> CreateExperience(
        [FromRoute] Guid professionalId,
        [FromBody] ExperienceInput input,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateExperience.Request { ProfessionalId = professionalId, Input = input };
        var result = await mediator.InvokeAsync<ExperienceModel?>(request, cancellationToken: cancellationToken);

        return result is null ? NotFound() : Created($"{Request.Path}/{result.Id}", result);
    }

    /// <summary>
    /// Updates a work experience of a professional.
    /// </summary>
    [HttpPut("{experienceId:guid}")]
    [ProducesResponseType(typeof(ExperienceModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExperienceModel>> UpdateExperience(
        [FromRoute] Guid professionalId,
        [FromRoute] Guid experienceId,
        [FromBody] ExperienceInput input,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateExperience.Request
        {
            ProfessionalId = professionalId,
            ExperienceId = experienceId,
            Input = input,
        };
        var result = await mediator.InvokeAsync<ExperienceModel?>(request, cancellationToken: cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Removes a work experience from a professional.
    /// </summary>
    [HttpDelete("{experienceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExperience(
        [FromRoute] Guid professionalId,
        [FromRoute] Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteExperience.Request { ProfessionalId = professionalId, ExperienceId = experienceId };
        var deleted = await mediator.InvokeAsync<bool>(request, cancellationToken: cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
