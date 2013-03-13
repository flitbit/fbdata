﻿#region COPYRIGHT© 2009-2012 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FlitBit.Core;
using FlitBit.Emit;
using FlitBit.Wireup;

namespace FlitBit.Data.Meta
{
	public partial class Mapping<M> : IMapping<M>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly List<IMapping> _baseTypes = new List<IMapping>();

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Dictionary<string, CollectionMapping<M>> _collections = new Dictionary<string, CollectionMapping<M>>();

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly List<ColumnMapping> _columns = new List<ColumnMapping>();

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly List<ColumnMapping> _declaredColumns = new List<ColumnMapping>();

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly List<Dependency> _dependencies = new List<Dependency>();

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Object _sync = new Object();

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly ConcurrentQueue<Action> _whenCompleted = new ConcurrentQueue<Action>();

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IModelBinder _binder;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		string _connectionName;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		List<string> _errors;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IdentityMapping<M> _identity;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		NaturalKeyMapping<M> _naturalKey;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		string _targetSchema;

		internal Mapping()
		{
			_identity = new IdentityMapping<M>(this);
			_naturalKey = new NaturalKeyMapping<M>(this);
			var name = typeof(M).Name;
			if (name.Length > 1 && name[0] == 'I' && Char.IsUpper(name[1]))
			{
				name = name.Substring(1);
			}
			this.TargetObject = name;
		}

		public bool HasIdentity
		{
			get { return _identity.Columns.Count() > 0; }
		}

		public object Discriminator { get; private set; }

		/// <summary>
		///   Gets the mapped type's identity mapping.
		/// </summary>
		public IdentityMapping<M> Identity
		{
			get { return _identity; }
		}

		public NaturalKeyMapping<M> NaturalKey
		{
			get { return _naturalKey; }
		}

		public IEnumerable<string> Errors
		{
			get { return (_errors == null) ? new String[0] : _errors.ToArray(); }
		}

		public EntityBehaviors Behaviors { get; internal set; }

		public bool HasBinder
		{
			get
			{
				return (!String.IsNullOrEmpty(ConnectionName))
					&& DbProviderHelpers.GetDbProviderHelperForDbConnection(ConnectionName) != null;
			}
		}

		public Type RuntimeType
		{
			get { return typeof(M); }
		}

		/// <summary>
		///   Indicates whether the mapping has been completed.
		/// </summary>
		public bool IsComplete { get; private set; }

		/// <summary>
		///   Indicates whether the entity is an enum type.
		/// </summary>
		public bool IsEnum
		{
			get { return Behaviors.HasFlag(EntityBehaviors.MapEnum); }
		}

		/// <summary>
		///   The Db object to which type T maps; either a table or view.
		/// </summary>
		public string TargetObject { get; set; }

		/// <summary>
		///   The Db schema where the target object resides.
		/// </summary>
		public string TargetSchema
		{
			get { return String.IsNullOrEmpty(_targetSchema) ? Mappings.Instance.DefaultSchema : _targetSchema; }
			set { _targetSchema = value; }
		}

		/// <summary>
		///   The Db catalog (database) where the target object resides.
		/// </summary>
		public string TargetCatalog { get; set; }

		/// <summary>
		///   The connection name where the type's data resides.
		/// </summary>
		public string ConnectionName
		{
			get { return String.IsNullOrEmpty(_connectionName) ? Mappings.Instance.DefaultConnection : _connectionName; }
			set { _connectionName = value; }
		}

		/// <summary>
		///   The ORM strategy.
		/// </summary>
		public MappingStrategy Strategy { get; private set; }

		public string DbObjectReference
		{
			get
			{
				return String.IsNullOrEmpty(TargetSchema)
					? QuoteObjectNameForSQL(TargetObject)
					: String.Concat(QuoteObjectNameForSQL(TargetSchema), '.', QuoteObjectNameForSQL(TargetObject));
			}
		}

		/// <summary>
		///   The columns that are mapped to the object.
		/// </summary>
		public IEnumerable<ColumnMapping> Columns
		{
			get { return _columns.AsReadOnly(); }
		}

		/// <summary>
		///   The columns that are mapped to the object.
		/// </summary>
		public IEnumerable<ColumnMapping> DeclaredColumns
		{
			get { return _declaredColumns.AsReadOnly(); }
		}

		public IEnumerable<MemberInfo> ParticipatingMembers
		{
			get
			{
				return Columns.Select(c => c.Member)
											.Concat(_collections.Select(kvp => kvp.Value.Member))
											.ToReadOnly();
			}
		}

		/// <summary>
		///   The collections that are mapped to the object.
		/// </summary>
		public IEnumerable<CollectionMapping> Collections
		{
			get
			{
				var res = new List<CollectionMapping>();
				foreach (var it in _baseTypes)
				{
					res.AddRange(it.DeclaredCollections);
				}
				res.AddRange(_collections.Values);
				return res.AsReadOnly();
			}
		}

		/// <summary>
		///   The collections that are mapped to the object.
		/// </summary>
		public IEnumerable<CollectionMapping> DeclaredCollections
		{
			get { return _collections.Values.ToReadOnly(); }
		}

		public string QuoteObjectNameForSQL(string name)
		{
			Contract.Assert(name != null);
			Contract.Assert(name.Length > 0);

			if (String.IsNullOrEmpty(ConnectionName))
			{
				return name;
			}
			else
			{
				var helper = DbProviderHelpers.GetDbProviderHelperForDbConnection(ConnectionName);

				return (helper != null) ? helper.QuoteObjectName(name) : name;
			}
		}

		public IEnumerable<Dependency> Dependencies
		{
			get
			{
				var res = new List<Dependency>();
				foreach (var it in _baseTypes)
				{
					res.AddRange(it.DeclaredDependencies);
				}
				res.AddRange(_dependencies);
				return res.AsReadOnly();
			}
		}

		public IEnumerable<Dependency> DeclaredDependencies
		{
			get { return _dependencies.ToArray(); }
		}

		public IMapping Completed(Action action)
		{
			if (IsComplete)
			{
				action();
			}
			else
			{
				_whenCompleted.Enqueue(action);
				if (IsComplete)
				{
					Action a;
					while (_whenCompleted.TryDequeue(out a))
					{
						a();
					}
				}
			}
			return this;
		}

		public IModelBinder GetBinder()
		{
			if (_binder == null)
			{
				if (!String.IsNullOrEmpty(ConnectionName))
				{
					var helper = DbProviderHelpers.GetDbProviderHelperForDbConnection(ConnectionName);
					if (HasBinder)
					{
						var get = typeof(DbProviderHelper).GetGenericMethod("GetModelBinder", 1, 2).MakeGenericMethod(typeof(M), DataModel<M>.IdentityKey.KeyType);
						_binder = (IModelBinder) get.Invoke(helper, new object[] {this});
					}
				}
			}
			return _binder;
		}

		public void NotifySubtype(IMapping mapping)
		{
			var ht = typeof(IHierarchyMapping<M>).GetGenericMethod("NotifySubtype", 1, 1).MakeGenericMethod(mapping.RuntimeType);
			ht.Invoke(DataModel<M>.Hierarchy, new object[] {mapping});
		}

		public CollectionMapping<M> Collection(Expression<Func<M, object>> expression)
		{
			Contract.Requires(expression != null);

			var member = expression.GetMemberFromExpression();
			Contract.Assert(member != null, "Expression must reference a field or property member");

			var memberType = member.MemberType;
			Contract.Assert(memberType == MemberTypes.Field
				|| memberType == MemberTypes.Property, "Expression must reference a field or property member");

			var name = member.Name;

			CollectionMapping<M> col;
			lock (_sync)
			{
				if (!_collections.TryGetValue(name, out col))
				{
					_collections.Add(name, col = new CollectionMapping<M>(this, member));
				}
			}
			return (CollectionMapping<M>) col;
		}

		/// <summary>
		///   Maps a column to a member (property or field) of the object.
		///   The property or field indicated will serve as the column definition.
		/// </summary>
		/// <param name="expression">
		///   An expression that identifies the member
		///   upon which a column will be mapped.
		/// </param>
		/// <returns>
		///   A ColumnMapping object for further refinement of the column's
		///   definition.
		/// </returns>
		public ColumnMapping<M> Column(Expression<Func<M, object>> expression)
		{
			Contract.Requires(expression != null);

			var member = expression.GetMemberFromExpression();
			Contract.Assert(member != null, "Expression must reference a field or property member");

			var memberType = member.MemberType;
			Contract.Assert(memberType == MemberTypes.Field
				|| memberType == MemberTypes.Property, "Expression must reference a field or property member");

			return DefineColumn(member);
		}

		/// <summary>
		///   Completes the mapping.
		/// </summary>
		/// <returns></returns>
		public Mappings End()
		{
			if (!HasIdentity)
			{
				// Try to discover identity columns from column definitions.
				foreach (var c in Columns.Where(c => c.Behaviors.HasFlag(ColumnBehaviors.Identity)))
				{
					Identity.AddColumn(c);
				}
			}
			this.MarkComplete();
			return Mappings.Instance;
		}

		public Mapping<M> InSchema(string schema)
		{
			Contract.Requires(schema != null);
			Contract.Requires(schema.Length > 0);

			this.TargetSchema = schema;
			return this;
		}

		public MemberInfo InferCollectionReferenceTargetMember(MemberInfo member, Type elementType)
		{
			var elmMapping = Mappings.AccessMappingFor(elementType);
			AddDependency(elmMapping, DependencyKind.Soft, member);

			var foreign_object_id = elmMapping.GetPreferredReferenceColumn();
			if (foreign_object_id == null)
			{
				throw new MappingException(String.Concat("Relationship not defined between ", typeof(M).Name, ".", member.Name,
																								" and the referenced type: ", elementType.Name));
			}

			return foreign_object_id.Member;
		}

		public Mapping<M> MapAllOperations() { return MapAllOperations(this.Strategy); }

		public Mapping<M> MapAllOperations(MappingStrategy strategy)
		{
			this.Strategy = strategy;
			var errors = new List<string>();
			var warnings = new List<string>();

			if (String.IsNullOrEmpty(this.ConnectionName))
			{
				errors.Add("ConnectionName has not been configured.");
			}

			if (String.IsNullOrEmpty(this.TargetCatalog))
			{
				warnings.Add("TargetCatalog has not been configured; the catalog will be determined by the connection string.");
			}
			if (String.IsNullOrEmpty(this.TargetSchema))
			{
				warnings.Add("TargetSchema has not been configured; none will be used.");
			}
			if (String.IsNullOrEmpty(this.TargetObject))
			{
				warnings.Add(String.Concat("TargetObject has not been configured; the type name will be used: ", typeof(M).Name));
			}

			//using (var container = Create.NewContainer())
			//{
			//  var cn = container.Scope.Add(DbExtensions.CreateAndOpenConnection(this.ConnectionName));
			//  var helper = DbProviderHelpers.GetDbProviderHelperForDbConnection(cn);
			//  if (String.IsNullOrEmpty(this.TargetCatalog)) this.TargetCatalog = cn.Database;
			//  else if (!String.Equals(cn.Database, this.TargetCatalog))
			//  {
			//    cn.ChangeDatabase(this.TargetCatalog);
			//  }
			//  if (!helper.SchemaExists(cn, this.TargetCatalog, this.TargetSchema))
			//  {
			//    helper.CreateSchema(cn, this.TargetCatalog, this.TargetSchema);
			//  }
			//}
			_errors = errors;

			return this;
		}

		public Mapping<M> ReferencesType<U>(Expression<Func<M, U, bool>> expression) { return this; }

		public Mapping<M> UsesConnection(string connection)
		{
			Contract.Requires(connection != null);
			Contract.Requires(connection.Length > 0);

			this.ConnectionName = connection;
			return this;
		}

		/// <summary>
		///   Sets the database object name
		/// </summary>
		/// <param name="name">
		///   Name of the database object where data is stored for
		///   the type.
		/// </param>
		/// <returns></returns>
		public Mapping<M> WithName(string name)
		{
			Contract.Requires(name != null);
			Contract.Requires(name.Length > 0);

			this.TargetObject = name;
			return this;
		}

		internal void AddContributedColumn(ColumnMapping contributed)
		{
			var name = contributed.TargetName;
			lock (_sync)
			{
				if (_columns.Where(c => c.TargetName == name).SingleOrDefault() != null)
				{
					throw new MappingException(String.Concat("Duplicate column definition: ", name));
				}

				_declaredColumns.Add(contributed);
				_columns.Add(contributed);
			}
		}

		internal void AddDependency(IMapping target, DependencyKind kind, MemberInfo member)
		{
			lock (_sync)
			{
				var dep = _dependencies.Find(d => d.Target == target);
				if (dep == null)
				{
					_dependencies.Add(new Dependency(kind, this, member, target).CalculateDependencyKind());
				}
			}
		}

		internal CollectionMapping<M> DefineCollection(PropertyInfo property)
		{
			var name = property.Name;

			CollectionMapping<M> col;
			lock (_sync)
			{
				if (!_collections.TryGetValue(name, out col))
				{
					_collections.Add(name, col = new CollectionMapping<M>(this, property));
				}
			}
			return (CollectionMapping<M>) col;
		}

		internal ColumnMapping<M> DefineColumn(MemberInfo member)
		{
			var name = member.Name;

			ColumnMapping col;
			lock (_sync)
			{
				if (_columns.Where(c => c.TargetName == name).SingleOrDefault() != null)
				{
					throw new MappingException(String.Concat("Duplicate column definition: ", name));
				}

				col = ColumnMapping.FromMember<M>(this, member, _columns.Count);
				_declaredColumns.Add(col);
				_columns.Add(col);
			}
			return (ColumnMapping<M>) col;
		}

		internal Mapping<M> InitFromMetadata()
		{
			var typ = typeof(M);
			var mod = typ.Module;
			var asm = typ.Assembly;
			WireupCoordinator.Instance.WireupDependencies(asm);
			// module, then assembly - module takes precedence
			if (mod.IsDefined(typeof(MapConnectionAttribute), false))
			{
				var conn = (MapConnectionAttribute) mod
					.GetCustomAttributes(typeof(MapConnectionAttribute), false).First();
				conn.PrepareMapping(this);
			}
			else if (asm.IsDefined(typeof(MapConnectionAttribute), false))
			{
				var conn = (MapConnectionAttribute) asm
					.GetCustomAttributes(typeof(MapConnectionAttribute), false).First();
				conn.PrepareMapping(this);
			}
			if (mod.IsDefined(typeof(MapSchemaAttribute), false))
			{
				var schema = (MapSchemaAttribute) mod
					.GetCustomAttributes(typeof(MapSchemaAttribute), false).First();
				schema.PrepareMapping(this);
			}
			else if (asm.IsDefined(typeof(MapSchemaAttribute), false))
			{
				var schema = (MapSchemaAttribute) asm
					.GetCustomAttributes(typeof(MapSchemaAttribute), false).First();
				schema.PrepareMapping(this);
			}
			foreach (var it in typeof(M).GetTypeHierarchyInDeclarationOrder()
																	.Except(new Type[]
																		{
																			typeof(Object),
																			typeof(INotifyPropertyChanged)
																		}))
			{
				if (it.IsDefined(typeof(MapEntityAttribute), false))
				{
					if (it != typeof(M))
					{
						var baseMapping = Mappings.AccessMappingFor(it);
						baseMapping.NotifySubtype(this);
						_baseTypes.Add(baseMapping);
						this.AddDependency(baseMapping, DependencyKind.Base, null);
						_columns.AddRange(baseMapping.DeclaredColumns);
					}
					else
					{
						var entity = (MapEntityAttribute) it
							.GetCustomAttributes(typeof(MapEntityAttribute), false).Single();
						entity.PrepareMapping(this, it);
					}
				}
			}
			if (Behaviors.HasFlag(EntityBehaviors.MapEnum))
			{
				var idcol = Identity.Columns.Where(c => c.RuntimeType.IsEnum).SingleOrDefault();
				if (idcol == null)
				{
					throw new MappingException(String.Concat("Entity type ", typeof(M).Name,
																									" declares behavior EntityBehaviors.MapEnum but the enum type cannot be determined. Specify an identity column of enum type."));
				}
				var namecol = Columns.Where(c => c.RuntimeType == typeof(String) && c.IsAlternateKey).FirstOrDefault();
				if (namecol == null)
				{
					throw new MappingException(String.Concat("Entity type ", typeof(M).Name,
																									" declares behavior EntityBehaviors.MapEnum but a column to hold the enum name cannot be determined. Specify a string column with alternate key behavior."));
				}
				var names = Enum.GetNames(idcol.Member.GetTypeOfValue());
				namecol.VariableLength = names.Max(n => n.Length);
			}
			return this;
		}

		internal void MapCollectionFromMeta(PropertyInfo p, MapCollectionAttribute mapColl)
		{
			var elmType = p.PropertyType.FindElementType();
			if (!Mappings.ExistsFor(elmType))
			{
				new MappingException(String.Concat(typeof(M).Name, ": reference collection must be mapped over other mapped types: ",
																					p.Name));
			}

			MemberInfo refColumn = null;
			if (mapColl.References == null || mapColl.References.Count() == 0)
			{
				refColumn = InferCollectionReferenceTargetMember(p, elmType);
			}
			else
			{
				var refColumnName = mapColl.References.Select(s => s.Trim()).Where(s => !String.IsNullOrEmpty(s)).First();
				refColumn = elmType.GetProperty(refColumnName);
				if (refColumn == null)
				{
					throw new InvalidOperationException(String.Concat(typeof(M).Name,
																														": relationship property doesn't exist on the target type: ", refColumnName));
				}
			}

			var coll = DefineCollection(p);
			coll.ReferenceJoinMember = refColumn;
			coll.ReferenceBehaviors = mapColl.ReferenceBehaviors;
		}

		internal void MapColumnFromMeta(PropertyInfo p, MapColumnAttribute mapColumn)
		{
			var column = this.DefineColumn(p);
			if (!String.IsNullOrEmpty(mapColumn.TargetName))
			{
				column.WithTargetName(mapColumn.TargetName);
			}
			var behaviors = mapColumn.Behaviors;
			if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				behaviors |= ColumnBehaviors.Nullable;
			}
			column.WithBehaviors(behaviors);
			if (mapColumn.Length > 0)
			{
				column.WithVariableLength(mapColumn.Length);
			}
			if (column.Behaviors.HasFlag(ColumnBehaviors.Identity))
			{
				Identity.AddColumn(column);
			}

			if (Mappings.ExistsFor(p.PropertyType))
			{
				var foreignMapping = Mappings.AccessMappingFor(p.PropertyType);
				var foreignColumn = default(ColumnMapping);
				AddDependency(foreignMapping, DependencyKind.Direct, column.Member);

				if (mapColumn.References == null || mapColumn.References.Count() == 0)
				{
					foreignColumn = foreignMapping.GetPreferredReferenceColumn();
					if (foreignColumn == null)
					{
						throw new MappingException(String.Concat("Relationship not defined between ", typeof(M).Name, ".", p.Name,
																										" and the referenced type: ", p.PropertyType.Name));
					}

					mapColumn.References = new string[] {foreignColumn.Member.Name};
				}
				else
				{
					// Only 1 reference column for now.
					foreignColumn = foreignMapping.Columns.Where(c => c.Member.Name == mapColumn.References.First()).FirstOrDefault();
				}

				if (foreignColumn == null)
				{
					throw new InvalidOperationException(String.Concat("Property '", p.Name,
																														"' references an entity but a relationship cannot be determined."));
				}
				column.DefineReference(foreignColumn, mapColumn.ReferenceBehaviors);
			}
		}

		internal void SetDiscriminator(object discriminator) { this.Discriminator = discriminator; }

		void MarkComplete()
		{
			this.IsComplete = true;
			Action a;
			while (_whenCompleted.TryDequeue(out a))
			{
				a();
			}
		}
	}
}