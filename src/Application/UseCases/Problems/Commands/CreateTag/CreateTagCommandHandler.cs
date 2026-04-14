using Application.Common.Interfaces;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Mappings;
using Application.UseCases.Problems.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Problems.Commands.CreateTag;

public sealed class CreateTagCommandHandler : IRequestHandler<CreateTagCommand , ProblemTagDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<Tag , Guid> _tagReadRepository;
    private readonly IWriteRepository<Tag , Guid> _tagWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTagCommandHandler(
        ICurrentUserService currentUser ,
        IReadRepository<Tag , Guid> tagReadRepository ,
        IWriteRepository<Tag , Guid> tagWriteRepository ,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _tagReadRepository = tagReadRepository;
        _tagWriteRepository = tagWriteRepository;
        _unitOfWork = unitOfWork;
    }

    private static string ToSlug(string value)
    {
        return value.Trim().ToLowerInvariant().Replace(' ' , '-');
    }

    public async Task<ProblemTagDto> Handle(CreateTagCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var name = request.Name?.Trim();
        if ( string.IsNullOrWhiteSpace(name) )
            throw new ArgumentException("Tag name is required.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? ToSlug(name)
            : ToSlug(request.Slug);

        var existed = await _tagReadRepository.FirstOrDefaultAsync(
            new TagByNameOrSlugSpec(name , slug) , ct);

        if ( existed is not null )
            throw new InvalidOperationException("Tag name or slug already exists.");

        var now = DateTime.UtcNow;

        var entity = new Tag
        {
            Id = Guid.NewGuid() ,
            Name = name ,
            Slug = slug ,
            Description = request.Description?.Trim() ,
            Color = request.Color?.Trim() ,
            Icon = request.Icon?.Trim() ,
            IsActive = true ,
            CreatedAt = now ,
            CreatedBy = _currentUser.UserId.Value ,
            UpdatedAt = now ,
            UpdatedBy = _currentUser.UserId.Value
        };

        await _tagWriteRepository.AddAsync(entity , ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return entity.ToDto();
    }
}