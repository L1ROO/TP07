namespace Tp07.Models;

public class Comentario
{
    public int Id { get; set; }
    public int IdPublicacion { get; set; }
    public int IdUsuarioComenta { get; set; }
    public string Texto { get; set; } = "";
    public DateTime FechaComentario { get; set; }

    // Viene del JOIN con Usuarios
    public string NombreUsuario { get; set; } = "";

    public string FechaFormateada => FechaComentario.ToString("dd/MM/yyyy HH:mm");
    public string Inicial => NombreUsuario.Length > 0 ? NombreUsuario[..1].ToUpper() : "?";
    public int Tono => Avatar.Tono(NombreUsuario);
}
