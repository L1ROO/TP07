using Microsoft.AspNetCore.Mvc;
using Tp07.Models;

namespace Tp07.Controllers;

public class CuentaController : Controller
{
    // ================================================================ Login

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.ObtenerIdUsuario() != null)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string nombreUsuario, string contrasena)
    {
        nombreUsuario = nombreUsuario?.Trim() ?? "";
        ViewBag.NombreUsuario = nombreUsuario;

        if (nombreUsuario == "" || string.IsNullOrEmpty(contrasena))
        {
            ViewBag.Error = "Completá el usuario y la contraseña.";
            return View();
        }

        Usuario? usuario = BD.Login(nombreUsuario, contrasena);
        if (usuario == null)
        {
            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        HttpContext.Session.IniciarSesion(usuario);
        return RedirectToAction("Index", "Home");
    }

    // ============================================================= Registro

    [HttpGet]
    public IActionResult Registro()
    {
        if (HttpContext.Session.ObtenerIdUsuario() != null)
            return RedirectToAction("Index", "Home");

        return View(new Usuario());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Registro(Usuario usuario, string contrasena, string confirmacion)
    {
        usuario.NombreUsuario = usuario.NombreUsuario?.Trim() ?? "";
        usuario.Nombre = usuario.Nombre?.Trim() ?? "";
        usuario.Apellido = usuario.Apellido?.Trim() ?? "";

        string? error = ValidarRegistro(usuario, contrasena, confirmacion);
        if (error != null)
        {
            ViewBag.Error = error;
            return View(usuario);
        }

        usuario.Id = BD.Registrar(usuario, contrasena);
        HttpContext.Session.IniciarSesion(usuario);
        return RedirectToAction("Index", "Home");
    }

    private static string? ValidarRegistro(Usuario usuario, string contrasena, string confirmacion)
    {
        if (usuario.NombreUsuario == "" || usuario.Nombre == "" || usuario.Apellido == "" || string.IsNullOrEmpty(contrasena))
            return "Todos los campos son obligatorios.";

        // Las columnas de Usuarios son varchar(50)
        if (usuario.NombreUsuario.Length > 50 || usuario.Nombre.Length > 50 || usuario.Apellido.Length > 50 || contrasena.Length > 50)
            return "Los campos no pueden superar los 50 caracteres.";

        if (usuario.NombreUsuario.Contains(' '))
            return "El nombre de usuario no puede tener espacios.";

        if (contrasena.Length < 6)
            return "La contraseña debe tener al menos 6 caracteres.";

        if (contrasena != confirmacion)
            return "Las contraseñas no coinciden.";

        if (BD.ExisteNombreUsuario(usuario.NombreUsuario))
            return "Ese nombre de usuario ya está en uso.";

        return null;
    }

    // =============================================================== Logout

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.CerrarSesion();
        return RedirectToAction("Login");
    }
}
