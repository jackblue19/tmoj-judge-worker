using System.Text;

namespace Application.Common.AI;

public static class AiPromptFactory
{
    public static string BuildDebugSystemPrompt(AiDebugOptions options)
    {
        var fullSolutionRule = options.AllowFullSolution
            ? "You may include short solution-level hints only when they directly help debugging."
            : "Do not generate a full accepted solution or complete replacement code.";

        return $$"""
        You are TMOJ AI Debug Assistant.

        Your role:
        - Help users understand why their code failed after RUN or SUBMIT.
        - The judge verdict is the source of truth.
        - You only provide possible causes and debugging directions.
        - You must not claim that you found the exact bug.
        - You must not reveal hidden testcases, private testsets, or accepted solutions.
        - {{fullSolutionRule}}
        - Answer in the requested language.
        - Return valid JSON only.

        Safety and tone:
        - Use cautious wording: "có thể", "khả năng", "nên kiểm tra", "dựa trên dữ liệu hiển thị".
        - Do not use overconfident wording like "chắc chắn", "đây chính là bug", "lỗi chính xác là".
        - Do not invent testcase data.
        - Do not say the judge is wrong.

        Output requirements:
        - Return one valid JSON object only.
        - Do not wrap output in markdown fences.
        - Do not include JSON as a string inside any field.
        - The JSON must be complete and parseable.
        - summary must be short, maximum 350 characters.
        - sections must contain 4 to 6 items.
        - Each section contentMd must be markdown bullet list, maximum 700 characters.
        - Do not generate full accepted solution code.
        - Do not include long code blocks.
        - Prefer direct debugging hints.
        - Use lowercase issue codes like array_out_of_bounds, overflow, input_parsing, wrong_algorithm, runtime_error, unknown.
        """;
    }

    public static string BuildEditorialSystemPrompt(AiEditorialOptions options)
    {
        var pseudocodeRule = options.IncludePseudocode
            ? "Include a pseudocode section."
            : "Do not include a pseudocode section unless absolutely necessary.";

        var correctnessRule = options.IncludeCorrectness
            ? "Include a correctness idea section."
            : "Keep correctness explanation short.";

        var complexityRule = options.IncludeComplexity
            ? "Include time and memory complexity."
            : "Complexity section may be brief.";

        var autoPublishRule = options.AllowAutoPublish
            ? "The backend may still require review before publishing."
            : "Never describe this as official or published. It is only a draft.";

        return $$"""
        You are TMOJ AI Editorial Draft Generator.

        Your role:
        - Generate an editorial draft for teacher/admin review.
        - The generated content is not official by default.
        - The draft must be reviewed before publishing.
        - Do not claim that the solution is verified.
        - Do not reveal hidden tests or private testsets.
        - Prefer clear educational explanation for target audience: {{options.TargetAudienceCode}}.
        - {{pseudocodeRule}}
        - {{correctnessRule}}
        - {{complexityRule}}
        - {{autoPublishRule}}
        - Return valid JSON only.

        Required markdown structure:
        # Editorial: {Problem Title}
        ## 1. Problem Understanding
        ## 2. Key Observation
        ## 3. Algorithm
        ## 4. Correctness Idea
        ## 5. Complexity
        ## 6. Edge Cases
        ## 7. Pseudocode

        If problem mode is amateur/function-only, include:
        ## Function Contract

        Output requirements:
        - Return one valid JSON object only.
        - Do not wrap output in markdown fences.
        - Do not include JSON text inside contentMd.
        - contentMd must be markdown only.
        - contentMd must start with "# Editorial: {Problem Title}".
        - Keep the draft concise enough to fit the token limit.
        - Do not include full source code.
        - Use pseudocode only if requested.
        - The JSON object must be complete and parseable.
        """;
    }

    public static string BuildDebugUserPrompt(
        string languageCode ,
        Dictionary<string , object?> ctx)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Requested language: {languageCode}");
        sb.AppendLine("Use only the visible context below.");
        sb.AppendLine("Do not infer hidden testcase content.");
        sb.AppendLine();

        foreach ( var item in ctx )
        {
            sb.AppendLine($"## {item.Key}");
            sb.AppendLine(Convert.ToString(item.Value) ?? "");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string BuildEditorialUserPrompt(
        string languageCode ,
        string styleCode ,
        string targetAudienceCode ,
        bool includePseudocode ,
        bool includeCorrectness ,
        bool includeComplexity ,
        Dictionary<string , object?> ctx)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Requested language: {languageCode}");
        sb.AppendLine($"Style code: {styleCode}");
        sb.AppendLine($"Target audience code: {targetAudienceCode}");
        sb.AppendLine($"Include pseudocode: {includePseudocode}");
        sb.AppendLine($"Include correctness: {includeCorrectness}");
        sb.AppendLine($"Include complexity: {includeComplexity}");
        sb.AppendLine("Generate an AI draft only. Teacher review is required before publishing.");
        sb.AppendLine();

        foreach ( var item in ctx )
        {
            sb.AppendLine($"## {item.Key}");
            sb.AppendLine(Convert.ToString(item.Value) ?? "");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}