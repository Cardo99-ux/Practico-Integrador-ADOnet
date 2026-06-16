using System;

namespace Practico.Starter.Datos;

public static class FabricaDeMotor
{
    public static IAccesoDatos Crear(Motor motor)
    {
        return motor switch
        {
            Motor.Postgresql => new AccesoPostgres(),
            Motor.SqlServer => new AccesoSqlServer(),
            Motor.MySql => new AccesoMySql(),
            _ => throw new ArgumentException("Motor de base de datos no soportado.")
        };
    }
}