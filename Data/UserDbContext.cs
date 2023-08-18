using ControleDeUsuarioDoBalta.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ControleDeUsuarioDoBalta.Data
{
    public class UserDbContext : IdentityDbContext<User>
    {
        public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options)
        {
        }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}