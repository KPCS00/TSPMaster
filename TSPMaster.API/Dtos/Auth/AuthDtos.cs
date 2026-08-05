namespace TSPMaster.API.Dtos.Auth;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    string FirstName,
    string LastName
);
