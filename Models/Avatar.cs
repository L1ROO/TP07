namespace Tp07.Models;

public static class Avatar
{
    /// <summary>
    /// Color (tono HSL de 0 a 359) fijo para cada nombre de usuario,
    /// así cada usuario tiene siempre el mismo color de avatar.
    /// </summary>
    public static int Tono(string? nombreUsuario) =>
        string.IsNullOrEmpty(nombreUsuario) ? 0 : nombreUsuario.Sum(c => (int)c) * 37 % 360;
}
