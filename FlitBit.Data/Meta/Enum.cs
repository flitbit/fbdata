﻿#region COPYRIGHT© 2009-2012 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;

namespace FlitBit.Data.Meta
{
	[Flags]
	public enum ColumnBehaviors
	{
		/// <summary>
		///   Default is interpreted as non-null.
		/// </summary>
		Default = 0,

		/// <summary>
		///   Indicates the column is nullable.
		/// </summary>
		Nullable = 1,

		/// <summary>
		///   Indicates the column is write-once (only on insert).
		/// </summary>
		Immutable = 1 << 1,

		/// <summary>
		///   Indicates the column participates in the entity's identity.
		/// </summary>
		Identity = 1 << 2,

		/// <summary>
		///   Indicates the column is an alternate key for the entity.
		/// </summary>
		AlternateKey = 1 << 3,

		/// <summary>
		///   Indicates the column is calculated by the database backend.
		/// </summary>
		Calculated = (1 << 4) | Immutable,

		/// <summary>
		///   Indicates the column is an identity that is system defined.
		/// </summary>
		Synthetic = (1 << 4) | Identity | Calculated,

		/// <summary>
		///   Indicates the column is a discriminator.
		/// </summary>
		Discriminator = (1 << 5) | Calculated,

		UDT = 1 << 6,

		/// <summary>
		///   Indicates the column's value is a timestamp that will will be set
		///   upon insert.
		/// </summary>
		TimestampOnInsert = (1 << 7) | Calculated,

		/// <summary>
		///   Indicates the column's value is a timestamp that will be updated
		///   with each update to the entity's data.
		/// </summary>
		TimestampOnUpdate = (1 << 8) | Calculated,

		/// <summary>
		///   Indicates the column's value is used for revision tracking (optimistic
		///   update logic).
		/// </summary>
		RevisionConcurrency = (1 << 9) | Calculated,

		/// <summary>
		///   Indicates the column's value is generated by LCG. Applicable to
		///   string, short, int, and long columns.
		/// </summary>
		LinearCongruentGenerated = (1 << 10) | Synthetic,

		/// <summary>
		///   Indicates the column's value is generated by LCG and stored as
		///   hexidecimal digits. Applicable to string columns.
		/// </summary>
		LinearCongruentGeneratedAsHexidecimal = (1 << 11) | LinearCongruentGenerated,

		LinearCongruentGeneratedWithCheckDigit = (1 << 12) | LinearCongruentGeneratedAsHexidecimal,
	}

	[Flags]
	public enum ReferenceBehaviors
	{
		Lazy = 0,
		Aggressive = 1,
		Enforced = 1 << 1,
		EnforcedAggressive = Enforced | Aggressive,
		OnUpdateCascade = 1 << 2,
		OnDeleteCascade = 1 << 3,
		OnDeleteSetNull = 1 << 4,
		OnDeleteSetDefault = 1 << 5
	}

	/// <summary>
	///   Indicates the model binding scheme used.
	/// </summary>
	/// <remarks>
	///   These are analogous to ORM patterns documented since
	///   the `90s here http://www.objectarchitects.de/ObjectArchitects/orpatterns/
	///   and later here http://martinfowler.com/eaaCatalog/
	/// </remarks>
	public enum MappingStrategy
	{
		/// <summary>
		///   Indicates the default `DynamicHybridInheritanceTree`
		/// </summary>
		Default = 0,

		/// <summary>
		///   A hybrid inheritance path scheme.
		/// </summary>
		DynamicHybridInheritanceTree = 0,

		/// <summary>
		///   Indicates that one class maps to one table (Reserved, not implemented).
		/// </summary>
		/// <remarks>
		///   As described by http://www.objectarchitects.de/ObjectArchitects/orpatterns/
		///   Map the attributes of each class to a separate table. Insert a Synthetic OID into each table
		///   to link derived classes rows with their parent table's corresponding rows.
		/// </remarks>
		OneClassOneTable = 1,

		/// <summary>
		///   Indicates that one class hierarchy maps to one table (Reserved, not implemented)..
		/// </summary>
		/// <remarks>
		///   As described by http://www.objectarchitects.de/ObjectArchitects/orpatterns/
		///   Use the union of all attributes of all objects in the inheritance hierarchy as the
		///   columns of a single database table. Use Null values to fill the unused fields in each record.
		/// </remarks>
		OneInheritanceTreeOneTable = 2,

		/// <summary>
		///   Maps each class in a hierarchy to its own table (Reserved, not implemented)..
		/// </summary>
		/// <remarks>
		///   As described by http://www.objectarchitects.de/ObjectArchitects/orpatterns/
		///   Map the attributes of each class to a separate table. To a classes’
		///   table add the attributes of all classes the class inherits from.
		/// </remarks>
		OneInheritancePathOneTable = 3,
	}

	public enum MappingAggregationStrategy
	{
		ForeignKeyAggregation = 0,
		SingleTableAggregation = 1,
		OverflowAggregation = 2,
	}

	public enum MappingAssociationStrategy
	{
		ViaForeignKey = 0,
		ViaAssociationTable = 1,
	}

	public enum DdlGeneration
	{
		/// <summary>
		///   Indicates the framework should not perform any DDL in relation to the type. This is the DEFAULT.
		/// </summary>
		None = 0,

		/// <summary>
		///   Indicates any missing database object should be generated.
		/// </summary>
		AnyMissingObject = 1,
	}

	[Flags]
	public enum EntityBehaviors
	{
		/// <summary>
		/// Maps according to declared metadata.
		/// </summary>
		Default = 0,

		/// <summary>
		///   Indicates that all properties should be mapped according to their
		///   names, default type mappings, and any column templates in force.
		/// </summary>
		MapAllProperties = 1,

		/// <summary>
		///   Indicates the entity represents an enum (identity column must be
		///   an enum type) and should be mapped as such.
		/// </summary>
		MapEnum = 1 << 1,

		/// <summary>
		///   Indicates the entity should be audited.
		/// </summary>
		Audited = 1 << 2,

		/// <summary>
		/// Indicates database objects are pluralized ('Person' becomes 'People').
		/// </summary>
		Pluralized = 1 << 3,

		/// <summary>
		/// Indicates the type is a lookup list type and is eligible for long-term
		/// memory caching.
		/// </summary>
		LookupList = 1 << 4,

		/// <summary>
		///   Indicates that the entity should be mapped as defined. This means
		///   any extension columns and mixins are not added to the type's definition.
		/// </summary>
		DefinedColumnsOnly = 0x40000000,
	}

	public enum EntityOwnershipBehaviors
	{
		Default = 0,
		Assembly = 1,
	}
}