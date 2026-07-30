using System;
using System.Collections.Generic;

namespace PQM.Core.Exceptions
{
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
}
