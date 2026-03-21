//using Domain.Entities;
//using Domain.ValueObjects;
//using MediatR;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Application.UseCases.Problems.Commands.CreateProblem;

//internal class CreateProblemCommandzzzzzzzzzzz      // 
//{
//    //  bắt buộc có
//    //  đc đặt tên theo usecase chơ ko phải lúc nào cũng create... đâu
//    //      có thể có lúc là SubmitSolution chẳng hạn (này theo bên feat submissions)

//    //  ngoài ra cái này có thể đặt ngay trong handler cũng được
//}

//public record CreateProblemCommand(
//    string Title ,
//    Slug Slug ,   //  có thể ko có, chẳng hạn như được gen ra từ title mới có slug (case ver 2)
//    //string Content ,
//    Difficulty Difficulty,
//    bool IsPublic
//) : IRequest<CreateProblemResult>;


using MediatR;

namespace Application.UseCases.Problems.Commands.CreateProblem;

public sealed record CreateProblemCommand(
    string Title ,
    string? Slug ,
    string? Difficulty ,
    string? TypeCode ,
    string? VisibilityCode ,
    string? ScoringCode ,
    string StatusCode ,
    string? DescriptionMd ,
    int? TimeLimitMs ,
    int? MemoryLimitKb
) : IRequest<Guid>;
