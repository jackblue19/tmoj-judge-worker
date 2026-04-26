using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Application.UseCases.Contests.Dtos;

namespace Application.UseCases.Contests.Queries;

public record GetContestDetailQuery(Guid ContestId)
    : IRequest<ContestDetailDto>;
