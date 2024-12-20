﻿using BlazingQuiz.Api.Data;
using BlazingQuiz.Api.Data.Entities;
using BlazingQuiz.Shared;
using BlazingQuiz.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BlazingQuiz.Api.Services;

public class AuthService
{
    private readonly QuizContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(QuizContext context,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == dto.Username);
        if (user is null)
        {
            // Invalid username
            return new AuthResponseDto(default, "Invalid username");

        }
        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            // Incorrect password
            return new AuthResponseDto(default, "Incorrect password");
        }

        // Generate JWT Token
        var jwt = GenerateJwtToken(user);
        var loggedInUser = new LoggedInUser(user.Id, user.Name, user.Role, jwt);
        return new AuthResponseDto(loggedInUser);
    }

    private string GenerateJwtToken(User user)
    {
        Claim[] claims =
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var secretKey = _configuration.GetValue<string>("Jwt:Secret");
        var symmetrickey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));
        var signingCred = new SigningCredentials(symmetrickey, SecurityAlgorithms.HmacSha256);

        var jwtSecurityToken = new JwtSecurityToken(
            issuer: _configuration.GetValue<string>("Jwt:Issuer"),
            audience: _configuration.GetValue<string>("Jwt:Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpireInMinutes")),
            signingCredentials: signingCred);

        var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

        return token;
    }
}