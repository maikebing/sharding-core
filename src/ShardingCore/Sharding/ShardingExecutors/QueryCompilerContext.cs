﻿using Microsoft.EntityFrameworkCore;
using ShardingCore.Core.EntityMetadatas;
using ShardingCore.Core.ShardingConfigurations.Abstractions;
using ShardingCore.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using ShardingCore.Extensions;
using ShardingCore.Sharding.Abstractions;
using ShardingCore.Sharding.ShardingExecutors.Abstractions;
using ShardingCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ShardingCore.Sharding.ShardingExecutors
{
    public class QueryCompilerContext: IQueryCompilerContext
    {
        private readonly ISet<Type> _queryEntities;
        private readonly IShardingDbContext _shardingDbContext;
        private readonly Expression _queryExpression;
        private readonly IEntityMetadataManager _entityMetadataManager;
        private readonly Type _shardingDbContextType;
        private readonly IShardingEntityConfigOptions _entityConfigOptions;
        private   QueryCompilerExecutor _queryCompilerExecutor;
        private bool? hasQueryCompilerExecutor;
        private bool? _isNoTracking;
        private bool _isUnion;
        private readonly bool _isParallelQuery;

        private QueryCompilerContext( IShardingDbContext shardingDbContext, Expression queryExpression)
        {
            _shardingDbContextType = shardingDbContext.GetType();
            _queryEntities = ShardingUtil.GetQueryEntitiesByExpression(queryExpression, _shardingDbContextType);
            _isNoTracking = queryExpression.GetIsNoTracking();
            _isUnion = queryExpression.GetIsUnion();
            _shardingDbContext = shardingDbContext;
            _queryExpression = queryExpression;
            _entityMetadataManager = (IEntityMetadataManager)ShardingContainer.GetService(typeof(IEntityMetadataManager<>).GetGenericType0(_shardingDbContextType));

            _entityConfigOptions = ShardingContainer.GetRequiredShardingEntityConfigOption(_shardingDbContextType);
            //原生对象的原生查询如果是读写分离就需要启用并行查询
            _isParallelQuery = shardingDbContext.IsUseReadWriteSeparation() && _shardingDbContext.CurrentIsReadWriteSeparation();
        }

        public static QueryCompilerContext Create(IShardingDbContext shardingDbContext, Expression queryExpression)
        {
            return new QueryCompilerContext(shardingDbContext, queryExpression);
        }

        public ISet<Type> GetQueryEntities()
        {
            return _queryEntities;
        }

        public IShardingDbContext GetShardingDbContext()
        {
            return _shardingDbContext;
        }

        public Expression GetQueryExpression()
        {
            return _queryExpression;
        }

        public IEntityMetadataManager GetEntityMetadataManager()
        {
            return _entityMetadataManager;
        }

        public Type GetShardingDbContextType()
        {
            return _shardingDbContextType;
        }

        public bool IsParallelQuery()
        {
            return _isParallelQuery;
        }

        public bool IsQueryTrack()
        {
            var shardingDbContext = (DbContext)_shardingDbContext;
            if (!shardingDbContext.ChangeTracker.AutoDetectChangesEnabled)
                return false;
            if (_isNoTracking.HasValue)
            {
                return !_isNoTracking.Value;
            }
            else
            {
                return shardingDbContext.ChangeTracker.QueryTrackingBehavior ==
                       QueryTrackingBehavior.TrackAll;
            }
        }

        public bool isUnion()
        {
            return _isUnion;
        }

        public QueryCompilerExecutor GetQueryCompilerExecutor()
        {
            if (!hasQueryCompilerExecutor.HasValue)
            {
                hasQueryCompilerExecutor = _queryEntities.All(o => !_entityMetadataManager.IsSharding(o));
                if (hasQueryCompilerExecutor.Value)
                {
                    var virtualDataSource = _shardingDbContext.GetVirtualDataSource();
                    var routeTailFactory = ShardingContainer.GetService<IRouteTailFactory>();
                    var dbContext = _shardingDbContext.GetDbContext(virtualDataSource.DefaultDataSourceName, IsParallelQuery(), routeTailFactory.Create(string.Empty));
                    _queryCompilerExecutor = new QueryCompilerExecutor(dbContext, _queryExpression);
                }
            }

            return _queryCompilerExecutor;
        }

        public bool IsEnumerableQuery()
        {
            return _queryExpression.Type
                .HasImplementedRawGeneric(typeof(IQueryable<>));
        }
    }
}
