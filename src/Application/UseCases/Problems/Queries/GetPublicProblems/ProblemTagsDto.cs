using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Queries.GetPublicProblems;

public sealed record ProblemTagsDto(
    Guid Id ,
    string Name ,
    string Slug
);

