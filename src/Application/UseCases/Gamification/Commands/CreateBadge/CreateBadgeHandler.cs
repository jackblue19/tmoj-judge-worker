using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Commands.CreateBadge;

public class CreateBadgeHandler : IRequestHandler<CreateBadgeCommand , Guid>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly ICloudinaryService _cloudinary;

    public CreateBadgeHandler(
        IGamificationRepository repo ,
        ICurrentUserService currentUser ,
        ICloudinaryService cloudinary)
    {
        _repo = repo;
        _currentUser = currentUser;
        _cloudinary = cloudinary;
    }

    public async Task<Guid> Handle(CreateBadgeCommand request , CancellationToken cancellationToken)
    {
        // validate category
        var validCategories = new[] { "contest" , "course" , "org" , "streak" , "problem" };

        if ( !validCategories.Contains(request.BadgeCategory) )
            throw new Exception("Invalid badge category");

        // check duplicate code
        var exists = await _repo.ExistsBadgeCodeAsync(request.BadgeCode);

        if ( exists )
            throw new Exception("Badge code already exists");

        string? iconUrl = request.IconUrl;

        if ( request.IconFile != null && request.IconFile.Length > 0 )
{
    var ext = System.IO.Path.GetExtension(request.IconFile.FileName);
    using var stream = request.IconFile.OpenReadStream();
    var imageId = await _cloudinary.UploadImageAsync(stream , ext , "badges" , cancellationToken);
    var url = _cloudinary.GetImageUrl(imageId , "badges");
    if ( !string.IsNullOrEmpty(url) )
    {
        iconUrl = url;
    }
}

        var badge = new Badge
        {
            BadgeId = Guid.NewGuid() ,
            Name = request.Name ,
            IconUrl = iconUrl ,
            Description = request.Description ,
            BadgeCode = request.BadgeCode ,
            BadgeCategory = request.BadgeCategory ,
            BadgeLevel = request.BadgeLevel ,
            IsRepeatable = request.IsRepeatable ,
            CreatedAt = DateTime.UtcNow ,
            CreatedBy = _currentUser.UserId
        };

        return await _repo.CreateBadgeAsync(badge);
    }
}