using FluentValidation;

namespace Application.UseCases.Problems.Queries.GetProblemsByUser;

public sealed class GetProblemsByUserValidator : AbstractValidator<GetProblemsByUserQuery>
{
    public const int MaxPageSize = 100;

    public GetProblemsByUserValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.CurrentUserId)
            .NotEmpty();

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1 , MaxPageSize);
    }
}