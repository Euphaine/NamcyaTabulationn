using Microsoft.EntityFrameworkCore;
using NamcyaTabulation.Data;
using NamcyaTabulation.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NamcyaTabulation.Services
{
    public class OrganizerAuthService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ProtectedSessionStorage _session;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _isInitialized = false;

        public Organizer? CurrentOrganizer { get; private set; }

        public OrganizerAuthService(IServiceScopeFactory scopeFactory, ProtectedSessionStorage session)
        {
            _scopeFactory = scopeFactory;
            _session = session;
        }

        public async Task InitializeSessionAsync()
        {
            if (_isInitialized) return;
            
            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized) return;
                
                var result = await _session.GetAsync<int>("OrganizerId");
                if (result.Success && result.Value > 0)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    CurrentOrganizer = await context.Organizers.FindAsync(result.Value);
                }
                _isInitialized = true;
            }
            catch { } // Fails safely during prerendering
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var organizer = await context.Organizers.FirstOrDefaultAsync(o => o.Username == username);
            if (organizer != null && VerifyPassword(password, organizer.PasswordHash))
            {
                CurrentOrganizer = organizer;
                try
                {
                    await _session.SetAsync("OrganizerId", organizer.Id);
                }
                catch { }
                return true;
            }
            return false;
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Check if username already exists
            if (await context.Organizers.AnyAsync(o => o.Username.ToLower() == username.ToLower()))
            {
                return false; 
            }

            var newOrganizer = new Organizer
            {
                Username = username,
                PasswordHash = HashPassword(password)
            };

            context.Organizers.Add(newOrganizer);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task LogoutAsync()
        {
            CurrentOrganizer = null;
            try
            {
                await _session.DeleteAsync("OrganizerId");
            }
            catch { }
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            return HashPassword(password) == storedHash;
        }
    }
}