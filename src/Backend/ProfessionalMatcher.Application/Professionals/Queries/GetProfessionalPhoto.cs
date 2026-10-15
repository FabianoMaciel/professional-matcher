using System.Diagnostics.CodeAnalysis;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Queries;

/// <summary>
/// Retrieves the profile picture of a professional.
/// </summary>
public class GetProfessionalPhoto
{
    public class Handler(IProfessionalPhotoRepository repository)
    {
        /// <returns>The photo, or null if the professional has none.</returns>
        public async Task<PhotoModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            var photo = await repository.Get(request.ProfessionalId, cancellationToken);
            return photo is null ? null : new PhotoModel { ContentType = photo.ContentType, Content = photo.Content };
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
    }
}
