namespace SobraBoa.Domain.Exceptions;

public class AuthenticationFailedException(string message) : Exception(message);