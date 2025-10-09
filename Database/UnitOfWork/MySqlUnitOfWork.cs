using System.Data;

namespace TelegramMediaRelayBot.Database.UnitOfWork;

public sealed class MySqlUnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public MySqlUnitOfWork(IDbConnection connection)
    {
        _connection = connection;
    }

    public IDbConnection Connection => _connection;
    public IDbTransaction? Transaction => _transaction;

    public void Begin()
    {
        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open();
        }
        _transaction = Connection.BeginTransaction();
    }

    public void Commit()
    {
        _transaction?.Commit();
        DisposeTransaction();
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        DisposeTransaction();
    }
    
    private void DisposeTransaction()
    {
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        DisposeTransaction();
        
        _disposed = true;
    }
}