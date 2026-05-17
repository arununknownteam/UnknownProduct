using BackendApi.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NHibernate;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using NHibernateSession = NHibernate.ISession;
using BackendApi.DTOs;
namespace BackendApi.Services
{
    public class LoginService
    {
        private readonly NHibernateSession _session;
        private readonly IConfiguration _configuration;

        public LoginService(
            NHibernateSession session,
            IConfiguration configuration
        )
        {
            _session = session;
            _configuration = configuration;
        }

        public string Login(LoginRequest request)
        {
            var user = _session.Query<User>()
                .FirstOrDefault(x => x.Email == request.Email);

            if (user == null)
                throw new Exception("Invalid email");

            bool validPassword =
                BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash
                );

            if (!validPassword)
                throw new Exception("Invalid password");

            return GenerateJwtToken(user);
        }

        public void Register(RegisterRequest request)
        {
            using var transaction = _session.BeginTransaction();

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _session.Save(user);

            transaction.Commit();
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.Email
                )
            };

            var secretKey =
                _configuration["Jwt:Key"];

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey!)
                );

            var creds =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                );

            var token =
                new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(7),
                    signingCredentials: creds
                );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}