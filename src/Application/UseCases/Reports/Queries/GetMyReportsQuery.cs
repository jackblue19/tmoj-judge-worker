using MediatR;
using Application.UseCases.Reports.Dtos;

public record GetMyReportsQuery : IRequest<List<ReportDto>>;