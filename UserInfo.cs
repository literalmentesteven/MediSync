namespace MediSync.Helpers
{
    /// <summary>
    /// Almacenamiento estático simple para la sesión actual.
    /// </summary>
    public static class UserInfo
    {
        public static string Token { get; set; } = string.Empty;
        public static string NombreUsuario { get; set; } = string.Empty;
        public static string Rol { get; set; } = string.Empty;
        public static string IdUsuario { get; set; } = string.Empty;

        public static bool IsAdminOrSuper => Rol == "Administrador" || Rol == "Superusuario";
    }
}