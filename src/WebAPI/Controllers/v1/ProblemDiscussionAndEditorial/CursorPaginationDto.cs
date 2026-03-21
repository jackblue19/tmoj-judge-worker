namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
{
    namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
    {
        public class CursorPaginationDto<T>
        {
            public List<T> Items { get; set; } = new();

            public DateTime? NextCursor { get; set; }

            public bool HasMore { get; set; }
        }
    }
}
