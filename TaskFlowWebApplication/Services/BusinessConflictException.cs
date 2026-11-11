namespace TaskFlowWebApplication.Services
{
    public class BusinessConflictException : InvalidOperationException
    {
        public BusinessConflictException(string message)
            : base(message)
        {
        }
    }
}
