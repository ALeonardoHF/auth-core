public class RolePermission
{
    public int Id { get; private set; }
    public Role Role { get; private set; }
    public string PermissionsJson { get; private set; } = string.Empty;

    private RolePermission() { }

    public static RolePermission Create(Role role, string permissionsJson)
    {
        return new RolePermission
        {
            Role = role,
            PermissionsJson = permissionsJson
        };
    }
}