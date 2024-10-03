using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Threading.Tasks;
using Moq.Protected;
using System.Threading;
using System.Net;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CoinCapTests
{
    public class CoinCapData
    {
        public string Id { get; set; }
        public string Symbol { get; set; }
        public string CurrencySymbol { get; set; }
        public decimal RateUsd { get; set; }
    }

    public class CoinCapResponse
    {
        public CoinCapData Data { get; set; }
    }

    [TestFixture]
    public class CoinCapServiceTests
    {
        private Mock<IMemoryCache> _memoryCacheMock;
        private Mock<IHttp
