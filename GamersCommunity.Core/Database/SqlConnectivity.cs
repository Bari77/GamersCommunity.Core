using System.ComponentModel;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;

namespace GamersCommunity.Core.Database;

/// <summary>
/// Detects transient SQL Server connectivity failures (startup, container reboot, network blips).
/// </summary>
public static class SqlConnectivity
{
    /// <summary>
    /// Returns whether the exception chain indicates SQL is temporarily unreachable.
    /// </summary>
    public static bool IsUnavailable(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case SqlException sql when IsTransientSqlError(sql):
                case SocketException:
                case Win32Exception { NativeErrorCode: 10053 or 10054 or 10060 or 10061 }:
                case InvalidOperationException when current.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase):
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Prefer the inner <see cref="SqlException"/> message when present.
    /// </summary>
    public static string GetErrorMessage(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql)
                return sql.Message;
        }

        return ex.GetBaseException().Message;
    }

    private static bool IsTransientSqlError(SqlException sql) =>
        sql.Class >= 20 || sql.Number is 2 or 53 or 64 or 233 or 4060 or -1 or -2;
}
