using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private static readonly List<User> _users = new();
    private static int _nextId = 1;

    // GET api/users
    [HttpGet]
    public ActionResult<IEnumerable<User>> GetAllUsers()
    {
        try
        {
            return Ok(_users);
        }
        catch (Exception ex)
        {
            return Problem(
                title: "An unexpected error occurred while retrieving users.",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    // GET api/users/{id}
    [HttpGet("{id}")]
    public ActionResult<User> GetUserById(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest($"Invalid ID '{id}'. ID must be a positive integer.");

            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user is null)
                return NotFound($"User with ID {id} was not found.");

            return Ok(user);
        }
        catch (Exception ex)
        {
            return Problem(
                title: $"An unexpected error occurred while retrieving user with ID {id}.",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    // POST api/users
    [HttpPost]
    public ActionResult<User> AddUser(User user)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
                return BadRequest("UserName is required and cannot be empty.");

            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest("Email is required and cannot be empty.");

            user.Id = _nextId++;
            _users.Add(user);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            return Problem(
                title: "An unexpected error occurred while adding the user.",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    // PUT api/users/{id}
    [HttpPut("{id}")]
    public IActionResult UpdateUser(int id, User updatedUser)
    {
        try
        {
            if (id <= 0)
                return BadRequest($"Invalid ID '{id}'. ID must be a positive integer.");

            if (string.IsNullOrWhiteSpace(updatedUser.UserName))
                return BadRequest("UserName is required and cannot be empty.");

            if (string.IsNullOrWhiteSpace(updatedUser.Email))
                return BadRequest("Email is required and cannot be empty.");

            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user is null)
                return NotFound($"User with ID {id} was not found.");

            user.UserName = updatedUser.UserName;
            user.Email = updatedUser.Email;

            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(
                title: $"An unexpected error occurred while updating user with ID {id}.",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    // DELETE api/users/{id}
    [HttpDelete("{id}")]
    public IActionResult DeleteUser(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest($"Invalid ID '{id}'. ID must be a positive integer.");

            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user is null)
                return NotFound($"User with ID {id} was not found.");

            _users.Remove(user);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(
                title: $"An unexpected error occurred while deleting user with ID {id}.",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
