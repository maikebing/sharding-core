﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShardingCore.Extensions;
using ShardingCore.Sharding.MergeEngines.Abstractions.InMemoryMerge;
using ShardingCore.Sharding.MergeEngines.EnumeratorStreamMergeEngines;

namespace ShardingCore.Sharding.MergeEngines
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: 2021/8/17 15:16:36
    * @Ver: 1.0
    * @Email: 326308290@qq.com
    */
    internal class FirstOrDefaultSkipAsyncInMemoryMergeEngine<TEntity> : IEnsureMergeResult<TEntity>
    {
        private readonly StreamMergeContext _streamMergeContext;

        public FirstOrDefaultSkipAsyncInMemoryMergeEngine(StreamMergeContext streamMergeContext)
        {
            _streamMergeContext = streamMergeContext;
        }

        // protected override IExecutor<RouteQueryResult<TEntity>> CreateExecutor0(bool async)
        // {
        //     return new FirstOrDefaultMethodExecutor<TEntity>(GetStreamMergeContext());
        // }
        //
        // protected override TEntity DoMergeResult0(List<RouteQueryResult<TEntity>> resultList)
        // {
        //     var notNullResult = resultList.Where(o => o != null && o.HasQueryResult()).Select(o => o.QueryResult).ToList();
        //
        //     if (notNullResult.IsEmpty())
        //         return default;
        //
        //     var streamMergeContext = GetStreamMergeContext();
        //     if (streamMergeContext.Orders.Any())
        //         return notNullResult.AsQueryable().OrderWithExpression(streamMergeContext.Orders, streamMergeContext.GetShardingComparer()).FirstOrDefault();
        //
        //     return notNullResult.FirstOrDefault();
        // }
        public TEntity MergeResult()
        {
            //将toke改成1
            var asyncEnumeratorStreamMergeEngine = new AsyncEnumeratorStreamMergeEngine<TEntity>(_streamMergeContext);
            var list = asyncEnumeratorStreamMergeEngine.ToStreamList();
            return GetFirstOrDefault(list);
        }

        public async Task<TEntity> MergeResultAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            //将toke改成1
            var asyncEnumeratorStreamMergeEngine = new AsyncEnumeratorStreamMergeEngine<TEntity>(_streamMergeContext);

            var take = _streamMergeContext.GetTake();
            var list = await asyncEnumeratorStreamMergeEngine.ToStreamListAsync(take, cancellationToken);
            return GetFirstOrDefault(list);

        }

        private TEntity GetFirstOrDefault(List<TEntity> list)
        {
            if (list.IsEmpty())
            {
                return default;
            }

            return list.FirstOrDefault();
        }
    }
}