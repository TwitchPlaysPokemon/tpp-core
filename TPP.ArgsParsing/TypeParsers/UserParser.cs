using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Model;
using TPP.Persistence;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser capable of looking up users by name in a <see cref="IUserRepo"/>
/// and return instances of <see cref="User"/>, if a user with that name was found.
/// Names may optionally be prefixed with '@' to allow for disambiguation if needed.
/// </summary>
public class UserParser : IArgumentParser<User>
{
    private readonly IUserRepo _userRepo;

    /// <summary>
    /// </summary>
    /// <param name="userRepo">user repository users will be looked up in by name.</param>
    public UserParser(IUserRepo userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<ArgsParseResult<User>> Parse(IImmutableList<string> args, Type[] genericTypes)
    {
        string displayName = args[0];
        bool isPrefixed = displayName.StartsWith('@');
        if (isPrefixed) displayName = displayName[1..];
        User? user = await _userRepo.FindByDisplayName(displayName);
        user ??= await _userRepo.FindBySimpleName(displayName.ToLower());
        return user == null
            ? ArgsParseResult<User>.Failure($"did not recognize a user with the name '{displayName}'",
                isPrefixed ? ErrorRelevanceConfidence.Likely : ErrorRelevanceConfidence.Default)
            : ArgsParseResult<User>.Success(user, args.Skip(1).ToImmutableList());
    }
}
