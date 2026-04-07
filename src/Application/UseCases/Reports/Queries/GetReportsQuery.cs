using MediatR;
using Application.UseCases.Reports.Dtos;

public record GetReportsQuery(string? Status) : IRequest<List<ReportDto>>;