using System;
using System.Collections.Generic;

namespace Practico.Starter.Dominio;

public class Pedido
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime Fecha { get; set; }

    // Propiedades de navegación relacionales
    public Cliente? Cliente { get; set; }
    public List<DetallePedido> Detalles { get; set; } = new();
}