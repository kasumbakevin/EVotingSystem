namespace EVotingSystem.Services
{
    /// <summary>
    /// Outcome of a service operation. Carries a user-facing message so that
    /// failures can be surfaced in the UI instead of being written to a console
    /// that does not exist in a WinForms application.
    /// </summary>
    public sealed record ServiceResult(bool Success, string Message)
    {
        public static ServiceResult Ok(string message) => new(true, message);

        public static ServiceResult Fail(string message) => new(false, message);
    }

    public sealed record ServiceResult<T>(bool Success, string Message, T? Value)
    {
        public static ServiceResult<T> Ok(T value, string message) => new(true, message, value);

        public static ServiceResult<T> Fail(string message) => new(false, message, default);
    }
}
