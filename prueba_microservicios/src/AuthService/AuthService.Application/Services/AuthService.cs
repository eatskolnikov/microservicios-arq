using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Application.DTOs;
using AuthService.Domain.Interfaces;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);
        
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        var token = await GenerateTokenAsync(user);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            Username = user.Username,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // Validate email uniqueness
        if (await _userRepository.ExistsByEmailAsync(registerDto.Email))
            throw new InvalidOperationException("Email already exists");

        // Validate username uniqueness
        if (await _userRepository.ExistsByUsernameAsync(registerDto.Username))
            throw new InvalidOperationException("Username already exists");

        // Validate role
        if (registerDto.Role != "Admin" && registerDto.Role != "User")
            registerDto.Role = "User";

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

        var user = new Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = passwordHash,
            Role = registerDto.Role
        };

        await _userRepository.CreateAsync(user);

        var token = await GenerateTokenAsync(user);

        _logger.LogInformation("User registered: {Email}", user.Email);

        return new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            Username = user.Username,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }

    public async Task<string> GenerateTokenAsync(Domain.Entities.User user)
    {
        var jwtSecret = _configuration["JWT:SecretKey"] 
            ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
        var jwtIssuer = _configuration["JWT:Issuer"] ?? "AuthService";
        var expirationMinutes = int.Parse(_configuration["JWT:ExpirationMinutes"] ?? "60");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

