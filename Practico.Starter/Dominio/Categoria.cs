using System;
using System.Collections.Generic;

namespace Practico.Starter.Dominio;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    // Propiedad de navegación relacional (1 a N)
    public List<Producto> Productos { get; set; } = new();
}