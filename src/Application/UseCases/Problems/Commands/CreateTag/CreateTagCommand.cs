using Application.UseCases.Problems.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Commands.CreateTag;

public sealed record CreateTagCommand(
    string Name ,
    string? Slug
) : IRequest<ProblemTagDto>;