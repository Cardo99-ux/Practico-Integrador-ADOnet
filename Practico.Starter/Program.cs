using System;
using Practico.Starter.Datos;

namespace Practico.Starter;

class Program
{
    static void Main(string[] args)
    {
        Motor? motorSeleccionado = null;

        // RF1 - Validación de Argumentos 
        if (args.Length > 0)
        {
            motorSeleccionado = args[0].ToLower() switch
            {
                "postgres" => Motor.Postgresql,
                "sqlserver" => Motor.SqlServer,
                "mysql" => Motor.MySql,
                _ => null
            };

            if (motorSeleccionado == null)
            {
                Console.WriteLine("Argumento inválido. Use: postgres, sqlserver o mysql");
                return;
            }
        }
        else // RF1 - Menú Interactivo 
        {
            Console.WriteLine("===== SELECCIÓN DE MOTOR DE BASE DE DATOS =====");
            Console.WriteLine("1. PostgreSQL");
            Console.WriteLine("2. SQL Server");
            Console.WriteLine("3. MySQL");
            Console.Write("Seleccione una opción (1-3): ");
            
            string? opcion = Console.ReadLine();
            motorSeleccionado = opcion switch
            {
                "1" => Motor.Postgresql,
                "2" => Motor.SqlServer,
                "3" => Motor.MySql,
                _ => null
            };

            if (motorSeleccionado == null)
            {
                Console.WriteLine("Opción no válida. Saliendo del programa.");
                return;
            }
        }

        Console.WriteLine($"\n===== MOTOR: {motorSeleccionado} =====");

        try
        {
            // Instanciación por Factory 
            IAccesoDatos acceso = FabricaDeMotor.Crear(motorSeleccionado.Value);

            // Flujo secuencial mandatorio de ejecución 
            Console.WriteLine("\nRF2 — Crear estructura...");
            acceso.CrearEstructura();

            Console.WriteLine("\nRF3 — Insertar datos de prueba...");
            acceso.InsertarDatosPrueba();

            Console.WriteLine("\nRF4 — Ejecutar operaciones (C1, C2, U1, D1)...");
            acceso.EjecutarOperaciones();

            Console.WriteLine("\nRF5 — Demostrar rollback...");
            acceso.DemostrarRollback();

            Console.WriteLine($"\n===== FIN ({motorSeleccionado}) =====");
        }
        catch (NotImplementedException ex)
        {
            Console.WriteLine($"\n[Falta Desarrollar]: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR GENERAL]: {ex.Message}");
        }
    }
}