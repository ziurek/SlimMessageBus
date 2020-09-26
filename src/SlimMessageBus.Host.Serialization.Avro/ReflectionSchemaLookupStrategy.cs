﻿using Avro;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SlimMessageBus.Host.Serialization.Avro
{
    /// <summary>
    /// Strategy to lookup message scheme from the static field _SCHEMA (C# classes generated by Apache.Avro.Tools).
    /// </summary>
    public class ReflectionSchemaLookupStrategy : ISchemaLookupStrategy
    {
        private readonly ILogger _logger;
        private readonly IDictionary<Type, Schema> _registry = new Dictionary<Type, Schema>();
        private readonly object _registryLock = new object();

        private readonly string _fieldName;

        public ReflectionSchemaLookupStrategy(ILogger<ReflectionSchemaLookupStrategy> logger, string fieldName = "_SCHEMA")
        {
            _logger = logger;
            _fieldName = fieldName;
        }

        public virtual Schema Lookup(Type type)
        {
            if (!_registry.TryGetValue(type, out var schema))
            {
                lock (_registryLock)
                {
                    // Note: Check again in case multiple threads have been waiting
                    if (!_registry.TryGetValue(type, out schema))
                    {
                        schema = CreateSchema(type);
                        _registry[type] = schema;
                    }
                }
            }

            return schema;
        }

        protected virtual Schema CreateSchema(Type type)
        {
            Schema schema;
            var field = type.GetField(_fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null || field.FieldType != typeof(Schema))
            {
                var msg = $"The type {type} does not have a static {_fieldName} field of type {typeof(Schema)}. Check your configuration.";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            schema = (Schema)field.GetValue(null);
            return schema;
        }
    }
}
