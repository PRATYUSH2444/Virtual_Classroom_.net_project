using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VirtualClassroom2.Models;

namespace VirtualClassroom2.Controllers
{
    [Authorize]
    public class AssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public AssignmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // GET: Assignments
        public async Task<IActionResult> Index(int? classroomId)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserId = user.Id;

            IQueryable<Assignment> assignmentsQuery = _context.Assignments
                .Include(a => a.Classroom)
                .Include(a => a.CreatedBy)
                .Include(a => a.Submissions)
                .OrderByDescending(a => a.CreatedAt);

            if (classroomId.HasValue)
            {
                assignmentsQuery = assignmentsQuery.Where(a => a.ClassroomId == classroomId.Value);

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
                    assignmentsQuery = assignmentsQuery.Where(a => myClassroomIds.Contains(a.ClassroomId));
                }
                else if (user.Role == "Student")
                {
                    var enrolledClassroomIds = await _context.Enrollments
                        .Where(e => e.StudentId == user.Id)
                        .Select(e => e.ClassroomId)
                        .ToListAsync();
                    assignmentsQuery = assignmentsQuery.Where(a => enrolledClassroomIds.Contains(a.ClassroomId));
                }
            }

            var assignments = await assignmentsQuery.ToListAsync();
            ViewBag.ClassroomId = classroomId;
            return View(assignments);
        }

        // GET: Assignments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Classroom)
                .Include(a => a.CreatedBy)
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role == "Student")
            {
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.ClassroomId == assignment.ClassroomId && e.StudentId == user.Id);
                if (!isEnrolled) return Forbid();

                ViewBag.HasSubmitted = await _context.Submissions
                    .AnyAsync(s => s.AssignmentId == id && s.StudentId == user.Id);
            }

            ViewBag.CurrentUserId = user.Id;
            return View(assignment);
        }

        // GET: Assignments/Create
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

        // POST: Assignments/Create - SIMPLIFIED VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create(string Title, string Instructions, int Points, DateTime DueDate, int ClassroomId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Instructions) || ClassroomId == 0)
                {
                    TempData["Error"] = "Title, Instructions, and Classroom are required fields.";
                    return await Create(ClassroomId);
                }

                if (DueDate <= DateTime.Now)
                {
                    TempData["Error"] = "Due date must be in the future.";
                    return await Create(ClassroomId);
                }

                var assignment = new Assignment
                {
                    Title = Title.Trim(),
                    Instructions = Instructions.Trim(),
                    Points = Points,
                    DueDate = DueDate,
                    ClassroomId = ClassroomId,
                    CreatedById = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Assignments.Add(assignment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Assignment created successfully!";
                return RedirectToAction(nameof(Index), new { classroomId = ClassroomId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating assignment: {ex.Message}";
                return await Create(ClassroomId);
            }
        }

        // GET: Assignments/Edit/5
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role == "Teacher" && assignment.CreatedById != user.Id) return Forbid();

            var classrooms = user.Role == "Admin"
                ? await _context.Classrooms.ToListAsync()
                : await _context.Classrooms.Where(c => c.CreatedById == user.Id).ToListAsync();

            ViewData["ClassroomId"] = new SelectList(classrooms, "Id", "Title", assignment.ClassroomId);
            return View(assignment);
        }

        // POST: Assignments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Edit(int id, string Title, string Instructions, int Points, DateTime DueDate, int ClassroomId)
        {
            try
            {
                var existingAssignment = await _context.Assignments.FindAsync(id);
                if (existingAssignment == null) return NotFound();

                var user = await _userManager.GetUserAsync(User);
                if (user.Role == "Teacher" && existingAssignment.CreatedById != user.Id) return Forbid();

                if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Instructions))
                {
                    TempData["Error"] = "Title and Instructions are required fields.";
                    return await Edit(id);
                }

                existingAssignment.Title = Title.Trim();
                existingAssignment.Instructions = Instructions.Trim();
                existingAssignment.Points = Points;
                existingAssignment.DueDate = DueDate;
                existingAssignment.ClassroomId = ClassroomId;

                _context.Assignments.Update(existingAssignment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Assignment updated successfully!";
                return RedirectToAction(nameof(Index), new { classroomId = ClassroomId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating assignment: {ex.Message}";
                return await Edit(id);
            }
        }

        // GET: Assignments/Delete/5
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Classroom)
                .Include(a => a.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role == "Teacher" && assignment.CreatedById != user.Id) return Forbid();

            return View(assignment);
        }

        // POST: Assignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment != null)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user.Role == "Teacher" && assignment.CreatedById != user.Id) return Forbid();

                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Assignment deleted successfully!";
            }
            return RedirectToAction(nameof(Index), new { classroomId = assignment?.ClassroomId });
        }

        // GET: Submit Assignment
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Classroom)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.ClassroomId == assignment.ClassroomId && e.StudentId == user.Id);

            if (!isEnrolled) return Forbid();

            var existingSubmission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.AssignmentId == id && s.StudentId == user.Id);

            ViewBag.Assignment = assignment;
            return View(existingSubmission);
        }

        // POST: Submit Assignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit(int assignmentId, IFormFile submissionFile)
        {
            var assignment = await _context.Assignments.FindAsync(assignmentId);
            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.ClassroomId == assignment.ClassroomId && e.StudentId == user.Id);

            if (!isEnrolled) return Forbid();

            if (submissionFile == null || submissionFile.Length == 0)
            {
                TempData["Error"] = "Please select a file to submit.";
                ViewBag.Assignment = assignment;
                return View();
            }

            if (submissionFile.Length > 104857600)
            {
                TempData["Error"] = "File size must be less than 100MB.";
                ViewBag.Assignment = assignment;
                return View();
            }

            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "submissions");
                if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(submissionFile.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await submissionFile.CopyToAsync(stream);
                }

                var existingSubmission = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == user.Id);

                if (existingSubmission != null)
                {
                    // Update existing submission
                    var oldFilePath = Path.Combine(_environment.WebRootPath, existingSubmission.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);

                    existingSubmission.FilePath = $"/uploads/submissions/{fileName}";
                    existingSubmission.SubmittedAt = DateTime.Now;
                    existingSubmission.Marks = null; // Reset grading since it's a new submission
                    existingSubmission.Feedback = null;

                    _context.Submissions.Update(existingSubmission);
                    TempData["Success"] = "Submission updated successfully!";
                }
                else
                {
                    // Create new submission
                    var submission = new Submission
                    {
                        AssignmentId = assignmentId,
                        StudentId = user.Id,
                        FilePath = $"/uploads/submissions/{fileName}",
                        SubmittedAt = DateTime.Now
                    };
                    _context.Submissions.Add(submission);
                    TempData["Success"] = "Assignment submitted successfully!";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = assignmentId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error submitting assignment: {ex.Message}";
                ViewBag.Assignment = assignment;
                return View();
            }
        }

        // Download submission file
        public async Task<IActionResult> DownloadSubmission(int id)
        {
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // Student can download their own submission, teacher can download any submission from their class
            if (user.Role == "Student" && submission.StudentId != user.Id) return Forbid();
            if (user.Role == "Teacher")
            {
                var assignment = await _context.Assignments.FindAsync(submission.AssignmentId);
                var classroom = await _context.Classrooms.FindAsync(assignment.ClassroomId);
                if (classroom.CreatedById != user.Id) return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, submission.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", $"Submission_{submission.Student.FirstName}_{submission.Assignment.Title}{Path.GetExtension(submission.FilePath)}");
        }

        // Grade Submission
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(int submissionId, decimal marks, string feedback)
        {
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role == "Teacher")
            {
                var assignment = await _context.Assignments.FindAsync(submission.AssignmentId);
                var classroom = await _context.Classrooms.FindAsync(assignment.ClassroomId);
                if (classroom.CreatedById != user.Id) return Forbid();
            }

            submission.Marks = marks;
            submission.Feedback = feedback;

            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Submission graded successfully!";
            return RedirectToAction(nameof(Details), new { id = submission.AssignmentId });
        }

        private bool AssignmentExists(int id)
        {
            return _context.Assignments.Any(e => e.Id == id);
        }
    }
}