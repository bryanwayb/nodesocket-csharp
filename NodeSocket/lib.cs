using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NodeSocket
{
	static class lib
	{
		public enum EnumEndianness
		{
			Unknown,
			BigEndian,
			LittleEndian
		};

		private static EnumEndianness _Endianness = EnumEndianness.Unknown;
		public static EnumEndianness Endianness
		{
			get
			{
				if(_Endianness == EnumEndianness.Unknown)
				{
					short endiannessTest = 0x1234;
					if(BitConverter.GetBytes(endiannessTest)[0] == 0x12)
					{
						_Endianness = EnumEndianness.BigEndian;
					}
					else
					{
						_Endianness = EnumEndianness.LittleEndian;
					}
				}

				return _Endianness;
			}
		}
		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int memcmp(byte[] b1, byte[] b2, long count);

		public static byte[] BytesFromInt16LE(Int16 i)
		{
			return new byte[2] { (byte)(i & 0xFF), (byte)((i >> 8) & 0xFF) };
		}

		public static Int16 Int16FromBytesLE(byte[] b, int offset = 0)
		{
			if(b == null || b.Length < offset + 2)
			{
				throw new ArgumentException("Byte array must be non-null and have a length of at least 2 past offset");
			}

			Int16 ret = 0;

			ret |= (Int16)b[offset];
			ret |= (Int16)(b[++offset] << 8);

			return ret;
		}

		public static byte[] BytesFromUInt16LE(UInt16 i)
		{
			return new byte[2] { (byte)(i & 0xFF), (byte)((i >> 8) & 0xFF) };
		}

		public static UInt16 UInt16FromBytesLE(byte[] b, int offset = 0)
		{
			if(b == null || b.Length < offset + 2)
			{
				throw new ArgumentException("Byte array must be non-null and have a length of at least 2 past offset");
			}

			UInt16 ret = 0;

			ret |= (UInt16)b[offset];
			ret |= (UInt16)(b[++offset] << 8);

			return ret;
		}

		public static byte[] BytesFromInt32LE(Int32 i)
		{
			return new byte[4] { (byte)(i & 0xFF), (byte)((i >> 8) & 0xFF), (byte)((i >> 16) & 0xFF), (byte)((i >> 24) & 0xFF) };
		}

		public static Int32 Int32FromBytesLE(byte[] b, int offset = 0)
		{
			if(b == null || b.Length < offset + 4)
			{
				throw new ArgumentException("Byte array must be non-null and have a length of at least 4 past offset");
			}

			Int32 ret = 0;

			ret |= (Int32)b[offset];
			ret |= (Int32)(b[++offset] << 8);
			ret |= (Int32)(b[++offset] << 16);
			ret |= (Int32)(b[++offset] << 24);

			return ret;
		}

		public static byte[] BytesFromUInt32LE(UInt32 i)
		{
			return new byte[4] { (byte)(i & 0xFF), (byte)((i >> 8) & 0xFF), (byte)((i >> 16) & 0xFF), (byte)((i >> 24) & 0xFF) };
		}

		public static UInt32 UInt32FromBytesLE(byte[] b, int offset = 0)
		{
			if(b == null || b.Length < 4 + offset)
			{
				throw new ArgumentException("Byte array must be non-null and have a length of at least 4 past offset");
			}

			UInt32 ret = 0;

			ret |= (UInt32)b[offset];
			ret |= (UInt32)(b[++offset] << 8);
			ret |= (UInt32)(b[++offset] << 16);
			ret |= (UInt32)(b[++offset] << 24);

			return ret;
		}

		public static byte[] BytesFromFloatLE(float f)
		{
			byte[] ret = BitConverter.GetBytes(f);

			if(Endianness == EnumEndianness.BigEndian)
			{
				byte medium = ret[0];
				ret[0] = ret[3];
				ret[3] = medium;

				medium = ret[1];
				ret[1] = ret[2];
				ret[2] = medium;
			}

			return ret;
		}

		public static float FloatFromBytesLE(byte[] b, int offset = 0)
		{
			if(b == null || b.Length < 4 + offset)
			{
				throw new ArgumentException("Byte array must be non-null and have a length of at least 4 past offset");
			}

			byte[] bc = new byte[4];
			for(int i = 0; i < bc.Length; i++, offset++)
			{
				bc[i] = b[offset];
			}

			if(Endianness == EnumEndianness.BigEndian)
			{
				byte medium = bc[0];
				bc[0] = bc[3];
				bc[3] = medium;

				medium = bc[1];
				bc[1] = bc[2];
				bc[2] = medium;
			}

			return BitConverter.ToSingle(bc, 0);
		}

		public static byte[] BytesFromDoubleLE(double f)
		{
			byte[] ret = BitConverter.GetBytes(f);

			if(Endianness == EnumEndianness.BigEndian)
			{
				byte medium = ret[0];
				ret[0] = ret[7];
				ret[7] = medium;

				medium = ret[1];
				ret[1] = ret[6];
				ret[6] = medium;

				medium = ret[2];
				ret[1] = ret[5];
				ret[5] = medium;

				medium = ret[3];
				ret[3] = ret[4];
				ret[4] = medium;
			}

			return ret;
		}

		public static double DoubleFromBytesLE(byte[] b, int offset = 0)
		{
			if(b == null || b.Length < 8 + offset)
			{
				throw new ArgumentException("Byte array must be non-null and have a length of at least 8 past offset");
			}

			byte[] bc = new byte[8];
			for(int i = 0; i < bc.Length; i++, offset++)
			{
				bc[i] = b[offset];
			}

			if(Endianness == EnumEndianness.BigEndian)
			{
				byte medium = bc[0];
				bc[0] = bc[7];
				bc[7] = medium;

				medium = bc[1];
				bc[1] = bc[6];
				bc[6] = medium;

				medium = bc[2];
				bc[1] = bc[5];
				bc[5] = medium;

				medium = bc[3];
				bc[3] = bc[4];
				bc[4] = medium;
			}

			return BitConverter.ToDouble(bc, 0);
		}

		public static void AddArray<T>(this List<T> a, T[] b)
		{
			foreach(T value in b)
			{
				a.Add(value);
			}
		}

		public static void AddList<T>(this List<T> a, List<T> b)
		{
			foreach(T value in b)
			{
				a.Add(value);
			}
		}
	}
}
