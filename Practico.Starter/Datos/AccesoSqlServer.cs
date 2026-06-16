using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Practico.Starter.Datos;

public class AccesoSqlServer : IAccesoDatos
{
    private const string ConnAdmin = "Server=localhost,1433;User Id=sa;Password=Curso.NET2026;Database=master;TrustServerCertificate=True;";
    private const string ConnApp = "Server=localhost,1433;User Id=sa;Password=Curso.NET2026;Database=practico;TrustServerCertificate=True;";

    public void CrearEstructura()
    {
        // 1. Conectarse a master para verificar/crear la base
        using (var adminConn = new SqlConnection(ConnAdmin))
        {
            adminConn.Open();

            // Este script verifica si existe. Si existe, la pone en modo SINGLE_USER para cerrar 
            // conexiones viejas en el pool de .NET, la borra y la crea limpia.
            string sqlDb = @"
                IF DB_ID('practico') IS NOT NULL
                BEGIN
                    ALTER DATABASE practico SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE practico;
                END
                CREATE DATABASE practico;";

            using (var cmd = new SqlCommand(sqlDb, adminConn))
            {
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("Base 'practico' recreada limpiamente en SQL Server.");
        }

        // Forzamos limpiar el pool de conexiones de .NET para que no use una conexión vieja a 'master'
        SqlConnection.ClearAllPools();

        // 2. Conectarse a practico para armar las tablas en el esquema 'dbo'
        using (var appConn = new SqlConnection(ConnApp))
        {
            appConn.Open();

            string ddlCreate = @"
                CREATE TABLE dbo.categorias (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    nombre VARCHAR(100) NOT NULL
                );

                CREATE TABLE dbo.productos (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    nombre VARCHAR(150) NOT NULL,
                    precio DECIMAL(12, 2) NOT NULL,
                    stock INT NOT NULL,
                    categoria_id INT NOT NULL,
                    CONSTRAINT fk_productos_categorias FOREIGN KEY (categoria_id) REFERENCES dbo.categorias(id)
                );

                CREATE TABLE dbo.clientes (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    nombre VARCHAR(100) NOT NULL,
                    email VARCHAR(150) NOT NULL
                );

                CREATE TABLE dbo.pedidos (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    cliente_id INT NOT NULL,
                    fecha DATETIME NOT NULL,
                    CONSTRAINT fk_pedidos_clientes FOREIGN KEY (cliente_id) REFERENCES dbo.clientes(id)
                );

                CREATE TABLE dbo.detalle_pedido (
                    pedido_id INT NOT NULL,
                    producto_id INT NOT NULL,
                    cantidad INT NOT NULL,
                    precio_unitario DECIMAL(12, 2) NOT NULL,
                    PRIMARY KEY (pedido_id, producto_id),
                    CONSTRAINT fk_detalle_pedidos FOREIGN KEY (pedido_id) REFERENCES dbo.pedidos(id),
                    CONSTRAINT fk_detalle_productos FOREIGN KEY (producto_id) REFERENCES dbo.productos(id)
                );
            ";

            using (var createCmd = new SqlCommand(ddlCreate, appConn))
            {
                createCmd.ExecuteNonQuery();
            }
            Console.WriteLine("Estructura (5 tablas dbo) creada.");
        }
    }

    public void InsertarDatosPrueba()
    {
        using (var conn = new SqlConnection(ConnApp))
        {
            conn.Open();
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // --- 1. CATEGORÍAS ---
                    int catElectronicaId, catLibrosId, catHogarId;
                    string sqlCat = "INSERT INTO dbo.categorias (nombre) VALUES (@nombre); SELECT SCOPE_IDENTITY();";
                    using (var cmd = new SqlCommand(sqlCat, conn, tx))
                    {
                        cmd.Parameters.Add("@nombre", SqlDbType.VarChar);

                        cmd.Parameters["@nombre"].Value = "Electrónica";
                        catElectronicaId = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.Parameters["@nombre"].Value = "Libros";
                        catLibrosId = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.Parameters["@nombre"].Value = "Hogar";
                        catHogarId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // --- 2. PRODUCTOS ---
                    int prodNotebook, prodMouse, prodTeclado, prodCleanCode, prodLampara;
                    string sqlProd = "INSERT INTO dbo.productos (nombre, precio, stock, categoria_id) VALUES (@nombre, @precio, @stock, @catId); SELECT SCOPE_IDENTITY();";
                    using (var cmd = new SqlCommand(sqlProd, conn, tx))
                    {
                        cmd.Parameters.Add("@nombre", SqlDbType.VarChar);
                        cmd.Parameters.Add("@precio", SqlDbType.Decimal);
                        cmd.Parameters.Add("@stock", SqlDbType.Int);
                        cmd.Parameters.Add("@catId", SqlDbType.Int);

                        cmd.Parameters["@nombre"].Value = "Notebook 14\""; cmd.Parameters["@precio"].Value = 850000.00m; cmd.Parameters["@stock"].Value = 10; cmd.Parameters["@catId"].Value = catElectronicaId;
                        prodNotebook = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.Parameters["@nombre"].Value = "Mouse inalámbrico"; cmd.Parameters["@precio"].Value = 12000.00m; cmd.Parameters["@stock"].Value = 50; cmd.Parameters["@catId"].Value = catElectronicaId;
                        prodMouse = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.Parameters["@nombre"].Value = "Teclado mecánico"; cmd.Parameters["@precio"].Value = 35000.00m; cmd.Parameters["@stock"].Value = 30; cmd.Parameters["@catId"].Value = catElectronicaId;
                        prodTeclado = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.Parameters["@nombre"].Value = "Clean Code"; cmd.Parameters["@precio"].Value = 28000.00m; cmd.Parameters["@stock"].Value = 15; cmd.Parameters["@catId"].Value = catLibrosId;
                        prodCleanCode = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.Parameters["@nombre"].Value = "Lámpara LED escritorio"; cmd.Parameters["@precio"].Value = 15000.00m; cmd.Parameters["@stock"].Value = 25; cmd.Parameters["@catId"].Value = catHogarId;
                        prodLampara = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // --- 3. CLIENTES ---
                    int cliJuan, cliMaria;
                    string sqlCli = "INSERT INTO dbo.clientes (nombre, email) VALUES (@nombre, @email); SELECT SCOPE_IDENTITY();";
                    using (var cmd = new SqlCommand(sqlCli, conn, tx))
                    {
                        cmd.Parameters.Add("@nombre", SqlDbType.VarChar);
                        cmd.Parameters.Add("@email", SqlDbType.VarChar);

                        cmd.Parameters["@nombre"].Value = "Juan Perez"; cmd.Parameters["@email"].Value = "juan@email.com";
                        cliJuan = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.Parameters["@nombre"].Value = "Maria Gomez"; cmd.Parameters["@email"].Value = "maria@email.com";
                        cliMaria = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // --- 4. PEDIDOS Y DETALLES ---
                    string sqlPed = "INSERT INTO dbo.pedidos (cliente_id, fecha) VALUES (@clienteId, @fecha); SELECT SCOPE_IDENTITY();";
                    string sqlDet = "INSERT INTO dbo.detalle_pedido (pedido_id, producto_id, cantidad, precio_unitario) VALUES (@pedidoId, @productoId, @cantidad, @precioUnitario);";

                    int ped1Id;
                    using (var cmd = new SqlCommand(sqlPed, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@clienteId", cliJuan);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        ped1Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    ExecuteDetalleSqlServer(conn, tx, sqlDet, ped1Id, prodMouse, 2, 12000.00m);
                    ExecuteDetalleSqlServer(conn, tx, sqlDet, ped1Id, prodNotebook, 1, 850000.00m);
                    ExecuteDetalleSqlServer(conn, tx, sqlDet, ped1Id, prodTeclado, 1, 35000.00m);

                    int ped2Id;
                    using (var cmd = new SqlCommand(sqlPed, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@clienteId", cliMaria);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        ped2Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    ExecuteDetalleSqlServer(conn, tx, sqlDet, ped2Id, prodCleanCode, 1, 28000.00m);
                    ExecuteDetalleSqlServer(conn, tx, sqlDet, ped2Id, prodLampara, 2, 15000.00m);

                    tx.Commit();
                    Console.WriteLine("Datos de prueba insertados (commit) en SQL Server.");
                }
                catch (Exception)
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
    }

    private void ExecuteDetalleSqlServer(SqlConnection c, SqlTransaction t, string sql, int pedId, int prodId, int cant, decimal precio)
    {
        using (var cmd = new SqlCommand(sql, c, t))
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
        using (var conn = new SqlConnection(ConnApp))
        {
            conn.Open();

            // --- [C1] ---
            Console.WriteLine("[C1] Productos con su categoría:");
            string queryC1 = @"
                SELECT p.id, p.nombre, p.precio, c.nombre AS categoria_nombre 
                FROM productos p 
                INNER JOIN categorias c ON p.categoria_id = c.id 
                ORDER BY p.id;";

            using (var cmd = new SqlCommand(queryC1, conn))
            using (var reader = cmd.ExecuteReader())
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

            // --- [C2] ---
            Console.WriteLine("\n[C2] Detalle y total del pedido #1:");
            string queryC2 = @"
                SELECT p.nombre, dp.cantidad, dp.precio_unitario 
                FROM detalle_pedido dp
                INNER JOIN productos p ON dp.producto_id = p.id
                WHERE dp.pedido_id = 1
                ORDER BY p.nombre DESC;";

            decimal totalPedido = 0;
            using (var cmd = new SqlCommand(queryC2, conn))
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
            using (var cmd = new SqlCommand(queryU1, conn))
            {
                cmd.Parameters.AddWithValue("@catId", 1);
                int filasAfectadas = cmd.ExecuteNonQuery();
                Console.WriteLine($"\n[U1] Subí 10% precios de categoría #1 -> {filasAfectadas} filas.");
            }

            // --- [D1] ---
            string queryD1 = "DELETE FROM detalle_pedido WHERE pedido_id = @pedidoId AND producto_id = @productoId;";
            using (var cmd = new SqlCommand(queryD1, conn))
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
        using (var conn = new SqlConnection(ConnApp))
        {
            conn.Open();

            decimal precioAntes = 0;
            string queryGet = "SELECT precio FROM productos WHERE id = 1;";
            using (var cmd = new SqlCommand(queryGet, conn))
            {
                precioAntes = Convert.ToDecimal(cmd.ExecuteScalar());
            }
            Console.WriteLine($"Precio del producto #1 ANTES: ${precioAntes:F2}");

            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    string sqlUpdate = "UPDATE productos SET precio = 1.00 WHERE id = 1;";
                    using (var cmd = new SqlCommand(sqlUpdate, conn, tx))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    Console.WriteLine("UPDATE aplicado (precio -> 1) dentro de la transacción.");

                    string sqlFalla = "FORCE_ERROR_HERE;";
                    using (var cmdFalla = new SqlCommand(sqlFalla, conn, tx))
                    {
                        cmdFalla.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
                catch (Exception)
                {
                    tx.Rollback();
                    Console.WriteLine("Excepción capturada -> ROLLBACK. (Error simulado: algo salió mal.)");
                }
            }

            decimal precioDespues = 0;
            using (var cmd = new SqlCommand(queryGet, conn))
            {
                precioDespues = Convert.ToDecimal(cmd.ExecuteScalar());
            }
            
            Console.WriteLine($"Precio del producto #1 DESPUÉS: ${precioDespues:F2} {(precioAntes == precioDespues ? "OK: el rollback funcionó, el dato NO cambió." : "ERROR")}");
        }
    }
}