using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rustles.Infrastructure.Model
{
    public class UserCookiePolicyConsent
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public DateTime TimeStamp { get; set; }
        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
