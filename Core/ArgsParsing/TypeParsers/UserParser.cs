using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Models;
using Persistence.Repos;

namespace Core.ArgsParsing.TypeParsers
{
    public class UserParser : BaseArgumentParser<User>
    {
        private readonly IUserRepo _userRepo;

        public UserParser(IUserRepo userRepo)
        {
            _userRepo = userRepo;
        }

        public override async Task<ArgsParseResult<User>> Parse(IReadOnlyCollection<string> args, Type[] genericTypes)
        {
            string simpleName = args.First().ToLower();
            var user = await _userRepo.FindBySimpleName(simpleName);
            return user == null
                ? ArgsParseResult<User>.Failure()
                : ArgsParseResult<User>.Success(user, args.Skip(1).ToImmutableList());
        }
    }
}
