using FluentValidation;

namespace Application.UseCases.Submissions.Queries.GetSubmissionsByUser;

public sealed class GetSubmissionsByUserValidator : AbstractValidator<GetSubmissionsByUserQuery>
{
    public const int MaxPageSize = 100;

    public GetSubmissionsByUserValidator()
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