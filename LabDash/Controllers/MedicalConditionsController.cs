
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabDash.Models;
using LabDash.Areas.Identity.Data;

public class MedicalConditionsController : Controller
{
    private readonly LabDbContext _context;

    public MedicalConditionsController(LabDbContext context)
    {
        _context = context;
    }

    // GET: MEDICALCONDITIONS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.MedicalConditions.ToListAsync());
    }

    // GET: MEDICALCONDITIONS/Details/5
    public async Task<IActionResult> Details(int? medicalconditionid)
    {
        if (medicalconditionid == null)
        {
            return NotFound();
        }

        var medicalcondition = await _context.MedicalConditions
            .FirstOrDefaultAsync(m => m.MedicalConditionId == medicalconditionid);
        if (medicalcondition == null)
        {
            return NotFound();
        }

        return View(medicalcondition);
    }

    // GET: MEDICALCONDITIONS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: MEDICALCONDITIONS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MedicalConditionId,ConditionName,CategoryId,Category,Description,IsActive")] MedicalCondition medicalcondition)
    {
        if (ModelState.IsValid)
        {
            _context.Add(medicalcondition);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(medicalcondition);
    }

    // GET: MEDICALCONDITIONS/Edit/5
    public async Task<IActionResult> Edit(int? medicalconditionid)
    {
        if (medicalconditionid == null)
        {
            return NotFound();
        }

        var medicalcondition = await _context.MedicalConditions.FindAsync(medicalconditionid);
        if (medicalcondition == null)
        {
            return NotFound();
        }
        return View(medicalcondition);
    }

    // POST: MEDICALCONDITIONS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? medicalconditionid, [Bind("MedicalConditionId,ConditionName,CategoryId,Category,Description,IsActive")] MedicalCondition medicalcondition)
    {
        if (medicalconditionid != medicalcondition.MedicalConditionId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(medicalcondition);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MedicalConditionExists(medicalcondition.MedicalConditionId))
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
        return View(medicalcondition);
    }

    // GET: MEDICALCONDITIONS/Delete/5
    public async Task<IActionResult> Delete(int? medicalconditionid)
    {
        if (medicalconditionid == null)
        {
            return NotFound();
        }

        var medicalcondition = await _context.MedicalConditions
            .FirstOrDefaultAsync(m => m.MedicalConditionId == medicalconditionid);
        if (medicalcondition == null)
        {
            return NotFound();
        }

        return View(medicalcondition);
    }

    // POST: MEDICALCONDITIONS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? medicalconditionid)
    {
        var medicalcondition = await _context.MedicalConditions.FindAsync(medicalconditionid);
        if (medicalcondition != null)
        {
            _context.MedicalConditions.Remove(medicalcondition);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool MedicalConditionExists(int? medicalconditionid)
    {
        return _context.MedicalConditions.Any(e => e.MedicalConditionId == medicalconditionid);
    }
}
