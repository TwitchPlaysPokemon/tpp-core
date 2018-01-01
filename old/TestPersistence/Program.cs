using System;
using System.Linq.Expressions;
using TPPCommon.Models;
using TPPCommon.Persistence;

namespace TestDatabaseThing
{
    class Program
    {
        static void Main(string[] args)
        {
            IPersistence persistence = new MongoPersistence("localhost", 27017, "tpp-new");

            const string id = "asdf123456";
            Expression<Func<User, bool>> idExpression = u => u.Id==id;
            
            var user = new User(
                id,
                "qwertzuiop",
                "Felkbot",
                "felkbot",
                "Félkböt");
            
//            persistence.Save(user);
            
            persistence.ReplaceOne(idExpression, user);
            
            var loadedUser = persistence.FindOne(idExpression);
            
            Console.WriteLine(loadedUser.ProvidedName);
        }
    }
}