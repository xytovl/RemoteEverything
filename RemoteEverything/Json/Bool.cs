using System;

namespace RemoteEverything.Json
{
	public class Bool : Node
	{
		readonly bool val;
		public Bool(bool val)
		{
			this.val = val;
		}

		public override void Write(System.IO.TextWriter stream)
		{
			Node.Write(stream, val);
		}
	}
}

