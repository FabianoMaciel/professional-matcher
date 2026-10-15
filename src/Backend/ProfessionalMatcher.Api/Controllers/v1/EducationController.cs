using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Commands;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Api.Controllers.v1;

/// <summary>
/// Provides API endpoints for the education entries of a professional.
/// </summary>
[Route("api/v{version:apiVersion}/professionals/{professionalId:guid}/education")]
[Produces(MediaTypeNames.Application.Json)]
[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous] // TODO: replace with the real authorization policy once auth is decided.
public class EducationController(IBusProvider mediator) : ControllerBase
{
    /// <summary>
    /// Adds an education entry to a professional.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EducationModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EducationModel>> CreateEducation(
        [FromRoute] Guid professionalId,
        [FromBody] EducationInput input,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateEducation.Request { ProfessionalId = professionalId, Input = input };
        var result = await mediator.InvokeAsync<EducationModel?>(request, cancellationToken: cancellationToken);

        return result is null ? NotFound() : Created($"{Request.Path}/{result.Id}", result);
    }

    /// <summary>
    /// Updates an education entry of a professional.
    /// </summary>
    [HttpPut("{educationId:guid}")]
    [ProducesResponseType(typeof(EducationModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EducationModel>> UpdateEducation(
        [FromRoute] Guid professionalId,
        [FromRoute] Guid educationId,
        [FromBody] EducationInput input,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateEducation.Request
        {
            ProfessionalId = professionalId,
            EducationId = educationId,
            Input = input,
        };
        var result = await mediator.InvokeAsync<EducationModel?>(request, cancellationToken: cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Removes an education entry from a professional.
    /// </summary>
    [HttpDelete("{educationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEducation(
        [FromRoute] Guid professionalId,
        [FromRoute] Guid educationId,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteEducation.Request { ProfessionalId = professionalId, EducationId = educationId };
        var deleted = await mediator.InvokeAsync<bool>(request, cancellationToken: cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
