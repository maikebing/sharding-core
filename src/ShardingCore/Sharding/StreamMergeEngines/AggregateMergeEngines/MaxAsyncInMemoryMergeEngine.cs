﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Sharding.Abstractions;
using ShardingCore.Sharding.StreamMergeEngines.Abstractions;

namespace ShardingCore.Sharding.StreamMergeEngines.AggregateMergeEngines
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: 2021/8/18 14:44:53
    * @Ver: 1.0
    * @Email: 326308290@qq.com
    */
    public class MaxAsyncInMemoryMergeEngine<TEntity>:AbstractGenericMethodCallInMemoryAsyncMergeEngine<TEntity>
    {
        public MaxAsyncInMemoryMergeEngine(MethodCallExpression methodCallExpression, IShardingDbContext shardingDbContext) : base(methodCallExpression, shardingDbContext)
        {
        }

        public override async Task<TResult> MergeResultAsync<TResult>(CancellationToken cancellationToken = new CancellationToken())
        {
            var result = await base.ExecuteAsync(async queryable => await ((IQueryable<TResult>)queryable).MaxAsync(cancellationToken), cancellationToken);
            return result.Max();
        }
    }
}