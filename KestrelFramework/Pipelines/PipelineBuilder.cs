﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KestrelFramework.Pipelines
{
    /// <summary>
    /// 表示中间件创建者
    /// </summary>
    public class PipelineBuilder<TContext>
    {
        private readonly InvokeDelegate<TContext> fallbackHandler;
        private readonly List<Func<InvokeDelegate<TContext>, InvokeDelegate<TContext>>> middlewares = new();

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        public IServiceProvider ApplicationServices { get; }

        /// <summary>
        /// 中间件创建者
        /// </summary>
        /// <param name="appServices"></param>
        public PipelineBuilder(IServiceProvider appServices)
            : this(appServices, context => Task.CompletedTask)
        {
        }

        /// <summary>
        /// 中间件创建者
        /// </summary>
        /// <param name="appServices"></param>
        /// <param name="fallbackHandler">回退处理者</param>
        public PipelineBuilder(IServiceProvider appServices, InvokeDelegate<TContext> fallbackHandler)
        {
            this.ApplicationServices = appServices;
            this.fallbackHandler = fallbackHandler;
        }

        /// <summary>
        /// 创建所有中间件执行处理者
        /// </summary>
        /// <returns></returns>
        public InvokeDelegate<TContext> Build()
        {
            var handler = this.fallbackHandler;
            for (var i = this.middlewares.Count - 1; i >= 0; i--)
            {
                handler = this.middlewares[i](handler);
            }
            return handler;
        }


        /// <summary>
        /// 使用默认配制创建新的PipelineBuilder
        /// </summary>
        /// <returns></returns>
        public PipelineBuilder<TContext> New()
        {
            return new PipelineBuilder<TContext>(this.ApplicationServices, this.fallbackHandler);
        }

        /// <summary>
        /// 条件中间件
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="handler"></param> 
        /// <returns></returns>
        public PipelineBuilder<TContext> When(Func<TContext, bool> predicate, InvokeDelegate<TContext> handler)
        {
            return this.Use(next => async context =>
            {
                if (predicate(context))
                {
                    await handler(context);
                }
                else
                {
                    await next(context);
                }
            });
        }


        /// <summary>
        /// 条件中间件
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="configureAction"></param>
        /// <returns></returns>
        public PipelineBuilder<TContext> When(Func<TContext, bool> predicate, Action<PipelineBuilder<TContext>> configureAction)
        {
            return this.Use(next => async context =>
            {
                if (predicate(context))
                {
                    var branchBuilder = this.New();
                    configureAction(branchBuilder);
                    await branchBuilder.Build().Invoke(context);
                }
                else
                {
                    await next(context);
                }
            });
        }

        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <typeparam name="TMiddleware"></typeparam>
        /// <returns></returns>
        public PipelineBuilder<TContext> Use<TMiddleware>()
            where TMiddleware : IMiddleware<TContext>
        {
            var middleware = ActivatorUtilities.GetServiceOrCreateInstance<TMiddleware>(this.ApplicationServices);
            return this.Use(middleware);
        }

        /// <summary>
        /// 使用中间件
        /// </summary> 
        /// <typeparam name="TMiddleware"></typeparam> 
        /// <param name="middleware"></param>
        /// <returns></returns>
        public PipelineBuilder<TContext> Use<TMiddleware>(TMiddleware middleware)
            where TMiddleware : IMiddleware<TContext>
        {
            return this.Use(middleware.InvokeAsync);
        }

        /// <summary>
        /// 使用中间件
        /// </summary>  
        /// <param name="middleware"></param>
        /// <returns></returns>
        public PipelineBuilder<TContext> Use(Func<InvokeDelegate<TContext>, TContext, Task> middleware)
        {
            return this.Use(next => context => middleware(next, context));
        }

        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="middleware"></param>
        /// <returns></returns>
        public PipelineBuilder<TContext> Use(Func<InvokeDelegate<TContext>, InvokeDelegate<TContext>> middleware)
        {
            this.middlewares.Add(middleware);
            return this;
        }
    }
}