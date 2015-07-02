using System;
using System.Collections.Generic;
using System.Text;

namespace NodeSocket
{
	public static class Common
	{
        public delegate T RemoteFunction<T>(params Object[] args);
		public static Encoding Encoding = Encoding.UTF8;
		public static byte[] NodeSocketSignature = Common.Encoding.GetBytes("nsockv01");

		public enum EnumConnectionState
		{
			Disconnected = 0x0,
			Connected = 0x1,
			Verified = 0x2,
			Processing = 0x3,
			WebSocketConnected = 0x4,
			_max = 0x5
		};

		public enum EnumExecutionCode
		{
			RequestMaster = 0x0,
			RequestSlave = 0x1,
			ExecFunction = 0x2,
			_max = 0x3
		};

		public enum EnumDataType
		{
			Byte = 0x0,
			UByte = 0x1,
			Short = 0x2,
			UShort = 0x3,
			Int = 0x4,
			UInt = 0x5,
			Float = 0x6,
			Double = 0x7,
			String = 0x8,
			Boolean = 0x9,
			_max = 0xA
		};

		public enum EnumNodeResponse
		{
			Okay = 0x0,
			NoResult = 0x1,
			InvalidFunction = 0x2,
			NodeError = 0x3,
			InvalidExecCode = 0x4,
			NotAllowed = 0x5,
			_max = 0x6
		};

		public static String[] EnumNodeResponseErrorString = new String[]
		{
			"Okay",
			"Okay (No result)",
			"An invalid function was specified",
			"Node reported an internal error",
			"An invalid execution code was specified",
			"Remote functions are not allowed from this node, remote is the current master"
		};

        public static byte[] ParseExecutePayload(Dictionary<String, Common.RemoteFunction<Object>> functions, byte[] buffer)
        {
            UInt32 payloadLength = lib.UInt32FromBytesLE(buffer, 0);
            UInt32 identifierLength = lib.UInt32FromBytesLE(buffer, 4);
            String identifier = Common.Encoding.GetString(buffer, 8, (int)identifierLength);

            int position = (int)identifierLength + 8;

            if (functions.ContainsKey(identifier))
            {
                List<Object> args = new List<Object>();

                for(; position < payloadLength;)
                {
                    Common.EnumDataType dataType = (Common.EnumDataType)buffer[position++];
                    UInt32 dataSize = lib.UInt32FromBytesLE(buffer, position);
                    position += 4;

                    Object result = null;

                    switch (dataType)
                    {
                        case EnumDataType.Byte:
                            result = (SByte)buffer[position++];
                            break;
                        case EnumDataType.UByte:
                            result = (byte)buffer[position++];
                            break;
                        case EnumDataType.Short:
                            result = lib.Int16FromBytesLE(buffer, position);
                            position += 2;
                            break;
                        case EnumDataType.UShort:
                            result = lib.UInt16FromBytesLE(buffer, position);
                            position += 2;
                            break;
                        case EnumDataType.Int:
                            result = lib.Int32FromBytesLE(buffer, position);
                            position += 4;
                            break;
                        case EnumDataType.UInt:
                            result = lib.UInt32FromBytesLE(buffer, position);
                            position += 4;
                            break;
                        case EnumDataType.Float:
                            result = lib.FloatFromBytesLE(buffer, position);
                            position += 4;
                            break;
                        case EnumDataType.Double:
                            result = lib.DoubleFromBytesLE(buffer, position);
                            position += 8;
                            break;
                        case EnumDataType.String:
                            String str = Common.Encoding.GetString(buffer, position, (int)dataSize);
                            position += str.Length;
                            result = str;
                            break;
                        case EnumDataType.Boolean:
                            result = (bool)(buffer[position++] > 0);
                            break;
                        default:
                            return new byte[] { (byte)Common.EnumNodeResponse.NodeError };
                    }

                    args.Add(result);
                }

                Object ret = functions[identifier](args);
                if (ret == null)
                {
                    return new byte[] { (byte)Common.EnumNodeResponse.NoResult };
                }
                else
                {
                    Type t = ret.GetType();
                    EnumDataType byteType = EnumDataType._max;
                    List<byte> byteData = new List<byte>();

                    if (t == typeof(SByte))
                    {
                        byteType = EnumDataType.Byte;
                        byteData.Add((byte)ret);
                    }
                    else if (t == typeof(Byte))
                    {
                        byteType = EnumDataType.UByte;
                        byteData.Add((byte)ret);
                    }
                    else if (t == typeof(Int16))
                    {
                        byteType = EnumDataType.Short;
                        byteData.AddArray(lib.BytesFromInt16LE((Int16)ret));
                    }
                    else if (t == typeof(UInt16))
                    {
                        byteType = EnumDataType.UShort;
                        byteData.AddArray(lib.BytesFromUInt16LE((UInt16)ret));
                    }
                    else if (t == typeof(Int32))
                    {
                        byteType = EnumDataType.Int;
                        byteData.AddArray(lib.BytesFromInt32LE((Int32)ret));
                    }
                    else if (t == typeof(UInt32))
                    {
                        byteType = EnumDataType.UInt;
                        byteData.AddArray(lib.BytesFromUInt32LE((UInt32)ret));
                    }
                    else if (t == typeof(float))
                    {
                        byteType = EnumDataType.Float;
                        byteData.AddArray(lib.BytesFromFloatLE((float)ret));
                    }
                    else if (t == typeof(double))
                    {
                        byteType = EnumDataType.Double;
                        byteData.AddArray(lib.BytesFromDoubleLE((double)ret));
                    }
                    else if (t == typeof(String))
                    {
                        byteType = EnumDataType.String;
                        byteData.AddArray(Common.Encoding.GetBytes((String)ret));
                    }
                    else if (t == typeof(Boolean))
                    {
                        byteType = EnumDataType.Boolean;
                        byteData.Add((byte)((bool)ret ? 0x1 : 0x0));
                    }
                    else
                    {
                        return new byte[] { (byte)Common.EnumNodeResponse.NodeError };
                    }

                    List<byte> retBytes = new List<byte>
                    {
                        (byte)Common.EnumNodeResponse.Okay,
                        (byte)byteType
                    };
                    retBytes.AddArray(lib.BytesFromUInt32LE((UInt32)byteData.Count));
                    retBytes.AddList(byteData);

                    return retBytes.ToArray();
                }
            }
            else
            {
                return new byte[] { (byte)Common.EnumNodeResponse.InvalidFunction };
            }
        }

		public static T ParseResultPayload<T>(byte[] buffer)
		{
			// Skip buffer[0], just the server return status
			EnumDataType dataType = (EnumDataType)buffer[1];
			UInt32 dataSize = lib.UInt32FromBytesLE(buffer, 2);

			Object result = null;

			switch(dataType)
			{
				case EnumDataType.Byte:
					result = (SByte)buffer[6];
					break;
				case EnumDataType.UByte:
					result = (byte)buffer[6];
					break;
				case EnumDataType.Short:
					result = lib.Int16FromBytesLE(buffer, 6);
					break;
				case EnumDataType.UShort:
					result = lib.UInt16FromBytesLE(buffer, 6);
					break;
				case EnumDataType.Int:
					result = lib.Int32FromBytesLE(buffer, 6);
					break;
				case EnumDataType.UInt:
					result = lib.UInt32FromBytesLE(buffer, 6);
					break;
				case EnumDataType.Float:
					result = lib.FloatFromBytesLE(buffer, 6);
					break;
				case EnumDataType.Double:
					result = lib.DoubleFromBytesLE(buffer, 6);
					break;
				case EnumDataType.String:
					result = Common.Encoding.GetString(buffer, 6, (int)dataSize);
					break;
				case EnumDataType.Boolean:
					result = (bool)(buffer[6] > 0);
					break;
			}

			return (T)result;
		}

		public static byte[] CreateExecutePayload(String identifier, List<Object> args)
		{
			byte[] identifierBytes = Common.Encoding.GetBytes(identifier);
			UInt32 payloadLength = 4 + (UInt32)identifierBytes.Length;

			List<byte> parameterBytes = new List<byte>();
			foreach(Object argument in args)
			{
				Type t = argument.GetType();
				EnumDataType byteType = EnumDataType._max;
				List<byte> byteData = new List<byte>();

				if(t == typeof(SByte))
				{
					byteType = EnumDataType.Byte;
					byteData.Add((byte)argument);
				}
				else if(t == typeof(Byte))
				{
					byteType = EnumDataType.UByte;
					byteData.Add((byte)argument);
				}
				else if(t == typeof(Int16))
				{
					byteType = EnumDataType.Short;
					byteData.AddArray(lib.BytesFromInt16LE((Int16)argument));
				}
				else if(t == typeof(UInt16))
				{
					byteType = EnumDataType.UShort;
					byteData.AddArray(lib.BytesFromUInt16LE((UInt16)argument));
				}
				else if(t == typeof(Int32))
				{
					byteType = EnumDataType.Int;
					byteData.AddArray(lib.BytesFromInt32LE((Int32)argument));
				}
				else if(t == typeof(UInt32))
				{
					byteType = EnumDataType.UInt;
					byteData.AddArray(lib.BytesFromUInt32LE((UInt32)argument));
				}
				else if(t == typeof(float))
				{
					byteType = EnumDataType.Float;
					byteData.AddArray(lib.BytesFromFloatLE((float)argument));
				}
				else if(t == typeof(double))
				{
					byteType = EnumDataType.Double;
					byteData.AddArray(lib.BytesFromDoubleLE((double)argument));
				}
				else if(t == typeof(String))
				{
					byteType = EnumDataType.String;
					byteData.AddArray(Common.Encoding.GetBytes((String)argument));
				}
				else if(t == typeof(Boolean))
				{
					byteType = EnumDataType.Boolean;
					byteData.Add((byte)((bool)argument ? 0x1 : 0x0));
				}
				else
				{
					return new byte[] { (byte)Common.EnumNodeResponse.NodeError };
				}

				parameterBytes.Add((byte)byteType);
				parameterBytes.AddArray(lib.BytesFromUInt32LE((UInt32)byteData.Count));
				parameterBytes.AddList(byteData);
				payloadLength += 5 + (UInt32)byteData.Count;
			}

			List<byte> buffer = new List<byte>
			{
				(byte)Common.EnumExecutionCode.ExecFunction
			};
			buffer.AddArray(lib.BytesFromUInt32LE(payloadLength));
			buffer.AddArray(lib.BytesFromUInt32LE((UInt32)identifierBytes.Length));
			buffer.AddArray(identifierBytes);
			buffer.AddList(parameterBytes);
			return buffer.ToArray();
		}
	}
}
