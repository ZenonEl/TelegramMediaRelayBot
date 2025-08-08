// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using System.Data;
using Microsoft.Data.Sqlite;

namespace TelegramMediaRelayBot.Database.UnitOfWork;

public sealed class SqliteUnitOfWork : IUnitOfWork
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;
    private SqliteTransaction? _transaction;
    private bool _disposed;

    public SqliteUnitOfWork(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection Connection => _connection ??= new SqliteConnection(_connectionString);
    public IDbTransaction? Transaction => _transaction;

    public void Begin()
    {
        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open();
        }
        _transaction = ((SqliteConnection)Connection).BeginTransaction();
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

