// Copyright (c) 2010 Travis Smith
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
namespace Cashbox.Engines
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using Community.CsharpSqlite.SQLiteClient;
	using log4net;
	using Messages;
	using Stact;
	using Stact.Internal;


	public class SqliteEngine :
		Engine
	{
		static readonly ILog _logger = LogManager.GetLogger("Cashbox.Engines.SqliteEngine");

		readonly SqliteConnection _connection;
		readonly Fiber _fiber = new ThreadFiber();
		readonly ChannelAdapter _input = new ChannelAdapter();
		readonly ChannelConnection _subscription;

		public SqliteEngine(string filename)
		{
			_connection = new SqliteConnection(string.Format("Uri=file:{0}, Version=3", filename));

			_subscription = _input.Connect(config =>
				{
					config.AddConsumerOf<Request<Startup>>()
						.UsingConsumer(Startup)
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
			_fiber.Shutdown(TimeSpan.FromSeconds(120));
			_subscription.Disconnect();
			_connection.Close();

			_subscription.Disconnect();
			_connection.Dispose();
		}

		public TResponse MakeRequest<TRequest, TResponse>(TRequest message) where TRequest : CashboxMessage
		{
			var response = new Magnum.Future<TResponse>();
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

				if (!response.WaitUntilCompleted(TimeSpan.FromSeconds(180)) && ex != null)
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
						cmd.CommandText = "CREATE TABLE store (key TEXT PRIMARY KEY ON CONFLICT REPLACE, value TEXT);";
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

		static void Respond<T, TK>(Request<TK> message, T response)
		{
			message.ResponseChannel.Send(new ReturnValue<T>
				{
					Value = response
				});
		}

		static void RespondWithException<T>(Request<T> message, Exception ex)
		{
			message.ResponseChannel.Send(new ReturnException
				{
					Exception = new SqliteEngineException(string.Format("Error with {0}", typeof(T).Name), ex)
				});
		}
	}
}