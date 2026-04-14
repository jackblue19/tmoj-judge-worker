//using Domain.Entities;

//namespace Application.UseCases.Problems.Helpers;

//internal static class ProblemTagSyncHelper
//{
//    public static void MergeTags(Problem problem , IReadOnlyCollection<Tag> incomingTags)
//    {
//        var existingIds = problem.Tags.Select(x => x.Id).ToHashSet();

//        foreach ( var tag in incomingTags )
//        {
//            if ( existingIds.Add(tag.Id) )
//                problem