using ControleDeUsuarioDoBalta.Data;
using ControleDeUsuarioDoBalta.Models;
using ControleDeUsuarioDoBalta.Requests;
using ControleDeUsuarioDoBalta.Responses;
using ControleDeUsuarioDoBalta.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleDeUsuarioDoBalta.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserDbContext _userDbContext;

    public UserController(ILogger<UserController> logger, UserManager<User> userManager, SignInManager<User> signInManager,
    RoleManager<IdentityRole> roleManager, UserDbContext userDbContext)
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _userDbContext = userDbContext;
    }

    [HttpPost]
    [Route("create")]
    public async Task<ActionResult<dynamic>> Create([FromBody]CreateUserRequest model)
    {
        var role = _roleManager.FindByNameAsync(model.Role).Result;
        if (role == null) return BadRequest("Role não existe");

        var newUser = new User
            {
                UserName = model.UserName,
                Email = model.Email,
            };
        
        var result = await _userManager.CreateAsync(newUser, model.Password);

        if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, role.Name);
                return Ok(result);
            }
            else
            {
                return BadRequest(result.Errors);
            }
    }

    [HttpGet]
    [Route("GetAll")]
    [AllowAnonymous]
    public async Task<ActionResult<dynamic>> GetAllAsync()
    {
        var allUsers = await _userManager.Users.ToListAsync();
        return Ok(allUsers);
    }

    [HttpGet]
    [Route("GetUserById")]
    [AllowAnonymous]
    public async Task<ActionResult<dynamic>> GetUserByIdAsync([FromForm]string id)
    {
        var user = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();
        if(user == null) return NoContent();
        return Ok(user);
    }

    [HttpDelete]
    [Route("DeletedUserById")]
    [Authorize(Roles = "manager")]
    
    public async Task<ActionResult<dynamic>> DeletedUserByIdAsync([FromForm]string id)
    {
        
        var user = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();
        if(user == null) return NoContent();

        await _userManager.DeleteAsync(user);

        return Ok();
    }

    [HttpPatch("AlterName/{id}")]
    [Authorize]
    public async Task<ActionResult<dynamic>> AlterName([FromBody]string userName, [FromRoute]string id)
    {
        
        var user = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();
        if(user == null) return NoContent();

        user.UserName = userName;
        await _userManager.UpdateAsync(user);

        return Ok();
    }
    
    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<dynamic>> Authenticate([FromBody]LoginUser model)
    {
        // Recupera o usuário
        var user = await _userManager.FindByEmailAsync(model.Email);

        // Verifica se o usuário existe
        if (user == null)
            return NotFound(new { message = "Usuário ou senha inválidos" });

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

                if (!signInResult.Succeeded)
                {
                    // O usuário está autenticado com sucesso
                    return NoContent();
                }
        
        var roles = await _userManager.GetRolesAsync(user);

        // Gera o Token
        var token = TokenService.GenerateToken(user, roles.First());

        var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                Expires = DateTime.UtcNow.AddDays(7) // Defina a expiração do token de atualização
            };

            _userDbContext.RefreshTokens.Add(refreshToken);
            await _userDbContext.SaveChangesAsync();

        var novoTokenDeAtualizacao = refreshToken.Token;

        return new
        {
            AccessToken = token,
            RefreshToken = novoTokenDeAtualizacao
        };
    }

    [HttpPost("ForgotPassword")]
    [Authorize]
    public async Task<IActionResult> ForgotPassword()
    {
        var userEmailClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

        var user = await _userManager.FindByEmailAsync(userEmailClaim.Value);
        // if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        // {
        //     // Não revelar se o usuário não existe ou não está confirmado
        //     return Ok("ForgotPasswordConfirmation");
        // }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, token }, protocol: HttpContext.Request.Scheme);

        // await _emailService.SendEmailAsync(user.Email, "Redefinição de Senha",
        //     $"Por favor, redefina sua senha clicando <a href='{callbackUrl}'>aqui</a>.");

        return Ok(token);
    }

    [HttpPost("ResetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody]ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);

        if (user == null)
        {
            // Usuário não encontrado
            return RedirectToAction("Error", "Home");
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (result.Succeeded)
        {
            // Redirecionar para uma página de sucesso
            return Ok("PasswordResetSuccess");
        }
        else
        {
            // Lidar com erros de reset de senha, talvez redirecionar para uma página de erro
            return BadRequest();
        }
    }

    [HttpGet]
    [Route("anonymous")]
    [AllowAnonymous]
    public string Anonymous() => "Anônimo";

    [HttpGet]
    [Route("authenticated")]
    [Authorize]
    public string Authenticated() => String.Format("Autenticado - {0}", User.Identity.Name);

    [HttpGet]
    [Route("employee")]
    [Authorize(Roles = "employee,manager")]
    public string Employee() => "Funcionário";

    [HttpGet]
    [Route("manager")]
    [Authorize(Roles = "manager")]
    public string Manager() => "Gerente";

    [HttpPost]
    [Route("RefreshToken")]
    public async Task<ActionResult<dynamic>> AtualizarTokenAsync([FromBody]string tokenDeAtualizacao)
    {
        var tokenExistente = await _userDbContext.RefreshTokens.FindAsync(tokenDeAtualizacao);

        if (tokenExistente == null || tokenExistente.Expires < DateTime.UtcNow)
        {
            return Unauthorized(); // Tratar tokens de atualização inválidos ou expirados
        }

        var user = await _userManager.FindByIdAsync(tokenExistente.UserId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Gerar um novo token de acesso
        var novoTokenDeAcesso = TokenService.GenerateToken(user, roles.First());

        // Gerar um novo token de atualização (opcional)
        var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                Expires = DateTime.UtcNow.AddDays(7) // Defina a expiração do token de atualização
            };

            _userDbContext.RefreshTokens.Add(refreshToken);
            await _userDbContext.SaveChangesAsync();

        var novoTokenDeAtualizacao = refreshToken.Token;

        return new
        {
            AccessToken = novoTokenDeAcesso,
            RefreshToken = novoTokenDeAtualizacao
        };
    }
}