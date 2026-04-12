using FluentValidation;

namespace Application.UseCases.Submissions.Queries.GetSubmissionsByProblem;

public sealed class GetSubmissionsByProblemValidator : AbstractValidator<GetSubmissionsByProblemQuery>
{
    public GetSubmissionsByProblemValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1 , 100);
    }
}