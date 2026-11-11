using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Services
{
    public interface ITokenIssue
    {
        Task<IssuedTokens> IssueAsync(User user);
    }
}
