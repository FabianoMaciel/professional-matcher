using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Commands;
using ProfessionalMatcher.Application.Professionals.Queries;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Api.Controllers.v1;

/// <summary>
/// Provides API endpoints for professionals.
/// </summary>
[Route("api/v{version:apiVersion}/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ApiController]
[ApiVersion("1.0")]
public class ProfessionalsController(IBusProvider mediator) : ControllerBase
{
    /// <summary>
    /// Gets a single professional, with skills, experiences and education, by id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous] // TODO: replace with the real authorization policy once auth is decided.
    [ProducesResponseType(typeof(ProfessionalModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfessionalModel>> GetProfessionalById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var request = new GetProfessionalById.Request { Id = id };
        var result = await mediator.InvokeAsync<ProfessionalModel?>(request, cancellationToken: cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Updates the general data of a professional.
    /// </summary>
    [HttpPut("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProfessionalModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfessionalModel>> UpdateProfessionalDetails(
        [FromRoute] Guid id,
        [FromBody] ProfessionalDetailsInput input,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateProfessionalDetails.Request { Id = id, Input = input };
        var result = await mediator.InvokeAsync<ProfessionalModel?>(request, cancellationToken: cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Gets the profile picture of a professional.
    /// </summary>
    [HttpGet("{id:guid}/photo")]
    [AllowAnonymous]
    [Produces("image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPhoto(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var request = new GetProfessionalPhoto.Request { ProfessionalId = id };
        var photo = await mediator.InvokeAsync<PhotoModel?>(request, cancellationToken: cancellationToken);

        return photo is null ? NotFound() : File(photo.Content, photo.ContentType);
    }

    /// <summary>
    /// Fills a professional's profile from a CV in PDF: general data, skills, experiences and education.
    /// Existing data is kept and already-present entries are skipped. General data (headline, location, summary)
    /// is only filled when blank, unless <paramref name="overwriteDetails"/> is true.
    /// </summary>
    /// <param name="id">The professional.</param>
    /// <param name="file">The CV, as a PDF of up to 5 MB.</param>
    /// <param name="overwriteDetails">Also replace name, headline, location and summary that are already filled.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpPost("{id:guid}/cv-import")]
    [AllowAnonymous]
    [RequestSizeLimit(ImportProfileFromCv.MaxSizeInBytes + 64 * 1024)] // room for the multipart envelope
    [ProducesResponseType(typeof(CvImportModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CvImportModel>> ImportCv(
        [FromRoute] Guid id,
        IFormFile file,
        [FromQuery] bool overwriteDetails = false,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var request = new ImportProfileFromCv.Request
        {
            ProfessionalId = id,
            Content = stream.ToArray(),
            OverwriteDetails = overwriteDetails,
        };
        var result = await mediator.InvokeAsync<CvImportModel?>(request, cancellationToken: cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Sets (or replaces) the profile picture of a professional. Accepts JPEG, PNG or WebP up to 2 MB.
    /// </summary>
    [HttpPost("{id:guid}/photo")]
    [AllowAnonymous]
    [RequestSizeLimit(UploadProfessionalPhoto.MaxSizeInBytes + 64 * 1024)] // room for the multipart envelope
    [ProducesResponseType(typeof(PhotoUploadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhotoUploadModel>> UploadPhoto(
        [FromRoute] Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var request = new UploadProfessionalPhoto.Request
        {
            ProfessionalId = id,
            ContentType = file.ContentType,
            Content = stream.ToArray(),
        };
        var result = await mediator.InvokeAsync<PhotoUploadModel?>(request, cancellationToken: cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}
