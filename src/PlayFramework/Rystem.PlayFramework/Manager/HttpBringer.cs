﻿using System.Text;

namespace Rystem.PlayFramework
{
    internal sealed class HttpBringer
    {
        public string? Method { get; set; }
        public StringBuilder? Query { get; internal set; }
        public string? BodyAsJson { get; internal set; }
    }
}
