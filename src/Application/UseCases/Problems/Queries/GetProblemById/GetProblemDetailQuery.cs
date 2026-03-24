using Application.UseCases.Problems.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Queries.GetProblemById;

public sealed record GetProblemDetailQuery(Guid ProblemId) : IRequest<ProblemDetailDto>;
