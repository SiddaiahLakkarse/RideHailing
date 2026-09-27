namespace RideHailing.Domain;

public enum UserRole { Rider = 1, Driver = 2, Admin = 3 }
public enum UserStatus { Active = 1, Suspended = 2 }
public enum DriverOnboardingStatus { Pending = 1, Approved = 2, Rejected = 3 }
public enum DriverAvailabilityStatus { Offline = 1, Available = 2, OnTrip = 3 }
public enum VehicleStatus { Active = 1, Inactive = 2 }
public enum VehicleType { Standard = 1, Premium = 2, XL = 3 }
public enum RideStatus { Requested = 1, Matching = 2, DriverAssigned = 3, DriverArriving = 4, DriverWaiting = 5, TripStarted = 6, TripCompleted = 7, PaymentPending = 8, Completed = 9, Cancelled = 10, NoDriverFound = 11 }
public enum PaymentMethod { Cash = 1, Online = 2 }
public enum PaymentStatus { Pending = 1, Processing = 2, Succeeded = 3, Failed = 4, RefundPending = 5, Refunded = 6 }
