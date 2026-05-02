using MediatR;
using Application.UseCases.ProblemDiscussions.Dtos;
using System;
using System.Collections.Generic;

namespace Application.UseCases.ProblemDiscussions.Queries;

public record GetUserActivitiesQuery(Guid UserId, int Limit = 20) : IRequest<List<UserActivityDto>>;
