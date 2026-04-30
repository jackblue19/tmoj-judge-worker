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
            summary = new
            {
                type = "string" ,
                description = "One short paragraph, maximum 350 characters."
            } ,
            suspectedIssueCode = new
            {
                type = "string" ,
                description = "Short snake_case issue code."
            } ,
            confidence = new
            {
                type = "integer" ,
                description = "Integer from 0 to 100."
            } ,
            confidenceLevelCode = new
            {
                type = "string" ,
                description = "low, medium, or high."
            } ,
            sections = new
            {
                type = "array" ,
                minItems = 4 ,
                maxItems = 6 ,
                items = new
                {
                    type = "object" ,
                    required = new[] { "title" , "contentMd" } ,
                    properties = new
                    {
                        title = new
                        {
                            type = "string" ,
                            description = "Short section title."
                        } ,
                        contentMd = new
                        {
                            type = "string" ,
                            description = "Markdown bullet list. Maximum 700 characters. Do not include JSON."
                        }
                    }
                }
            } ,
            safetyNote = new
            {
                type = "string" ,
                description = "Short safety disclaimer."
            }
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
            "warnings"
        } ,
        properties = new
        {
            title = new
            {
                type = "string" ,
                description = "Short editorial draft title."
            } ,
            summaryMd = new
            {
                type = "string" ,
                description = "Short markdown summary, maximum 600 characters."
            } ,
            contentMd = new
            {
                type = "string" ,
                description = "Markdown editorial content only. Do not include JSON inside this field."
            } ,
            confidence = new
            {
                type = "integer" ,
                description = "Integer from 0 to 100."
            } ,
            confidenceLevelCode = new
            {
                type = "string" ,
                description = "low, medium, or high."
            } ,
            warnings = new
            {
                type = "array" ,
                maxItems = 3 ,
                items = new
                {
                    type = "string"
                }
            } ,
            assumptions = new
            {
                type = "array" ,
                maxItems = 5 ,
                items = new
                {
                    type = "string"
                }
            }
        }
    };
}