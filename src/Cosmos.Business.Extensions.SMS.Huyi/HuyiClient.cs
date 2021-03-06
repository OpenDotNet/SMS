﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Cosmos.Business.Extensions.SMS.Exceptions;
using Cosmos.Business.Extensions.SMS.Huyi.Configuration;
using Cosmos.Business.Extensions.SMS.Huyi.Core;
using Cosmos.Business.Extensions.SMS.Huyi.Models;
using Cosmos.Business.Extensions.SMS.Huyi.Models.Results;
using WebApiClient;

namespace Cosmos.Business.Extensions.SMS.Huyi {
    public class HuyiClient {
        private readonly HuyiConfig _config;
        private readonly HuyiAccount _account;
        private readonly IHuyiApis _proxy;
        private readonly Action<Exception> _exceptionHandler;

        public HuyiClient(HuyiConfig config, Action<Exception> exceptionHandler = null) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _account = config.Account ?? throw new ArgumentNullException(nameof(config.Account));

            _proxy = HttpApiClient.Create<IHuyiApis>();

            var globalHandle = ExceptionHandleResolver.ResolveHandler();
            globalHandle += exceptionHandler;
            _exceptionHandler = globalHandle;
        }

        public async Task<HuyiResult> SendAsync(HuyiMessage message) {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrWhiteSpace(_account.ApiKey)) throw new ArgumentNullException(nameof(_account.ApiKey));
            if (string.IsNullOrWhiteSpace(_account.AppId)) throw new ArgumentNullException(nameof(_account.AppId));

            message.CheckParameters();

            var bizParams = new Dictionary<string, string>();
            bizParams.Add("account", _account.AppId);
            bizParams.Add("password", _account.ApiKey);
            bizParams.Add("mobile", message.PhoneNumber);
            bizParams.Add("content", message.Message);
            bizParams.Add("format", "json");

            var content = new FormUrlEncodedContent(bizParams);

            return await _proxy.SendAsync(content)
                .Retry(_config.RetryTimes)
                .Handle().WhenCatch<Exception>(e => {
                    _exceptionHandler?.Invoke(e);
                    return ReturnAsDefautlResponse();
                });
        }

        private static HuyiResult ReturnAsDefautlResponse()
            => new HuyiResult {
                Code = 500,
                Message = "解析错误，返回默认结果"
            };

    }
}