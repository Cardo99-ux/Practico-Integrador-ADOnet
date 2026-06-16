using System;
using MySqlConnector;

namespace Practico.Starter.Datos;

public class AccesoMySql : IAccesoDatos
{
    private const string ConnAdmin = "Server=localhost;Port=3307;User=root;Password=Curso.NET2026;";
    private const string ConnApp = "Server=localhost;Port=3307;User=root;Password=Curso.NET2026;Database=practico;";

    public void CrearEstructura()
    {
        // 1. Conectarse al servidor sin especificar base para crearla
        using (var adminConn = new MySqlConnection(ConnAdmin))
        {
            adminConn.Open();
            string sqlDb = "CREATE DATABASE IF NOT EXISTS practico;";
            using (var cmd = new MySqlCommand(sqlDb, adminConn))
            {
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("Base 'practico' verificada/creada.");
        }

        // 2. Conectarse a la base practico para las tablas
        using (var appConn = new MySqlConnection(ConnApp))
        {
            appConn.Open();

            string ddlDrop = @"
                DROP TABLE IF EXISTS detalle_pedido;
                DROP TABLE IF EXISTS pedidos;
                DROP TABLE IF EXISTS clientes;
                DROP TABLE IF EXISTS productos;
                DROP TABLE IF EXISTS categorias;
            ";

            using (var dropCmd = new MySqlCommand(ddlDrop, appConn))
            {
                dropCmd.ExecuteNonQuery();
            }

            string ddlCreate = @"
                CREATE TABLE categorias (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    nombre VARCHAR(100) NOT NULL
                );

                CREATE TABLE productos (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    nombre VARCHAR(150) NOT NULL,
                    precio DECIMAL(12, 2) NOT NULL,
                    stock INT NOT NULL,
                    categoria_id INT NOT NULL,
                    CONSTRAINT fk_productos_categorias FOREIGN KEY (categoria_id) REFERENCES categorias(id)
                );

                CREATE TABLE clientes (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    nombre VARCHAR(100) NOT NULL,
                    email VARCHAR(150) NOT NULL
                );

                CREATE TABLE pedidos (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    cliente_id INT NOT NULL,
                    fecha DATETIME NOT NULL,
                    CONSTRAINT fk_pedidos_clientes FOREIGN KEY (cliente_id) REFERENCES clientes(id)
                );

                CREATE TABLE detalle_pedido (
                    pedido_id INT NOT NULL,
                    producto_id INT NOT NULL,
                    cantidad INT NOT NULL,
                    precio_unitario DECIMAL(12, 2) NOT NULL,
                    PRIMARY KEY (pedido_id, producto_id),
                    CONSTRAINT fk_detalle_pedidos FOREIGN KEY (pedido_id) REFERENCES pedidos(id),
                    CONSTRAINT fk_detalle_productos FOREIGN KEY (producto_id) REFERENCES productos(id)
                );
            ";

            using (var createCmd = new MySqlCommand(ddlCreate, appConn))
            {
                createCmd.ExecuteNonQuery();
            }
            Console.WriteLine("Estructura (5 tablas) creada.");
        }
    }

    public void InsertarDatosPrueba()
    {
        using (var conn = new MySqlConnection(ConnApp))
        {
            conn.Open();
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // --- 1. CATEGORÍAS ---
                    int catElectronicaId, catLibrosId, catHogarId;
                    string sqlCat = "INSERT INTO categorias (nombre) VALUES (@nombre);";
                    using (var cmd = new MySqlCommand(sqlCat, conn, tx))
                    {
                        cmd.Parameters.Add("@nombre", MySqlDbType.VarChar);

                        cmd.Parameters["@nombre"].Value = "Electrónica"; cmd.ExecuteNonQuery();
                        catElectronicaId = Convert.ToInt32(cmd.LastInsertedId);

                        cmd.Parameters["@nombre"].Value = "Libros"; cmd.ExecuteNonQuery();
                        catLibrosId = Convert.ToInt32(cmd.LastInsertedId);

                        cmd.Parameters["@nombre"].Value = "Hogar"; cmd.ExecuteNonQuery();
                        catHogarId = Convert.ToInt32(cmd.LastInsertedId);
                    }

                    // --- 2. PRODUCTOS ---
                    int prodNotebook, prodMouse, prodTeclado, prodCleanCode, prodLampara;
                    string sqlProd = "INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@nombre, @precio, @stock, @catId);";
                    using (var cmd = new MySqlCommand(sqlProd, conn, tx))
                    {
                        cmd.Parameters.Add("@nombre", MySqlDbType.VarChar);
                        cmd.Parameters.Add("@precio", MySqlDbType.Decimal);
                        cmd.Parameters.Add("@stock", MySqlDbType.Int32);
                        cmd.Parameters.Add("@catId", MySqlDbType.Int32);

                        cmd.Parameters["@nombre"].Value = "Notebook 14\""; cmd.Parameters["@precio"].Value = 850000.00m; cmd.Parameters["@stock"].Value = 10; cmd.Parameters["@catId"].Value = catElectronicaId;
                        cmd.ExecuteNonQuery(); prodNotebook = Convert.ToInt32(cmd.LastInsertedId);

                        cmd.Parameters["@nombre"].Value = "Mouse inalámbrico"; cmd.Parameters["@precio"].Value = 12000.00m; cmd.Parameters["@stock"].Value = 50; cmd.Parameters["@catId"].Value = catElectronicaId;
                        cmd.ExecuteNonQuery(); prodMouse = Convert.ToInt32(cmd.LastInsertedId);

                        cmd.Parameters["@nombre"].Value = "Teclado mecánico"; cmd.Parameters["@precio"].Value = 35000.00m; cmd.Parameters["@stock"].Value = 30; cmd.Parameters["@catId"].Value = catElectronicaId;
                        cmd.ExecuteNonQuery(); prodTeclado = Convert.ToInt32(cmd.LastInsertedId);

                        cmd.Parameters["@nombre"].Value = "Clean Code"; cmd.Parameters["@precio"].Value = 28000.00m; cmd.Parameters["@stock"].Value = 15; cmd.Parameters["@catId"].Value = catLibrosId;
                        cmd.ExecuteNonQuery(); prodCleanCode = Convert.ToInt32(cmd.LastInsertedId);

                        cmd.Parameters["@nombre"].Value = "Lámpara LED escritorio"; cmd.Parameters["@precio"].Value = 15000.00m; cmd.Parameters["@stock"].Value = 25; cmd.Parameters["@catId"].Value = catHogarId;
                        cmd.ExecuteNonQuery(); prodLampara = Convert.ToInt32(cmd.LastInsertedId);
                    }

                    // --- 3. CLIENTES ---
                    int cliJuan, cliMaria;
                    string sqlCli = "INSERT INTO clientes (nombre, email) VALUES (@nombre, @email);";
                    using (var cmd = new MySqlCommand(sqlCli, conn, tx))
                    {
                        cmd.Parameters.Add("@nombre", MySqlDbType.VarChar);
                        cmd.Parameters.Add("@email", MySqlDbType.VarChar);

                        cmd.Parameters["@nombre"].Value = "Juan Perez"; cmd.Parameters["@email"].Value = "juan@email.com";
                        cmd.ExecuteNonQuery(); cliJuan = Convert.ToInt32(cmd.LastInsertedId);

                        cmd.Parameters["@nombre"].Value = "Maria Gomez"; cmd.Parameters["@email"].Value = "maria@email.com";
                        cmd.ExecuteNonQuery(); cliMaria = Convert.ToInt32(cmd.LastInsertedId);
                    }

                    // --- 4. PEDIDOS Y DETALLES ---
                    string sqlPed = "INSERT INTO pedidos (cliente_id, fecha) VALUES (@clienteId, @fecha);";
                    string sqlDet = "INSERT INTO detalle_pedido (pedido_id, producto_id, cantidad, precio_unitario) VALUES (@pedidoId, @productoId, @cantidad, @precioUnitario);";

                    int ped1Id;
                    using (var cmd = new MySqlCommand(sqlPed, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@clienteId", cliJuan);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmd.ExecuteNonQuery(); ped1Id = Convert.ToInt32(cmd.LastInsertedId);
                    }
                    ExecuteDetalleMySql(conn, tx, sqlDet, ped1Id, prodMouse, 2, 12000.00m);
                    ExecuteDetalleMySql(conn, tx, sqlDet, ped1Id, prodNotebook, 1, 850000.00m);
                    ExecuteDetalleMySql(conn, tx, sqlDet, ped1Id, prodTeclado, 1, 35000.00m);

                    int ped2Id;
                    using (var cmd = new MySqlCommand(sqlPed, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@clienteId", cliMaria);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmd.ExecuteNonQuery(); ped2Id = Convert.ToInt32(cmd.LastInsertedId);
                    }
                    ExecuteDetalleMySql(conn, tx, sqlDet, ped2Id, prodCleanCode, 1, 28000.00m);
                    ExecuteDetalleMySql(conn, tx, sqlDet, ped2Id, prodLampara, 2, 15000.00m);

                    tx.Commit();
                    Console.WriteLine("Datos de prueba insertados (commit).");
                }
                catch (Exception)
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
    }

    private void ExecuteDetalleMySql(MySqlConnection c, MySqlTransaction t, string sql, int pedId, int prodId, int cant, decimal precio)
    {
        using (var cmd = new MySqlCommand(sql, c, t))
        {
            cmd.Parameters.AddWithValue("@pedidoId", pedId);
            cmd.Parameters.AddWithValue("@productoId", prodId);
            cmd.Parameters.AddWithValue("@cantidad", cant);
            cmd.Parameters.AddWithValue("@precioUnitario", precio);
            cmd.ExecuteNonQuery();
        }
    }
    public void EjecutarOperaciones()
    {
        using (var conn = new MySqlConnection(ConnApp))
        {
            conn.Open();

            // --- [C1] ---
            Console.WriteLine("[C1] Productos con su categoría:");
            string queryC1 = @"
                SELECT p.id, p.nombre, p.precio, c.nombre AS categoria_nombre 
                FROM productos p 
                INNER JOIN categorias c ON p.categoria_id = c.id 
                ORDER BY p.id;";

            using (var cmd = new MySqlConnection(ConnApp).CreateCommand())
            {
                // Alternativa estándar directa
                using (var cmdReal = new MySqlCommand(queryC1, conn))
                using (var reader = cmdReal.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string nombre = reader.GetString(1);
                        decimal precio = reader.GetDecimal(2);
                        string catNombre = reader.GetString(3);
                        Console.WriteLine($"#{id} {nombre} — ${precio:F2} [{catNombre}]");
                    }
                }
            }

            // --- [C2] ---
            Console.WriteLine("\n[C2] Detalle y total del pedido #1:");
            string queryC2 = @"
                SELECT p.nombre, dp.cantidad, dp.precio_unitario 
                FROM detalle_pedido dp
                INNER JOIN productos p ON dp.producto_id = p.id
                WHERE dp.pedido_id = 1
                ORDER BY p.nombre DESC;";

            decimal totalPedido = 0;
            using (var cmd = new MySqlCommand(queryC2, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string prodNombre = reader.GetString(0);
                    int cantidad = reader.GetInt32(1);
                    decimal precioUnitario = reader.GetDecimal(2);
                    decimal subtotal = cantidad * precioUnitario;
                    totalPedido += subtotal;

                    Console.WriteLine($"{prodNombre} x{cantidad} @ ${precioUnitario:F2} = ${subtotal:F2}");
                }
            }
            Console.WriteLine($"TOTAL pedido #1: ${totalPedido:F2}");

            // --- [U1] ---
            string queryU1 = "UPDATE productos SET precio = precio * 1.10 WHERE categoria_id = @catId;";
            using (var cmd = new MySqlCommand(queryU1, conn))
            {
                cmd.Parameters.AddWithValue("@catId", 1);
                int filasAfectadas = cmd.ExecuteNonQuery();
                Console.WriteLine($"\n[U1] Subí 10% precios de categoría #1 -> {filasAfectadas} filas.");
            }

            // --- [D1] ---
            string queryD1 = "DELETE FROM detalle_pedido WHERE pedido_id = @pedidoId AND producto_id = @productoId;";
            using (var cmd = new MySqlCommand(queryD1, conn))
            {
                cmd.Parameters.AddWithValue("@pedidoId", 1);
                cmd.Parameters.AddWithValue("@productoId", 2);
                int filasAfectadas = cmd.ExecuteNonQuery();
                Console.WriteLine($"[D1] Borré línea (pedido 1, producto 2) -> {filasAfectadas} filas.");
            }
        }
    }

    public void DemostrarRollback()
    {
        using (var conn = new MySqlConnection(ConnApp))
        {
            conn.Open();
            int pedidosIniciales = 0;
            using (var cmdCount = new MySqlCommand("SELECT COUNT(*) FROM pedidos;", conn))
            {
                pedidosIniciales = Convert.ToInt32(cmdCount.ExecuteScalar());
            }

            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    string sqlInsertValido = "INSERT INTO pedidos (cliente_id, fecha) VALUES (@clienteId, @fecha);";
                    using (var cmd = new MySqlCommand(sqlInsertValido, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@clienteId", 1);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }

                    string sqlInvalido = "INSERT INTO tabla_inexistente_que_falla_adrede VALUES (1,2,3);";
                    using (var cmdFalla = new MySqlCommand(sqlInvalido, conn, tx))
                    {
                        cmdFalla.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Console.WriteLine($"\n[Rollback Exitoso] Se capturó excepción esperada: {ex.Message}");
                }
            }

            using (var cmdCountFinal = new MySqlCommand("SELECT COUNT(*) FROM pedidos;", conn))
            {
                int pedidosFinales = Convert.ToInt32(cmdCountFinal.ExecuteScalar());
                Console.WriteLine($"Pedidos antes de la falla: {pedidosIniciales} | Pedidos después del rollback: {pedidosFinales}");
                if (pedidosIniciales == pedidosFinales)
                {
                    Console.WriteLine("Resultado: Atomicidad comprobada al 100%. No se guardaron datos parciales.");
                }
            }
        }
    }
}