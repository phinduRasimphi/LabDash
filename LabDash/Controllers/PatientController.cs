using Microsoft.AspNetCore.Mvc;

namespace LabDash.Controllers
{
    public class PatientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
