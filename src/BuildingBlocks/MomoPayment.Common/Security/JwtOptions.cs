namespace MomoPayment.Common.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string SecretKey { get; set; } = "MoMoSecureSecretKey2026SuperStrongKeyForPaymentSystem!#@$";
    public string Issuer { get; set; } = "MomoPayment";
    public string Audience { get; set; } = "MomoPaymentApp";
    public int ExpiryMinutes { get; set; } = 1440; // 24 hours
}
