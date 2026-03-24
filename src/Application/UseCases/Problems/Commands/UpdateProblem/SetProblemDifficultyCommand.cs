using Application.UseCases.Problems.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Commands.UpdateProblem;

public sealed record SetProblemDifficultyCommand(
    Guid ProblemId ,
    string Difficulty
) : IRequest<ProblemDetailDto>;
