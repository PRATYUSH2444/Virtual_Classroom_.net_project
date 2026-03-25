using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VirtualClassroom2.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public int ClassroomId { get; set; }

        [ForeignKey("ClassroomId")]
        public virtual Classroom Classroom { get; set; } = null!;

        [Required]
        public string PostedById { get; set; } = string.Empty;

        [ForeignKey("PostedById")]
        public virtual ApplicationUser PostedBy { get; set; } = null!;

        public DateTime PostedAt { get; set; } = DateTime.Now;
    }
}