using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Mappings;
using Application.UseCases.Problems.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Problems.Commands.ReplaceProblemTags;

public sealed class ReplaceProblemTagsCommandHandler : IRequestHandler<ReplaceProblemTagsCommand , ProblemDetailDto>
{
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly IReadRepository<Tag , Guid> _tagReadRepository;
    private readonly IWriteRepository<Problem , Guid> _problemWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReplaceProblemTagsCommandHandler(
        IReadRepository<Problem , Guid> problemReadRepository ,
        IReadRepository<Tag , Guid> tagReadRepository ,
        IWriteRepository<Problem , Guid> problemWriteRepository ,
        IUnitOfWork unitOfWork)
    {
        _problemReadRepository = problemReadRepository;
        _tagReadRepository = tagReadRepository;
        _problemWriteRepository = problemWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProblemDetailDto> Handle(ReplaceProblemTagsCommand request , CancellationToken ct)
    {
        var problem = await _problemReadRepository.FirstOrDefaultAsync(
            new ProblemWithTagsAndTestsetsSpec(request.ProblemId) , ct);

        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        var tags = request.TagIds.Count == 0
            ? []
            : await _tagReadRepository.ListAsync(new TagsByIdsSpec(request.TagIds) , ct);

        var foundIds = tags.Select(x => x.Id).ToHashSet();
        var missingIds = request.TagIds.Where(x => !foundIds.Contains(x)).ToArray();
        if ( missingIds.Length > 0 )
            throw new KeyNotFoundException($"Some tags were not found: {string.Join(", " , missingIds)}");

        problem.Tags.Clear();
        foreach ( var tag in tags )
            problem.Tags.Add(tag);

        _problemWriteRepository.Update(problem);
        await _unitOfWork.SaveChangesAsync(ct);

        return problem.ToDetailDto();
    }
}