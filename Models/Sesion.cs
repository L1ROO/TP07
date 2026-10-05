namespace Tp07.Models;

/// <summary>
/// Métodos de extensión para guardar y leer el usuario logueado en Session.
/// </summary>
public static class Sesion
{
    private const string ClaveId = "IdUsuario";
    private const string ClaveNombreUsuario = "NombreUsuario";
    private const string ClaveNombre = "Nombre";

    public static void IniciarSesion(this ISession session, Usuario usuario)
    {
        session.SetInt32(ClaveId, usuario.Id);
        session.SetString(ClaveNombreUsuario, usuario.NombreUsuario);
        session.SetString(ClaveNombre, $"{usuario.Nombre} {usuario.Apellido}".Trim());
    }

    public static void CerrarSesion(this ISession session) => session.Clear();

    public static int? ObtenerIdUsuario(this ISession session) => session.GetInt32(ClaveId);

    public static string? ObtenerNombreUsuario(this ISession session) => session.GetString(ClaveNombreUsuario);

    public static string? ObtenerNombreCompleto(this ISession session) => session.GetString(ClaveNombre);
}
