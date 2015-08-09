using System;
using System.Collections.Generic;

namespace RemoteEverything.Json
{
	public class Object : Node
	{
		readonly Dictionary<string, Node> _items;

		public void Add(string key, Node value)
		{
			_items.Add(key, value);
		}

		public Object()
		{
			_items = new Dictionary<string, Node>();
		}
		public Object(Dictionary<string, Node> items)
		{
			_items = items;
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

