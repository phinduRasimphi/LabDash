using LabDash.Models;
using Microsoft.AspNetCore.Mvc;

namespace LabDash.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var model = new AdminDashboardViewModel
            {
                ConditionCount = 0,
                AllergyCount = 0,
                MedicationCount = 0,
                UserCount = 14
            };

            return View(model);
        }
    }
}