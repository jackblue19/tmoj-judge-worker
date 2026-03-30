using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Editorials
{
    public record EditorialDto(
       Guid EditorialId,
       Guid ProblemId,
       Guid? AuthorId,
       string FilePath,
       string FileType,
       DateTime CreatedAt
   );
}
