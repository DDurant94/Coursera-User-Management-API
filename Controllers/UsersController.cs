using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserStore _store;

    public UsersController(IUserStore store)
    {
        _store = store;
    }

    // GET api/users?page=1&pageSize=20
    [HttpGet]
    public ActionResult<IEnumerable<User>> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page <= 0)
                return BadRequest("Page must be a positive integer.");

            if (pageSize <= 0 || pageSize > 100)
                return BadRequest("PageSize must be between 1 and 100.");

            var pagedUsers = _store.GetAll()
                .OrderBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return Ok(pagedUsers);
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

            if (!_store.TryGetById(id, out var user))
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
            var created = _store.Add(user);
            return CreatedAtAction(nameof(GetUserById), new { id = created.Id }, created);
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

            if (!_store.TryUpdate(id, updatedUser))
                return NotFound($"User with ID {id} was not found.");

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

            if (!_store.TryRemove(id))
                return NotFound($"User with ID {id} was not found.");

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
