namespace Application.Common.AI;

public static class AiJsonSchemas
{
    public static object DebugResponseSchema => new
    {
        type = "object" ,
        required = new[]
        {
            "summary",
            "suspectedIssueCode",
            "confidence",
            "confidenceLevelCode",
            "sections",
            "safetyNote"
        } ,
        properties = new
        {
            summary = new { type = "string" } ,
            suspectedIssueCode = new { type = "string" } ,
            confidence = new { type = "integer" } ,
            confidenceLevelCode = new { type = "string" } ,
            sections = new
            {
                type = "array" ,
                items = new
                {
                    type = "object" ,
                    required = new[] { "title" , "contentMd" } ,
                    properties = new
                    {
                        title = new { type = "string" } ,
                        contentMd = new { type = "string" }
                    }
                }
            } ,
            safetyNote = new { type = "string" }
        }
    };

    public static object EditorialResponseSchema => new
    {
        type = "object" ,
        required = new[]
        {
            "title",
            "summaryMd",
            "contentMd",
            "confidence",
            "confidenceLevelCode",
            "outline",
            "warnings",
            "assumptions"
        } ,
        properties = new
        {
            title = new { type = "string" } ,
            summaryMd = new { type = "string" } ,
            contentMd = new { type = "string" } ,
            confidence = new { type = "integer" } ,
            confidenceLevelCode = new { type = "string" } ,
            outline = new
            {
                type = "object" ,
                properties = new
                {
                    sections = new
                    {
                        type = "array" ,
                        items = new { type = "string" }
                    }
                }
            } ,
            warnings = new
            {
                type = "array" ,
                items = new { type = "string" }
            } ,
            assumptions = new
            {
                type = "array" ,
                items = new { type = "string" }
            }
        }
    };
}