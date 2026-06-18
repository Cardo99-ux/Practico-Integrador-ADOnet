using System;

namespace Practico.Starter.Dominio;

public class DetallePedido
{
    public int PedidoId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnit { get; set; } // Nombre corregido según laboratorios físicos

    // Propiedad calculada en C# en sintonía con la base de datos
    public decimal Subtotal => Cantidad * PrecioUnit;

    // Propiedades de navegación relacionales (N a N)
    public Pedido? Pedido { get; set; }
    public Producto? Producto { get; set; }
}