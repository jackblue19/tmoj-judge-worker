using Application.Common.Pagination;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Queries.GetPublicProblems;


public sealed record GetPublicProblemsQuery(
    int Page = 1 ,
    int PageSize = 20 ,
    string? Search = null ,
    string? Difficulty = null
) : IRequest<ApiPagedResponse<PublicProblemListItemDto>>;