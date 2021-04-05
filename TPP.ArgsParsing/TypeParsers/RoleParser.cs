using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPP.Persistence.Models;

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
                Role parsedRole = (Role)Enum.Parse(typeof(Role), roleToParse, true);
                result = ArgsParseResult<Role>.Success(parsedRole, args.Skip(1).ToImmutableList());
            }
            catch (ArgumentException)
            {
                result = ArgsParseResult<Role>.Failure(string.Format("Did not find a role named '{0}'", roleToParse));
            }
            return Task.FromResult(result);
        }
    }
}
