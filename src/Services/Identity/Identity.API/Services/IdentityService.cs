using Identity.API.Data;
using Identity.API.DTOs;
using Identity.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MomoPayment.Caching;
using MomoPayment.Common.Exceptions;
using MomoPayment.Common.Security;

using MomoPayment.EventBus;
using MomoPayment.EventBus.Events;

namespace Identity.API.Services;

public interface IIdentityService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserLookupResponse> LookupUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);
}

public class IdentityService : IIdentityService
{
    private readonly IdentityDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        IdentityDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        ICacheService cacheService,
        IEventBus eventBus,
        ILogger<IdentityService> logger)
    {
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = request.PhoneNumber.Trim();

        var exists = await _dbContext.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken);
        if (exists)
        {
            throw new BadRequestException("Số điện thoại này đã được đăng ký ví MoMo", "PHONE_ALREADY_EXISTS");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            PhoneNumber = normalizedPhone,
            FullName = request.FullName.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New user registered successfully: {UserId}, Phone: {Phone}", user.Id, user.PhoneNumber);

        // Publish event to RabbitMQ so Wallet Service can provision a wallet account
        await _eventBus.PublishAsync(new UserRegisteredEvent
        {
            UserId = user.Id,
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName,
            Email = user.Email
        }, cancellationToken);

        var token = JwtHelper.GenerateToken(user.Id, user.PhoneNumber, user.FullName, _jwtOptions, user.Role);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresIn = _jwtOptions.ExpiryMinutes * 60,
            User = MapToResponse(user)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = request.PhoneNumber.Trim();

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken);

        if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new BadRequestException("Số điện thoại hoặc mật khẩu không chính xác", "INVALID_CREDENTIALS");
        }

        if (!user.IsActive)
        {
            throw new BadRequestException("Tài khoản đã bị tạm khóa, vui lòng liên hệ CSKH", "ACCOUNT_LOCKED");
        }

        var token = JwtHelper.GenerateToken(user.Id, user.PhoneNumber, user.FullName, _jwtOptions, user.Role);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresIn = _jwtOptions.ExpiryMinutes * 60,
            User = MapToResponse(user)
        };
    }

    public async Task<UserResponse> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user_profile:{userId}";
        var cached = await _cacheService.GetAsync<UserResponse>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("Không tìm thấy thông tin người dùng", "USER_NOT_FOUND");
        }

        var response = MapToResponse(user);
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10), cancellationToken);

        return response;
    }

    public async Task<UserLookupResponse> LookupUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phoneNumber.Trim();
        var cacheKey = $"user_lookup:{normalizedPhone}";

        var cached = await _cacheService.GetAsync<UserLookupResponse>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("Số điện thoại chưa đăng ký tài khoản MoMo", "RECIPIENT_NOT_FOUND");
        }

        var maskedPhone = MaskPhoneNumber(user.PhoneNumber);
        var result = new UserLookupResponse
        {
            Id = user.Id,
            PhoneNumber = user.PhoneNumber,
            MaskedPhoneNumber = maskedPhone,
            FullName = user.FullName
        };

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15), cancellationToken);
        return result;
    }

    private static string MaskPhoneNumber(string phone)
    {
        if (phone.Length < 7) return phone;
        return string.Concat(phone.AsSpan(0, 3), "****", phone.AsSpan(phone.Length - 3, 3));
    }

    private static UserResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        PhoneNumber = user.PhoneNumber,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        CreatedAt = user.CreatedAt
    };
}
