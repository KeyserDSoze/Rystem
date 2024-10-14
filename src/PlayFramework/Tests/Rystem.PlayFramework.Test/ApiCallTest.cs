﻿namespace Rystem.PlayFramework.Test
{
    public sealed class ApiCallTest
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiCallTest(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        [Theory]
        [InlineData("Che tempo fa oggi a Milano?")]
        public async ValueTask TestAsync(string message)
        {
            var client = _httpClientFactory.CreateClient("client");
            var swagger = await client.GetAsync("/swagger/v1/swagger.json");
            var response = await client.GetAsync($"api/ai/message?m={message}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }
    }
}
