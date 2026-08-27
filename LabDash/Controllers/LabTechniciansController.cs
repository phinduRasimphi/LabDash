using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class LabTechniciansController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public LabTechniciansController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // =========================================================
        // INDEX - DISPLAY ALL LAB TECHNICIANS
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var technicians = await _userManager.Users
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            var technicianList = new List<LabUser>();

            foreach (var user in technicians)
            {
                if (await _userManager.IsInRoleAsync(user, "Lab_Technician"))
                {
                    technicianList.Add(user);
                }
            }

            return View(technicianList);
        }


        // =========================================================
        // DETAILS
        // =========================================================

        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var technician = await _userManager.FindByIdAsync(id);

            if (technician == null)
            {
                return NotFound();
            }

            // Make sure the user is actually a technician
            if (!await _userManager.IsInRoleAsync(technician, "Lab_Technician"))
            {
                return NotFound();
            }

            var assignments = await _context.TechnicianTestTypes
                .Include(x => x.TestType)
                .Where(x => x.TechnicianId == technician.Id)
                .ToListAsync();

            ViewBag.Assignments = assignments;

            return View(technician);
        }


        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var testTypes = await _context.TestTypes
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.TestTypes = testTypes;

            return View();
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            LabUser technician,
            List<int>? selectedTestTypes)
        {
            // Remove properties managed by Identity
            ModelState.Remove("UserName");
            ModelState.Remove("NormalizedUserName");
            ModelState.Remove("NormalizedEmail");
            ModelState.Remove("PasswordHash");
            ModelState.Remove("SecurityStamp");
            ModelState.Remove("ConcurrencyStamp");

            // -----------------------------------------------------
            // Validate test types
            // -----------------------------------------------------

            if (selectedTestTypes == null || !selectedTestTypes.Any())
            {
                ModelState.AddModelError(
                    "selectedTestTypes",
                    "At least one test type must be assigned to the technician.");
            }

            // -----------------------------------------------------
            // Check SA ID uniqueness
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(technician.SouthAfricanID))
            {
                var existingId = await _userManager.Users
                    .AnyAsync(u =>
                        u.SouthAfricanID == technician.SouthAfricanID);

                if (existingId)
                {
                    ModelState.AddModelError(
                        "SouthAfricanID",
                        "This South African ID number is already registered.");
                }
            }

            // -----------------------------------------------------
            // Check employee number uniqueness
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(technician.EmployeeNumber))
            {
                var existingEmployee = await _userManager.Users
                    .AnyAsync(u =>
                        u.EmployeeNumber == technician.EmployeeNumber);

                if (existingEmployee)
                {
                    ModelState.AddModelError(
                        "EmployeeNumber",
                        "This employee number is already registered.");
                }
            }

            // -----------------------------------------------------
            // Check email uniqueness
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(technician.Email))
            {
                var existingEmail = await _userManager.FindByEmailAsync(
                    technician.Email);

                if (existingEmail != null)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email address is already registered.");
                }
            }

            // -----------------------------------------------------
            // Return form if validation failed
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                ViewBag.TestTypes = await _context.TestTypes
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                ViewBag.SelectedTestTypes = selectedTestTypes;

                return View(technician);
            }

            // -----------------------------------------------------
            // Generate temporary password
            // -----------------------------------------------------

            string temporaryPassword = GenerateTemporaryPassword();

            // -----------------------------------------------------
            // Prepare Identity user
            // -----------------------------------------------------

            technician.UserName = technician.Email;
            technician.Email = technician.Email;

            technician.EmailConfirmed = true;

            technician.MustChangePassword = true;

            technician.Timestamp_AccountCreated = DateTime.Now;

            // -----------------------------------------------------
            // Create Identity account
            // -----------------------------------------------------

            var result = await _userManager.CreateAsync(
                technician,
                temporaryPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                ViewBag.TestTypes = await _context.TestTypes
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                ViewBag.SelectedTestTypes = selectedTestTypes;

                return View(technician);
            }

            // -----------------------------------------------------
            // Make sure Lab_Technician role exists
            // -----------------------------------------------------

            if (!await _roleManager.RoleExistsAsync("Lab_Technician"))
            {
                await _roleManager.CreateAsync(
                    new IdentityRole("Lab_Technician"));
            }

            // -----------------------------------------------------
            // Assign technician role
            // -----------------------------------------------------

            await _userManager.AddToRoleAsync(
                technician,
                "Lab_Technician");

            // -----------------------------------------------------
            // Assign selected test types
            // -----------------------------------------------------

            foreach (var testTypeId in selectedTestTypes!)
            {
                var testTypeExists = await _context.TestTypes
                    .AnyAsync(t => t.Id == testTypeId);

                if (!testTypeExists)
                {
                    continue;
                }

                var assignment = new TechnicianTestType
                {
                    TechnicianId = technician.Id,
                    TestTypeId = testTypeId
                };

                _context.TechnicianTestTypes.Add(assignment);
            }

            await _context.SaveChangesAsync();

            // -----------------------------------------------------
            // Store temporary password for now
            // -----------------------------------------------------

            // This allows the manager to see the temporary password.
            // Later we can replace this with automatic email sending.

            TempData["Success"] =
                "Lab technician created successfully.";

            TempData["TemporaryPassword"] =
                temporaryPassword;

            TempData["TechnicianEmail"] =
                technician.Email;

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var technician = await _userManager.FindByIdAsync(id);

            if (technician == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(
                technician,
                "Lab_Technician"))
            {
                return NotFound();
            }

            var assignments = await _context.TechnicianTestTypes
                .Where(x => x.TechnicianId == technician.Id)
                .Select(x => x.TestTypeId)
                .ToListAsync();

            var testTypes = await _context.TestTypes
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.TestTypes = testTypes;
            ViewBag.SelectedTestTypes = assignments;

            return View(technician);
        }


        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            LabUser technician,
            List<int>? selectedTestTypes)
        {
            if (id != technician.Id)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // At least one test type is required
            // -----------------------------------------------------

            if (selectedTestTypes == null || !selectedTestTypes.Any())
            {
                ModelState.AddModelError(
                    "selectedTestTypes",
                    "At least one test type must be assigned.");
            }

            // -----------------------------------------------------
            // Find existing user
            // -----------------------------------------------------

            var existingTechnician =
                await _userManager.FindByIdAsync(id);

            if (existingTechnician == null)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // Check SA ID uniqueness
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                technician.SouthAfricanID))
            {
                var duplicateId = await _userManager.Users
                    .AnyAsync(u =>
                        u.Id != id &&
                        u.SouthAfricanID ==
                        technician.SouthAfricanID);

                if (duplicateId)
                {
                    ModelState.AddModelError(
                        "SouthAfricanID",
                        "This South African ID number is already registered.");
                }
            }

            // -----------------------------------------------------
            // Check employee number uniqueness
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                technician.EmployeeNumber))
            {
                var duplicateEmployee = await _userManager.Users
                    .AnyAsync(u =>
                        u.Id != id &&
                        u.EmployeeNumber ==
                        technician.EmployeeNumber);

                if (duplicateEmployee)
                {
                    ModelState.AddModelError(
                        "EmployeeNumber",
                        "This employee number is already registered.");
                }
            }

            // -----------------------------------------------------
            // Check email
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                technician.Email))
            {
                var duplicateEmail = await _userManager.Users
                    .AnyAsync(u =>
                        u.Id != id &&
                        u.Email == technician.Email);

                if (duplicateEmail)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email address is already registered.");
                }
            }

            // -----------------------------------------------------
            // Return if validation failed
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                ViewBag.TestTypes = await _context.TestTypes
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                ViewBag.SelectedTestTypes =
                    selectedTestTypes;

                return View(technician);
            }

            // -----------------------------------------------------
            // Update user details
            // -----------------------------------------------------

            existingTechnician.FirstName =
                technician.FirstName;

            existingTechnician.LastName =
                technician.LastName;

            existingTechnician.SouthAfricanID =
                technician.SouthAfricanID;

            existingTechnician.EmployeeNumber =
                technician.EmployeeNumber;

            existingTechnician.PhoneNumb =
                technician.PhoneNumb;

            existingTechnician.Email =
                technician.Email;

            existingTechnician.UserName =
                technician.Email;

            // -----------------------------------------------------
            // Update Identity
            // -----------------------------------------------------

            var updateResult =
                await _userManager.UpdateAsync(
                    existingTechnician);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                ViewBag.TestTypes = await _context.TestTypes
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                ViewBag.SelectedTestTypes =
                    selectedTestTypes;

                return View(technician);
            }

            // -----------------------------------------------------
            // Remove old test assignments
            // -----------------------------------------------------

            var oldAssignments =
                await _context.TechnicianTestTypes
                    .Where(x =>
                        x.TechnicianId == existingTechnician.Id)
                    .ToListAsync();

            _context.TechnicianTestTypes.RemoveRange(
                oldAssignments);

            // -----------------------------------------------------
            // Add new test assignments
            // -----------------------------------------------------

            foreach (var testTypeId in selectedTestTypes!)
            {
                var testTypeExists =
                    await _context.TestTypes
                        .AnyAsync(t => t.Id == testTypeId);

                if (!testTypeExists)
                {
                    continue;
                }

                _context.TechnicianTestTypes.Add(
                    new TechnicianTestType
                    {
                        TechnicianId =
                            existingTechnician.Id,

                        TestTypeId =
                            testTypeId
                    });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Lab technician updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // DELETE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var technician =
                await _userManager.FindByIdAsync(id);

            if (technician == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(
                technician,
                "Lab_Technician"))
            {
                return NotFound();
            }

            var assignments =
                await _context.TechnicianTestTypes
                    .Include(x => x.TestType)
                    .Where(x =>
                        x.TechnicianId == technician.Id)
                    .ToListAsync();

            ViewBag.Assignments = assignments;

            return View(technician);
        }


        // =========================================================
        // DELETE - POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            string id)
        {
            var technician =
                await _userManager.FindByIdAsync(id);

            if (technician == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(
                technician,
                "Lab_Technician"))
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // Remove test assignments first
            // -----------------------------------------------------

            var assignments =
                await _context.TechnicianTestTypes
                    .Where(x =>
                        x.TechnicianId == technician.Id)
                    .ToListAsync();

            _context.TechnicianTestTypes.RemoveRange(
                assignments);

            await _context.SaveChangesAsync();

            // -----------------------------------------------------
            // Delete Identity user
            // -----------------------------------------------------

            var result =
                await _userManager.DeleteAsync(
                    technician);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(technician);
            }

            TempData["Success"] =
                "Lab technician deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // ASSIGN TEST TYPES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AssignTestTypes(
            string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var technician =
                await _userManager.FindByIdAsync(id);

            if (technician == null)
            {
                return NotFound();
            }

            var testTypes =
                await _context.TestTypes
                    .OrderBy(t => t.Name)
                    .ToListAsync();

            var selected =
                await _context.TechnicianTestTypes
                    .Where(x =>
                        x.TechnicianId == technician.Id)
                    .Select(x => x.TestTypeId)
                    .ToListAsync();

            ViewBag.Technician = technician;
            ViewBag.TestTypes = testTypes;
            ViewBag.SelectedTestTypes = selected;

            return View();
        }


        // =========================================================
        // ASSIGN TEST TYPES - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTestTypes(
            string id,
            List<int>? selectedTestTypes)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var technician =
                await _userManager.FindByIdAsync(id);

            if (technician == null)
            {
                return NotFound();
            }

            // At least one test must be assigned
            if (selectedTestTypes == null ||
                !selectedTestTypes.Any())
            {
                TempData["Error"] =
                    "At least one test type must be assigned.";

                return RedirectToAction(
                    nameof(AssignTestTypes),
                    new { id });
            }

            // Remove existing assignments
            var existingAssignments =
                await _context.TechnicianTestTypes
                    .Where(x =>
                        x.TechnicianId == technician.Id)
                    .ToListAsync();

            _context.TechnicianTestTypes.RemoveRange(
                existingAssignments);

            // Add selected assignments
            foreach (var testTypeId in selectedTestTypes)
            {
                var exists =
                    await _context.TestTypes
                        .AnyAsync(t => t.Id == testTypeId);

                if (!exists)
                {
                    continue;
                }

                _context.TechnicianTestTypes.Add(
                    new TechnicianTestType
                    {
                        TechnicianId = technician.Id,
                        TestTypeId = testTypeId
                    });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Test types assigned successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        // =========================================================
        // GENERATE TEMPORARY PASSWORD
        // =========================================================

        private string GenerateTemporaryPassword()
        {
            return "Lab@" +
                   Guid.NewGuid()
                       .ToString("N")
                       .Substring(0, 8) +
                   "1!";
        }
    }
}