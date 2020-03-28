using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Models;
using Persistence.Repos;

namespace ArgsParsing.TypeParsers
{
    /// <summary>
    /// A parser capable of looking up users by name in a <see cref="IUserRepo"/>
    /// and return instances of <see cref="User"/>, if a user with that name was found.
    /// </summary>
    public class UserParser : BaseArgumentParser<User>
    {
        private readonly IUserRepo _userRepo;

        /// <summary>
        /// </summary>
        /// <param name="userRepo">user repository users will be looked up in by name.</param>
        public UserParser(IUserRepo userRepo)
        {
            _userRepo = userRepo;
        }

        public override async Task<ArgsParseResult<User>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            string simpleName = args[0].ToLower();
            var user = await _userRepo.FindBySimpleName(simpleName);
            return user == null
                ? ArgsParseResult<User>.Failure($"did not recognize a user with the name '{simpleName}'")
                : ArgsParseResult<User>.Success(user, args.Skip(1).ToImmutableList());
        }
    }
}
