using Worker.Execution.Testset;

namespace Worker.Execution.Runtimes.Cp;

public interface ICpExecutorProfile
{
    string Name { get; }
    string SourceFileName { get; }
    bool HasCompileStep { get; }
    string CompileCommand { get; }
    string RunCommand { get; }
}

//public interface ICpExecutorProfile
//{
//    string Name { get; }
//    string SourceFileName { get; }
//    bool HasCompileStep { get; }

//    string BuildCompileCommand(string sourceFileName);

//    string BuildRunCommand(
//        string executablePath ,
//        string inputPath ,
//        string outputPath);
//}

//Command = profile.BuildRunCommand(execPath, input, output);

//public interface ICpExecutorProfile
//{
//    string Name { get; }

//    ExecutionPlan BuildCompilePlan(string workDir);
//    ExecutionPlan BuildRunPlan(PreparedJudgeCaseLayout prepared);
//}

//public sealed class ExecutionPlan
//{
//    public string Command { get; init; }
//    public int TimeoutMs { get; init; }
//    public string? StdInPath { get; init; }
//    public string? StdOutPath { get; init; }
//}

//Command = profile.BuildRunCommand(inputRelative, outputRelative)

//public sealed class CppExecutorProfile : ICpExecutorProfile
//{
//    public string Name => "cpp";
//    public string SourceFileName => "main.cpp";
//    public bool HasCompileStep => true;

//    public string BuildCompileCommand()
//        => "g++ -O2 -std=c++17 main.cpp -o main";

//    public string BuildRunCommand(string input , string output)
//        => $"bash -lc \"./main < '{input}' > '{output}'\"";
//}