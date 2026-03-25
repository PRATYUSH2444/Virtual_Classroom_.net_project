using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualClassroom2.Models;
using System.Diagnostics;

namespace VirtualClassroom2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.UserRole = user.Role;

                    if (user.Role == "Teacher")
                    {
                        var myClassrooms = await _context.Classrooms
                            .Where(c => c.CreatedById == user.Id)
                            .Include(c => c.Enrollments)
                            .Include(c => c.CreatedBy) // ADD THIS LINE
                            .ToListAsync();
                        return View("TeacherDashboard", myClassrooms);
                    }
                    else if (user.Role == "Student")
                    {
                        var enrolledClassrooms = await _context.Enrollments
                            .Where(e => e.StudentId == user.Id)
                            .Include(e => e.Classroom)
                                .ThenInclude(c => c.CreatedBy) // ADD THIS LINE
                            .Select(e => e.Classroom)
                            .ToListAsync();
                        return View("StudentDashboard", enrolledClassrooms);
                    }
                    else if (user.Role == "Admin")
                    {
                        var stats = new
                        {
                            TotalUsers = await _context.Users.CountAsync(),
                            TotalClassrooms = await _context.Classrooms.CountAsync(),
                            TotalTeachers = await _context.Users.CountAsync(u => u.Role == "Teacher"),
                            TotalStudents = await _context.Users.CountAsync(u => u.Role == "Student")
                        };
                        return View("AdminDashboard", stats);
                    }
                }
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}