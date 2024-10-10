using System;

namespace LivingFSM
{
    public class StepResult
    {
        public StepResult(Result result) : this(result, default, default, default)
        {
        }

        public StepResult(Result result, object parameter) : this(result, default, parameter, default)
        {
        }

        public StepResult(Result result, Exception exception) : this(result, exception, default, default)
        {
        }

        public StepResult(Result result, string error) : this(result, default, default, error)
        {
        }

        public StepResult(Result result, Exception exception, object parameter, string error)
        {
            Result = result;
            Exception = default;
            Error = error;
            Parameter = default;
        }

        public static StepResult Failed { get; } = new StepResult(Result.Failed);
        public static StepResult Finished { get; } = new StepResult(Result.Finished);
        public static StepResult Forward { get; } = new StepResult(Result.Forward);
        public string Error { get; }
        public Exception Exception { get; }
        public object Parameter { get; }
        public Result Result { get; }
    }
}