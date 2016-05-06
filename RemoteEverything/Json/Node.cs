using System;

namespace RemoteEverything.Json
{
	public abstract class Node
	{
		public static Node MakeValue(string val)
		{
			return new String(val);
		}
		public static Node MakeValue(double val)
		{
			return new Double(val);
		}
		public static Node MakeValue(bool val)
		{
			return new Bool(val);
		}

		public abstract void Write(System.IO.TextWriter stream);

		protected static void Write(System.IO.TextWriter stream, string val)
		{
			stream.Write('"');
			foreach (var c in val)
			{
				switch (c)
				{
				case '\b':
					stream.Write("\\b");
					break;
				case '\f':
					stream.Write("\\f");
					break;
				case '\n':
					stream.Write("\\n");
					break;
				case '\r':
					stream.Write("\\r");
					break;
				case '\t':
					stream.Write("\\t");
					break;
				case '"':
				case '\\':
					stream.Write('\\');
					stream.Write(c);
					break;
				default:
					stream.Write(c);
					break;
				}
			}
			stream.Write('"');
		}

		protected static void Write(System.IO.TextWriter stream, bool val)
		{
			if (val)
				stream.Write("true");
			else
				stream.Write("false");
		}

		protected static void Write(System.IO.TextWriter stream, int val)
		{
			stream.Write(val);
		}

		protected static void Write(System.IO.TextWriter stream, double val)
		{
			if (System.Double.IsNaN(val))
			{
				Write(stream, "NaN");
			}
			else if (System.Double.IsPositiveInfinity(val))
			{
				Write(stream, "Infinity");
			}
			else if (System.Double.IsNegativeInfinity(val))
			{
				Write(stream, "-Infinity");
			}
			else
			{
				stream.Write(val);
			}
		}
	}
}
