
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabDash.Models;
using LabDash.Areas.Identity.Data;

public class TestRequestsController : Controller
{
    private readonly LabDbContext _context;

    public TestRequestsController(LabDbContext context)
    {
        _context = context;
    }

    // GET: TESTREQUESTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.TestRequests.ToListAsync());
    }

    // GET: TESTREQUESTS/Details/5
    public async Task<IActionResult> Details(int? requestid)
    {
        if (requestid == null)
        {
            return NotFound();
        }

        var testrequest = await _context.TestRequests
            .FirstOrDefaultAsync(m => m.RequestId == requestid);
        if (testrequest == null)
        {
            return NotFound();
        }

        return View(testrequest);
    }

    // GET: TESTREQUESTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TESTREQUESTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("RequestId,PatientId,Patient,RequestingDoctorId,RequestingDoctor,RequestDate,Urgency,ClinicalNotes,Status,DateTimeReceived,SubmittedDate,CancellationReason,Samples,SampleReceives,TestRequestItems")] TestRequest testrequest)
    {
        if (ModelState.IsValid)
        {
            _context.Add(testrequest);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(testrequest);
    }

    // GET: TESTREQUESTS/Edit/5
    public async Task<IActionResult> Edit(int? requestid)
    {
        if (requestid == null)
        {
            return NotFound();
        }

        var testrequest = await _context.TestRequests.FindAsync(requestid);
        if (testrequest == null)
        {
            return NotFound();
        }
        return View(testrequest);
    }

    // POST: TESTREQUESTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? requestid, [Bind("RequestId,PatientId,Patient,RequestingDoctorId,RequestingDoctor,RequestDate,Urgency,ClinicalNotes,Status,DateTimeReceived,SubmittedDate,CancellationReason,Samples,SampleReceives,TestRequestItems")] TestRequest testrequest)
    {
        if (requestid != testrequest.RequestId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(testrequest);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TestRequestExists(testrequest.RequestId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(testrequest);
    }

    // GET: TESTREQUESTS/Delete/5
    public async Task<IActionResult> Delete(int? requestid)
    {
        if (requestid == null)
        {
            return NotFound();
        }

        var testrequest = await _context.TestRequests
            .FirstOrDefaultAsync(m => m.RequestId == requestid);
        if (testrequest == null)
        {
            return NotFound();
        }

        return View(testrequest);
    }

    // POST: TESTREQUESTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? requestid)
    {
        var testrequest = await _context.TestRequests.FindAsync(requestid);
        if (testrequest != null)
        {
            _context.TestRequests.Remove(testrequest);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool TestRequestExists(int? requestid)
    {
        return _context.TestRequests.Any(e => e.RequestId == requestid);
    }
}
