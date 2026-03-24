using DoAnWebDemo.Data;
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnWebDemo.Controllers
{
    //Đăng nhập mới được xem
    [Authorize]
    public class LoanPackagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoanPackagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LoanPackages
        public async Task<IActionResult> Index()
        {
            return View(await _context.LoanPackages.ToListAsync());
        }

        // GET: LoanPackages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loanPackage = await _context.LoanPackages
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanPackage == null)
            {
                return NotFound();
            }

            return View(loanPackage);
        }

        // GET: LoanPackages/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: LoanPackages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,PackageName,InterestRate,MinAmount,MaxAmount,DefaultTermMonths")] LoanPackage loanPackage)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loanPackage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loanPackage);
        }

        // GET: LoanPackages/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loanPackage = await _context.LoanPackages.FindAsync(id);
            if (loanPackage == null)
            {
                return NotFound();
            }
            return View(loanPackage);
        }

        // POST: LoanPackages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PackageName,InterestRate,MinAmount,MaxAmount,DefaultTermMonths")] LoanPackage loanPackage)
        {
            if (id != loanPackage.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loanPackage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanPackageExists(loanPackage.Id))
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
            return View(loanPackage);
        }

        // GET: LoanPackages/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loanPackage = await _context.LoanPackages
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanPackage == null)
            {
                return NotFound();
            }

            return View(loanPackage);
        }

        // POST: LoanPackages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loanPackage = await _context.LoanPackages.FindAsync(id);
            if (loanPackage != null)
            {
                _context.LoanPackages.Remove(loanPackage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LoanPackageExists(int id)
        {
            return _context.LoanPackages.Any(e => e.Id == id);
        }
    }
}
