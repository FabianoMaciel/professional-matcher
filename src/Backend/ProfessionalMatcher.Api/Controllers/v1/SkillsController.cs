using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Commands;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Api.Controllers.v1;

/// <summary>
/// Provides API endpoints for the skills of a professional.
/// </summary>
[Route("api/v{version:apiVersion}/professionals/{professionalId:guid}/skills")]
[Produces(MediaTypeNames.Application.Json)]
[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous] // TODO: replace with the real authorization policy once auth is decided.
public class SkillsController(IBusProvider mediator) : ControllerBase
{
    /// <summary>
    /// Adds a skill to a professional.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SkillModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillModel>> CreateSkill(
        [FromRoute] Guid professionalId,
        [FromBody] SkillInput input,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateSkill.Request { ProfessionalId = professionalId, Input = input };
        var result = await mediator.InvokeAsync<SkillModel?>(request, cancellationToken: cancellationToken);

        return result is null ? NotFound() : Created($"{Request.Path}/{result.Id}", result);
    }

    /// <summary>
    /// Updates a skill of a professional.
    /// </summary>
    [HttpPut("{skillId:guid}")]
    [ProducesResponseType(typeof(SkillModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillModel>> UpdateSkill(
        [FromRoute] Guid professionalId,
        [FromRoute] Guid skillId,
        [FromBody] SkillInput input,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateSkill.Request { ProfessionalId = professionalId, SkillId = skillId, Input = input };
        var result = await mediator.InvokeAsync<SkillModel?>(request, cancellationToken: cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Removes a skill from a professional.
    /// </summary>
    [HttpDelete("{skillId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSkill(
        [FromRoute] Guid professionalId,
        [FromRoute] Guid skillId,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteSkill.Request { ProfessionalId = professionalId, SkillId = skillId };
        var deleted = await mediator.InvokeAsync<bool>(request, cancellationToken: cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
