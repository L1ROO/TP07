using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tp07.Models;

namespace Tp07.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        int? idUsuario = HttpContext.Session.ObtenerIdUsuario();
        if (idUsuario == null)
            return RedirectToAction("Login", "Cuenta");

        // Al entrar se muestran solo las 10 más recientes; el resto se pide con "Ver más"
        List<Publicacion> publicaciones = BD.ObtenerPublicaciones(0, Publicacion.CantidadPorPagina, idUsuario.Value);
        ViewBag.HayMas = BD.ContarPublicaciones() > publicaciones.Count;

        return View(publicaciones);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
