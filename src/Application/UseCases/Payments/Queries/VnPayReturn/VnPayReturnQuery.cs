using Application.UseCases.Payments.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Payments.Queries.VnPayReturn;


public class VnPayReturnQuery : IRequest<VnPayReturnResult>
{
    public IQueryCollection Query { get; set; } = default!;
}

