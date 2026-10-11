using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Services
{
    public interface ITokenIssue
    {
        Task<IssuedTokens> IssueAsync(Users user);
    }
}
