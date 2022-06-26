using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShardingCore.Extensions;
using ShardingCore.Jobs;
using ShardingCore.Jobs.Abstaractions;
using ShardingCore.Logger;
using ShardingCore.Sharding.MergeEngines.ParallelControl;

namespace ShardingCore.Bootstrappers
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: Monday, 21 December 2020 09:10:07
    * @Email: 326308290@qq.com
    */
    public class ShardingBootstrapper : IShardingBootstrapper
    {
        private readonly ILogger<ShardingBootstrapper> _logger;
        private readonly IServiceProvider _internalServiceProvider;
        private readonly IEnumerable<IDbContextTypeCollector> _dbContextTypeCollectors;
        private readonly IJobManager _jobManager;
        private readonly DoOnlyOnce _doOnlyOnce = new DoOnlyOnce();

        public ShardingBootstrapper(IServiceProvider internalServiceProvider,IEnumerable<IDbContextTypeCollector> dbContextTypeCollectors,IJobManager jobManager)
        {
            _logger = InternalLoggerFactory.DefaultFactory .CreateLogger<ShardingBootstrapper>();
            _internalServiceProvider = internalServiceProvider;
            _dbContextTypeCollectors = dbContextTypeCollectors;
            _jobManager = jobManager;
        }
        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            if (!_doOnlyOnce.IsUnDo())
                return;
            _logger.LogDebug("sharding core starting......");
            foreach (var dbContextTypeCollector in _dbContextTypeCollectors)
            {
                var instance = (IShardingDbContextBootstrapper)ActivatorUtilities.CreateInstance(_internalServiceProvider,typeof(ShardingDbContextBootstrapper<>).GetGenericType0(dbContextTypeCollector.ShardingDbContextType));
                _logger.LogDebug($"{dbContextTypeCollector.ShardingDbContextType}  start init......");
                instance.Initialize();
                _logger.LogDebug($"{dbContextTypeCollector.ShardingDbContextType}  complete init");
            }

            if (_jobManager != null && _jobManager.HasAnyJob())
            {
                Task.Factory.StartNew(async () =>
                {
                    await _internalServiceProvider.GetRequiredService<JobRunnerService>().StartAsync();
                }, TaskCreationOptions.LongRunning);
            }
        }

    }
}