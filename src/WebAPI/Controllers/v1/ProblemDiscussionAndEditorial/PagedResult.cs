namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
{
    public class PagedResult<T>
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public List<T> Items { get; set; } = new();
    }
}