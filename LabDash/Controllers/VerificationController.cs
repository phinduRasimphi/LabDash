using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class VerificationController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public VerificationController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //---------------------------------------------------------
        // Verification Queue
        //---------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var tests = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .Include(t => t.TestType)
                .Include(t => t.AssignedTechnician)
                .Where(t =>
                    t.Status == "Completed" &&
                    t.AssignedTechnicianId != technician.Id)
                .OrderBy(t => t.CompletionDateTime)
                .ToListAsync();

            return View(tests);
        }

        //---------------------------------------------------------
        // Display Verification Page
        //---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Verify(int id)
        {
            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                .Include(t => t.AssignedTechnician)
                .FirstOrDefaultAsync(t =>
                    t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            var result = await _context.TestResults
                .FirstOrDefaultAsync(r =>
                    r.TestRequestItemId == id);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.PatientID == item.TestRequest.PatientId);

            ViewBag.TestItem = item;
            ViewBag.Patient = patient;
            ViewBag.Result = result;

            return View(new TestVerification
            {
                TestRequestItemId = id
            });
        }

        //---------------------------------------------------------
        // Verify Test
        //---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(TestVerification verification)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems
                .Include(t => t.TestRequest)
                .FirstOrDefaultAsync(t =>
                    t.TestRequestItemId ==
                    verification.TestRequestItemId);

            if (item == null)
                return NotFound();

            verification.VerifiedByTechnicianId = technician.Id;
            verification.VerificationDate = DateTime.Now;

            if (verification.Status == "Verified")
            {
                item.Status = "Verified";
            }
            else
            {
                item.Status = "To Be Reviewed";
            }

            _context.TestVerifications.Add(verification);

            //---------------------------------------------------------
            // If every test is verified,
            // update request status
            //---------------------------------------------------------

            bool allVerified = await _context.TestRequestItems
                .Where(x => x.RequestId == item.RequestId)
                .AllAsync(x => x.Status == "Verified");

            if (allVerified)
            {
                item.TestRequest.Status = "Verified";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Verification completed successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
