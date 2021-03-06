﻿using System;
using System.Data;
using ProductiveRage.SqlProxyAndReplay.DataProviderInterface.IDs;
using ProductiveRage.SqlProxyAndReplay.DataProviderInterface.Interfaces;

namespace ProductiveRage.SqlProxyAndReplay.DataProviderClient
{
	public sealed class RemoteSqlConnectionClient : IDbConnection
	{
		private readonly IRemoteSqlConnection _connection;
		private readonly IRemoteSqlCommand _command;
		private readonly IRemoteSqlTransaction _transaction;
		private readonly IRemoteSqlParameterSet _parameters;
		private readonly IRemoteSqlParameter _parameter;
		private readonly IRemoteSqlDataReader _reader;
		private bool _disposed;
		public RemoteSqlConnectionClient(
			IRemoteSqlConnection connection,
			IRemoteSqlCommand command,
			IRemoteSqlTransaction transaction,
			IRemoteSqlParameterSet parameters,
			IRemoteSqlParameter parameter,
			IRemoteSqlDataReader reader,
			ConnectionId connectionId)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (command == null)
				throw new ArgumentNullException(nameof(command));
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));
			if (parameter == null)
				throw new ArgumentNullException(nameof(parameter));
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			_connection = connection;
			_command = command;
			_transaction = transaction;
			_parameters = parameters;
			_parameter = parameter;
			_reader = reader;
			ConnectionId = connectionId;
			_disposed = false;
		}
		public RemoteSqlConnectionClient(ISqlProxy proxy, ConnectionId connectionId) : this(proxy, proxy, proxy, proxy, proxy, proxy, connectionId) { }
		~RemoteSqlConnectionClient()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		private void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
				_connection.Dispose(ConnectionId); // Tell the service that the current Connection is finished with

			_disposed = true;
		}

		internal ConnectionId ConnectionId { get; }

		public string ConnectionString
		{
			get { ThrowIfDisposed(); return _connection.GetConnectionString(ConnectionId); }
			set { ThrowIfDisposed(); _connection.SetConnectionString(ConnectionId, value); }
		}

		public int ConnectionTimeout
		{
			get { ThrowIfDisposed(); return _connection.GetConnectionTimeout(ConnectionId); }
		}

		public string Database
		{
			get { ThrowIfDisposed(); return _connection.GetDatabase(ConnectionId); }
		}
		public ConnectionState State
		{
			get { ThrowIfDisposed(); return _connection.GetState(ConnectionId); }
		}

		public void ChangeDatabase(string databaseName) { ThrowIfDisposed(); _connection.ChangeDatabase(ConnectionId, databaseName); }

		public void Open() { ThrowIfDisposed(); _connection.Open(ConnectionId); }
		public void Close() { ThrowIfDisposed(); _connection.Close(ConnectionId); }
		public RemoteSqlCommandClient CreateCommand()
		{
			ThrowIfDisposed();
			return new RemoteSqlCommandClient(_connection, _command, _transaction, _parameters, _parameter, _reader, _connection.CreateCommand(ConnectionId));
		}
		IDbCommand IDbConnection.CreateCommand() { return CreateCommand(); }

		public IDbTransaction BeginTransaction()
		{
			ThrowIfDisposed();
			return new RemoteSqlTransactionClient(_connection, _command, _transaction, _parameters, _parameter, _reader, _connection.BeginTransaction(ConnectionId));
		}
		public IDbTransaction BeginTransaction(IsolationLevel il)
		{
			ThrowIfDisposed();
			return new RemoteSqlTransactionClient(_connection, _command, _transaction, _parameters, _parameter, _reader, _connection.BeginTransaction(ConnectionId, il));
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException("connection");
		}
	}
}
