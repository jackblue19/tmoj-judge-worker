//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Application.UseCases.Problems.Validators
//{
//    internal class CreateProblemValidator
//    {
//    }
//}
using Application.UseCases.Problems.Commands.CreateProblem;
using FluentValidation;

namespace Application.UseCases.Problems.Validators;

public sealed class CreateProblemValidator
    : AbstractValidator<CreateProblemCommand>
{
    public CreateProblemValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.StatusCode)
            .Must(x => new[] { "draft" , "pending" , "published" , "archived" }
                .Contains(x))
            .WithMessage("Invalid status.");

        RuleFor(x => x.TimeLimitMs)
            .GreaterThan(0)
            .When(x => x.TimeLimitMs.HasValue);

        RuleFor(x => x.MemoryLimitKb)
            .GreaterThan(0)
            .When(x => x.MemoryLimitKb.HasValue);
    }
}