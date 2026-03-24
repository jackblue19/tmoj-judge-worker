using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems;

public interface IProblemAuthorizationService
{
    bool CanManage(Problem problem , Guid currentUserId , bool isAdmin);
}
