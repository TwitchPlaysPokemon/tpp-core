using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Model;

namespace TPP.ArgsParsing.TypeParsers
{
    /// <summary>
    /// A parser that finds a role by name.
    /// </summary>
    public class RoleParser : BaseArgumentParser<Role>
    {
        public override Task<ArgsParseResult<Role>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            string roleToParse = args[0];
            ArgsParseResult<Role> result;
            try
            {
                Role parsedRole = (Role)Enum.Parse(typeof(Role), roleToParse, ignoreCase: true);
                result = ArgsParseResult<Role>.Success(parsedRole, args.Skip(1).ToImmutableList());
            }
            catch (ArgumentException)
            {
                result = ArgsParseResult<Role>.Failure($"Did not find a role named '{roleToParse}'");
            }
            return Task.FromResult(result);
        }
    }
}
