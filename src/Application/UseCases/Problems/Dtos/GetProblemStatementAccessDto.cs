using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Dtos;

public sealed class GetProblemStatementAccessDto
{
    public string Mode { get; init; } = null!; // "redirect" | "inline"
    public string? Url { get; init; }
    public byte[]? Bytes { get; init; }
    public string? ContentType { get; init; }
}
