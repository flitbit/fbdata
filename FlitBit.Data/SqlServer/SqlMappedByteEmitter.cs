﻿#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Reflection.Emit;
using FlitBit.Emit;

namespace FlitBit.Data.SqlServer
{
	internal class SqlMappedByteEmitter : SqlDbTypeEmitter<byte>
	{
		internal SqlMappedByteEmitter()
			: base(DbType.Byte, SqlDbType.TinyInt)
		{
		  DbDataReaderGetValueMethodName = "GetByte";
		}

	}
}