﻿using Rystem.Api.Test.Domain;

namespace Rystem.Api.TestServer.Clients
{
    public class Salubry : ISalubry
    {
        public Task<bool> GetAsync(int id, Stream stream)
            => Task.FromResult(true);
    }
}
