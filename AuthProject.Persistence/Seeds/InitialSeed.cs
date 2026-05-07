// NOTA: Para cambiar los permisos iniciales, modificar los valores aquí y ejecutar nueva migración.
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public static class InitialSeed
{
    public static void Apply(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasData(
            new { Id = 1, Role = Role.Admin,  PermissionsJson = """{"users":["read","create","update","delete"],"orders":["read","create","update","delete"]}""" },
            new { Id = 2, Role = Role.Helper, PermissionsJson = """{"users":["read"],"orders":["read","update"]}""" },
            new { Id = 3, Role = Role.Client, PermissionsJson = """{"orders":["read","create"]}""" }
        );
    }
}
