using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

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


   