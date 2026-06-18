using System;
using System.Collections.Generic;

namespace Practico.Starter.Dominio;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Propiedad de navegación relacional (1 a N)
    public List<Pedido> Pedidos { get; set; } = new();
}