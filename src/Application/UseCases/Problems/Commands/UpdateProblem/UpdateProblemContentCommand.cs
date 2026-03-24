using Application.UseCases.Problems.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Commands.UpdateProblem;

public sealed record UpdateProblemContentCommand(
    Guid ProblemId ,
    string Title ,
    string Slug ,
    string? DescriptionMd ,
    int? TimeLimitMs ,
    int? MemoryLimitKb ,
    string? TypeCode ,
    string? ScoringCode ,
    string? VisibilityCode
) : IRequest<ProblemDetailDto>;
