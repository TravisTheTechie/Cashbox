// Copyright 2010 Travis Smith
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Cashbox.Implementations
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using Community.CsharpSqlite.SQLiteClient;
	using Magnum;
	using Magnum.Channels;
	using Magnum.Extensions;
	using Magnum.Fibers;
	using Messages;


	public class SqliteEngine :
		Engine
	{
		readonly SqliteConnection _connection;
		readonly Fiber _fiber = new SynchronousFiber();
		readonly ChannelAdapter _input = new ChannelAdapter();
		readonly ChannelConnection _subscription;

		public SqliteEngine(string filename)
		{
			_connection = new SqliteConnection("Uri=file:{0}, Version=3".FormatWith(filename));

			_subscription = _input.Connect(config =>
				{
					config.AddConsumerOf<Request<Startup>>()
						.UsingConsumer(Startup)
						.HandleOnFiber(_fiber);

					config.AddConsumerOf<Request<Shutdown>>()
						.UsingConsumer(Shutdown)
						.HandleOnFiber(_fiber);

					config.AddConsumerOf<RemoveValue>()
						.UsingConsumer(RemoveKeyFromSession)
						.HandleOnFiber(_fiber);

					config.AddConsumerOf<Request<RetrieveValue>>()
						.UsingConsumer(RetrieveValue)
						.HandleOnFiber(_fiber);

					config.AddConsumerOf<Request<ListValuesForType>>()
						.UsingConsumer(RetrieveListFromType)
						.HandleOnFiber(_fiber);

					config.AddConsumerOf<StoreValue>()
						.UsingConsumer(StoreValue)
						.HandleOnFiber(_fiber);
				});
		}

		public void Dispose()
		{
			_connection.Dispose();
			_subscription.Disconnect();
		}

		public TResponse MakeRequest<TRequest, TResponse>(TRequest message) where TRequest : CashboxMessage
		{
			var response = new Future<TResponse>();
			var channel = new ChannelAdapter();
			Exception ex = null;

			using (channel.Connect(config =>
				{
					config.AddConsumerOf<ReturnValue<TResponse>>()
						.UsingConsumer(msg => response.Complete(msg.Value));

					config.AddConsumerOf<ReturnException>()
						.UsingConsumer(msg => ex = msg.Exception);
				}))
			{
				_input.Request(message, channel);

				if (!response.WaitUntilCompleted(1.Minutes()) && ex != null)
					throw ex;

				return response.Value;
			}
		}

		public void Send<T>(T message) where T : CashboxMessage
		{
			_input.Send(message);
		}

		void StoreValue(StoreValue message)
		{
			using (SqliteCommand cmd = _connection.CreateCommand())
			{
				cmd.CommandText = "INSERT INTO store (key, value) VALUES (@key, @value)";
				cmd.CommandType = CommandType.Text;
				cmd.Parameters.Add("@key", message.Key);
				cmd.Parameters.Add("@value", message.Value);
				cmd.ExecuteNonQuery();
			}
		}

		void RetrieveListFromType(Request<ListValuesForType> message)
		{
			try
			{
				var items = new List<string>();
				using (SqliteCommand cmd = _connection.CreateCommand())
				{
					cmd.CommandText = "SELECT value FROM store WHERE key LIKE @key || '%'";
					cmd.CommandType = CommandType.Text;
					cmd.Parameters.Add("@key", message.Body.Key);
					using (SqliteDataReader dr = cmd.ExecuteReader())
					{
						while (dr.Read())
							items.Add(Convert.ToString(dr["value"]));
					}
				}
				Respond(message, items);
			}
			catch (Exception ex)
			{
				RespondWithException(message, ex);
			}
		}

		void RetrieveValue(Request<RetrieveValue> message)
		{
			try
			{
				using (SqliteCommand cmd = _connection.CreateCommand())
				{
					cmd.CommandText = "SELECT value FROM store WHERE key = @key";
					cmd.CommandType = CommandType.Text;
					cmd.Parameters.Add("@key", message.Body.Key);
					using (SqliteDataReader dr = cmd.ExecuteReader())
					{
						string value = null;
						if (dr.Read())
							value = dr.GetString(0);
						Respond(message, value);
					}
				}
			}
			catch (Exception ex)
			{
				RespondWithException(message, ex);
			}
		}

		void RemoveKeyFromSession(RemoveValue message)
		{
			using (SqliteCommand cmd = _connection.CreateCommand())
			{
				cmd.CommandText = "DELETE FROM store WHERE key = @key";
				cmd.CommandType = CommandType.Text;
				cmd.Parameters.Add("@key", message.Key);
				cmd.ExecuteNonQuery();
			}
		}

		void Startup(Request<Startup> message)
		{
			try
			{
				_connection.Open();
				Int64 exists;

				using (SqliteCommand cmd = _connection.CreateCommand())
				{
					cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table'";
					cmd.CommandType = CommandType.Text;
					exists = (Int64)cmd.ExecuteScalar();
				}

				if (exists == 0)
				{
					using (SqliteCommand cmd = _connection.CreateCommand())
					{
						cmd.CommandText = "CREATE TABLE store (key TEXT PRIMARY KEY, value TEXT);";
						cmd.CommandType = CommandType.Text;
						cmd.ExecuteNonQuery();
					}
				}

				Respond(message, string.Empty);
			}
			catch (Exception ex)
			{
				RespondWithException(message, ex);
			}
		}

		void Shutdown(Request<Shutdown> message)
		{
			try
			{
				_connection.Close();
				_subscription.Disconnect();
				Respond(message, string.Empty);
			}
			catch (Exception ex)
			{
				RespondWithException(message, ex);
			}
		}

		void Respond<T, TK>(Request<TK> message, T response)
		{
			message.ResponseChannel.Send(new ReturnValue<T>
				{
					Value = response
				});
		}

		void RespondWithException<T>(Request<T> message, Exception ex)
		{
			message.ResponseChannel.Send(new ReturnException
				{
					Exception = new SqliteEngineException("Error with {0}".FormatWith(typeof(T).Name), ex)
				});
		}
	}
}