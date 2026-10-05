namespace Tp07.Models;

public class Publicacion
{
    public const string CarpetaImagenes = "/img/publicaciones/";
    public const int CantidadPorPagina = 10;

    public int Id { get; set; }
    public int IdUsuario { get; set; }
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Imagen { get; set; } = "";
    public DateTime FechaPublicacion { get; set; }

    // Calculados en la consulta (JOIN y subconsultas)
    public string NombreUsuario { get; set; } = "";
    public int CantidadMeGusta { get; set; }
    public bool UsuarioDioMeGusta { get; set; }

    public List<Comentario> Comentarios { get; set; } = new();

    public string FechaFormateada => FechaPublicacion.ToString("dd/MM/yyyy HH:mm");
    public string UrlImagen => CarpetaImagenes + Imagen;
    public string Inicial => NombreUsuario.Length > 0 ? NombreUsuario[..1].ToUpper() : "?";
    public int Tono => Avatar.Tono(NombreUsuario);
}
