using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace rustles.Infrastructure.Model
{
    public class UserAgeSelfAttestation
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        [Required]
        public DateTime TimeStamp { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public User User { get; set; } = null!;
    }
}
