using Contracts.Submissions.Judging;

namespace WebAPI.Services.Judging;

public static class JudgeStatusClassifier
{
    public static bool IsInfrastructureFailure(JudgeJobCompletedContract req)
    {
        if ( req is null )
            return true;

        if ( string.Equals(req.Status , "failed" , StringComparison.OrdinalIgnoreCase)
             && string.Equals(req.Summary.Verdict , "ie" , StringComparison.OrdinalIgnoreCase) )
            return true;

        return false;
    }

    public static string NormalizeSubmissionStatus(JudgeJobCompletedContract req)
        => IsInfrastructureFailure(req) ? "failed" : "done";

    public static string NormalizeJudgeRunStatus(JudgeJobCompletedContract req)
        => IsInfrastructureFailure(req) ? "failed" : "done";

    public static string NormalizeJudgeJobStatus(JudgeJobCompletedContract req)
        => IsInfrastructureFailure(req) ? "failed" : "done";
}