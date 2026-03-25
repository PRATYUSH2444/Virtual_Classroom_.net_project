using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VirtualClassroom2.Models
{
    public class Assignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Instructions { get; set; } = string.Empty;

        public int Points { get; set; } = 100;

        public DateTime DueDate { get; set; }

        [Required]
        public int ClassroomId { get; set; }

        [ForeignKey("ClassroomId")]
        public virtual Classroom Classroom { get; set; } = null!;

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}