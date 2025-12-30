using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendorEcommerceProject.Dtos.UserRoles;
using VendorEcommerceProject.Models.UserDetailsTable;

namespace VendorEcommerceProject.Controllers
{
    [ApiController]
    [Route("api/user-roles")]
    //[Authorize(Roles = "SuperAdmin,Admin")]
    public class UserRolesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UserRolesController(AppDbContext db)
        {
            _db = db;
        }

        // =====================================================
        // ADD ROLE TO USER
        // =====================================================
        [HttpPost("add")]
        public async Task<IActionResult> AddRole(AssignRoleDto dto)
        {
            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("User not found");

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null) return NotFound("Role not found");

            bool alreadyAssigned = await _db.UserRoleAssignments
                .AnyAsync(x => x.UserId == dto.UserId && x.RoleId == role.Id);

            if (alreadyAssigned)
                return BadRequest("User already has this role");

            _db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = dto.UserId,
                RoleId = role.Id
            });

            await _db.SaveChangesAsync();
            return Ok("Role added successfully");
        }

        // =====================================================
        // CHANGE ROLE (REMOVE OLD + ADD NEW)
        // =====================================================
        [HttpPut("change")]
        public async Task<IActionResult> ChangeRole(ChangeRoleDto dto)
        {
            var oldRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.OldRole);
            var newRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.NewRole);

            if (oldRole == null || newRole == null)
                return NotFound("Role not found");

            var assignment = await _db.UserRoleAssignments
                .FirstOrDefaultAsync(x =>
                    x.UserId == dto.UserId &&
                    x.RoleId == oldRole.Id);

            if (assignment == null)
                return BadRequest("User does not have the old role");

            _db.UserRoleAssignments.Remove(assignment);

            _db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = dto.UserId,
                RoleId = newRole.Id
            });

            await _db.SaveChangesAsync();
            return Ok("Role changed successfully");
        }

        // =====================================================
        // REMOVE ROLE FROM USER
        // =====================================================
        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveRole(AssignRoleDto dto)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null) return NotFound("Role not found");

            var assignment = await _db.UserRoleAssignments
                .FirstOrDefaultAsync(x =>
                    x.UserId == dto.UserId &&
                    x.RoleId == role.Id);

            if (assignment == null)
                return BadRequest("User does not have this role");

            _db.UserRoleAssignments.Remove(assignment);
            await _db.SaveChangesAsync();

            return Ok("Role removed successfully");
        }
    }
}
