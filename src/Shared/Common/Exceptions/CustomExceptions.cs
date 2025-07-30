namespace Common.Exceptions;

public class BusinessException : Exception
{
    public string ErrorCode { get; }
    
    public BusinessException(string message, string errorCode = "BUSINESS_ERROR") 
        : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public BusinessException(string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class ValidationException : Exception
{
    public Dictionary<string, List<string>> ValidationErrors { get; }
    
    public ValidationException(Dictionary<string, List<string>> validationErrors) 
        : base("One or more validation errors occurred.")
    {
        ValidationErrors = validationErrors;
    }
    
    public ValidationException(string property, string error) 
        : base("Validation error occurred.")
    {
        ValidationErrors = new Dictionary<string, List<string>>
        {
            { property, new List<string> { error } }
        };
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id) 
        : base($"{entityName} with id '{id}' was not found.")
    {
    }
    
    public NotFoundException(string message) 
        : base(message)
    {
    }
}
