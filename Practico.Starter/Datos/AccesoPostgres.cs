using System;
using Npgsql;

namespace Practico.Starter.Datos;

public class AccesoPostgres : IAccesoDatos
{
    private const string ConnAdmin = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres;";
    private const string ConnApp = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=practico;";

    public void CrearEstructura()
    {
        // 1. Conectarse a la base 'postgres' de administración para verificar/crear la base 'practico'
        using (var adminConn = new NpgsqlConnection(ConnAdmin))
        {
            adminConn.Open();

            // Verificamos si la base de datos ya existe
            string checkDbSql = "SELECT COUNT(*) FROM pg_database WHERE datname = 'practico';";
            using (var checkCmd = new NpgsqlCommand(checkDbSql, adminConn))
            {
                long exists = (long)checkCmd.ExecuteScalar()!;
                if (exists == 0)
                {
                    // Al ser DDL de base de datos en Postgres, se ejecuta directamente
                    string createDbSql = "CREATE DATABASE practico;";
                    using (var createCmd = new NpgsqlCommand(createDbSql, adminConn))
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
        }

        // 2. Conectarse a la nueva base 'practico' para crear las tablas
        using (var appConn = new NpgsqlConnection(ConnApp))
        {
            appConn.Open();

            // DDL Re-ejecutable: Borrar en orden inverso a las dependencias de FK
            string ddlDrop = @"
              DROP TABLE IF EXISTS public.detalle_pedido CASCADE;
              DROP TABLE IF EXISTS public.pedidos CASCADE;
              DROP TABLE IF EXISTS public.clientes CASCADE;
              DROP TABLE IF EXISTS public.productos CASCADE;
              DROP TABLE IF EXISTS public.categorias CASCADE;
              ";

            using (var dropCmd = new NpgsqlCommand(ddlDrop, appConn))
            {
                dropCmd.ExecuteNonQuery();
            }

            // Crear las 5 tablas con sus restricciones y tipos correspondientes de Postgres
            string ddlCreate = @"
    CREATE TABLE public.categorias (
        id SERIAL PRIMARY KEY,
        nombre VARCHAR(100) NOT NULL
    );

    CREATE TABLE public.productos (
        id SERIAL PRIMARY KEY,
        nombre VARCHAR(150) NOT NULL,
        precio NUMERIC(12, 2) NOT NULL,
        stock INT NOT NULL,
        categoria_id INT NOT NULL,
        CONSTRAINT fk_productos_categorias FOREIGN KEY (categoria_id) REFERENCES public.categorias(id)
    );

    CREATE TABLE public.clientes (
        id SERIAL PRIMARY KEY,
        nombre VARCHAR(100) NOT NULL,
        email VARCHAR(150) NOT NULL
    );

    CREATE TABLE public.pedidos (
        id SERIAL PRIMARY KEY,
        cliente_id INT NOT NULL,
        fecha TIMESTAMP NOT NULL,
        CONSTRAINT fk_pedidos_clientes FOREIGN KEY (cliente_id) REFERENCES public.clientes(id)
    );

    CREATE TABLE public.detalle_pedido (
        pedido_id INT NOT NULL,
        producto_id INT NOT NULL,
        cantidad INT NOT NULL,
        precio_unitario NUMERIC(12, 2) NOT NULL,
        PRIMARY KEY (pedido_id, producto_id),
        CONSTRAINT fk_detalle_pedidos FOREIGN KEY (pedido_id) REFERENCES public.pedidos(id),
        CONSTRAINT fk_detalle_productos FOREIGN KEY (producto_id) REFERENCES public.productos(id)
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
            // Iniciamos la transacción única requerida
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // --- 1. INSERTAR CATEGORÍAS ---
                    int catElectronicaId, catLibrosId, catHogarId;

                    string sqlCat = "INSERT INTO categorias (nombre) VALUES (@nombre) RETURNING id;";
                    using (var cmd = new NpgsqlCommand(sqlCat, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@nombre", "Electrónica");
                        catElectronicaId = (int)cmd.ExecuteScalar()!;

                        cmd.Parameters["@nombre"].Value = "Libros";
                        catLibrosId = (int)cmd.ExecuteScalar()!;

                        cmd.Parameters["@nombre"].Value = "Hogar";
                        catHogarId = (int)cmd.ExecuteScalar()!;
                    }

                    // --- 2. INSERTAR PRODUCTOS ---
                    int prodNotebook, prodMouse, prodTeclado, prodCleanCode, prodLampara;

                    string sqlProd = "INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@nombre, @precio, @stock, @catId) RETURNING id;";
                    using (var cmd = new NpgsqlCommand(sqlProd, conn, tx))
                    {
                        cmd.Parameters.Add("@nombre", NpgsqlTypes.NpgsqlDbType.Varchar);
                        cmd.Parameters.Add("@precio", NpgsqlTypes.NpgsqlDbType.Numeric);
                        cmd.Parameters.Add("@stock", NpgsqlTypes.NpgsqlDbType.Integer);
                        cmd.Parameters.Add("@catId", NpgsqlTypes.NpgsqlDbType.Integer);

                        // P1
                        cmd.Parameters["@nombre"].Value = "Notebook 14\""; cmd.Parameters["@precio"].Value = 850000.00m; cmd.Parameters["@stock"].Value = 10; cmd.Parameters["@catId"].Value = catElectronicaId;
                        prodNotebook = (int)cmd.ExecuteScalar()!;
                        // P2
                        cmd.Parameters["@nombre"].Value = "Mouse inalámbrico"; cmd.Parameters["@precio"].Value = 12000.00m; cmd.Parameters["@stock"].Value = 50; cmd.Parameters["@catId"].Value = catElectronicaId;
                        prodMouse = (int)cmd.ExecuteScalar()!;
                        // P3
                        cmd.Parameters["@nombre"].Value = "Teclado mecánico"; cmd.Parameters["@precio"].Value = 35000.00m; cmd.Parameters["@stock"].Value = 30; cmd.Parameters["@catId"].Value = catElectronicaId;
                        prodTeclado = (int)cmd.ExecuteScalar()!;
                        // P4
                        cmd.Parameters["@nombre"].Value = "Clean Code"; cmd.Parameters["@precio"].Value = 28000.00m; cmd.Parameters["@stock"].Value = 15; cmd.Parameters["@catId"].Value = catLibrosId;
                        prodCleanCode = (int)cmd.ExecuteScalar()!;
                        // P5
                        cmd.Parameters["@nombre"].Value = "Lámpara LED escritorio"; cmd.Parameters["@precio"].Value = 15000.00m; cmd.Parameters["@stock"].Value = 25; cmd.Parameters["@catId"].Value = catHogarId;
                        prodLampara = (int)cmd.ExecuteScalar()!;
                    }

                    // --- 3. INSERTAR CLIENTES ---
                    int cliJuan, cliMaria;
                    string sqlCli = "INSERT INTO clientes (nombre, email) VALUES (@nombre, @email) RETURNING id;";
                    using (var cmd = new NpgsqlCommand(sqlCli, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@nombre", "Juan Perez"); cmd.Parameters.AddWithValue("@email", "juan@email.com");
                        cliJuan = (int)cmd.ExecuteScalar()!;

                        cmd.Parameters["@nombre"].Value = "Maria Gomez"; cmd.Parameters["@email"].Value = "maria@email.com";
                        cliMaria = (int)cmd.ExecuteScalar()!;
                    }

                    // --- 4. INSERTAR PEDIDOS Y DETALLES ---
                    string sqlPed = "INSERT INTO pedidos (cliente_id, fecha) VALUES (@clienteId, @fecha) RETURNING id;";
                    string sqlDet = "INSERT INTO detalle_pedido (pedido_id, producto_id, cantidad, precio_unitario) VALUES (@pedidoId, @productoId, @cantidad, @precioUnitario);";

                    // Pedido 1 (Juan Perez) - 3 productos (Notebook x1, Mouse x2, Teclado x1)
                    int ped1Id;
                    using (var cmd = new NpgsqlCommand(sqlPed, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@clienteId", cliJuan);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        ped1Id = (int)cmd.ExecuteScalar()!;
                    }

                    using (var cmd = new NpgsqlCommand(sqlDet, conn, tx))
                    {
                        cmd.Parameters.Add("@pedidoId", NpgsqlTypes.NpgsqlDbType.Integer);
                        cmd.Parameters.Add("@productoId", NpgsqlTypes.NpgsqlDbType.Integer);
                        cmd.Parameters.Add("@cantidad", NpgsqlTypes.NpgsqlDbType.Integer);
                        cmd.Parameters.Add("@precioUnitario", NpgsqlTypes.NpgsqlDbType.Numeric);

                        // Línea 1: Mouse
                        cmd.Parameters["@pedidoId"].Value = ped1Id; cmd.Parameters["@productoId"].Value = prodMouse; cmd.Parameters["@cantidad"].Value = 2; cmd.Parameters["@precioUnitario"].Value = 12000.00m;
                        cmd.Parameters.Clear(); // Evitar conflictos o reusar limpiamente reasignando:
                    }
                    // Nota: Para optimizar código reusaremos comandos de forma directa asignando valores explícitos:
                    ExecuteDetallePostgres(conn, tx, sqlDet, ped1Id, prodMouse, 2, 12000.00m);
                    ExecuteDetallePostgres(conn, tx, sqlDet, ped1Id, prodNotebook, 1, 850000.00m);
                    ExecuteDetallePostgres(conn, tx, sqlDet, ped1Id, prodTeclado, 1, 35000.00m);

                    // Pedido 2 (Maria Gomez) - 2 productos (Clean Code x1, Lámpara x2)
                    int ped2Id;
                    using (var cmd = new NpgsqlCommand(sqlPed, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@clienteId", cliMaria);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        ped2Id = (int)cmd.ExecuteScalar()!;
                    }
                    ExecuteDetallePostgres(conn, tx, sqlDet, ped2Id, prodCleanCode, 1, 28000.00m);
                    ExecuteDetallePostgres(conn, tx, sqlDet, ped2Id, prodLampara, 2, 15000.00m);

                    tx.Commit(); // Confirmamos de forma atómica si todo anduvo bien
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

    // Helper auxiliar local para evitar duplicar código de parámetros en los detalles
    private void ExecuteDetallePostgres(NpgsqlConnection c, NpgsqlTransaction t, string sql, int pedId, int prodId, int cant, decimal precio)
    {
        using (var cmd = new NpgsqlCommand(sql, c, t))
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
        using (var conn = new NpgsqlConnection(ConnApp))
        {
            conn.Open();

            // --- [C1] Productos con su categoría ---
            Console.WriteLine("[C1] Productos con su categoría:");
            string queryC1 = @"
                SELECT p.id, p.nombre, p.precio, c.nombre AS categoria_nombre 
                FROM public.productos p 
                INNER JOIN public.categorias c ON p.categoria_id = c.id 
                ORDER BY p.id;";

            using (var cmd = new NpgsqlCommand(queryC1, conn))
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

            // --- [C2] Detalle y total del pedido #1 ---
            Console.WriteLine("\n[C2] Detalle y total del pedido #1:");
            string queryC2 = @"
                SELECT p.nombre, dp.cantidad, dp.precio_unitario 
                FROM public.detalle_pedido dp
                INNER JOIN public.productos p ON dp.producto_id = p.id
                WHERE dp.pedido_id = 1
                ORDER BY p.nombre DESC;"; // Orden descriptivo según salida esperada

            decimal totalPedido = 0;
            using (var cmd = new NpgsqlCommand(queryC2, conn))
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

            // --- [U1] Subir 10% precios de categoría #1 ---
            string queryU1 = "UPDATE public.productos SET precio = precio * 1.10 WHERE categoria_id = @catId;";
            using (var cmd = new NpgsqlCommand(queryU1, conn))
            {
                cmd.Parameters.AddWithValue("@catId", 1);
                int filasAfectadas = cmd.ExecuteNonQuery();
                Console.WriteLine($"\n[U1] Subí 10% precios de categoría #1 -> {filasAfectadas} filas.");
            }

            // --- [D1] Borrar línea (pedido 1, producto 2) ---
            string queryD1 = "DELETE FROM public.detalle_pedido WHERE pedido_id = @pedidoId AND producto_id = @productoId;";
            using (var cmd = new NpgsqlCommand(queryD1, conn))
            {
                cmd.Parameters.AddWithValue("@pedidoId", 1);
                cmd.Parameters.AddWithValue("@productoId", 2); // ID del mouse inalámbrico
                int filasAfectadas = cmd.ExecuteNonQuery();
                Console.WriteLine($"[D1] Borré línea (pedido 1, producto 2) -> {filasAfectadas} filas.");
            }
        }
    }

    public void DemostrarRollback()
    {
        using (var conn = new NpgsqlConnection(ConnApp))
        {
            conn.Open();

            // Guardamos la cantidad de pedidos iniciales para corroborar el rollback al final
            int pedidosIniciales = 0;
            using (var cmdCount = new NpgsqlCommand("SELECT COUNT(*) FROM public.pedidos;", conn))
            {
                pedidosIniciales = Convert.ToInt32(cmdCount.ExecuteScalar());
            }

            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // 1. Intentamos una inserción válida
                    string sqlInsertValido = "INSERT INTO public.pedidos (cliente_id, fecha) VALUES (@clienteId, @fecha);";
                    using (var cmd = new NpgsqlCommand(sqlInsertValido, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@clienteId", 1);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Provocamos un error de sintaxis SQL a propósito (Falla voluntaria)
                    string sqlInvalido = "INSERT INTO tabla_inexistente_que_falla_adrede VALUES (1,2,3);";
                    using (var cmdFalla = new NpgsqlCommand(sqlInvalido, conn, tx))
                    {
                        cmdFalla.ExecuteNonQuery();
                    }

                    tx.Commit(); // Nunca llegará acá
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Console.WriteLine($"\n[Rollback Exitoso] Se capturó excepción esperada: {ex.Message}");
                }
            }

            // Validamos que la base de datos no haya sufrido cambios parciales
            using (var cmdCountFinal = new NpgsqlCommand("SELECT COUNT(*) FROM public.pedidos;", conn))
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