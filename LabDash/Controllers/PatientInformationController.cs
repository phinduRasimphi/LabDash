using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class PatientInformationController : Controller
    {
        private readonly LabDbContext _context;

        public PatientInformationController(LabDbContext context)
        {
            _context = context;
        }

        // Display all patients
        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients
                .OrderBy(p => p.Surname)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return View(patients);
        }

        // Display one patient's medical information
        public async Task<IActionResult> Details(int id)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PatientID == id);

            if (patient == null)
                return NotFound();

            return View(patient);
        }
    }
}