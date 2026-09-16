using System;
using System.Collections.Generic;
using System.Linq;

namespace PQM.Core.BaseExceptions
{
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
        public DomainException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ValidationException : DomainException
    {
        public IEnumerable<string> Errors { get; }

        public ValidationException(string message) : base(message)
        {
            Errors = new[] { message };
        }

        public ValidationException(IEnumerable<string> errors) : base("One or more validation failures have occurred.")
        {
            Errors = errors;
        }
    }

    public class NotFoundException : DomainException
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string entityName, object key) : base($"Entity '{entityName}' with key '{key}' was not found.") { }
    }
}