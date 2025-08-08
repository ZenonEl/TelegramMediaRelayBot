// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using System.Data;
using MySql.Data.MySqlClient;

namespace TelegramMediaRelayBot.Database.UnitOfWork;

public sealed class MySqlUnitOfWork : IUnitOfWork
{
    private readonly string _connectionString;
    private MySqlConnection? _connection;
    private MySqlTransaction? _transaction;
    private bool _disposed;

    public MySqlUnitOfWork(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection Connection => _connection ??= new MySqlConnection(_connectionString);
    public IDbTransaction? Transaction => _transaction;

    public void Begin()
    {
        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open();
        }
        _transaction = ((MySqlConnection)Connection).BeginTransaction();
    }

    public void Commit()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _transaction?.Dispose();
        _connection?.Dispose();
        _disposed = true;
    }
}

