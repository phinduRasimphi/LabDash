using Microsoft.AspNetCore.Mvc;

namespace LabDash.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
