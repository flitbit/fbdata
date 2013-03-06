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
		/// Default is interpreted as non-null.
		/// </summary>
		Default = 0,
		/// <summary>
		/// Indicates the column is nullable.
		/// </summary>
		Nullable = 1,
		/// <summary>
		/// Indicates the column is write-once (only on insert).
		/// </summary>
		Immutable = 2,
		/// <summary>
		/// Indicates the column participates in the entity's identity.
		/// </summary>
		Identity = 4,
		/// <summary>
		/// Indicates the column is an alternate key for the entity.
		/// </summary>
		AlternateKey = 8,
		/// <summary>
		/// Indicates the column is calculated by the database backend.
		/// </summary>
		Calculated = 0x10 | Immutable,
		/// <summary>
		/// Indicates the column is an identity that is system defined.
		/// </summary>
		Synthetic = 0x20 | Identity | Calculated,

		UDT = 0x40,
		/// <summary>
		/// Indicates the column's value is a timestamp that will will be set 
		/// upon insert.
		/// </summary>
		TimestampOnInsert = 0x100 | Calculated,
		/// <summary>
		/// Indicates the column's value is a timestamp that will be updated
		/// with each update to the entity's data.
		/// </summary>
		TimestampOnUpdate = 0x200 | Calculated,
		
		/// <summary>
		/// Indicates the column's value is used for revision tracking (optimistic
		/// update logic).
		/// </summary>
		RevisionTracking = 0x400 | Calculated,

		/// <summary>
		/// Indicates the column's value is generated by LCG. Applicable to 
		/// string, short, int, and long columns.
		/// </summary>
		LinearCongruentGenerated = 0x1000 | Synthetic,
		/// <summary>
		/// Indicates the column's value is generated by LCG and stored as 
		/// hexidecimal digits. Applicable to string columns.
		/// </summary>
		LinearCongruentGeneratedAsHexidecimal = 0x2000 | LinearCongruentGenerated,

		LinearCongruentGeneratedWithCheckDigit = 0x4000 | LinearCongruentGeneratedAsHexidecimal,
	}

	[Flags]
	public enum ReferenceBehaviors
	{
		Lazy = 0,
		Aggressive = 1,
		Enforced = 2,
		EnforcedLazy = Enforced | Lazy,
		EnforcedAggressive = Enforced | Aggressive,
		OnUpdateCascade = 4,
		OnDeleteCascade = 8,
	}

	public enum MappingStrategy
	{
		/// <summary>
		/// Default value.
		/// </summary>
		OneClassOneTable = 0,
		/// <summary>
		/// Reserved (not implemented).
		/// </summary>
		OneInheritancePathOneTable = 1,
		/// <summary>
		/// Reserved (not implemented).
		/// </summary>
		OneInheritanceTreeOneTable = 2,
		/// <summary>
		/// Reserved (not implemented).
		/// </summary>
		OneClassOneTableWithInheritancePathView = 3,
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
		/// Indicates the framework should not perform any DDL in relation to the type. This is the DEFAULT.
		/// </summary>
		None = 0,
		/// <summary>
		/// Indicates any missing database object should be generated.
		/// </summary>
		AnyMissingObject = 1,
	}
	
	public enum EntityBehaviors
	{
		Default = 0,
		/// <summary>
		/// Indicates that all properties should be mapped according to their
		/// names, default type mappings, and any column templates in force.
		/// </summary>
		MapAllProperties = 1,
		/// <summary>
		/// Indicates the entity represents an enum (identity column must be
		/// an enum type) and should be mapped as such.
		/// </summary>
		MapEnum = 2,
		/// <summary>
		/// Indicates the entity should be audited.
		/// </summary>
		Audited = 4,
		/// <summary>
		/// Indicates that the entity should be mapped as defined. This means
		/// any extension columns are not added to the type's data definition 
		/// (such as ETL columns).
		/// </summary>
		DefinedColumnsOnly = 0x40000000,
	}

	public enum EntityOwnershipBehaviors
	{
		Default = 0,
		Assembly = 1,
	}
}