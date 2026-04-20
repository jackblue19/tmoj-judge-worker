using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos;

public class AddProblemToCollectionRequest
{
    public Guid ProblemId { get; set; }
}
