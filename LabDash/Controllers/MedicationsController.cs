
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabDash.Models;
using LabDash.Areas.Identity.Data;

public class MedicationsController : Controller
{
    private readonly LabDbContext _context;

    public MedicationsController(LabDbContext context)
    {
        _context = context;
    }

    // GET: MEDICATIONS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Medications.ToListAsync());
    }

    // GET: MEDICATIONS/Details/5
    public async Task<IActionResult> Details(int? medicationid)
    {
        if (medicationid == null)
        {
            return NotFound();
        }

        var medication = await _context.Medications
            .FirstOrDefaultAsync(m => m.MedicationId == medicationid);
        if (medication == null)
        {
            return NotFound();
        }

        return View(medication);
    }

    // GET: MEDICATIONS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: MEDICATIONS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MedicationId,MedicationName,Category,Description,IsActive")] Medication medication)
    {
        if (ModelState.IsValid)
        {
            _context.Add(medication);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(medication);
    }

    // GET: MEDICATIONS/Edit/5
    public async Task<IActionResult> Edit(int? medicationid)
    {
        if (medicationid == null)
        {
            return NotFound();
        }

        var medication = await _context.Medications.FindAsync(medicationid);
        if (medication == null)
        {
            return NotFound();
        }
        return View(medication);
    }

    // POST: MEDICATIONS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? medicationid, [Bind("MedicationId,MedicationName,Category,Description,IsActive")] Medication medication)
    {
        if (medicationid != medication.MedicationId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(medication);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MedicationExists(medication.MedicationId))
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
        return View(medication);
    }

    // GET: MEDICATIONS/Delete/5
    public async Task<IActionResult> Delete(int? medicationid)
    {
        if (medicationid == null)
        {
            return NotFound();
        }

        var medication = await _context.Medications
            .FirstOrDefaultAsync(m => m.MedicationId == medicationid);
        if (medication == null)
        {
            return NotFound();
        }

        return View(medication);
    }

    // POST: MEDICATIONS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? medicationid)
    {
        var medication = await _context.Medications.FindAsync(medicationid);
        if (medication != null)
        {
            _context.Medications.Remove(medication);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool MedicationExists(int? medicationid)
    {
        return _context.Medications.Any(e => e.MedicationId == medicationid);
    }
}
