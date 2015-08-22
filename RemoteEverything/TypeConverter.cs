using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RemoteEverything
{
	public class TypeConverter
	{
		public readonly Func<object, double> AsDouble;
		public readonly Func<object, string> AsString;

		readonly static TypeCode[] NumericTypeCodes = new TypeCode[]
		{
			TypeCode.Byte,
			TypeCode.SByte,
			TypeCode.UInt16,
			TypeCode.UInt32,
			TypeCode.UInt64,
			TypeCode.Int16,
			TypeCode.Int32,
			TypeCode.Int64,
			TypeCode.Decimal,
			TypeCode.Double,
			TypeCode.Single
		};

		static double UnBox(object numeric, TypeCode typeCode)
		{
			switch (typeCode)
			{
			case TypeCode.Byte:
				return (double)(Byte) numeric;
			case TypeCode.SByte:
				return (double)(SByte) numeric;
			case TypeCode.UInt16:
				return (double)(UInt16) numeric;
			case TypeCode.UInt32:
				return (double)(UInt32) numeric;
			case TypeCode.UInt64:
				return (double)(UInt64) numeric;
			case TypeCode.Int16:
				return (double)(Int16) numeric;
			case TypeCode.Int32:
				return (double)(Int32) numeric;
			case TypeCode.Int64:
				return (double)(Int64) numeric;
			case TypeCode.Decimal:
				return (double)(Decimal) numeric;
			case TypeCode.Double:
				return (double)(Double) numeric;
			case TypeCode.Single:
				return (double)(Single) numeric;
			}
			throw new InvalidCastException(string.Format("Cannot unbox typecode {0} to double", typeCode));
		}

		public TypeConverter(Type type)
		{
			var typeCode = Type.GetTypeCode(type);
			if (NumericTypeCodes.Contains(typeCode))
			{
				AsDouble = x => UnBox(x, typeCode);
				return;
			}

			var conversion = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Where(m => m.Name == "op_Explicit" || m.Name == "op_Implicit") // Conversion operator
				.Where(m => NumericTypeCodes.Contains(Type.GetTypeCode(m.ReturnType))) // Numeric return type
				.Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == type)
				.FirstOrDefault();
			if (conversion != null)
			{
				typeCode = Type.GetTypeCode(conversion.ReturnType);
				AsDouble = x => UnBox(conversion.Invoke(null, new object[] {x}), typeCode);
				return;
			}

			AsString = x => x.ToString();
		}

		static bool IsNumeric(Type type)
		{
			switch(Type.GetTypeCode(type))
			{
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Decimal:
			case TypeCode.Double:
			case TypeCode.Single:
				return true;
			default:
				return false;
			}
		}
	}
}

