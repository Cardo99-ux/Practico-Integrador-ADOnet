using System;
using Npgsql;

namespace Practico.Starter.Datos;

public class AccesoPostgres : IAccesoDatos
{
    private const string ConnAdmin = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres;";
    private const string ConnApp = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=practico;";

    public void CrearEstructura()
    {
        // 1. Conectarse a la base de administración 'postgres' para verificar/crear la base del práctico
        using (var adminConn = new NpgsqlConnection(ConnAdmin))
        {
            adminConn.Open();
            
            // PostgreSQL no tiene 'CREATE DATABASE IF NOT EXISTS', consultamos el catálogo de sistema pg_database
            string checkDbSql = "SELECT COUNT(*) FROM pg_database WHERE datname = 'practico';";
            bool dbExiste = false;
            using (var checkCmd = new NpgsqlCommand(checkDbSql, adminConn))
            {
                dbExiste = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            }

            if (!dbExiste)
            {
                using (var createCmd = new NpgsqlCommand("CREATE DATABASE practico;", adminConn))
                {
                    createCmd.ExecuteNonQuery();
                }
                Console.WriteLine("Base 'practico' creada.");
            }
            else
            {
                Console.WriteLine("Base 'practico' ya existe.");
            }
        }

        // 2. Conectarse a la base 'practico' para armar las tablas (re-ejecutable y fuera de transacción)
        using (var appConn = new NpgsqlConnection(ConnApp))
        {
            appConn.Open();

            string ddlDrop = @"
                DROP TABLE IF EXISTS public.detalle_pedido CASCADE;
                DROP TABLE IF EXISTS public.pedidos CASCADE;
                DROP TABLE IF EXISTS public.productos CASCADE;
                DROP TABLE IF EXISTS public.clientes CASCADE;
                DROP TABLE IF EXISTS public.categorias CASCADE;
            ";

            using (var dropCmd = new NpgsqlCommand(ddlDrop, appConn))
            {
                dropCmd.ExecuteNonQuery();
            }

            // Estructuras exactas alineadas a tu script laboratorio-sql-postgresql.sql
            string ddlCreate = @"
                CREATE TABLE public.categorias (
                    id SERIAL PRIMARY KEY,
                    nombre VARCHAR(60) NOT NULL UNIQUE
                );

                CREATE TABLE public.clientes (
                    id SERIAL PRIMARY KEY,
                    nombre VARCHAR(80) NOT NULL,
                    email VARCHAR(120) NOT NULL UNIQUE
                );

                CREATE TABLE public.productos (
                    id SERIAL PRIMARY KEY,
                    nombre VARCHAR(100) NOT NULL,
                    precio DECIMAL(10,2) NOT NULL,
                    stock INT NOT NULL DEFAULT 0,
                    categoria_id INT NOT NULL,
                    CONSTRAINT fk_producto_categoria FOREIGN KEY (categoria_id) REFERENCES public.categorias(id) ON DELETE RESTRICT
                );

                CREATE TABLE public.pedidos (
                    id SERIAL PRIMARY KEY,
                    cliente_id INT NOT NULL,
                    fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT fk_pedido_cliente FOREIGN KEY (cliente_id) REFERENCES public.clientes(id) ON DELETE CASCADE
                );

                CREATE TABLE public.detalle_pedido (
                    pedido_id INT NOT NULL,
                    producto_id INT NOT NULL,
                    cantidad INT NOT NULL,
                    precio_unit DECIMAL(10,2) NOT NULL, -- Columna exacta del laboratorio
                    PRIMARY KEY (pedido_id, producto_id),
                    CONSTRAINT fk_detalle_pedido FOREIGN KEY (pedido_id) REFERENCES public.pedidos(id) ON DELETE CASCADE,
                    CONSTRAINT fk_detalle_producto FOREIGN KEY (producto_id) REFERENCES public.productos(id) ON DELETE RESTRICT
                );
            ";

            using (var createCmd = new NpgsqlCommand(ddlCreate, appConn))
            {
                createCmd.ExecuteNonQuery();
            }
            Console.WriteLine("Estructura (5 tablas) creada.");
        }
    }

    public void InsertarDatosPrueba()
    {
        using (var conn = new NpgsqlConnection(ConnApp))
        {
            conn.Open();
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // --- 1. CATEGORÍAS (Uso nativo de RETURNING id) ---
                    int catElectronicaId, catLibrosId, catHogarId;
                    string sqlCat = "INSERT INTO public.categorias (nombre) VALUES (@nombre) RETURNING id;";
                    using (var cmd = new NpgsqlCommand(sqlCat, conn, tx))
                    {
                        cmd.Parameters.Add("@nombre", NpgsqlTypes.NpgsqlDbType.Varchar);

                        cmd.Parameters["@nombre"].Value = "Electrónica";
                        catElectronicaId = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.Parameters["@nombre"].Value = "Libros";
                        catLibrosId = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.Parameters["@nombre"].Value = "Hogar";
                        catHogarId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // --- 2. PRODUCTOS ---
                    int prodNotebook, prodMouse, prodTeclado, prodCleanCode, prodLampara;
                    string sqlProd = "INSERT INTO public.productos (nombre, precio, stock, categoria_id) VALUES (@nombre, @precio, @stock, @catId) RETURNING id;";
                    using (var cmd = new NpgsqlCommand(sqlProd, conn, tx))
                    {
                        cmd.Parameters.Add("@nombre", NpgsqlTypes.NpgsqlDbType.Varchar);
                        cmd.Parameters.Add("@precio", NpgsqlTypes.NpgsqlDbType.Numeric);
                        cmd.Parameters.Add("@stock", NpgsqlTypes.NpgsqlDbType.Integer);
                        cmd.Parameters.Add("@catId", NpgsqlTypes.NpgsqlDbType.Integer);

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
                    string sqlCli = "INSERT INTO public.clientes (nombre, email) VALUES (@nombre, @email) RETURNING id;";
                    using (var cmd = new NpgsqlCommand(sqlCli, conn, tx))
                    {
                        cmd.Parameters.Add("@nombre", NpgsqlTypes.NpgsqlDbType.Varchar);
                        cmd.Parameters.Add("@email", NpgsqlTypes.NpgsqlDbType.Varchar);

                        cmd.Parameters["@nombre"].Value = "Juan Perez"; cmd.Parameters["@email"].Value = "juan@email.com";
                        cliJuan = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.Parameters["@nombre"].Value = "Maria Gomez"; cmd.Parameters["@email"].Value = "maria@email.com";
                        cliMaria = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // --- 4. PEDIDOS Y DETALLES ---
                    string sqlPed = "INSERT INTO public.pedidos (cliente_id, fecha) VALUES (@clienteId, @fecha) RETURNING id;";
                    string sqlDet = "INSERT INTO public.detalle_pedido (pedido_id, producto_id, cantidad, precio_unit) VALUES (@pedidoId, @productoId, @cantidad, @precioUnit);";

                    int ped1Id;
                    using (var cmd = new NpgsqlCommand(sqlPed, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@clienteId", cliJuan);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        ped1Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    ExecuteDetallePostgres(conn, tx, sqlDet, ped1Id, prodMouse, 2, 12000.00m);
                    ExecuteDetallePostgres(conn, tx, sqlDet, ped1Id, prodNotebook, 1, 850000.00m);
                    ExecuteDetallePostgres(conn, tx, sqlDet, ped1Id, prodTeclado, 1, 35000.00m);

                    int ped2Id;
                    using (var cmd = new NpgsqlCommand(sqlPed, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@clienteId", cliMaria);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        ped2Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    ExecuteDetallePostgres(conn, tx, sqlDet, ped2Id, prodCleanCode, 1, 28000.00m);
                    ExecuteDetallePostgres(conn, tx, sqlDet, ped2Id, prodLampara, 2, 15000.00m);

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

    private void ExecuteDetallePostgres(NpgsqlConnection c, NpgsqlTransaction t, string sql, int pedId, int prodId, int cant, decimal precio)
    {
        using (var cmd = new NpgsqlCommand(sql, c, t))
        {
            cmd.Parameters.AddWithValue("@pedidoId", pedId);
            cmd.Parameters.AddWithValue("@productoId", prodId);
            cmd.Parameters.AddWithValue("@cantidad", cant);
            cmd.Parameters.AddWithValue("@precioUnit", precio);
            cmd.ExecuteNonQuery();
        }
    }

    public void EjecutarOperaciones()
    {
        using (var conn = new NpgsqlConnection(ConnApp))
        {
            conn.Open();
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // --- [C1] ---
                    Console.WriteLine("[C1] Productos con su categoría:");
                    string queryC1 = @"
                        SELECT p.id, p.nombre, p.precio, c.nombre AS categoria_nombre 
                        FROM public.productos p 
                        INNER JOIN public.categorias c ON p.categoria_id = c.id 
                        ORDER BY p.id;";

                    using (var cmd = new NpgsqlCommand(queryC1, conn, tx))
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
                        SELECT p.nombre, dp.cantidad, dp.precio_unit 
                        FROM public.detalle_pedido dp
                        INNER JOIN public.productos p ON dp.producto_id = p.id
                        WHERE dp.pedido_id = 1
                        ORDER BY p.nombre DESC;";

                    decimal totalPedido = 0;
                    using (var cmd = new NpgsqlCommand(queryC2, conn, tx))
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
                    string queryU1 = "UPDATE public.productos SET precio = precio * 1.10 WHERE categoria_id = @catId;";
                    using (var cmd = new NpgsqlCommand(queryU1, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@catId", 1);
                        int filasAfectadas = cmd.ExecuteNonQuery();
                        Console.WriteLine($"\n[U1] Subí 10% precios de categoría #1 -> {filasAfectadas} filas.");
                    }

                    // --- [D1] ---
                    string queryD1 = "DELETE FROM public.detalle_pedido WHERE pedido_id = @pedidoId AND producto_id = @productoId;";
                    using (var cmd = new NpgsqlCommand(queryD1, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@pedidoId", 1);
                        cmd.Parameters.AddWithValue("@productoId", 2);
                        int filasAfectadas = cmd.ExecuteNonQuery();
                        Console.WriteLine($"[D1] Borré línea (pedido 1, producto 2) -> {filasAfectadas} filas.");
                    }

                    tx.Commit();
                    Console.WriteLine("Operaciones confirmadas (commit).");
                }
                catch (Exception)
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
    }

    public void DemostrarRollback()
    {
        using (var conn = new NpgsqlConnection(ConnApp))
        {
            conn.Open();

            // 1. Obtener precio ANTES
            decimal precioAntes = 0;
            string queryGet = "SELECT precio FROM public.productos WHERE id = 1;";
            using (var cmd = new NpgsqlCommand(queryGet, conn))
            {
                precioAntes = Convert.ToDecimal(cmd.ExecuteScalar());
            }
            Console.WriteLine($"Precio del producto #1 ANTES: ${precioAntes:F2}");

            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // 2. Modificar el precio a $1.00 provisionalmente
                    string sqlUpdate = "UPDATE public.productos SET precio = 1.00 WHERE id = 1;";
                    using (var cmd = new NpgsqlCommand(sqlUpdate, conn, tx))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    Console.WriteLine("UPDATE aplicado (precio -> 1) dentro de la transacción.");

                    // 3. Forzar el error de sintaxis exigido
                    string sqlFalla = "FORCE_ERROR_HERE;";
                    using (var cmdFalla = new NpgsqlCommand(sqlFalla, conn, tx))
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

            // 4. Obtener precio DESPUÉS para validar la atomicidad
            decimal precioDespues = 0;
            using (var cmd = new NpgsqlCommand(queryGet, conn))
            {
                precioDespues = Convert.ToDecimal(cmd.ExecuteScalar());
            }
            
            Console.WriteLine($"Precio del producto #1 DESPUÉS: ${precioDespues:F2} {(precioAntes == precioDespues ? "OK: el rollback funcionó, el dato NO cambió." : "ERROR")}");
        }
    }
}