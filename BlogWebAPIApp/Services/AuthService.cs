using BlogWebAPIApp.Context;
using BlogWebAPIApp.Exceptions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Services
{


    public class AuthService : IAuthService
    {
        private readonly BlogContext _db;
        private readonly IPasswordService _passwords;
        private readonly ITokenService _tokens;

        public AuthService(BlogContext db, IPasswordService passwords, ITokenService tokens)
        {
            _db = db;
            _passwords = passwords;
            _tokens = tokens;
        }

        //Register
        public async Task<(User user, string token)> Register(string email, string username, string password, string? displayName,UserRole
            role)
        {
            email = email.Trim();
            username = username.Trim();

            if (await _db.Users.AnyAsync(u => u.Email == email || u.Username == username))
                throw new InvalidOperationException("Email or username already exists.");

            var pwdHash = _passwords.HashPassword(password, null, out var key);

            var user = new User
            {
                Email = email,
                Username = username,
                DisplayName = displayName,
                Role = role, // or Blogger if you prefer
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                Password = pwdHash,      // byte[]
                PasswordHash = key       // byte[]
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = _tokens.CreateToken(user.Id, user.Username, user.Role.ToString());
            return (user, token);
        }

       //Login
        public async Task<(User user, string token)> Login(string emailOrUsername, string password)
        {
            emailOrUsername = emailOrUsername.Trim();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == emailOrUsername || u.Username == emailOrUsername);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid username or email.");

            var computed = _passwords.HashPassword(password, user.PasswordHash, out _);

            var ok = user.Password.Length == computed.Length &&
                     CryptographicOperations.FixedTimeEquals(user.Password, computed);

            if (!ok)
                throw new UnauthorizedAccessException("Invalid password.");

            var token = _tokens.CreateToken(user.Id, user.Username, user.Role.ToString());
            return (user, token);
        }
    }



}
