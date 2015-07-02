using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NodeSocket
{
    public class Client
    {
		private ushort port = 0;
		private Socket socket = null;
		private IPAddress ipaddress = null;
		private bool master = false;
		private Common.EnumConnectionState state = Common.EnumConnectionState.Disconnected;
		private bool continueListen = true; // Use to break listen loop

		public Action<Socket> OnConnect = null;
		public Action<Socket> OnVerified = null;
		public Action<Socket> OnMaster = null;
		public Action<Socket> OnTimeout = null;

		public bool Bidirectional = false;
		public bool KeepAlive = false;
		public int PollTime = -1;
		public bool DenyMasterRequest = false;

		private bool _verified = false;
		public bool Verified
		{
			get
			{
				return _verified;
			}
		}

		public Client(ushort port, IPAddress ipaddress)
		{
			this.port = port;

			if(ipaddress != null)
			{
				this.ipaddress = ipaddress;
			}
			else
			{
				throw new ArgumentNullException("An IP address must be specified");
			}

			this.socket = new Socket(this.ipaddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		}

		public Client(ushort port, String hostname, List<AddressFamily> addressTypes = null)
		{
			this.port = port;

			if(hostname != null)
			{
				IPHostEntry hosts = Dns.GetHostEntry(hostname);

				foreach(IPAddress address in hosts.AddressList)
				{
					if((addressTypes == null && (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6))
						|| (addressTypes != null && addressTypes.Contains(address.AddressFamily)))
					{
						ipaddress = address;
						break;
					}
				}

				if(ipaddress == null)
				{
					throw new Exception("DNS record had no records with matching address families");
				}
			}
			else
			{
				throw new ArgumentNullException("An IP address must be specified");
			}

			this.socket = new Socket(this.ipaddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		}

		public T RemoteExecute<T>(String identifier, List<Object> args = null)
		{
			if(this.Bidirectional)
			{
				this.RequestMaster();
			}

			if(this.master)
			{
				if(this.state == Common.EnumConnectionState.Verified)
				{
					this.state = Common.EnumConnectionState.Processing;

					if(args == null)
					{
						args = new List<Object>();
					}

					byte[] buffer = Common.CreateExecutePayload(identifier, args);
					this.writeSocket(buffer);

					buffer = this.readSocket();

					if(buffer[0] == (byte)Common.EnumNodeResponse.Okay)
					{
						return Common.ParseResultPayload<T>(buffer);
					}
					else if(buffer[0] == (byte)Common.EnumNodeResponse.NoResult)
					{
						return default(T);
					}
					else if(buffer[0] < (byte)Common.EnumNodeResponse._max)
					{
						throw new Exception(Common.EnumNodeResponseErrorString[(int)buffer[0]]);
					}
					else
					{
						throw new Exception("Unknown response received from the connected node");
					}
				}
				else
				{
					throw new Exception("Unable to execute remote function on an unverified/disconnected node");
				}
			}
			else
			{
				throw new Exception("Unable to execute remote function when acting as a slave");
			}
		}

		public bool RequestMaster()
		{
			if(!this.master)
			{
				if(this.state == Common.EnumConnectionState.Verified)
				{
					this.writeSocket(new byte[] { (byte)Common.EnumExecutionCode.RequestMaster });
					this.master = true;

					if(this.OnMaster != null)
					{
						this.OnMaster(this.socket);
					}
				}
				else
				{
					throw new Exception("A master request must be done over an idle connection");
				}
			}

			return true;
		}

		public bool Connect()
		{
			if(this.ipaddress == null)
			{
				throw new ArgumentException("An IP address ha not been specified");
			}
			else if(this.socket == null)
			{
				throw new ArgumentException("Socket object has not been initialized");
			}

			this.socket.Connect(this.ipaddress, this.port);

			this.state = Common.EnumConnectionState.Connected;

			if(this.OnConnect != null)
			{
				this.OnConnect(this.socket);
			}

			this.writeSocket(Common.NodeSocketSignature, SocketFlags.None);

			byte[] response = readSocket(Common.NodeSocketSignature.Length);

			if(response.Length == Common.NodeSocketSignature.Length && lib.memcmp(response, Common.NodeSocketSignature, response.Length) == 0)
			{
				this.state = Common.EnumConnectionState.Verified;
				this._verified = true;

				this.RequestMaster();

				if(this.OnVerified != null)
				{
					this.OnVerified(this.socket);
				}

				this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, this.KeepAlive);
			}
			else
			{
				return false;
			}

			return true;
		}

		public void Listen()
		{
			byte[] buffer = null;
			while(continueListen)
			{
				buffer = this.readSocket(1, true);

				if(this.master)
				{
					if(buffer[0] == (byte)Common.EnumExecutionCode.RequestMaster)
					{
						this.master = this.DenyMasterRequest;

						if(!this.master)
						{
							continue; // Continue to listen if no longer the master
						}
					}
					else if(buffer[0] != (byte)Common.EnumExecutionCode.RequestSlave)
					{
						this.writeSocket(new byte[] { (byte)NodeSocket.Common.EnumNodeResponse.NotAllowed });
					}
					break; // Listening while a master only lasts for one command
				}
				else
				{
					if(buffer[0] == (byte)Common.EnumExecutionCode.ExecFunction)
					{
						// TODO: Execute function
					}
					else if(buffer[0] == (byte)Common.EnumExecutionCode.RequestSlave)
					{
						this.RequestMaster();
					}
					else if(buffer[0] != (byte)Common.EnumExecutionCode.RequestMaster)
					{
						this.writeSocket(new byte[] { (byte)NodeSocket.Common.EnumNodeResponse.InvalidExecCode });
					}
				}
			}
		}

		protected byte[] readSocket(int maxBytes = -1, bool indefinite = false)
		{
			byte[] buffer = null;

			if(this.socket.Poll(indefinite ? -1 : this.PollTime, SelectMode.SelectRead))
			{
				int bufferSize = this.socket.Available;

				if(maxBytes != -1 && maxBytes < bufferSize)
				{
					bufferSize = maxBytes;
				}

				buffer = new byte[bufferSize];
				this.socket.Receive(buffer);
			}
			else
			{
				if(this.OnTimeout != null)
				{
					this.OnTimeout(this.socket);
				}
				
			}

			return buffer;
		}

		protected void writeSocket(byte[] buffer, SocketFlags flags = SocketFlags.None)
		{
			this.socket.Send(buffer, buffer.Length, flags);
		}
    }
}
