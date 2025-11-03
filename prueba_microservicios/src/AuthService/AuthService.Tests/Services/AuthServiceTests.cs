using System;
using System.Threading.Tasks;
using AuthService.Application.DTOs;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService.Application.Services.AuthService>> _loggerMock;
    private readonly AuthService.Application.Services.AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService.Application.Services.AuthService>>();

        // Setup default configuration
        _configurationMock.Setup(c => c["JWT:SecretKey"])
            .Returns("YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!");
        _configurationMock.Setup(c => c["JWT:Issuer"]).Returns("AuthService");
        _configurationMock.Setup(c => c["JWT:ExpirationMinutes"]).Returns("60");

        _authService = new AuthService.Application.Services.AuthService(
            _userRepositoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = "testuser",
            PasswordHash = passwordHash,
            Role = "User"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);

        var loginDto = new LoginDto { Email = email, Password = password };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
        result.Username.Should().Be("testuser");
        result.Role.Should().Be("User");
        result.Token.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "invalid@example.com", Password = "Password123!" };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(loginDto.Email))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.LoginAsync(loginDto));
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var email = "test@example.com";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = "testuser",
            PasswordHash = passwordHash,
            Role = "User"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);

        var loginDto = new LoginDto { Email = email, Password = "WrongPassword123!" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.LoginAsync(loginDto));
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsAuthResponse()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!",
            Role = "User"
        };

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(registerDto.Email))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.ExistsByUsernameAsync(registerDto.Username))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(registerDto.Email);
        result.Username.Should().Be(registerDto.Username);
        result.Role.Should().Be("User");
        result.Token.Should().NotBeNullOrEmpty();
        
        _userRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "Password123!",
            Role = "User"
        };

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(registerDto.Email))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _authService.RegisterAsync(registerDto));
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "existinguser",
            Email = "new@example.com",
            Password = "Password123!",
            Role = "User"
        };

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(registerDto.Email))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.ExistsByUsernameAsync(registerDto.Username))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _authService.RegisterAsync(registerDto));
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidRole_DefaultsToUser()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!",
            Role = "InvalidRole"
        };

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(registerDto.Email))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.ExistsByUsernameAsync(registerDto.Username))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Role.Should().Be("User");
    }

    [Fact]
    public async Task GenerateTokenAsync_ReturnsValidJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser",
            Role = "Admin"
        };

        // Act
        var token = await _authService.GenerateTokenAsync(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        // Verify token can be parsed
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        jsonToken.Should().NotBeNull();
        jsonToken.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Email && c.Value == user.Email);
        jsonToken.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == user.Role);
    }
}

