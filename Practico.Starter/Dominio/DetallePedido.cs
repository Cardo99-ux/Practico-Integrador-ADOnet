namespace Practico.Starter.Dominio;

public class DetallePedido
{
    public int PedidoId { get; set; }     // PK, FK
    public int ProductoId { get; set; }   // PK, FK
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}