using System;
using System.Data;
using System.Data.Common;
using System.Reflection.Emit;
using FlitBit.Emit;

namespace FlitBit.Data.DataModel.DbTypeEmitters
{
	internal class MappedStringEmitter : MappedDbTypeEmitter<string, DbType>
	{
		internal MappedStringEmitter(DbType dbType)
			: base(dbType, dbType)
		{
			switch (dbType)
			{
				case DbType.AnsiString:
					this.SpecializedSqlTypeName = "VARCHAR";
					this.LengthRequirements = DbTypeLengthRequirements.Length;
					break;
				case DbType.String:
					this.SpecializedSqlTypeName = "NVARCHAR";
					this.LengthRequirements = DbTypeLengthRequirements.Length;
					break;
				case DbType.AnsiStringFixedLength:
					this.SpecializedSqlTypeName = "CHAR";
					this.LengthRequirements = DbTypeLengthRequirements.Length;
					break;
				case DbType.StringFixedLength:
					this.SpecializedSqlTypeName = "NCHAR";
					this.LengthRequirements = DbTypeLengthRequirements.Length;
					break;
				default:
					throw new ArgumentOutOfRangeException("dbType");
			}
		  DbDataReaderGetValueMethodName = "GetString";
		}

		
	}
}