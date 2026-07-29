using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class SampleReceiveController : Controller
    {
        private readonly LabDbContext _context;

        public SampleReceiveController(LabDbContext context)
        {
            _context = context;
        }

        // Display Receive Sample Form
        [HttpGet]
        public IActionResult Receive()
        {
            ViewBag.RequestList = new SelectList(
                _context.TestRequests.Where(r => r.Status == "Pending").ToList(),
                "RequestId", "RequestId");

            return View();
        }

        // Save Received Sample
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Receive(SampleReceive sample)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RequestList = new SelectList(
                    _context.TestRequests.Where(r => r.Status == "Pending").ToList(),
                    "RequestId", "RequestId");
                return View(sample);
            }

            var request = _context.TestRequests
                                  .FirstOrDefault(r => r.RequestId == sample.RequestId);

            if (request == null)
            {
                ModelState.AddModelError("", "The selected test request could not be found.");
                ViewBag.RequestList = new SelectList(
                    _context.TestRequests.Where(r => r.Status == "Pending").ToList(),
                    "RequestId", "RequestId");
                return View(sample);
            }

            if (request.Status != "Pending")
            {
                ModelState.AddModelError("", "This request has already been received or is no longer pending.");
                ViewBag.RequestList = new SelectList(
                    _context.TestRequests.Where(r => r.Status == "Pending").ToList(),
                    "RequestId", "RequestId");
                return View(sample);
            }

            request.Status = "Samples Received";
            request.DateTimeReceived = DateTime.Now;

            sample.DateTimeReceived = DateTime.Now;
            sample.Status = "Samples Received";
            sample.TechnicianName = User.Identity?.Name ?? "Unknown";

            _context.SampleReceives.Add(sample);
            _context.SaveChanges();

            TempData["Success"] = "Sample received successfully.";
            return RedirectToAction(nameof(Index));
        }

        // List of Received Samples
        [HttpGet]
        public IActionResult Index()
        {
            var samples = _context.SampleReceives
                                  .Include(s => s.TestRequest)
                                  .OrderByDescending(s => s.DateTimeReceived)
                                  .ToList();

            return View(samples);
        }
    }
}
