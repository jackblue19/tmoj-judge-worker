//namespace WebAPI.Models.Common;


//public sealed record PaginationMeta(
//    int Page ,
//    int PageSize ,
//    long TotalCount ,
//    long TotalPages ,
//    bool HasPrevious ,
//    bool HasNext
//);

//public sealed record ApiPagedResponse<T>(
//                            IReadOnlyList<T> Data ,
//                            PaginationMeta Pagination ,
//                            string? Message = null ,
//                            string? TraceId = null)
//{
//    public static ApiPagedResponse<T> Ok(
//                            IReadOnlyList<T> data ,
//                            PaginationMeta pagination ,
//                            string? message = null ,
//                            string? traceId = null)
//        => new(data , pagination , message , traceId);
//}
