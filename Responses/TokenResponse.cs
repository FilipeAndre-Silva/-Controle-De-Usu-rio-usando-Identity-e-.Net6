using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ControleDeUsuarioDoBalta.Responses
{
    public class TokenResponse
    {
        public object AccessToken { get; set; }
        public Func<string> RefreshToken { get; set; }
        public DateTime AccessTokenExpiration { get; set; }
    }
}