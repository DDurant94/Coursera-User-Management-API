using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Controllers;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Tests.Controllers;

public class UsersControllerTests
{
    // Creates a fresh controller with an isolated in-memory store for each test
    private static UsersController CreateController() =>
        new UsersController(new InMemoryUserStore());

    private static User MakeUser(string userName = "jdoe", string email = "jdoe@example.com") =>
        new User { UserName = userName, Email = email };

    // -------------------------------------------------------------------------
    // GET api/users
    // -------------------------------------------------------------------------

    [Fact]
    public void GetAllUsers_ReturnsOk_WithEmptyList_WhenNoUsersExist()
    {
        var controller = CreateController();

        var result = controller.GetAllUsers(1, 20);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<User>>(ok.Value);
        Assert.Empty(users);
    }

    [Fact]
    public void GetAllUsers_ReturnsBadRequest_WhenPageIsZero()
    {
        var controller = CreateController();

        var result = controller.GetAllUsers(0, 20);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void GetAllUsers_ReturnsBadRequest_WhenPageIsNegative()
    {
        var controller = CreateController();

        var result = controller.GetAllUsers(-1, 20);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void GetAllUsers_ReturnsBadRequest_WhenPageSizeIsZero()
    {
        var controller = CreateController();

        var result = controller.GetAllUsers(1, 0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void GetAllUsers_ReturnsBadRequest_WhenPageSizeExceedsLimit()
    {
        var controller = CreateController();

        var result = controller.GetAllUsers(1, 101);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void GetAllUsers_ReturnsCorrectPageSize()
    {
        var controller = CreateController();
        for (int i = 0; i < 5; i++)
            controller.AddUser(MakeUser($"user{i}", $"user{i}@example.com"));

        var result = controller.GetAllUsers(page: 1, pageSize: 3);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<User>>(ok.Value).ToList();
        Assert.Equal(3, users.Count);
    }

    [Fact]
    public void GetAllUsers_ReturnsCorrectSecondPage()
    {
        var controller = CreateController();
        for (int i = 0; i < 5; i++)
            controller.AddUser(MakeUser($"user{i}", $"user{i}@example.com"));

        var result = controller.GetAllUsers(page: 2, pageSize: 3);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<User>>(ok.Value).ToList();
        Assert.Equal(2, users.Count);
    }

    [Fact]
    public void GetAllUsers_ReturnsUsersOrderedById()
    {
        var controller = CreateController();
        controller.AddUser(MakeUser("alpha", "alpha@example.com"));
        controller.AddUser(MakeUser("beta", "beta@example.com"));
        controller.AddUser(MakeUser("gamma", "gamma@example.com"));

        var result = controller.GetAllUsers(1, 20);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<User>>(ok.Value).ToList();
        Assert.Equal(users.OrderBy(u => u.Id).ToList(), users);
    }

    // -------------------------------------------------------------------------
    // GET api/users/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public void GetUserById_ReturnsOk_WithCorrectUser()
    {
        var controller = CreateController();
        controller.AddUser(MakeUser("jdoe", "jdoe@example.com"));

        var result = controller.GetUserById(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<User>(ok.Value);
        Assert.Equal(1, user.Id);
        Assert.Equal("jdoe", user.UserName);
        Assert.Equal("jdoe@example.com", user.Email);
    }

    [Fact]
    public void GetUserById_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var controller = CreateController();

        var result = controller.GetUserById(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public void GetUserById_ReturnsBadRequest_WhenIdIsNegative()
    {
        var controller = CreateController();

        var result = controller.GetUserById(-1);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void GetUserById_ReturnsBadRequest_WhenIdIsZero()
    {
        var controller = CreateController();

        var result = controller.GetUserById(0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // -------------------------------------------------------------------------
    // POST api/users
    // -------------------------------------------------------------------------

    [Fact]
    public void AddUser_ReturnsCreated_WithAssignedId()
    {
        var controller = CreateController();

        var result = controller.AddUser(MakeUser());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var user = Assert.IsType<User>(created.Value);
        Assert.True(user.Id > 0);
    }

    [Fact]
    public void AddUser_ReturnsCreated_WithCorrectUserData()
    {
        var controller = CreateController();

        var result = controller.AddUser(MakeUser("jdoe", "jdoe@example.com"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var user = Assert.IsType<User>(created.Value);
        Assert.Equal("jdoe", user.UserName);
        Assert.Equal("jdoe@example.com", user.Email);
    }

    [Fact]
    public void AddUser_AssignsUniqueIncrementingIds()
    {
        var controller = CreateController();

        var r1 = controller.AddUser(MakeUser("user1", "user1@example.com"));
        var r2 = controller.AddUser(MakeUser("user2", "user2@example.com"));

        var id1 = Assert.IsType<User>(Assert.IsType<CreatedAtActionResult>(r1.Result).Value).Id;
        var id2 = Assert.IsType<User>(Assert.IsType<CreatedAtActionResult>(r2.Result).Value).Id;
        Assert.NotEqual(id1, id2);
        Assert.Equal(id1 + 1, id2);
    }

    [Fact]
    public void AddUser_PersistsUser_RetrievableByGetUserById()
    {
        var controller = CreateController();
        controller.AddUser(MakeUser("jdoe", "jdoe@example.com"));

        var result = controller.GetUserById(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<User>(ok.Value);
        Assert.Equal("jdoe", user.UserName);
    }

    // -------------------------------------------------------------------------
    // PUT api/users/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public void UpdateUser_ReturnsNoContent_WhenSuccessful()
    {
        var controller = CreateController();
        controller.AddUser(MakeUser());

        var result = controller.UpdateUser(1, MakeUser("updated", "updated@example.com"));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void UpdateUser_UpdatesUserName_AndEmail()
    {
        var controller = CreateController();
        controller.AddUser(MakeUser("original", "original@example.com"));

        controller.UpdateUser(1, MakeUser("updated", "updated@example.com"));

        var getResult = controller.GetUserById(1);
        var ok = Assert.IsType<OkObjectResult>(getResult.Result);
        var user = Assert.IsType<User>(ok.Value);
        Assert.Equal("updated", user.UserName);
        Assert.Equal("updated@example.com", user.Email);
    }

    [Fact]
    public void UpdateUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var controller = CreateController();

        var result = controller.UpdateUser(999, MakeUser());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void UpdateUser_ReturnsBadRequest_WhenIdIsNegative()
    {
        var controller = CreateController();

        var result = controller.UpdateUser(-5, MakeUser());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void UpdateUser_ReturnsBadRequest_WhenIdIsZero()
    {
        var controller = CreateController();

        var result = controller.UpdateUser(0, MakeUser());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // -------------------------------------------------------------------------
    // DELETE api/users/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public void DeleteUser_ReturnsNoContent_WhenSuccessful()
    {
        var controller = CreateController();
        controller.AddUser(MakeUser());

        var result = controller.DeleteUser(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void DeleteUser_RemovesUser_SoGetReturnsNotFound()
    {
        var controller = CreateController();
        controller.AddUser(MakeUser());
        controller.DeleteUser(1);

        var result = controller.GetUserById(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public void DeleteUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var controller = CreateController();

        var result = controller.DeleteUser(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void DeleteUser_ReturnsBadRequest_WhenIdIsZero()
    {
        var controller = CreateController();

        var result = controller.DeleteUser(0);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void DeleteUser_ReturnsBadRequest_WhenIdIsNegative()
    {
        var controller = CreateController();

        var result = controller.DeleteUser(-3);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
