namespace RideHailing.Contracts;

public sealed record ErrorResponse(string Code, string Message, string TraceId);
