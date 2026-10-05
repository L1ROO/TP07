using Dapper;
using Microsoft.Data.SqlClient;

namespace Tp07.Models;

public static class BD
{
    // Se carga desde appsettings.json en Program.cs
    public static string ConnectionString { get; set; } = "";

    private static SqlConnection Conectar() => new SqlConnection(ConnectionString);

    // ============================================================ Usuarios

    public static Usuario? Login(string nombreUsuario, string contrasena)
    {
        // COLLATE ..._CS_AS hace que la contraseña distinga mayúsculas y minúsculas
        const string sql = @"
            SELECT Id, NombreUsuario, Nombre, Apellido
            FROM Usuarios
            WHERE NombreUsuario = @NombreUsuario
              AND [Contraseña] = @Contrasena COLLATE Latin1_General_CS_AS";

        using var db = Conectar();
        return db.QueryFirstOrDefault<Usuario>(sql, new { NombreUsuario = nombreUsuario, Contrasena = contrasena });
    }

    public static bool ExisteNombreUsuario(string nombreUsuario)
    {
        using var db = Conectar();
        return db.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Usuarios WHERE NombreUsuario = @NombreUsuario",
            new { NombreUsuario = nombreUsuario }) > 0;
    }

    public static int Registrar(Usuario usuario, string contrasena)
    {
        const string sql = @"
            INSERT INTO Usuarios (NombreUsuario, [Contraseña], Nombre, Apellido)
            VALUES (@NombreUsuario, @Contrasena, @Nombre, @Apellido);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        using var db = Conectar();
        return db.ExecuteScalar<int>(sql, new
        {
            usuario.NombreUsuario,
            Contrasena = contrasena,
            usuario.Nombre,
            usuario.Apellido
        });
    }

    // ======================================================= Publicaciones

    /// <summary>
    /// Devuelve "cantidad" publicaciones a partir de la posición "desde",
    /// de la más reciente a la más antigua, con sus comentarios y Me Gusta.
    /// </summary>
    public static List<Publicacion> ObtenerPublicaciones(int desde, int cantidad, int idUsuarioActual)
    {
        const string sqlPublicaciones = @"
            SELECT p.Id, p.IdUsuario,
                   ISNULL(p.Titulo, '') AS Titulo,
                   ISNULL(CAST(p.Descripcion AS varchar(max)), '') AS Descripcion,
                   ISNULL(p.Imagen, '') AS Imagen,
                   p.FechaPublicacion,
                   u.NombreUsuario,
                   (SELECT COUNT(*) FROM PublicacionesMeGusta mg
                     WHERE mg.[IdPublicación] = p.Id) AS CantidadMeGusta,
                   CAST(CASE WHEN EXISTS (SELECT 1 FROM PublicacionesMeGusta mg
                                           WHERE mg.[IdPublicación] = p.Id
                                             AND mg.IdUsuario = @IdUsuarioActual)
                             THEN 1 ELSE 0 END AS bit) AS UsuarioDioMeGusta
            FROM Publicaciones p
            INNER JOIN Usuarios u ON u.Id = p.IdUsuario
            ORDER BY p.FechaPublicacion DESC, p.Id DESC
            OFFSET @Desde ROWS FETCH NEXT @Cantidad ROWS ONLY";

        const string sqlComentarios = @"
            SELECT c.Id, c.IdPublicacion, c.IdUsuarioComenta,
                   CAST(c.Texto AS varchar(max)) AS Texto,
                   c.FechaComentario, u.NombreUsuario
            FROM Comentarios c
            INNER JOIN Usuarios u ON u.Id = c.IdUsuarioComenta
            WHERE c.IdPublicacion IN @Ids
            ORDER BY c.FechaComentario, c.Id";

        using var db = Conectar();
        List<Publicacion> publicaciones = db.Query<Publicacion>(sqlPublicaciones, new
        {
            Desde = desde,
            Cantidad = cantidad,
            IdUsuarioActual = idUsuarioActual
        }).ToList();

        if (publicaciones.Count == 0) return publicaciones;

        // Una sola consulta para traer los comentarios de todas las publicaciones de la página
        var comentarios = db.Query<Comentario>(sqlComentarios, new { Ids = publicaciones.Select(p => p.Id) })
                            .ToLookup(c => c.IdPublicacion);

        foreach (Publicacion p in publicaciones)
            p.Comentarios = comentarios[p.Id].ToList();

        return publicaciones;
    }

    public static int ContarPublicaciones()
    {
        using var db = Conectar();
        return db.ExecuteScalar<int>("SELECT COUNT(*) FROM Publicaciones");
    }

    public static bool ExistePublicacion(int idPublicacion)
    {
        using var db = Conectar();
        return db.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Publicaciones WHERE Id = @Id",
            new { Id = idPublicacion }) > 0;
    }

    public static void CrearPublicacion(Publicacion publicacion)
    {
        const string sql = @"
            INSERT INTO Publicaciones (IdUsuario, Titulo, Descripcion, Imagen, FechaPublicacion)
            VALUES (@IdUsuario, @Titulo, @Descripcion, @Imagen, @FechaPublicacion)";

        using var db = Conectar();
        db.Execute(sql, publicacion);
    }

    // ============================================================ Me Gusta

    /// <summary>
    /// Si el usuario ya había dado Me Gusta lo quita; si no, lo agrega.
    /// Devuelve el estado final y la nueva cantidad de Me Gusta.
    /// </summary>
    public static (bool leGusta, int cantidad) AlternarMeGusta(int idPublicacion, int idUsuario)
    {
        const string sql = @"
            DECLARE @LeGusta bit;

            IF EXISTS (SELECT 1 FROM PublicacionesMeGusta
                        WHERE [IdPublicación] = @IdPublicacion AND IdUsuario = @IdUsuario)
            BEGIN
                DELETE FROM PublicacionesMeGusta
                 WHERE [IdPublicación] = @IdPublicacion AND IdUsuario = @IdUsuario;
                SET @LeGusta = 0;
            END
            ELSE
            BEGIN
                INSERT INTO PublicacionesMeGusta ([IdPublicación], IdUsuario)
                VALUES (@IdPublicacion, @IdUsuario);
                SET @LeGusta = 1;
            END

            SELECT @LeGusta AS LeGusta,
                   (SELECT COUNT(*) FROM PublicacionesMeGusta
                     WHERE [IdPublicación] = @IdPublicacion) AS Cantidad;";

        using var db = Conectar();
        var resultado = db.QuerySingle<(bool LeGusta, int Cantidad)>(sql, new
        {
            IdPublicacion = idPublicacion,
            IdUsuario = idUsuario
        });
        return (resultado.LeGusta, resultado.Cantidad);
    }

    // ========================================================= Comentarios

    public static Comentario AgregarComentario(int idPublicacion, int idUsuario, string texto)
    {
        const string sql = @"
            INSERT INTO Comentarios (IdPublicacion, IdUsuarioComenta, Texto, FechaComentario)
            VALUES (@IdPublicacion, @IdUsuario, @Texto, @Fecha);

            SELECT c.Id, c.IdPublicacion, c.IdUsuarioComenta,
                   CAST(c.Texto AS varchar(max)) AS Texto,
                   c.FechaComentario, u.NombreUsuario
            FROM Comentarios c
            INNER JOIN Usuarios u ON u.Id = c.IdUsuarioComenta
            WHERE c.Id = SCOPE_IDENTITY();";

        using var db = Conectar();
        return db.QuerySingle<Comentario>(sql, new
        {
            IdPublicacion = idPublicacion,
            IdUsuario = idUsuario,
            Texto = texto,
            Fecha = DateTime.Now
        });
    }
}
