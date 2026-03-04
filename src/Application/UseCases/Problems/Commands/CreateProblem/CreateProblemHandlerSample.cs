//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Domain.Entities;
//using MediatR;

//namespace Application.UseCases.Problems.Commands.CreateProblem;

////  ở đoạn này có thể đặt record cho command ở đây cũng được
////  ko nhất thiết tạo thêm 1 file CreateProblemCommand ...
////  chả qua là muốn tường minh nên có tách ra

//public class CreateProblemHandler : IRequestHandler<CreateProblemCommand , CreateProblemResult>
//{
//    private readonly IRepository<Problem> _repo;

//    public CreateProblemHandler(IRepository<Problem> repo)
//    {
//        _repo = repo;
//    }

//    public async Task<CreateProblemResult> Handle(CreateProblemCommand request , CancellationToken ct)
//    {
//        //  Mapster CodeGen extension
//        var problem = request.ToProblem();      // cần build ngay khi có CreateProblemMapping mới có

//        await _repo.AddAsync(problem , ct);

//        return new CreateProblemResult(problem.Id);
//        //  return new CreateProblemResult(problem.Id, problem.slug);   //ver 2
//    }
//}


////  🔥🔥🔥

//public interface IRepository<T>
//{
//    Task<T> AddAsync(T entity , CancellationToken ct = default);
//    Task<T?> GetByIdAsync(Guid id , CancellationToken ct = default);
//    Task<List<T>> ListAsync(CancellationToken ct = default);
//}

////  Giả sử đã build rồi thì sẽ có thêm file này (Application\Generated\CreateProblemCommand.g.cs)
//public static partial class CreateProblemCommandMapper
//{
//    public static Problem ToProblem(this CreateProblemCommand src)
//    {
//        return Problem.Create(
//            src.Title ,
//            src.Slug ,
//            //src.Content ,
//            src.Difficulty,
//            src.IsPublic
//        );
//    }
//}
