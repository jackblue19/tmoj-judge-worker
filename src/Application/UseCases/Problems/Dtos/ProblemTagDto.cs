using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Dtos;

public sealed class ProblemTagDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
}
