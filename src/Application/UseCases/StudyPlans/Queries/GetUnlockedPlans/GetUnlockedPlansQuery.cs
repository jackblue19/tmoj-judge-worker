using MediatR;
using System;
using System.Collections.Generic;
using Application.UseCases.StudyPlans.Dtos;

namespace Application.UseCases.StudyPlans.Queries.GetUnlockedPlans
{
    public class GetUnlockedPlansQuery : IRequest<List<StudyPlanDto>>
    {
      
        public Guid UserId { get; set; }
    }
}