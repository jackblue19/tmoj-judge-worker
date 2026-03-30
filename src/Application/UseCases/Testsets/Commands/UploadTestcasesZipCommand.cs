using Application.UseCases.Testsets.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Testsets.Commands;

public sealed record UploadTestcasesZipCommand(
    Guid ProblemId ,
    Guid TestsetId ,
    bool ReplaceExisting ,
    string FileName ,
    Stream FileStream
) : IRequest<UploadTestcasesResultDto>;