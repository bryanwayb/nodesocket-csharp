using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Debugging
{
	class Program
	{
		static void Main(string[] args)
		{
			/*Remote r = new Remote();
			Console.WriteLine(r.ServerFunction());*/

			NodeSocket.Client client = new NodeSocket.Client(8080, "localhost");

			client.OnConnect = delegate(Socket socket)
			{
				Console.WriteLine("Connected");
			};

			client.OnVerified = delegate(Socket socket)
			{
				Console.WriteLine("Verified");

				client.RemoteExecute<object>("serverFunction");
				client.Listen();
			};

			client.Connect();
		}
	}
}