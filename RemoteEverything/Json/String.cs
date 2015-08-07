using System;

namespace RemoteEverything.Json
{
	public class String : Node
	{
		readonly string val;
		public String(string val)
		{
			this.val = val;
		}

		public override void Write(System.IO.TextWriter stream)
		{
			Node.Write(stream, val);
		}
	}
}

