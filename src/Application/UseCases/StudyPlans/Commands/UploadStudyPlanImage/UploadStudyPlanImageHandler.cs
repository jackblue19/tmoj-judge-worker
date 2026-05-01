using Application.Abstractions.Outbound.Services;
using MediatR;

namespace Application.UseCases.StudyPlans.Commands.UploadStudyPlanImage;

public class UploadStudyPlanImageHandler : IRequestHandler<UploadStudyPlanImageCommand, string>
{
    private readonly ICloudinaryService _cloudinary;

    public UploadStudyPlanImageHandler(ICloudinaryService cloudinary)
    {
        _cloudinary = cloudinary;
    }

    public async Task<string> Handle(UploadStudyPlanImageCommand request, CancellationToken ct)
    {
        // Upload to Cloudinary under the "study_plans" folder
        var imageId = await _cloudinary.UploadImageAsync(request.FileStream, request.Extension, "study_plans", ct);
        
        // Return the public URL
        var imageUrl = _cloudinary.GetImageUrl(imageId, "study_plans");
        
        if (string.IsNullOrEmpty(imageUrl))
            throw new Exception("Failed to retrieve image URL from Cloudinary.");

        return imageUrl;
    }
}
