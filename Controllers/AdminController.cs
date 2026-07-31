using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WonderlandBackend.Services;

namespace WonderlandBackend.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly IEmailService _emailService; // ✅ Add this
        private readonly ILogger<AdminController> _logger; // ✅ Add this

        public AdminController(
            AdminService adminService,
            IEmailService emailService, // ✅ Add this parameter
            ILogger<AdminController> logger) // ✅ Add this parameter
        {
            _adminService = adminService;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: api/admin/dashboard - Get dashboard statistics
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _adminService.GetDashboardStats();
            return Ok(stats);
        }

        // GET: api/admin/users - Get all users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminService.GetAllUsers();
            return Ok(users);
        }

        // PUT: api/admin/users/{userId}/role - Update user role
        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] string newRole)
        {
            try
            {
                var user = await _adminService.UpdateUserRole(userId, newRole);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(new { message = "User role updated successfully", user });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("test-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                _logger.LogInformation("📧 Test email endpoint called");

                await _emailService.SendAdminNotificationEmail(
                    999,
                    "Test Customer",
                    "abdulmoid47628@gmail.com",
                    100.00m
                );

                return Ok(new { message = "Test email sent successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test email failed");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}