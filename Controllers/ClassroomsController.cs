using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualClassroom2.Models;

namespace VirtualClassroom2.Controllers
{
    [Authorize]
    public class ClassroomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClassroomsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Classrooms
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserId = user.Id;

            if (user.Role == "Teacher")
            {
                var myClassrooms = await _context.Classrooms
                    .Where(c => c.CreatedById == user.Id)
                    .Include(c => c.Enrollments)
                    .Include(c => c.CreatedBy)
                    .ToListAsync();
                return View(myClassrooms);
            }
            else if (user.Role == "Student")
            {
                var enrolledClassrooms = await _context.Enrollments
                    .Where(e => e.StudentId == user.Id)
                    .Include(e => e.Classroom)
                    .ThenInclude(c => c.CreatedBy)
                    .Select(e => e.Classroom)
                    .ToListAsync();
                return View(enrolledClassrooms);
            }
            else
            {
                var allClassrooms = await _context.Classrooms
                    .Include(c => c.CreatedBy)
                    .Include(c => c.Enrollments)
                    .ToListAsync();
                return View(allClassrooms);
            }
        }

        // GET: Browse Classrooms
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Browse()
        {
            var user = await _userManager.GetUserAsync(User);

            var enrolledClassroomIds = await _context.Enrollments
                .Where(e => e.StudentId == user.Id)
                .Select(e => e.ClassroomId)
                .ToListAsync();

            var availableClassrooms = await _context.Classrooms
                .Where(c => !enrolledClassroomIds.Contains(c.Id))
                .Include(c => c.CreatedBy)
                .Include(c => c.Enrollments)
                .ToListAsync();

            return View(availableClassrooms);
        }

        // POST: Join Classroom
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int classroomId, string enrolmentKey)
        {
            var user = await _userManager.GetUserAsync(User);
            var classroom = await _context.Classrooms.FindAsync(classroomId);

            if (classroom == null)
            {
                return NotFound();
            }

            if (classroom.EnrolmentKey != enrolmentKey)
            {
                TempData["Error"] = "Invalid enrolment key!";
                return RedirectToAction(nameof(Browse));
            }

            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.ClassroomId == classroomId && e.StudentId == user.Id);

            if (existingEnrollment != null)
            {
                TempData["Error"] = "You are already enrolled in this classroom!";
                return RedirectToAction(nameof(Browse));
            }

            var enrollment = new Enrollment
            {
                ClassroomId = classroomId,
                StudentId = user.Id,
                EnrolledAt = DateTime.Now
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Successfully joined the classroom!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Classrooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var classroom = await _context.Classrooms
                .Include(c => c.CreatedBy)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .Include(c => c.Contents)
                .Include(c => c.Assignments)
                .Include(c => c.Announcements)
                    .ThenInclude(a => a.PostedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role == "Student")
            {
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.ClassroomId == id && e.StudentId == user.Id);
                if (!isEnrolled) return Forbid();
            }

            return View(classroom);
        }

        // GET: Classrooms/Create
        [Authorize(Roles = "Teacher")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Classrooms/Create - WORKING VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create(string Title, string Subject, string EnrolmentKey, string Description = "")
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Basic validation
                if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(EnrolmentKey))
                {
                    TempData["Error"] = "Title, Subject, and Enrolment Key are required fields.";
                    return View();
                }

                var classroom = new Classroom
                {
                    Title = Title.Trim(),
                    Subject = Subject.Trim(),
                    EnrolmentKey = EnrolmentKey.Trim(),
                    Description = Description?.Trim() ?? "",
                    CreatedById = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Classrooms.Add(classroom);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Classroom '{Title}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating classroom: {ex.Message}";
                return View();
            }
        }

        // GET: Classrooms/Edit/5
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var classroom = await _context.Classrooms.FindAsync(id);
            if (classroom == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (classroom.CreatedById != user.Id && user.Role != "Admin")
            {
                return Forbid();
            }

            return View(classroom);
        }

        // POST: Classrooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Edit(int id, string Title, string Subject, string EnrolmentKey, string Description = "")
        {
            try
            {
                var existingClassroom = await _context.Classrooms.FindAsync(id);
                if (existingClassroom == null) return NotFound();

                var user = await _userManager.GetUserAsync(User);
                if (existingClassroom.CreatedById != user.Id && user.Role != "Admin")
                {
                    return Forbid();
                }

                // Basic validation
                if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(EnrolmentKey))
                {
                    TempData["Error"] = "Title, Subject, and Enrolment Key are required fields.";
                    return View(existingClassroom);
                }

                existingClassroom.Title = Title.Trim();
                existingClassroom.Subject = Subject.Trim();
                existingClassroom.EnrolmentKey = EnrolmentKey.Trim();
                existingClassroom.Description = Description?.Trim() ?? "";

                _context.Classrooms.Update(existingClassroom);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Classroom updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating classroom: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // GET: Classrooms/Delete/5
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var classroom = await _context.Classrooms
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classroom == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (classroom.CreatedById != user.Id && user.Role != "Admin")
            {
                return Forbid();
            }

            return View(classroom);
        }

        // POST: Classrooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classroom = await _context.Classrooms.FindAsync(id);
            if (classroom != null)
            {
                var user = await _userManager.GetUserAsync(User);
                if (classroom.CreatedById != user.Id && user.Role != "Admin")
                {
                    return Forbid();
                }

                _context.Classrooms.Remove(classroom);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Classroom deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // Manage Enrollments
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ManageEnrollments(int id)
        {
            var classroom = await _context.Classrooms
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (classroom == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (classroom.CreatedById != user.Id) return Forbid();

            return View(classroom);
        }

        // Remove Enrollment
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEnrollment(int enrollmentId)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Classroom)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (enrollment.Classroom.CreatedById != user.Id) return Forbid();

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Student removed from classroom!";
            return RedirectToAction(nameof(ManageEnrollments), new { id = enrollment.ClassroomId });
        }

        private bool ClassroomExists(int id)
        {
            return _context.Classrooms.Any(e => e.Id == id);
        }
    }
}