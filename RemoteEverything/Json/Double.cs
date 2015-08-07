using System;

namespace RemoteEverything.Json
{
	public class Double : Node
	{
		readonly double val;
		public Double(double val)
		{
			this.val = val;
		}

		public override void Write(System.IO.TextWriter stream)
		{
			Node.Write(stream, val);
		}
	}
}

