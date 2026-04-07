using MediatR;
using Application.UseCases.Reports.Dtos;

public record GetReportByIdQuery(Guid Id) : IRequest<ReportDto>;