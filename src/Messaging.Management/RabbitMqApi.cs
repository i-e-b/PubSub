using System;
using System.IO;
using System.Net;
using RabbitMQ.Client;
using ServiceStack.Text;

namespace SevenDigital.Messaging.Management
{
	public class RabbitMqApi
	{
		readonly string virtualHost;
		readonly Uri _managementApiHost;
		readonly NetworkCredential _credentials;
		readonly string slashHost;

		public RabbitMqApi(Uri managementApiHost, NetworkCredential credentials)
		{
			_managementApiHost = managementApiHost;
			_credentials = credentials;
		}

		public RabbitMqApi(string hostUri, string username, string password, string virtualHost = "/")
			: this(new Uri(hostUri), new NetworkCredential(username, password))
		{
			this.virtualHost = virtualHost;
			slashHost = (virtualHost.StartsWith("/")) ? (virtualHost) : ("/" + virtualHost);
		}

		public RMQueue[] ListQueues()
		{
			using (var stream = Get("/api"+slashHost+"queues"))
				return JsonSerializer.DeserializeFromStream<RMQueue[]>(stream);
		}

		public RMNode[] ListNodes()
		{
			using (var stream = Get("/api"+slashHost+"nodes"))
				return JsonSerializer.DeserializeFromStream<RMNode[]>(stream);
		}

		public Stream Get(string endpoint)
		{
			Uri result;

			if (Uri.TryCreate(_managementApiHost, endpoint, out result))
			{
				var webRequest = WebRequest.Create(result);
				webRequest.Credentials = _credentials;

				return webRequest.GetResponse().GetResponseStream();
			}

			return null;
		}

		public void PurgeQueue(RMQueue queue)
		{
			var factory = new ConnectionFactory
			{
				Protocol = Protocols.FromEnvironment(),
				HostName = _managementApiHost.Host,
				VirtualHost = virtualHost
			};

			var conn = factory.CreateConnection();
			var ch = conn.CreateModel();
			ch.QueuePurge(queue.name);
			ch.Close();
			conn.Close();
		}

		public void DeleteQueue(string queueName)
		{
			var factory = new ConnectionFactory
			{
				Protocol = Protocols.FromEnvironment(),
				HostName = _managementApiHost.Host,
				VirtualHost = virtualHost
			};

			var conn = factory.CreateConnection();
			var ch = conn.CreateModel();
			ch.QueueDelete(queueName);
			ch.ExchangeDelete(queueName);
			ch.Close();
			conn.Close();
		}
	}
}