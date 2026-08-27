using Microsoft.AspNetCore.Mvc;

namespace LabDash.Controllers
{
    public class TestAssignmentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
