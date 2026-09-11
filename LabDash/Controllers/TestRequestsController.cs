using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class TestRequestController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly IEmailSender _emailSender;

        public TestRequestController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var allRequests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .OrderByDescending(tr => tr.RequestDate)
                .ToListAsync();

            var folders = allRequests
                .GroupBy(tr => tr.PatientId)
                .Select(g => new TrackRequestViewModel
                {
                    Patient = g.First().Patient,

                    Requests = g
                        .OrderByDescending(r => r.RequestDate)
                        .ToList()
                })
                .OrderBy(x => x.Patient.Name)
                .ToList();

            return View(folders);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockFolder(
            int patientId,
            string idNumber)
        {
            var patient = await _context.Patients
                .FindAsync(patientId);

            if (patient == null)
            {
                return NotFound();
            }

            if (patient.IDNumber != idNumber?.Trim())
            {
                TempData["Error"] =
                    "Incorrect ID number. Folder remains locked.";

                return RedirectToAction(nameof(Index));
            }

            var requests = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestingDoctor)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(
                "PatientRequests",
                requests);
        }


        // ============================================================
        // GET: /TestRequest/Create?patientId=5
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Create(int? patientId)
        {
            // --------------------------------------------------------
            // Check patient
            // --------------------------------------------------------

            if (patientId == null)
            {
                TempData["Error"] =
                    "No patient selected. Please search for a patient first.";

                return RedirectToAction(
                    "ManagePatients",
                    "Doctor");
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(
                    p => p.PatientID == patientId.Value);

            if (patient == null)
            {
                TempData["Error"] =
                    "Patient not found.";

                return RedirectToAction(
                    "ManagePatients",
                    "Doctor");
            }


            // --------------------------------------------------------
            // Patient information
            // --------------------------------------------------------

            ViewBag.PatientId =
                patient.PatientID;

            ViewBag.PatientName =
                $"{patient.Name} {patient.Surname}";

            ViewBag.PatientIDNumber =
                patient.IDNumber;

            ViewBag.PatientDOB =
                patient.DOB.ToString("dd MMM yyyy");


            // --------------------------------------------------------
            // Load laboratory tests
            // --------------------------------------------------------

            var testTypes = await _context.TestTypes
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync();

            ViewBag.TestTypes =
                testTypes;

            return View();
        }


        // ============================================================
        // POST: /TestRequest/Create
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int patientId,
            DateTime requestDate,
            string? urgency,
            string? clinicalNotes,
            int[]? selectedTestTypeIds,
            string[]? sampleTypes,
            string[]? sampleBarcodes)
        {
            // ========================================================
            // 1. VALIDATE PATIENT ID
            // ========================================================

            if (patientId <= 0)
            {
                TempData["Error"] =
                    "Please select a valid patient.";

                return RedirectToAction(
                    "ManagePatients",
                    "Doctor");
            }


            // ========================================================
            // 2. VALIDATE TEST TYPES
            // ========================================================

            if (selectedTestTypeIds == null ||
                selectedTestTypeIds.Length == 0)
            {
                TempData["Error"] =
                    "Please select at least one laboratory test.";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }

            selectedTestTypeIds =
                selectedTestTypeIds
                    .Distinct()
                    .ToArray();


            // ========================================================
            // 3. VALIDATE URGENCY
            // ========================================================

            string[] allowedUrgencies =
            {
                "Routine",
                "Urgent",
                "STAT"
            };

            if (string.IsNullOrWhiteSpace(urgency))
            {
                TempData["Error"] =
                    "Please select an urgency.";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }

            urgency = urgency.Trim();

            string? validUrgency =
                allowedUrgencies.FirstOrDefault(
                    x => string.Equals(
                        x,
                        urgency,
                        StringComparison.OrdinalIgnoreCase));

            if (validUrgency == null)
            {
                TempData["Error"] =
                    "Please select a valid urgency.";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }

            urgency = validUrgency;


            // ========================================================
            // 4. VALIDATE REQUEST DATE
            // ========================================================

            if (requestDate.Date > DateTime.Today)
            {
                TempData["Error"] =
                    "Request date cannot be in the future.";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }


            // ========================================================
            // 5. FIND LOGGED-IN DOCTOR
            // ========================================================

            var doctor =
                await _userManager.GetUserAsync(User);

            if (doctor == null)
            {
                TempData["Error"] =
                    "Unable to identify the logged-in doctor.";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }


            // ========================================================
            // 6. FIND PATIENT
            // ========================================================

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        p => p.PatientID == patientId);

            if (patient == null)
            {
                TempData["Error"] =
                    "Patient not found.";

                return RedirectToAction(
                    "ManagePatients",
                    "Doctor");
            }


            // ========================================================
            // 7. LOAD SELECTED TEST TYPES
            // ========================================================

            var selectedTypes =
                await _context.TestTypes
                    .Where(t =>
                        selectedTestTypeIds.Contains(t.Id))
                    .OrderBy(t => t.RequiredSampleType)
                    .ThenBy(t => t.Category)
                    .ThenBy(t => t.Name)
                    .ToListAsync();

            if (selectedTypes.Count !=
                selectedTestTypeIds.Length)
            {
                TempData["Error"] =
                    "One or more selected laboratory tests could not be found. " +
                    "Please refresh the page and select the tests again.";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }


            // ========================================================
            // 8. DETERMINE REQUIRED SAMPLE TYPES
            // ========================================================

            var requiredSampleTypes =
                selectedTypes
                    .Select(t =>
                        t.RequiredSampleType?.Trim())
                    .Where(s =>
                        !string.IsNullOrWhiteSpace(s))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();

            if (requiredSampleTypes.Count == 0)
            {
                TempData["Error"] =
                    "The selected tests do not have valid sample types configured.";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }


            // ========================================================
            // 9. VALIDATE SAMPLE ARRAYS
            // ========================================================

            sampleTypes ??=
                Array.Empty<string>();

            sampleBarcodes ??=
                Array.Empty<string>();

            if (sampleTypes.Length == 0 ||
                sampleBarcodes.Length == 0)
            {
                TempData["Error"] =
                    "At least one sample with a barcode is required.";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }

            if (sampleTypes.Length !=
                sampleBarcodes.Length)
            {
                TempData["Error"] =
                    "There was a problem with the sample information. " +
                    "Please refresh the page and enter the sample barcodes again.";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }


            // ========================================================
            // 10. VALIDATE EACH SAMPLE
            // ========================================================

            var submittedSamples =
                new List<(string SampleType, string Barcode)>();

            for (int i = 0;
                 i < sampleTypes.Length;
                 i++)
            {
                string sampleType =
                    sampleTypes[i]?.Trim() ?? "";

                string barcode =
                    sampleBarcodes[i]?.Trim() ?? "";


                // ----------------------------------------------------
                // Sample type required
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(sampleType))
                {
                    TempData["Error"] =
                        "A sample type is missing. " +
                        "Please refresh the page and try again.";

                    return RedirectToAction(
                        nameof(Create),
                        new { patientId });
                }


                // ----------------------------------------------------
                // Barcode required
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(barcode))
                {
                    TempData["Error"] =
                        $"Please enter a barcode for the {sampleType} sample.";

                    return RedirectToAction(
                        nameof(Create),
                        new { patientId });
                }


                // ----------------------------------------------------
                // Check sample type is actually required
                // ----------------------------------------------------

                bool isRequired =
                    requiredSampleTypes.Any(
                        x => string.Equals(
                            x,
                            sampleType,
                            StringComparison.OrdinalIgnoreCase));

                if (!isRequired)
                {
                    TempData["Error"] =
                        $"The sample type '{sampleType}' " +
                        "is not required for the selected tests.";

                    return RedirectToAction(
                        nameof(Create),
                        new { patientId });
                }


                // ----------------------------------------------------
                // Prevent duplicate sample type
                // ----------------------------------------------------

                bool duplicateSampleType =
                    submittedSamples.Any(
                        x => string.Equals(
                            x.SampleType,
                            sampleType,
                            StringComparison.OrdinalIgnoreCase));

                if (duplicateSampleType)
                {
                    TempData["Error"] =
                        $"The {sampleType} sample has been entered more than once.";

                    return RedirectToAction(
                        nameof(Create),
                        new { patientId });
                }


                // ----------------------------------------------------
                // Prevent duplicate barcode in this request
                // ----------------------------------------------------

                bool duplicateBarcode =
                    submittedSamples.Any(
                        x => string.Equals(
                            x.Barcode,
                            barcode,
                            StringComparison.OrdinalIgnoreCase));

                if (duplicateBarcode)
                {
                    TempData["Error"] =
                        $"The sample barcode '{barcode}' " +
                        "has been entered more than once.";

                    return RedirectToAction(
                        nameof(Create),
                        new { patientId });
                }


                submittedSamples.Add(
                    (sampleType, barcode));
            }


            // ========================================================
            // 11. MAKE SURE ALL REQUIRED SAMPLE TYPES ARE PROVIDED
            // ========================================================

            var submittedSampleTypes =
                submittedSamples
                    .Select(x => x.SampleType)
                    .ToList();

            var missingSampleTypes =
                requiredSampleTypes
                    .Where(required =>
                        !submittedSampleTypes.Any(
                            submitted =>
                                string.Equals(
                                    required,
                                    submitted,
                                    StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            if (missingSampleTypes.Count > 0)
            {
                TempData["Error"] =
                    "Please enter barcodes for: " +
                    string.Join(
                        ", ",
                        missingSampleTypes) +
                    ".";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }


            // ========================================================
            // 12. CHECK EXISTING BARCODES
            // ========================================================

            var submittedBarcodes =
                submittedSamples
                    .Select(x => x.Barcode)
                    .ToList();

            var existingBarcode =
                await _context.Samples
                    .Where(s =>
                        submittedBarcodes.Contains(
                            s.Barcode))
                    .Select(s => s.Barcode)
                    .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(
                existingBarcode))
            {
                TempData["Error"] =
                    $"The sample barcode '{existingBarcode}' " +
                    "is already registered. Please use a unique barcode.";

                return RedirectToAction(
                    nameof(Create),
                    new { patientId });
            }


            // ========================================================
            // 13. CREATE TEST REQUEST
            // ========================================================

            var testRequest =
                new TestRequest
                {
                    PatientId = patientId,

                    RequestingDoctorId =
                        doctor.Id,

                    RequestDate =
                        requestDate,

                    Urgency =
                        urgency,

                    ClinicalNotes =
                        string.IsNullOrWhiteSpace(
                            clinicalNotes)
                            ? null
                            : clinicalNotes.Trim(),

                    Status =
                        "Submitted",

                    SubmittedDate =
                        DateTime.Now,

                    // Keep this because your
                    // existing system uses it
                    SampleBarcodes =
                        string.Join(
                            ", ",
                            submittedSamples
                                .Select(x => x.Barcode))
                };


            // ========================================================
            // 14. CREATE TEST REQUEST ITEMS
            // ========================================================

            foreach (var testType in selectedTypes)
            {
                var testItem =
                    new TestRequestItem
                    {
                        TestRequest =
                            testRequest,

                        TestTypeId =
                            testType.Id,

                        Status =
                            "Submitted",

                        AssignedTechnicianId =
                            null,

                        StartDateTime =
                            null,

                        CompletionDateTime =
                            null
                    };

                testRequest
                    .TestRequestItems
                    .Add(testItem);
            }


            // ========================================================
            // 15. CREATE SAMPLE RECORDS
            // ========================================================

            foreach (var submittedSample
                     in submittedSamples)
            {
                var sample =
                    new Sample
                    {
                        Barcode =
                            submittedSample.Barcode,

                        SampleType =
                            submittedSample.SampleType,

                        IsReceived =
                            false,

                        DateReceived =
                            null,

                        // Sample has not been
                        // received by a technician yet
                        ReceivedByTechnician =
                            string.Empty,

                        // Connect sample to request
                        TestRequest =
                            testRequest
                    };

                testRequest
                    .Samples
                    .Add(sample);
            }


            // ========================================================
            // 16. SAVE REQUEST + TEST ITEMS + SAMPLES
            // ========================================================

            _context.TestRequests
                .Add(testRequest);

            await _context.SaveChangesAsync();


            // ========================================================
            // 17. VERIFY TEST ITEMS
            // ========================================================

            int itemCount =
                await _context.TestRequestItems
                    .CountAsync(
                        x => x.RequestId ==
                             testRequest.RequestId);

            int sampleCount =
                await _context.Samples
                    .CountAsync(
                        x => x.TestRequestId ==
                             testRequest.RequestId);


            if (itemCount == 0)
            {
                TempData["Error"] =
                    $"Request #{testRequest.RequestId} " +
                    "was created, but no laboratory test items were created.";

                return RedirectToAction(
                    nameof(Index));
            }


            if (sampleCount == 0)
            {
                TempData["Error"] =
                    $"Request #{testRequest.RequestId} " +
                    "was created, but no samples were recorded.";

                return RedirectToAction(
                    nameof(Index));
            }


            // ========================================================
            // 18. PREPARE PATIENT EMAIL
            // ========================================================

            string testListHtml =
                string.Join(
                    ", ",
                    selectedTypes
                        .Select(t => t.Name));

            string sampleListHtml =
                string.Join(
                    ", ",
                    submittedSamples
                        .Select(s => s.SampleType));


            string emailBody = $@"
                <p>Dear {patient.Name},</p>

                <p>
                    Dr. {doctor.LastName} has submitted a
                    laboratory test request for you at
                    <strong>NMB LAB</strong>.
                </p>

                <p>
                    <strong>Tests requested:</strong>
                    {testListHtml}<br/>

                    <strong>Request date:</strong>
                    {testRequest.RequestDate:dd MMM yyyy}<br/>

                    <strong>Urgency:</strong>
                    {testRequest.Urgency}
                </p>

                <p>
                    <strong>Samples required:</strong>
                    {sampleListHtml}
                </p>

                <p>
                    Please visit the laboratory to provide
                    the required sample(s).
                </p>

                <p>
                    Thank you,<br/>
                    <strong>NMB LAB</strong>
                </p>";


            // ========================================================
            // 19. SEND PATIENT EMAIL
            // ========================================================

            try
            {
                if (!string.IsNullOrWhiteSpace(
                    patient.Email))
                {
                    await _emailSender.SendEmailAsync(
                        patient.Email,
                        "New Test Request Submitted",
                        emailBody);
                }
            }
            catch
            {
                // The request has already been
                // successfully saved.

                TempData["SuccessMessage"] =
                    $"Test request #{testRequest.RequestId} " +
                    "was created successfully, but the patient " +
                    "notification email could not be sent.";

                return RedirectToAction(
                    nameof(Index));
            }


            // ========================================================
            // 20. SUCCESS
            // ========================================================

            TempData["SuccessMessage"] =
                $"Test request #{testRequest.RequestId} " +
                $"created successfully with {itemCount} " +
                $"laboratory test(s) and {sampleCount} sample(s).";

            return RedirectToAction(
                nameof(Index));
        }


        // ============================================================
        // GET: /TestRequest/Edit/5
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.TestRequestItems)
                    .ThenInclude(tri => tri.TestType)
                .FirstOrDefaultAsync(
                    tr => tr.RequestId == id);

            if (request == null)
            {
                return NotFound();
            }


            ViewBag.RequestId =
                request.RequestId;

            ViewBag.PatientName =
                $"{request.Patient.Name} {request.Patient.Surname}";

            ViewBag.PatientIDNumber =
                request.Patient.IDNumber;


            var barcodes =
                string.IsNullOrEmpty(
                    request.SampleBarcodes)
                    ? new List<string>()
                    : request.SampleBarcodes
                        .Split(',')
                        .Select(x => x.Trim())
                        .ToList();


            ViewBag.Barcode1 =
                barcodes.Count > 0
                    ? barcodes[0]
                    : "";

            ViewBag.Barcode2 =
                barcodes.Count > 1
                    ? barcodes[1]
                    : "";


            var testTypes =
                await _context.TestTypes
                    .OrderBy(t => t.Category)
                    .ThenBy(t => t.Name)
                    .ToListAsync();

            ViewBag.TestTypes =
                testTypes;


            ViewBag.SelectedTestTypeIds =
                request.TestRequestItems
                    .Select(i => i.TestTypeId)
                    .ToList();


            return View();
        }


        // ============================================================
        // POST: /TestRequest/Edit/5
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int requestId,
            DateTime requestDate,
            string urgency,
            string? clinicalNotes,
            int[] selectedTestTypeIds,
            string? sampleBarcode1,
            string? sampleBarcode2)
        {
            var request =
                await _context.TestRequests
                    .Include(tr =>
                        tr.TestRequestItems)
                    .FirstOrDefaultAsync(
                        tr => tr.RequestId ==
                              requestId);

            if (request == null)
            {
                return NotFound();
            }


            if (selectedTestTypeIds == null ||
                !selectedTestTypeIds.Any())
            {
                TempData["Error"] =
                    "Select at least one test type.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id = requestId });
            }


            request.RequestDate =
                requestDate;

            request.Urgency =
                urgency;

            request.ClinicalNotes =
                string.IsNullOrWhiteSpace(
                    clinicalNotes)
                    ? null
                    : clinicalNotes.Trim();


            request.SampleBarcodes =
                string.Join(
                    ",",
                    new[]
                    {
                        sampleBarcode1,
                        sampleBarcode2
                    }
                    .Where(b =>
                        !string.IsNullOrWhiteSpace(b))
                    .Select(b => b!.Trim())
                    .ToArray());


            // Remove old test items
            _context.TestRequestItems
                .RemoveRange(
                    request.TestRequestItems);

            await _context.SaveChangesAsync();


            var selectedTypes =
                await _context.TestTypes
                    .Where(t =>
                        selectedTestTypeIds
                            .Contains(t.Id))
                    .ToListAsync();


            foreach (var type in selectedTypes)
            {
                _context.TestRequestItems
                    .Add(
                        new TestRequestItem
                        {
                            RequestId =
                                request.RequestId,

                            TestTypeId =
                                type.Id,

                            Status =
                                "Submitted"
                        });
            }


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Test request updated successfully!";

            return RedirectToAction(
                nameof(Index));
        }


        // ============================================================
        // POST: /TestRequest/Cancel
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(
            int requestId,
            string cancellationReason)
        {
            var doctor =
                await _userManager
                    .GetUserAsync(User);

            if (doctor == null)
            {
                return Forbid();
            }

            var request =
                await _context.TestRequests
                    .FindAsync(requestId);

            if (request == null)
            {
                return NotFound();
            }


            if (request.RequestingDoctorId !=
                doctor.Id)
            {
                return Forbid();
            }


            if (request.Status != "Submitted" &&
                request.Status != "Samples Received")
            {
                TempData["Error"] =
                    "This request can no longer be cancelled.";

                return RedirectToAction(
                    nameof(Index));
            }


            if (string.IsNullOrWhiteSpace(
                cancellationReason))
            {
                TempData["Error"] =
                    "A cancellation reason is required.";

                return RedirectToAction(
                    nameof(Index));
            }


            request.Status =
                "Cancelled";

            request.CancellationReason =
                cancellationReason.Trim();


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Test request cancelled.";

            return RedirectToAction(
                nameof(Index));
        }


        // ============================================================
        // GET: /TestRequest/Track
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Track()
        {
            var doctor =
                await _userManager
                    .GetUserAsync(User);

            if (doctor == null)
            {
                return Forbid();
            }


            var allRequests =
                await _context.TestRequests
                    .Include(tr => tr.Patient)
                    .Include(tr =>
                        tr.RequestingDoctor)
                    .Include(tr =>
                        tr.TestRequestItems)
                        .ThenInclude(tri =>
                            tri.TestType)
                    .Where(r =>
                        r.RequestingDoctorId ==
                        doctor.Id)
                    .ToListAsync();


            var folders =
                allRequests
                    .GroupBy(tr =>
                        tr.PatientId)
                    .Select(g =>
                        new TrackRequestViewModel
                        {
                            Patient =
                                g.First().Patient,

                            Requests =
                                g.OrderByDescending(
                                    r => r.RequestDate)
                                 .ToList()
                        })
                    .OrderBy(x =>
                        x.Patient.Name)
                    .ToList();


            return View(folders);
        }


        // ============================================================
        // GET: /TestRequest/AllRequests
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AllRequests()
        {
            var testRequests =
                await _context.TestRequests
                    .Include(tr => tr.Patient)
                    .Include(tr =>
                        tr.RequestingDoctor)
                    .Include(tr =>
                        tr.TestRequestItems)
                        .ThenInclude(tri =>
                            tri.TestType)
                    .OrderByDescending(
                        tr => tr.RequestDate)
                    .ToListAsync();


            var viewModel =
                testRequests.Select(
                    tr =>
                        new TestRequestListViewModel
                        {
                            RequestId =
                                tr.RequestId,

                            PatientName =
                                tr.Patient != null
                                    ? $"{tr.Patient.Name} {tr.Patient.Surname}"
                                    : "Unknown",

                            DoctorName =
                                tr.RequestingDoctor != null
                                    ? tr.RequestingDoctor.FullName
                                    : "Unknown",

                            RequestDate =
                                tr.RequestDate,

                            Urgency =
                                tr.Urgency,

                            Status =
                                tr.Status,

                            HasAbnormalResults =
                                false,

                            ResultCount =
                                tr.TestRequestItems?.Count ??
                                0,


                            // ------------------------------------------------
                            // TEST TYPES
                            // ------------------------------------------------

                            TestTypeNames =
                                tr.TestRequestItems?
                                    .Select(tri =>
                                        tri.TestType?.Name ??
                                        "Unknown")
                                    .ToList()
                                ??
                                new List<string>(),


                            TestTypesDisplay =
                                tr.TestRequestItems != null &&
                                tr.TestRequestItems.Any()
                                    ? string.Join(
                                        ", ",
                                        tr.TestRequestItems
                                            .Select(tri =>
                                                tri.TestType?.Name ??
                                                "Unknown"))
                                    : "No tests",


                            // ------------------------------------------------
                            // BARCODES
                            // ------------------------------------------------

                            SampleBarcodes =
                                !string.IsNullOrEmpty(
                                    tr.SampleBarcodes)
                                    ? tr.SampleBarcodes
                                        .Split(',')
                                        .Select(x =>
                                            x.Trim())
                                        .ToList()
                                    : new List<string>(),


                            SampleBarcodesString =
                                tr.SampleBarcodes ??
                                "",


                            // ------------------------------------------------
                            // CANCELLATION
                            // ------------------------------------------------

                            CancellationReason =
                                tr.CancellationReason,


                            // ------------------------------------------------
                            // CLINICAL NOTES
                            // ------------------------------------------------

                            ClinicalNotes =
                                tr.ClinicalNotes
                        });


            return View(viewModel);
        }


        // ============================================================
        // POST: /TestRequest/ReleaseResults
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseResults(
            int requestId,
            string releaseNote)
        {
            var doctor =
                await _userManager
                    .GetUserAsync(User);

            if (doctor == null)
            {
                return Forbid();
            }


            var request =
                await _context.TestRequests
                    .Include(r => r.Patient)
                    .FirstOrDefaultAsync(
                        r => r.RequestId ==
                             requestId);

            if (request == null)
            {
                return NotFound();
            }


            if (request.RequestingDoctorId !=
                doctor.Id)
            {
                return Forbid();
            }


            if (request.Status !=
                "Completed")
            {
                TempData["Error"] =
                    "Only completed results can be released.";

                return RedirectToAction(
                    nameof(Results));
            }


            if (string.IsNullOrWhiteSpace(
                releaseNote))
            {
                TempData["Error"] =
                    "Please enter a release note.";

                return RedirectToAction(
                    nameof(Results));
            }


            request.Status =
                "Released by doctor";

            request.ReleaseNote =
                releaseNote.Trim();

            request.ReleaseDate =
                DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Results released to patient.";

            return RedirectToAction(
                nameof(Track));
        }


        // ============================================================
        // GET: /TestRequest/Results
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Results()
        {
            var doctor =
                await _userManager
                    .GetUserAsync(User);

            if (doctor == null)
            {
                return Forbid();
            }


            var allRequests =
                await _context.TestRequests
                    .Include(tr => tr.Patient)
                    .Include(tr =>
                        tr.TestRequestItems)
                        .ThenInclude(tri =>
                            tri.TestType)
                    .Where(r =>
                        r.RequestingDoctorId ==
                        doctor.Id &&
                        (
                            r.Status ==
                            "Completed" ||

                            r.Status ==
                            "Released by doctor"
                        ))
                    .ToListAsync();


            var folders =
                allRequests
                    .GroupBy(tr =>
                        tr.PatientId)
                    .Select(g =>
                        new
                        {
                            Patient =
                                g.First().Patient,

                            Requests =
                                g.OrderByDescending(
                                    r => r.RequestDate)
                                 .ToList()
                        })
                    .OrderBy(x =>
                        x.Patient.Name)
                    .ToList();


            return View(folders);
        }


        // ============================================================
        // GET: /TestRequest/ViewRequest/5
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ViewRequest(
            int id)
        {
            var request =
                await _context.TestRequests
                    .Include(tr => tr.Patient)
                    .Include(tr =>
                        tr.RequestingDoctor)
                    .Include(tr =>
                        tr.TestRequestItems)
                        .ThenInclude(tri =>
                            tri.TestType)
                    .Include(tr =>
                        tr.Samples)
                    .FirstOrDefaultAsync(
                        tr => tr.RequestId ==
                             id);

            if (request == null)
            {
                return NotFound();
            }


            // Pass as a list because the existing
            // PatientRequests view expects a list
            return View(
                "PatientRequests",
                new List<TestRequest>
                {
                    request
                });
        }
    }
}