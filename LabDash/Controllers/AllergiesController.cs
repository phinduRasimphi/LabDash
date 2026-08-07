
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabDash.Models;
using LabDash.Areas.Identity.Data;

public class AllergiesController : Controller
{
    private readonly LabDbContext _context;

    public AllergiesController(LabDbContext context)
    {
        _context = context;
    }

    // GET: ALLERGYS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Allergies.ToListAsync());
    }

    // GET: ALLERGYS/Details/5
    public async Task<IActionResult> Details(int? allergyid)
    {
        if (allergyid == null)
        {
            return NotFound();
        }

        var allergy = await _context.Allergies
            .FirstOrDefaultAsync(m => m.AllergyId == allergyid);
        if (allergy == null)
        {
            return NotFound();
        }

        return View(allergy);
    }

    // GET: ALLERGYS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ALLERGYS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AllergyId,AllergyName,Category,Description,IsActive")] Allergy allergy)
    {
        if (ModelState.IsValid)
        {
            _context.Add(allergy);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(allergy);
    }

    // GET: ALLERGYS/Edit/5
    public async Task<IActionResult> Edit(int? allergyid)
    {
        if (allergyid == null)
        {
            return NotFound();
        }

        var allergy = await _context.Allergies.FindAsync(allergyid);
        if (allergy == null)
        {
            return NotFound();
        }
        return View(allergy);
    }

    // POST: ALLERGYS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? allergyid, [Bind("AllergyId,AllergyName,Category,Description,IsActive")] Allergy allergy)
    {
        if (allergyid != allergy.AllergyId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(allergy);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AllergyExists(allergy.AllergyId))
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
        return View(allergy);
    }

    // GET: ALLERGYS/Delete/5
    public async Task<IActionResult> Delete(int? allergyid)
    {
        if (allergyid == null)
        {
            return NotFound();
        }

        var allergy = await _context.Allergies
            .FirstOrDefaultAsync(m => m.AllergyId == allergyid);
        if (allergy == null)
        {
            return NotFound();
        }

        return View(allergy);
    }

    // POST: ALLERGYS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? allergyid)
    {
        var allergy = await _context.Allergies.FindAsync(allergyid);
        if (allergy != null)
        {
            _context.Allergies.Remove(allergy);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AllergyExists(int? allergyid)
    {
        return _context.Allergies.Any(e => e.AllergyId == allergyid);
    }
}
