﻿using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Reflection;
using System.Reflection.Emit;
using FlitBit.Data.Meta;
using FlitBit.Emit;

namespace FlitBit.Data.SqlServer
{
	internal class SqlMappedAnyToStringEmitter<T> : SqlDbTypeEmitter<T>
	{
		internal SqlMappedAnyToStringEmitter(SqlDbType dbType)
			: this(dbType, typeof(T))
		{
		}
		internal SqlMappedAnyToStringEmitter(SqlDbType dbType, Type underlying)
			: base(default(DbType), dbType, underlying)
		{
			switch (dbType)
			{
				case SqlDbType.VarChar:
					DbType = DbType.AnsiString;
					LengthRequirements = DbTypeLengthRequirements.Length;
					break;
				case SqlDbType.NVarChar:
					DbType = DbType.String;
					LengthRequirements = DbTypeLengthRequirements.Length;
					break;
				case SqlDbType.Char:
					DbType = DbType.AnsiStringFixedLength;
					LengthRequirements = DbTypeLengthRequirements.Length;
					break;
				case SqlDbType.NChar:
					DbType = DbType.StringFixedLength;
					LengthRequirements = DbTypeLengthRequirements.Length;
					break;
				default:
					throw new ArgumentOutOfRangeException("dbType");
			}
			TreatMissingLengthAsMaximum = true;
			LengthMaximum = "MAX";
		  DbDataReaderGetValueMethodName = "GetString";
		}

    protected internal override void EmitDbParameterSetValue(ILGenerator il, ColumnMapping column, LocalBuilder parm,
      LocalBuilder local, LocalBuilder flag)
    {
      Label fin = il.DefineLabel();
      Label gotoSetNonNullValue = il.DefineLabel();

      // if (field != null) {

      il.LoadLocal(local);
      il.LoadNull();
      il.CompareEqual();
      il.LoadValue(0);
      il.CompareEqual();
      il.StoreLocal(flag);
      il.LoadLocal(flag);
      il.BranchIfTrue(gotoSetNonNullValue);

      //   field.Value = DBNull.Value;

      il.LoadLocal(parm);
      il.LoadField(typeof(DBNull).GetField("Value", BindingFlags.Static | BindingFlags.Public));
      EmitInvokeDbParameterSetValue(il);
      il.Branch(fin);

      // } else {
      //   field.Value = field;

      il.MarkLabel(gotoSetNonNullValue);
      il.LoadLocal(parm);
      EmitTranslateRuntimeType(il, local);
      EmitInvokeDbParameterSetValue(il);

      // }

      il.MarkLabel(fin);
    }
	}
}