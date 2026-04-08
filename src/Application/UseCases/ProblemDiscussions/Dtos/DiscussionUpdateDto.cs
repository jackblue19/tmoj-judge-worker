using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.ProblemDiscussions.Dtos;

public class DiscussionUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
