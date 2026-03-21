using Application.UseCases.Problems.Mappings;
using Domain.Abstractions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Queries.GetAllProblems;

public sealed record GetProblemsQuery(
    string? Difficulty ,
    string? Status ,
    int Page = 1 ,
    int PageSize = 20
) : IRequest<PagedResult<ProblemListItemDto>>;