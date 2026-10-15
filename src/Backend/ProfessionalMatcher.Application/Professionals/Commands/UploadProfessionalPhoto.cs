using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Sets (or replaces) the profile picture of a professional.
/// </summary>
public class UploadProfessionalPhoto
{
    public const int MaxSizeInBytes = 2 * 1024 * 1024;

    public static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public class Handler(IProfessionalRepository professionals, IProfessionalPhotoRepository photos)
    {
        /// <returns>The new photo URL, or null if the professional was not found.</returns>
        public async Task<PhotoUploadModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            if (await professionals.GetById(request.ProfessionalId, cancellationToken) is null) return null;

            var photo = new ProfessionalPhoto
            {
                ProfessionalId = request.ProfessionalId,
                ContentType = request.ContentType,
                Content = request.Content,
            };
            await photos.Save(photo, cancellationToken);

            return new PhotoUploadModel { PhotoUrl = ProfessionalModel.PhotoUrlFor(photo.ProfessionalId, photo.UpdatedAt)! };
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public byte[] Content { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Content).NotEmpty().Must(c => c.Length <= MaxSizeInBytes)
                .WithMessage($"The photo must be at most {MaxSizeInBytes / 1024 / 1024} MB.");
            RuleFor(x => x.ContentType).Must(t => AllowedContentTypes.Contains(t))
                .WithMessage($"The photo must be one of: {string.Join(", ", AllowedContentTypes)}.");
        }
    }
}
