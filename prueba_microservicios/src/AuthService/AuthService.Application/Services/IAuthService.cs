using AuthService.Application.DTOs;

namespace AuthService.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<string> GenerateTokenAsync(Domain.Entities.User user);
}

