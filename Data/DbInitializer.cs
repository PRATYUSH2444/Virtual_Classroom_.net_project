using Microsoft.AspNetCore.Identity;
using VirtualClassroom2.Models;

namespace VirtualClassroom2.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Create roles
            string[] roleNames = { "Admin", "Teacher", "Student" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create Admin user
            var adminUser = new ApplicationUser
            {
                UserName = "admin@virtualclassroom.com",
                Email = "admin@virtualclassroom.com",
                FirstName = "System",
                LastName = "Admin",
                Role = "Admin",
                EmailConfirmed = true
            };

            var adminExists = await userManager.FindByEmailAsync(adminUser.Email);
            if (adminExists == null)
            {
                var createAdmin = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createAdmin.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create sample teacher
            var teacherUser = new ApplicationUser
            {
                UserName = "teacher@virtualclassroom.com",
                Email = "teacher@virtualclassroom.com",
                FirstName = "John",
                LastName = "Smith",
                Role = "Teacher",
                EmailConfirmed = true
            };

            var teacherExists = await userManager.FindByEmailAsync(teacherUser.Email);
            if (teacherExists == null)
            {
                var createTeacher = await userManager.CreateAsync(teacherUser, "Teacher123!");
                if (createTeacher.Succeeded)
                {
                    await userManager.AddToRoleAsync(teacherUser, "Teacher");
                }
            }

            // Create sample student
            var studentUser = new ApplicationUser
            {
                UserName = "student@virtualclassroom.com",
                Email = "student@virtualclassroom.com",
                FirstName = "Alice",
                LastName = "Johnson",
                Role = "Student",
                EmailConfirmed = true
            };

            var studentExists = await userManager.FindByEmailAsync(studentUser.Email);
            if (studentExists == null)
            {
                var createStudent = await userManager.CreateAsync(studentUser, "Student123!");
                if (createStudent.Succeeded)
                {
                    await userManager.AddToRoleAsync(studentUser, "Student");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}