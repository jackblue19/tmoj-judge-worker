using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Queries.GetPublicProblems;

public sealed class GetPublicProblemsQueryValidator : AbstractValidator<GetPublicProblemsQuery>
{
    public const int MaxPageSize = 100;

    public GetPublicProblemsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1 , MaxPageSize);

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.Difficulty)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Difficulty));
    }
}