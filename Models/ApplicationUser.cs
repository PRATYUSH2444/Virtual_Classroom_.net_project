using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace VirtualClassroom2.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Classroom> ClassroomsCreated { get; set; } = new List<Classroom>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Content> ContentsUploaded { get; set; } = new List<Content>();
        public virtual ICollection<Assignment> AssignmentsCreated { get; set; } = new List<Assignment>();
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public virtual ICollection<Announcement> AnnouncementsPosted { get; set; } = new List<Announcement>();
    }
}