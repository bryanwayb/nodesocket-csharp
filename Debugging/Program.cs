﻿using System;
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

            NodeSocket.Common.RemoteFunction<Object> serverFunction = client.LinkFunction<Object>("serverFunction");

            client.DefineFunction("clientFunction", (arguments =>
            {
                Console.WriteLine("Executed on client\n");

                client.StopListening();

                return "Hello";
            }));

			client.OnConnect = delegate(Socket socket)
			{
				Console.WriteLine("Connected");
			};

			client.OnVerified = delegate(Socket socket)
			{
				Console.WriteLine("Verified");

                serverFunction();
				client.Listen();
			};

			client.Connect();
		}
	}
}