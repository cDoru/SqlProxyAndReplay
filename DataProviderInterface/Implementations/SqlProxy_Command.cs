﻿using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using ProductiveRage.SqlProxyAndReplay.DataProviderInterface.IDs;
using ProductiveRage.SqlProxyAndReplay.DataProviderInterface.Interfaces;

namespace ProductiveRage.SqlProxyAndReplay.DataProviderInterface.Implementations
{
	public sealed partial class SqlProxy : ISqlProxy
	{
		public string GetCommandText(CommandId commandId) { return _commandStore.Get(commandId).CommandText; }
		public void SetCommandText(CommandId commandId, string value) { _commandStore.Get(commandId).CommandText = value; }

		public int GetCommandTimeout(CommandId commandId) { return _commandStore.Get(commandId).CommandTimeout; }
		public void SetCommandTimeout(CommandId commandId, int value) { _commandStore.Get(commandId).CommandTimeout = value; }

		public CommandType GetCommandType(CommandId commandId) { return _commandStore.Get(commandId).CommandType; }
		public void SetCommandType(CommandId commandId, CommandType value) { _commandStore.Get(commandId).CommandType = value; }

		public ConnectionId? GetConnection(CommandId commandId)
		{
			var command = _commandStore.Get(commandId);
			if (command.Connection == null)
				return null;
			var sqlConnection = command.Connection as SqlConnection;
			if (sqlConnection == null)
				throw new Exception("All connnections should be of type SqlConnection, but this one is \"" + command.Connection.GetType() + "\")");
			return _connectionStore.GetIdFor(sqlConnection);
		}
		public void SetConnection(CommandId commandId, ConnectionId? connectionId)
		{
			var command = _commandStore.Get(commandId);
			if (connectionId == null)
				command.Connection = null;
			else
				command.Connection = _connectionStore.Get(connectionId.Value);
		}

		public ParameterId CreateParameter(CommandId commandId)
		{
			// Note: We need to track created parameters and tidy them up when the command is disposed (because parameters are not disposable and so
			// we won't be told via a Dispose call when parameters are no longer required; we know that they're no longer required when the command
			// that owns them is disposed, though)
			var parameterId = _parameterStore.Add(_commandStore.Get(commandId).CreateParameter());
			_parametersToTidy.AddOrUpdate(
				commandId,
				addValueFactory: key => { var value = new ConcurrentBag<ParameterId>(); value.Add(parameterId); return value; },
				updateValueFactory: (key, currentValue) => { currentValue.Add(parameterId); return currentValue; }
			);
			return parameterId;
		}

		public TransactionId? GetTransaction(CommandId commandId)
		{
			var command = _commandStore.Get(commandId);
			if (command.Transaction == null)
				return null;
			var sqlTransaction = command.Transaction as SqlTransaction;
			if (sqlTransaction == null)
				throw new Exception("All connnections should be of type SqlTransaction, but this one is \"" + command.Transaction.GetType() + "\")");
			return _transactionStore.GetIdFor(sqlTransaction);
		}

		public void SetTransaction(CommandId commandId, TransactionId? transactionId)
		{
			var command = _commandStore.Get(commandId);
			if (transactionId == null)
				command.Transaction = null;
			else
				command.Transaction = _transactionStore.Get(transactionId.Value);
		}

		public UpdateRowSource GetUpdatedRowSource(CommandId commandId) { return _commandStore.Get(commandId).UpdatedRowSource; }
		public void SetUpdatedRowSource(CommandId commandId, UpdateRowSource value) { _commandStore.Get(commandId).UpdatedRowSource = value; }

		public void Prepare(CommandId commandId)
		{
			_commandStore.Get(commandId).Prepare();
		}
		public void Cancel(CommandId commandId)
		{
			_commandStore.Get(commandId).Cancel();
		}
		public void Dispose(CommandId commandId)
		{
			// Parameters are not individually disposed of when use of them is complete - instead, the entries from the parameter store must
			// be removed when the command that created them is disposed of
			_commandStore.Get(commandId).Dispose();
			ConcurrentBag<ParameterId> parametersToTidy;
			if (!_parametersToTidy.TryRemove(commandId, out parametersToTidy))
				throw new Exception("Unable to locate parametersToTidy for command being disposed");
			foreach (var parameterId in parametersToTidy)
				_parameterStore.Remove(parameterId);
			_commandStore.Remove(commandId);
		}

		public int ExecuteNonQuery(CommandId commandId)
		{
			return _commandStore.Get(commandId).ExecuteNonQuery();
		}
		public object ExecuteScalar(CommandId commandId)
		{
			return _commandStore.Get(commandId).ExecuteScalar();
		}
		public DataReaderId ExecuteReader(CommandId commandId, CommandBehavior behavior = CommandBehavior.Default)
		{
			var reader = _commandStore.Get(commandId).ExecuteReader(behavior);
			try
			{
				return _readerStore.Add(reader);
			}
			catch
			{
				reader.Dispose();
				throw;
			}
		}
	}
}