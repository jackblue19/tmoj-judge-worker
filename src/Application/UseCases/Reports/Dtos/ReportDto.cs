namespace Application.UseCases.Reports.Dtos;

public class ReportDto
{
    public Guid Id { get; set; }
    public Guid TargetId { get; set; }
    public string TargetType { get; set; }
    public string Reason { get; set; }
    public string Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Guid? AuthorId { get; set; }
    public string? AuthorName { get; set; }
}