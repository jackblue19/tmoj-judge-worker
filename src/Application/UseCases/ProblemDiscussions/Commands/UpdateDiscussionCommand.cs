using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

namespace Application.UseCases.ProblemDiscussions.Commands;

public record UpdateDiscussionCommand(
    Guid Id,
    string Title,
    string Content
) : IRequest<bool>;
