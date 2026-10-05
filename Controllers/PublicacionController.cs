using Microsoft.AspNetCore.Mvc;
using Tp07.Models;

namespace Tp07.Controllers;

public class PublicacionController : Controller
{
    private const long TamanoMaximoImagen = 5 * 1024 * 1024; // 5 MB
    private const int LargoMaximoComentario = 500;
    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly IWebHostEnvironment _env;

    public PublicacionController(IWebHostEnvironment env)
    {
        _env = env;
    }

    // ======================================== Crear (formulario normal, sin Fetch)

    [HttpGet]
    public IActionResult Crear()
    {
        if (HttpContext.Session.ObtenerIdUsuario() == null)
            return RedirectToAction("Login", "Cuenta");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(string titulo, string descripcion, IFormFile? imagen)
    {
        int? idUsuario = HttpContext.Session.ObtenerIdUsuario();
        if (idUsuario == null)
            return RedirectToAction("Login", "Cuenta");

        titulo = titulo?.Trim() ?? "";
        descripcion = descripcion?.Trim() ?? "";
        ViewBag.Titulo = titulo;
        ViewBag.Descripcion = descripcion;

        string? error = ValidarPublicacion(titulo, descripcion, imagen);
        if (error != null)
        {
            ViewBag.Error = error;
            return View();
        }

        string nombreArchivo = await GuardarImagen(imagen!);

        BD.CrearPublicacion(new Publicacion
        {
            IdUsuario = idUsuario.Value,
            Titulo = titulo,
            Descripcion = descripcion,
            Imagen = nombreArchivo,
            FechaPublicacion = DateTime.Now
        });

        return RedirectToAction("Index", "Home");
    }

    private static string? ValidarPublicacion(string titulo, string descripcion, IFormFile? imagen)
    {
        if (titulo == "" || descripcion == "")
            return "El título y la descripción son obligatorios.";

        if (titulo.Length > 200)
            return "El título no puede superar los 200 caracteres.";

        if (imagen == null || imagen.Length == 0)
            return "Tenés que elegir una imagen.";

        if (imagen.Length > TamanoMaximoImagen)
            return "La imagen no puede superar los 5 MB.";

        string extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
            return "Formato no permitido. Usá JPG, PNG, GIF o WEBP.";

        return null;
    }

    /// <summary>
    /// Guarda la imagen en wwwroot/img/publicaciones con un nombre único.
    /// Un GUID sin guiones (32) + extensión entra en el varchar(50) de la columna Imagen.
    /// </summary>
    private async Task<string> GuardarImagen(IFormFile imagen)
    {
        string extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
        string nombreArchivo = Guid.NewGuid().ToString("N") + extension;

        string carpeta = Path.Combine(_env.WebRootPath, "img", "publicaciones");
        Directory.CreateDirectory(carpeta);

        using var stream = new FileStream(Path.Combine(carpeta, nombreArchivo), FileMode.Create);
        await imagen.CopyToAsync(stream);

        return nombreArchivo;
    }

    // ============================================== Ver más (Fetch GET -> JSON)

    // GET /Publicacion/ObtenerMas?desde=10
    [HttpGet]
    public IActionResult ObtenerMas(int desde)
    {
        int? idUsuario = HttpContext.Session.ObtenerIdUsuario();
        if (idUsuario == null)
            return Unauthorized(new { error = "Tu sesión expiró. Volvé a iniciar sesión." });

        if (desde < 0)
            return BadRequest(new { error = "El parámetro 'desde' no es válido." });

        List<Publicacion> publicaciones = BD.ObtenerPublicaciones(desde, Publicacion.CantidadPorPagina, idUsuario.Value);
        bool hayMas = BD.ContarPublicaciones() > desde + publicaciones.Count;

        return Json(new { publicaciones, hayMas });
    }

    // ============================================= Me Gusta (Fetch POST -> JSON)

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MeGusta(int idPublicacion)
    {
        int? idUsuario = HttpContext.Session.ObtenerIdUsuario();
        if (idUsuario == null)
            return Unauthorized(new { error = "Tu sesión expiró. Volvé a iniciar sesión." });

        if (!BD.ExistePublicacion(idPublicacion))
            return NotFound(new { error = "La publicación no existe." });

        var (leGusta, cantidad) = BD.AlternarMeGusta(idPublicacion, idUsuario.Value);

        return Json(new { leGusta, cantidad });
    }

    // ============================================= Comentar (Fetch POST -> JSON)

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Comentar(int idPublicacion, string texto)
    {
        int? idUsuario = HttpContext.Session.ObtenerIdUsuario();
        if (idUsuario == null)
            return Unauthorized(new { error = "Tu sesión expiró. Volvé a iniciar sesión." });

        if (!BD.ExistePublicacion(idPublicacion))
            return NotFound(new { error = "La publicación no existe." });

        texto = texto?.Trim() ?? "";
        if (texto == "")
            return BadRequest(new { error = "El comentario no puede estar vacío." });

        if (texto.Length > LargoMaximoComentario)
            return BadRequest(new { error = $"El comentario no puede superar los {LargoMaximoComentario} caracteres." });

        Comentario comentario = BD.AgregarComentario(idPublicacion, idUsuario.Value, texto);

        return Json(new
        {
            comentario.Id,
            comentario.NombreUsuario,
            comentario.Inicial,
            comentario.Tono,
            comentario.Texto,
            comentario.FechaFormateada
        });
    }
}
