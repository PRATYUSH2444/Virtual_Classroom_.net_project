using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VirtualClassroom2.Models
{
    public class Enrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClassroomId { get; set; }

        [ForeignKey("ClassroomId")]
        public virtual Classroom Classroom { get; set; }

        [Required]
        public string StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.Now;
    }
}