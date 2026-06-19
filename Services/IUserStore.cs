using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public interface IUserStore
{
    IEnumerable<User> GetAll();
    bool TryGetById(int id, out User? user);
    User Add(User user);
    bool TryUpdate(int id, User updatedUser);
    bool TryRemove(int id);
}
