using Application.UseCases.Problems.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Problems.Commands.CreateTag;

public sealed class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, ProblemTagDto>
{
    private readonly ITagRepository _tagRepository;
    private readonly IWriteRepository<Tag, Guid> _tagWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTagCommandHandler(
        ITagRepository tagRepository,
        IWriteRepository<Tag, Guid> tagWriteRepository,
        IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _tagWriteRepository = tagWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProblemTagDto> Handle(CreateTagCommand request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name is required.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? name.Trim().ToLower().Replace(' ', '-')
            : request.Slug.Trim().ToLower();

        if (await _tagRepository.ExistsByNameAsync(name, ct))
            throw new InvalidOperationException($"Tag name '{name}' already exists.");

        if (!string.IsNullOrWhiteSpace(slug) && await _tagRepository.ExistsBySlugAsync(slug, ct))
            throw new InvalidOperationException($"Tag slug '{slug}' already exists.");

        var entity = new Tag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug
        };

        await _tagWriteRepository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ProblemTagDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug
        };
    }
}
