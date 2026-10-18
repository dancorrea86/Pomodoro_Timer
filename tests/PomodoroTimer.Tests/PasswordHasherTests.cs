using PomodoroTimer.Services.Security;
using Xunit;

namespace PomodoroTimer.Tests;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void GenerateSalt_ShouldReturnUniqueNonEmptyBase64Strings()
    {
        var salt1 = _hasher.GenerateSalt();
        var salt2 = _hasher.GenerateSalt();

        Assert.False(string.IsNullOrWhiteSpace(salt1));
        Assert.False(string.IsNullOrWhiteSpace(salt2));
        Assert.NotEqual(salt1, salt2);

        var bytes = Convert.FromBase64String(salt1);
        Assert.Equal(16, bytes.Length);
    }

    [Fact]
    public void HashPassword_ShouldReturnConsistentHash_ForSamePasswordAndSalt()
    {
        var salt = _hasher.GenerateSalt();
        var password = "SecurePassword123!";

        var hash1 = _hasher.HashPassword(password, salt);
        var hash2 = _hasher.HashPassword(password, salt);

        Assert.False(string.IsNullOrWhiteSpace(hash1));
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashPassword_ShouldReturnDifferentHashes_ForDifferentSalts()
    {
        var salt1 = _hasher.GenerateSalt();
        var salt2 = _hasher.GenerateSalt();
        var password = "SecurePassword123!";

        var hash1 = _hasher.HashPassword(password, salt1);
        var hash2 = _hasher.HashPassword(password, salt2);

        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData("password123")]
    [InlineData("MySecret#2026")]
    [InlineData("123456")]
    [InlineData("!@#$%^&*()_+~")]
    public void VerifyPassword_ShouldReturnTrue_ForCorrectPassword(string password)
    {
        var salt = _hasher.GenerateSalt();
        var hash = _hasher.HashPassword(password, salt);

        var isValid = _hasher.VerifyPassword(password, hash, salt);

        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_ForWrongPassword()
    {
        var salt = _hasher.GenerateSalt();
        var hash = _hasher.HashPassword("CorrectPassword", salt);

        var isValid = _hasher.VerifyPassword("WrongPassword", hash, salt);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData("", "somehash", "somesalt")]
    [InlineData("pass", "", "somesalt")]
    [InlineData("pass", "somehash", "")]
    [InlineData("pass", "invalid_base64!", "invalid_base64!")]
    public void VerifyPassword_ShouldReturnFalse_ForInvalidInputsWithoutThrowing(string password, string hash, string salt)
    {
        var isValid = _hasher.VerifyPassword(password, hash, salt);

        Assert.False(isValid);
    }
}
