﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using FlitBit.Core.Collections;
using FlitBit.Data.DataModel;
using FlitBit.Data.Expressions;
using FlitBit.Data.Meta;
using FlitBit.Data.SPI;
using FlitBit.Emit;

namespace FlitBit.Data.SqlServer
{
	internal static class OneClassOneTableEmitter
	{
		static readonly Lazy<EmittedModule> LazyModule =
			new Lazy<EmittedModule>(() => RuntimeAssemblies.DynamicAssembly.DefineModule("SqlServerMapping", null),
				LazyThreadSafetyMode.ExecutionAndPublication
				);
		
		static EmittedModule Module
		{
			get { return LazyModule.Value; }
		}

		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "By design.")]
		internal static Type CreateCommand<TDataModel, TImpl>(Mapping<TDataModel> mapping)
			where TImpl : class, IDataModel, TDataModel, new()
		{
			Contract.Ensures(Contract.Result<Type>() != null);

			var targetType = typeof(TDataModel);
			var typeName = RuntimeAssemblies.PrepareTypeName(targetType, "CreateCommand");

			var module = Module;
			lock (module)
			{
				var type = module.Builder.GetType(typeName, false, false) ??
									EmitImplementation<TDataModel, TImpl>.BuildCreateCommand(module, typeName, mapping);
				return type;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "By design.")]
		internal static Type UpdateCommand<TDataModel, TImpl>(Mapping<TDataModel> mapping)
			where TImpl : class, IDataModel, TDataModel, new()
		{
			Contract.Ensures(Contract.Result<Type>() != null);

			var targetType = typeof(TDataModel);
			var typeName = RuntimeAssemblies.PrepareTypeName(targetType, "UpdateCommand");

			var module = Module;
			lock (module)
			{
				var type = module.Builder.GetType(typeName, false, false) ??
									EmitImplementation<TDataModel, TImpl>.BuildUpdateCommand(module, typeName, mapping);
				return type;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "By design.")]
		internal static Type ReadByIdCommand<TDataModel, TImpl, TIdentityKey>(Mapping<TDataModel> mapping)
			where TImpl : class, IDataModel, TDataModel, new()
		{
			Contract.Ensures(Contract.Result<Type>() != null);

			var targetType = typeof(TDataModel);
			var typeName = RuntimeAssemblies.PrepareTypeName(targetType, "ReadByIdCommand");

			var module = Module;
			lock (module)
			{
				var type = module.Builder.GetType(typeName, false, false) ??
									EmitImplementation<TDataModel, TImpl>.BuildReadByIdCommand<TIdentityKey>(module, typeName, mapping);
				return type;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "By design.")]
		internal static Type MakeQueryCommand<TDataModel, TImpl, TParam>(Mapping<TDataModel> mapping, string queryKey, Constraints cns)
			where TImpl : class, IDataModel, TDataModel, new()
		{
			Contract.Ensures(Contract.Result<Type>() != null);

			var targetType = typeof(TDataModel);
			var typeName = RuntimeAssemblies.PrepareTypeName(targetType, queryKey);

			var module = Module;
			lock (module)
			{
				var type = module.Builder.GetType(typeName, false, false) ??
									EmitImplementation<TDataModel, TImpl>.BuildQueryCommand<TParam>(module, typeName, mapping, cns);
				return type;
			}
		}

		static class EmitImplementation<TDataModel, TImpl>
			where TImpl : class, IDataModel, TDataModel, new()
		{
			internal static Type BuildCreateCommand(EmittedModule module, string typeName, Mapping<TDataModel> mapping)
			{
				Contract.Requires<ArgumentNullException>(module != null);
				Contract.Requires<ArgumentNullException>(typeName != null);
				Contract.Requires<ArgumentException>(typeName.Length > 0);
				Contract.Requires<InvalidOperationException>(mapping.HasBinder);
				Contract.Ensures(Contract.Result<Type>() != null);
				var baseType = typeof(SingleUpdateQueryCommand<TDataModel, TImpl>);
				var builder = module.DefineClass(typeName, EmittedClass.DefaultTypeAttributes,
					baseType, null);
				builder.Attributes = TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit;

				var ctor = builder.DefineCtor();
				ctor.DefineParameter("sql", typeof(DynamicSql));
				ctor.DefineParameter("offsets", typeof(int[]));
				ctor.ContributeInstructions((m, il) =>
				{
					il.LoadArg_0();
					il.LoadArg_1();
					il.LoadArg_2();
					il.Call(baseType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(DynamicSql), typeof(int[]) }, null));
				});

				ImplementCreateBindCommand(builder, baseType, mapping);

				builder.Compile();
				return builder.Ref.Target;
			}

			static void ImplementCreateBindCommand(EmittedClass builder, Type baseType, Mapping<TDataModel> mapping)
			{
				var method =
					builder.DefineOverrideMethod(baseType.GetMethod("BindCommand", BindingFlags.NonPublic | BindingFlags.Instance));
				var helper = mapping.GetDbProviderHelper();
				var args = new
				{
					self = 0,
					cmd = 1,
					sql = 2,
					model = 3,
					dirty = 4,
					offsets = 5
				};
				method.ContributeInstructions((m, il) =>
				{
					var columnList = il.DeclareLocal(typeof(List<string>));
					var valueList = il.DeclareLocal(typeof(List<string>));
					var flag = il.DeclareLocal(typeof(bool));
					il.New<List<string>>();
					il.StoreLocal(columnList);
					il.New<List<string>>();
					il.StoreLocal(valueList);
					foreach (var column in mapping.Columns)
					{
						var col = column;
						if (column.IsTimestampOnInsert || column.IsTimestampOnUpdate)
						{
							il.LoadLocal(columnList);
							il.LoadValue(mapping.QuoteObjectName(column.TargetName));
							il.CallVirtual<List<string>>("Add");
							il.LoadLocal(valueList);
							il.LoadValue("@generated_timestamp");
							il.CallVirtual<List<string>>("Add");
						}
						else if (!column.IsCalculated)
						{
							var emitter = column.Emitter;
							var bindingName = helper.FormatParameterName(column.DbTypeDetails.BindingName);
							var next = il.DefineLabel();
							il.LoadArg(args.dirty);
							il.LoadArg(args.offsets);
							il.LoadValue(column.Ordinal);
							il.Emit(OpCodes.Ldelem_I4);
							il.Call<BitVector>("get_Item");
							il.LoadValue(0);
							il.CompareEqual();
							il.StoreLocal(flag);
							il.LoadLocal(flag);
							il.BranchIfTrue(next);
							il.LoadLocal(columnList);
							il.LoadValue(mapping.QuoteObjectName(column.TargetName));
							il.CallVirtual<List<string>>("Add");
							il.LoadLocal(valueList);
							il.LoadValue(bindingName);
							il.CallVirtual<List<string>>("Add");
							if (column.IsReference)
							{
								var reftype = col.ReferenceTargetMember.GetTypeOfValue();
								emitter.BindParameterOnDbCommand<SqlParameter>(method.Builder, column, bindingName,
									gen => gen.LoadArg(args.cmd),
									gen => gen.LoadArg(args.model),
									gen =>
									{
										gen.LoadValue(col.Member.Name);
										gen.CallVirtual(typeof(IDataModel).MatchGenericMethod("GetReferentID", 1,
											reftype, typeof(string)).MakeGenericMethod(reftype));
									},
									flag);
							}
							else
							{
								emitter.BindParameterOnDbCommand<SqlParameter>(method.Builder, column, bindingName,
									gen => gen.LoadArg(args.cmd),
									gen => gen.LoadArg(args.model),
									gen => gen.CallVirtual(((PropertyInfo)col.Member).GetGetMethod()),
									flag);
							}
							il.MarkLabel(next);
						}
					}
					il.LoadArg(args.cmd);
					il.LoadArg(args.cmd);
					il.CallVirtual<DbCommand>("get_CommandText");
					il.LoadValue("\r\n\t");
					il.LoadLocal(columnList);
					il.Call<String>("Join", BindingFlags.Static | BindingFlags.Public, typeof(string), typeof(IEnumerable<string>));
					il.LoadValue("\r\n\t");
					il.LoadLocal(valueList);
					il.Call<String>("Join", BindingFlags.Static | BindingFlags.Public, typeof(string), typeof(IEnumerable<string>));
					il.Call<String>("Format", BindingFlags.Static | BindingFlags.Public, typeof(string), typeof(object), typeof(object));
					il.CallVirtual<DbCommand>("set_CommandText");
				});
			}

			internal static Type BuildUpdateCommand(EmittedModule module, string typeName, Mapping<TDataModel> mapping)
			{
				Contract.Requires<ArgumentNullException>(module != null);
				Contract.Requires<ArgumentNullException>(typeName != null);
				Contract.Requires<ArgumentException>(typeName.Length > 0);
				Contract.Requires<InvalidOperationException>(mapping.HasBinder);
				Contract.Ensures(Contract.Result<Type>() != null);

				var baseType = typeof(SingleUpdateQueryCommand<TDataModel, TImpl>);
				var builder = module.DefineClass(typeName, EmittedClass.DefaultTypeAttributes,
					baseType, null);
				builder.Attributes = TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit;

				var ctor = builder.DefineCtor();
				ctor.DefineParameter("sql", typeof(DynamicSql));
				ctor.DefineParameter("offsets", typeof(int[]));
				ctor.ContributeInstructions((m, il) =>
				{
					il.LoadArg_0();
					il.LoadArg_1();
					il.LoadArg_2();
					il.Call(baseType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(DynamicSql), typeof(int[]) }, null));
				});

				ImplementUpdateBindCommand(builder, baseType, mapping);

				builder.Compile();
				return builder.Ref.Target;
			}

			static void ImplementUpdateBindCommand(EmittedClass builder, Type baseType, Mapping<TDataModel> mapping)
			{
				var method =
					builder.DefineOverrideMethod(baseType.GetMethod("BindCommand", BindingFlags.NonPublic | BindingFlags.Instance));
				var helper = mapping.GetDbProviderHelper();
				var args = new
				{
					self = 0,
					cmd = 1,
					sql = 2,
					model = 3,
					dirty = 4,
					offsets = 5
				};
				method.ContributeInstructions((m, il) =>
				{
					var columnList = il.DeclareLocal(typeof(List<string>));
					var parm = il.DeclareLocal(typeof(SqlParameter));
					var flag = il.DeclareLocal(typeof(bool));
					il.New<List<string>>();
					il.StoreLocal(columnList);
					foreach (var column in mapping.Columns)
					{
						if (column.IsTimestampOnUpdate)
						{
							il.LoadLocal(columnList);
							il.LoadValue(String.Concat(mapping.QuoteObjectName(column.TargetName), " = @generated_timestamp"));
							il.CallVirtual<List<string>>("Add");
						}
						else if (!column.IsCalculated)
						{
							var col = column;
							var emitter = column.Emitter;
							var bindingName = helper.FormatParameterName(column.DbTypeDetails.BindingName);
							var next = il.DefineLabel();
							il.LoadArg(args.dirty);
							il.LoadValue(column.Ordinal);
							il.Call<BitVector>("get_Item");
							il.LoadValue(0);
							il.CompareEqual();
							il.StoreLocal(flag);
							il.LoadLocal(flag);
							il.BranchIfTrue(next);
							il.LoadLocal(columnList);
							il.LoadValue(String.Concat(mapping.QuoteObjectName(column.TargetName), " = ", bindingName));
							il.CallVirtual<List<string>>("Add");
							if (column.IsReference)
							{
								var reftype = col.ReferenceTargetMember.GetTypeOfValue();
								emitter.BindParameterOnDbCommand<SqlParameter>(method.Builder, column, bindingName,
									gen => gen.LoadArg(args.cmd),
									gen => gen.LoadArg(args.model),
									gen =>
									{
										gen.LoadValue(col.Member.Name);
										gen.CallVirtual(typeof(IDataModel).MatchGenericMethod("GetReferentID", 1,
											reftype, typeof(string)).MakeGenericMethod(reftype));
									},
									flag);
							}
							else
							{
								emitter.BindParameterOnDbCommand<SqlParameter>(method.Builder, column, bindingName,
									gen => gen.LoadArg(args.cmd),
									gen => gen.LoadArg(args.model),
									gen => gen.CallVirtual(((PropertyInfo)col.Member).GetGetMethod()),
									flag);
							}
							il.MarkLabel(next);
						}
					}
					var idcol = mapping.Identity.Columns[0].Column;
					var idemit = idcol.Emitter;
					idemit.BindParameterOnDbCommand<SqlParameter>(method.Builder, idcol, "@identity",
									gen => gen.LoadArg(args.cmd),
									gen => gen.LoadArg(args.model),
									gen => gen.CallVirtual(((PropertyInfo)idcol.Member).GetGetMethod()),
									flag);
					il.LoadArg(args.cmd);
					il.LoadArg(args.cmd);
					il.CallVirtual<DbCommand>("get_CommandText");
					il.LoadValue("\r\n\t");
					il.LoadLocal(columnList);
					il.Call<String>("Join", BindingFlags.Static | BindingFlags.Public, typeof(string), typeof(IEnumerable<string>));
					il.Call<String>("Format", BindingFlags.Static | BindingFlags.Public, typeof(string), typeof(object), typeof(object));
					il.CallVirtual<DbCommand>("set_CommandText");
				});
			}

			internal static Type BuildReadByIdCommand<TIdentityKey>(EmittedModule module, string typeName, Mapping<TDataModel> mapping)
			{
				Contract.Requires<ArgumentNullException>(module != null);
				Contract.Requires<ArgumentNullException>(typeName != null);
				Contract.Requires<ArgumentException>(typeName.Length > 0);
				Contract.Requires<InvalidOperationException>(mapping.HasBinder);
				Contract.Ensures(Contract.Result<Type>() != null);

				var baseType = typeof(SqlDataModelQuerySingleCommand<TDataModel, TImpl, TIdentityKey>);
				var builder = module.DefineClass(typeName, EmittedClass.DefaultTypeAttributes,
					baseType, null);
				builder.Attributes = TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit;

				var ctor = builder.DefineCtor();
				ctor.DefineParameter("commandText", typeof(String));
				ctor.DefineParameter("offsets", typeof(int[]));
				ctor.ContributeInstructions((m, il) =>
				{
					il.LoadArg_0();
					il.LoadArg_1();
					il.LoadArg_2();
					il.Call(baseType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(int[]) }, null));
				});

				ImplementReadByIdBindCommand<TIdentityKey>(builder, baseType, mapping);

				builder.Compile();
				return builder.Ref.Target;
			}

			static void ImplementReadByIdBindCommand<TIdentityKey>(EmittedClass builder, Type baseType, Mapping<TDataModel> mapping)
			{
				var method =
					builder.DefineOverrideMethod(baseType.GetMethod("BindCommand", BindingFlags.NonPublic | BindingFlags.Instance));
				var helper = mapping.GetDbProviderHelper();
				var args = new
				{
					self = 0,
					cmd = 1,
					parm = 2,
					offsets = 3
				};
				method.ContributeInstructions((m, il) =>
				{
					var flag = il.DeclareLocal(typeof(bool));
					var column = mapping.Identity.Columns[0].Column;
					var emitter = column.Emitter;
					var bindingName = helper.FormatParameterName(column.DbTypeDetails.BindingName);

					emitter.BindParameterOnDbCommand<SqlParameter>(method.Builder, column, bindingName,
									gen => gen.LoadArg(args.cmd),
									gen => gen.LoadArg(args.parm),
									gen => { },
									flag);
				});
			}


			internal static Type BuildQueryCommand<TParam>(EmittedModule module, string typeName, Mapping<TDataModel> mapping, Constraints cns)
			{
				Contract.Requires<ArgumentNullException>(module != null);
				Contract.Requires<ArgumentNullException>(typeName != null);
				Contract.Requires<ArgumentException>(typeName.Length > 0);
				Contract.Requires<InvalidOperationException>(mapping.HasBinder);
				Contract.Ensures(Contract.Result<Type>() != null);

				var baseType = typeof(SqlDataModelQueryCommand<TDataModel, TImpl, TParam>);
				var builder = module.DefineClass(typeName, EmittedClass.DefaultTypeAttributes,
					baseType, null);
				builder.Attributes = TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit;

				var ctor = builder.DefineCtor();
				ctor.DefineParameter("all", typeof(string));
				ctor.DefineParameter("page", typeof(DynamicSql));
				ctor.DefineParameter("offsets", typeof(int[]));
				ctor.ContributeInstructions((m, il) =>
				{
					il.LoadArg_0();
					il.LoadArg_1();
					il.LoadArg_2();
					il.LoadArg_3();
					il.Call(baseType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(DynamicSql), typeof(int[]) }, null));
				});

				ImplementBindQueryCommand<TParam>(builder, baseType, mapping, cns);

				builder.Compile();
				return builder.Ref.Target;
			}

			static void ImplementBindQueryCommand<TParam>(EmittedClass builder, Type baseType, Mapping<TDataModel> mapping, Constraints cns)
			{
				var method =
					builder.DefineOverrideMethod(baseType.GetMethod("BindCommand", BindingFlags.NonPublic | BindingFlags.Instance));
				var helper = mapping.GetDbProviderHelper();
				var args = new
				{
					self = 0,
					cmd = 1,
					offsets = 2
				};
				const int paramOffset = 3;

				method.ContributeInstructions((m, il) =>
				{
					var flag = il.DeclareLocal(typeof(bool));
					foreach (var p in cns.Parameters.Values)
					{
						var arg = p.Argument;
						Action<ILGenerator> loadSource = (stream) => stream.LoadArg(paramOffset + arg.Ordinal);
						if (p.Members != null && p.Members.Length > 0)
						{
							// Optimization: Consider evaluating dotted notation to resolve properties to local variable only once when binding several.
							foreach (PropertyInfo prop in p.Members)
							{
								loadSource(il);
								var dotted = il.DeclareLocal(prop.GetTypeOfValue());
								il.LoadValue(prop.GetGetMethod());
								il.StoreLocal(dotted);
								// TODO: test for null and if so fallout to bind DBNull
								loadSource = stream => stream.LoadLocal(dotted);
							}
						}
					}
				});
			}
		}
	}
}
