using System;
using System.Collections.Generic;

namespace RemoteEverything.Json
{
	public class Object : Node
	{
		Dictionary<string, Node> _items = new Dictionary<string, Node>();

		public void Add(string key, Node value)
		{
			_items.Add(key, value);
		}

		public override void Write(System.IO.TextWriter stream)
		{
			stream.Write("{");
			bool first = true;
			foreach (var kv in _items)
			{
				if (first)
					first = false;
				else
					stream.Write(',');
				Node.Write(stream, kv.Key);
				stream.Write(':');
				kv.Value.Write(stream);
			}
			stream.Write("}");
		}
	}
}

