using System;
using System.Collections.Generic;

namespace RemoteEverything.Json
{
	public class List : Node
	{
		readonly IList<Node> val = new List<Node>();
		public List() {}
		public List(IList<Node> val)
		{
			this.val = val;
		}

		public void Add(Node node)
		{
			val.Add(node);
		}

		public override void Write(System.IO.TextWriter stream)
		{
			stream.Write('[');
			bool first = true;
			foreach (var node in val)
			{
				if (first)
					first = false;
				else
					stream.Write(',');
				node.Write(stream);
			}
			stream.Write(']');
		}
	}
}

