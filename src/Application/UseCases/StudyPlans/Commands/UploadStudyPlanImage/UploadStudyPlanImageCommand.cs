using MediatR;
using System.IO;

namespace Application.UseCases.StudyPlans.Commands.UploadStudyPlanImage;

public record UploadStudyPlanImageCommand(Stream FileStream, string Extension) : IRequest<string>;
