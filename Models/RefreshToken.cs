using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ControleDeUsuarioDoBalta.Models
{
    public class RefreshToken
    {
        [Key]
    public string Token { get; set; }
    public string UserId { get; set; }
    public DateTime Expires { get; set; }
    }
}