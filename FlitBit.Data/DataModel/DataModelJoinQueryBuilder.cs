﻿#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using FlitBit.Data.Expressions;

namespace FlitBit.Data.DataModel
{
  internal class DataModelJoinQueryBuilder<TDataModel, TIdentityKey, TDbConnection, TJoin>
    : BasicDataModelQueryBuilder<TDataModel, TIdentityKey, TDbConnection>,
      IDataModelJoinQueryBuilder<TDataModel, TIdentityKey, TDbConnection, TJoin>
    where TDbConnection : DbConnection
  {
    readonly DataModelSqlExpression<TDataModel> _sql;

    public DataModelJoinQueryBuilder(IDataModelRepository<TDataModel, TIdentityKey, TDbConnection> repo,
      IDataModelWriter<TDataModel> writer, DataModelSqlExpression<TDataModel> sql)
      : this(repo, writer, Guid.NewGuid().ToString("N"), sql)
    {}

    public DataModelJoinQueryBuilder(IDataModelRepository<TDataModel, TIdentityKey, TDbConnection> repo,
      IDataModelWriter<TDataModel> writer, string key, DataModelSqlExpression<TDataModel> sql)
      : base(repo, writer, key)
    {
      Contract.Requires<ArgumentNullException>(repo != null);
      Contract.Requires<ArgumentNullException>(writer != null);
      Contract.Requires<ArgumentNullException>(key != null);
      Contract.Requires<ArgumentException>(key.Length > 0);
      Contract.Requires<ArgumentNullException>(sql != null);
      _sql = sql;
    }

    public IDataModelJoin2QueryBuilder<TDataModel, TIdentityKey, TDbConnection, TJoin, TJoin1> Join<TJoin1>()
    {
      throw new NotImplementedException();
    }

    public IDataModelJoin2QueryBuilder<TDataModel, TIdentityKey, TDbConnection, TJoin, TJoin1> Join<TJoin1>(
      Expression<Func<TDataModel, TJoin, TJoin1, bool>> predicate)
    {
      throw new NotImplementedException();
    }

    public IDataModelQueryCommand<TDataModel, TDbConnection, TParam> Where<TParam>(
      Expression<Func<TDataModel, TJoin, TParam, bool>> predicate)
    {
      var lambda = (LambdaExpression)predicate;
      var parms = new List<ParameterExpression>(lambda.Parameters);
      _sql.DuplicateSelfParameter(parms[0]);
      _sql.DuplicateJoinParameter(0, parms[1]);
      _sql.AddValueParameter(parms[2]);
      _sql.IngestExpression(lambda.Body);

      return (IDataModelQueryCommand<TDataModel, TDbConnection, TParam>)Repository.ConstructQueryCommand(Key, _sql);
    }

    public IDataModelQueryCommand<TDataModel, TDbConnection, TParam> Where<TParam>(
      Expression<Func<TDataModel, TJoin, TParam, bool>> predicate,
      Action<OrderByBuilder<TDataModel, TJoin>, TDataModel, TJoin> orderByClause)
    {
      var lambda = (LambdaExpression)predicate;
      var parms = new List<ParameterExpression>(lambda.Parameters);
      _sql.DuplicateSelfParameter(parms[0]);
      _sql.DuplicateJoinParameter(0, parms[1]);
      _sql.AddValueParameter(parms[2]);
      _sql.IngestExpression(lambda.Body);
      _sql.SetOrderBy(
        exp => new OrderByBuilder<TDataModel, TJoin>(exp),
        orderByClause);
      
      return (IDataModelQueryCommand<TDataModel, TDbConnection, TParam>)Repository.ConstructQueryCommand(Key, _sql);
    }

    public IDataModelQueryCommand<TDataModel, TDbConnection, TParam, TParam1> Where<TParam, TParam1>(
      Expression<Func<TDataModel, TJoin, TParam, TParam1, bool>> predicate)
    {
      var lambda = (LambdaExpression)predicate;
      var parms = new List<ParameterExpression>(lambda.Parameters);
      _sql.DuplicateSelfParameter(parms[0]);
      _sql.DuplicateJoinParameter(0, parms[1]);
      _sql.AddValueParameter(parms[2]);
      _sql.AddValueParameter(parms[3]);
      _sql.IngestExpression(lambda.Body);

      return (IDataModelQueryCommand<TDataModel, TDbConnection, TParam, TParam1>)Repository.ConstructQueryCommand(Key, _sql);
    }

    public IDataModelQueryCommand<TDataModel, TDbConnection, TParam, TParam1> Where<TParam, TParam1>(
      Expression<Func<TDataModel, TJoin, TParam, TParam1, bool>> predicate,
      Action<OrderByBuilder<TDataModel, TJoin>, TDataModel, TJoin> orderByClause)
    {
      var lambda = (LambdaExpression)predicate;
      var parms = new List<ParameterExpression>(lambda.Parameters);
      _sql.DuplicateSelfParameter(parms[0]);
      _sql.DuplicateJoinParameter(0, parms[1]);
      _sql.AddValueParameter(parms[2]);
      _sql.AddValueParameter(parms[3]);
      _sql.IngestExpression(lambda.Body);
      _sql.SetOrderBy(
        exp => new OrderByBuilder<TDataModel, TJoin>(exp),
        orderByClause
        );

      return (IDataModelQueryCommand<TDataModel, TDbConnection, TParam, TParam1>)Repository.ConstructQueryCommand(Key, _sql);
    }
  }

  internal class DataModelJoinQueryBuilder<TDataModel, TIdentityKey, TDbConnection, TJoin, TParam>
    : BasicDataModelQueryBuilder<TDataModel, TIdentityKey, TDbConnection>,
      IDataModelJoinQueryBuilder<TDataModel, TIdentityKey, TDbConnection, TJoin, TParam>
    where TDbConnection : DbConnection
  {
    readonly DataModelSqlExpression<TDataModel> _sql;

    public DataModelJoinQueryBuilder(IDataModelRepository<TDataModel, TIdentityKey, TDbConnection> repo,
      IDataModelWriter<TDataModel> writer, DataModelSqlExpression<TDataModel> sql)
      : this(repo, writer, Guid.NewGuid().ToString("N"), sql)
    {}

    public DataModelJoinQueryBuilder(IDataModelRepository<TDataModel, TIdentityKey, TDbConnection> repo,
      IDataModelWriter<TDataModel> writer, string key, DataModelSqlExpression<TDataModel> sql)
      : base(repo, writer, key)
    {
      Contract.Requires<ArgumentNullException>(repo != null);
      Contract.Requires<ArgumentNullException>(writer != null);
      Contract.Requires<ArgumentNullException>(key != null);
      Contract.Requires<ArgumentException>(key.Length > 0);
      Contract.Requires<ArgumentNullException>(sql != null);
      _sql = sql;
    }

    public IDataModelJoin2QueryBuilder<TDataModel, TIdentityKey, TDbConnection, TJoin, TJoin1> Join<TJoin1>()
    {
      throw new NotImplementedException();
    }

    public IDataModelJoin2QueryBuilder<TDataModel, TIdentityKey, TDbConnection, TJoin, TJoin1, TParam> Join<TJoin1>(
      Expression<Func<TDataModel, TJoin, TJoin1, bool>> predicate)
    {
      throw new NotImplementedException();
    }

    public IDataModelQueryCommand<TDataModel, TDbConnection, TParam, TParam1> Where<TParam1>(
      Expression<Func<TDataModel, TJoin, TParam, bool>> predicate)
    {
      var lambda = (LambdaExpression)predicate;
      var parms = new List<ParameterExpression>(lambda.Parameters);
      _sql.DuplicateSelfParameter(parms[0]);
      _sql.DuplicateJoinParameter(0, parms[1]);
      _sql.DuplicateValueParameter(0, parms[2]);
      _sql.AddValueParameter(parms[3]);
      _sql.IngestExpression(lambda.Body);

      return
        (IDataModelQueryCommand<TDataModel, TDbConnection, TParam, TParam1>)Repository.ConstructQueryCommand(Key, _sql);
    }

    public IDataModelQueryCommand<TDataModel, TDbConnection, TParam> Construct()
    {
      return (IDataModelQueryCommand<TDataModel, TDbConnection, TParam>)Repository.ConstructQueryCommand(Key, _sql);
    }
  }
}