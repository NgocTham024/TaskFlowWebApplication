using Microsoft.AspNetCore.Mvc;

namespace TaskFlowWebApplication.Controller
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
