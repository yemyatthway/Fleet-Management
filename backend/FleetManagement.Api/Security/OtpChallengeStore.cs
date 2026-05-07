using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace FleetManagement.Api.Security;

public record OtpChallenge(string ChallengeId, string UserId, string Code, bool RememberMe, DateTime ExpiresAt);

public class OtpChallengeStore
{
  private readonly ConcurrentDictionary<string, OtpChallenge> _challenges = new(StringComparer.OrdinalIgnoreCase);

  public OtpChallenge Create(string userId, bool rememberMe)
  {
    var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    var challenge = new OtpChallenge(Guid.NewGuid().ToString("N"), userId, code, rememberMe, DateTime.UtcNow.AddMinutes(10));
    _challenges[challenge.ChallengeId] = challenge;
    return challenge;
  }

  public bool TryConsume(string challengeId, string code, out OtpChallenge challenge)
  {
    challenge = new OtpChallenge(string.Empty, string.Empty, string.Empty, false, DateTime.MinValue);
    if (!_challenges.TryRemove(challengeId, out var savedChallenge)) return false;
    if (savedChallenge.ExpiresAt <= DateTime.UtcNow) return false;
    if (!string.Equals(savedChallenge.Code, code.Trim(), StringComparison.Ordinal)) return false;

    challenge = savedChallenge;
    return true;
  }

  public void Remove(string challengeId)
  {
    _challenges.TryRemove(challengeId, out _);
  }
}
