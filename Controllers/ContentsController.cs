using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VirtualClassroom2.Models;

namespace VirtualClassroom2.Controllers
{
    [Authorize]
    public class ContentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ContentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // GET: Contents
        public async Task<IActionResult> Index(int? classroomId)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserId = user.Id;

            IQueryable<Content> contentsQuery = _context.Contents
                .Include(c => c.Classroom)
                .Include(c => c.UploadedBy);

            if (classroomId.HasValue)
            {
                contentsQuery = contentsQuery.Where(c => c.ClassroomId == classroomId.Value);

                if (user.Role == "Student")
                {
                    var isEnrolled = await _context.Enrollments
                        .AnyAsync(e => e.ClassroomId == classroomId.Value && e.StudentId == user.Id);
                    if (!isEnrolled) return Forbid();
                }
            }
            else
            {
                if (user.Role == "Teacher")
                {
                    var myClassroomIds = await _context.Classrooms
                        .Where(c => c.CreatedById == user.Id)
                        .Select(c => c.Id)
                        .ToListAsync();
                    contentsQuery = contentsQuery.Where(c => myClassroomIds.Contains(c.ClassroomId));
                }
                else if (user.Role == "Student")
                {
                    var enrolledClassroomIds = await _context.Enrollments
                        .Where(e => e.StudentId == user.Id)
                        .Select(e => e.ClassroomId)
                        .ToListAsync();
                    contentsQuery = contentsQuery.Where(c => enrolledClassroomIds.Contains(c.ClassroomId));
                }
            }

            var contents = await contentsQuery.ToListAsync();
            ViewBag.ClassroomId = classroomId;
            return View(contents);
        }

        // GET: Contents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var content = await _context.Contents
                .Include(c => c.Classroom)
                .Include(c => c.UploadedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (content == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role == "Student")
            {
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.ClassroomId == content.ClassroomId && e.StudentId == user.Id);
                if (!isEnrolled) return Forbid();
            }

            return View(content);
        }

        // GET: Contents/Create
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create(int? classroomId)
        {
            var user = await _userManager.GetUserAsync(User);
            var classrooms = user.Role == "Admin"
                ? await _context.Classrooms.ToListAsync()
                : await _context.Classrooms.Where(c => c.CreatedById == user.Id).ToListAsync();

            ViewData["ClassroomId"] = new SelectList(classrooms, "Id", "Title", classroomId);
            return View();
        }

        // POST: Contents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create(Content content, IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to upload.");
            }
            else
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "content");
                if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                content.FilePath = $"/uploads/content/{fileName}";
                content.FileType = Path.GetExtension(file.FileName).ToLower().TrimStart('.');
                content.FileSize = file.Length;
                content.UploadedById = user.Id;
                content.UploadedAt = DateTime.Now;

                _context.Add(content);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Content uploaded successfully!";
                return RedirectToAction(nameof(Index), new { classroomId = content.ClassroomId });
            }

            var classrooms = user.Role == "Admin"
                ? await _context.Classrooms.ToListAsync()
                : await _context.Classrooms.Where(c => c.CreatedById == user.Id).ToListAsync();

            ViewData["ClassroomId"] = new SelectList(classrooms, "Id", "Title", content.ClassroomId);
            return View(content);
        }

        // GET: Contents/Delete/5
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var content = await _context.Contents
                .Include(c => c.Classroom)
                .Include(c => c.UploadedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (content == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role == "Teacher" && content.UploadedById != user.Id) return Forbid();

            return View(content);
        }

        // POST: Contents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var content = await _context.Contents.FindAsync(id);
            if (content != null)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user.Role == "Teacher" && content.UploadedById != user.Id) return Forbid();

                var filePath = Path.Combine(_environment.WebRootPath, content.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

                _context.Contents.Remove(content);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Content deleted successfully!";
            }
            return RedirectToAction(nameof(Index), new { classroomId = content?.ClassroomId });
        }

        // Download file
        public async Task<IActionResult> Download(int id)
        {
            var content = await _context.Contents.FindAsync(id);
            if (content == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role == "Student")
            {
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.ClassroomId == content.ClassroomId && e.StudentId == user.Id);
                if (!isEnrolled) return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, content.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", content.Title + Path.GetExtension(content.FilePath));
        }

        private bool ContentExists(int id)
        {
            return _context.Contents.Any(e => e.Id == id);
        }
    }
}