using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ControleDeUsuarioDoBalta.Requests
{
    public class RefreshTokenRequest
    {
        public object AccessToken { get; internal set; }
        public object RefreshToken { get; internal set; }
    }
}