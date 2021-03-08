using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Internal;
using ShardingCore.Core.VirtualTables;
using ShardingCore.DbContexts;
using ShardingCore.DbContexts.ShardingDbContexts;
using ShardingCore.EFCores;
using ShardingCore.Extensions;

namespace ShardingCore.SqlServer
{
/*
* @Author: xjm
* @Description:
* @Date: Tuesday, 29 December 2020 15:22:50
* @Email: 326308290@qq.com
*/
    public class ShardingSqlServerParallelDbContextFactory : IShardingParallelDbContextFactory
    {
        private readonly IVirtualTableManager _virtualTableManager;
        private readonly IShardingDbContextFactory _shardingDbContextFactory;
        private readonly IShardingCoreOptions _shardingCoreOptions;

        public ShardingSqlServerParallelDbContextFactory(IVirtualTableManager virtualTableManager,IShardingCoreOptions shardingCoreOptions, IShardingDbContextFactory shardingDbContextFactory)
        {
            _virtualTableManager = virtualTableManager;
            _shardingDbContextFactory = shardingDbContextFactory;
            _shardingCoreOptions = shardingCoreOptions;
        }

        public DbContext Create(string connectKey,string tail)
        {
            var shardingConfigEntry = _shardingCoreOptions.GetShardingConfig(connectKey);
            var virtualTableConfigs = _virtualTableManager.GetAllVirtualTables(connectKey).GetVirtualTableDbContextConfigs();
            var shardingDbContextOptions = new ShardingDbContextOptions(CreateOptions(connectKey, shardingConfigEntry.ConnectionString), tail, virtualTableConfigs);
            return _shardingDbContextFactory.Create(connectKey, shardingDbContextOptions);
        }

        private DbContextOptions CreateOptions(string connectKey, string connectString)
        {
            return CreateDbContextOptionBuilder(connectKey)
                .UseSqlServer(connectString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .ReplaceService<IQueryCompiler, ShardingQueryCompiler>()
                .ReplaceService<IModelCacheKeyFactory, ShardingModelCacheKeyFactory>()
                .UseShardingSqlServerQuerySqlGenerator()
                .Options;
        }
        private DbContextOptionsBuilder CreateDbContextOptionBuilder(string connectKey)
        {
            var shardingConfigEntry = _shardingCoreOptions.GetShardingConfig(connectKey);
            Type type = typeof(DbContextOptionsBuilder<>);
            type = type.MakeGenericType(shardingConfigEntry.DbContextType);
            return (DbContextOptionsBuilder)Activator.CreateInstance(type);
        }
    }
}