using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Debugging
{
	public class Remote : NodeSocket.Client
	{
		public Remote() : base(8080, "localhost")
		{
			this.Connect();
		}

		public String ServerFunction()
		{
			if(this.Verified)
			{
				return this.RemoteExecute<String>("serverFunction", new List<Object> { "this is a string parameter" });
			}

			return null;
		}
	}
}
