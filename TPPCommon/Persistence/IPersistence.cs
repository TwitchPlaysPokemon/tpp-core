using System;
using System.Linq.Expressions;
using TPPCommon.Models;

namespace TPPCommon.Persistence
{
    /// <summary>
    /// a persistency layer which is capable of storing and retrieving instances of
    /// <see cref="Model"/> in some sort of persistent datastorage, like a database.
    /// </summary>
    public interface IPersistence
    {
        /// <summary>
        /// Saves a <see cref="Model"/> to the persistency layer.
        /// Throws an exception if the model already existed in the persistency layer.
        /// </summary>
        void Save<TModel>(TModel model) where TModel : Model;
        
        /// <summary>
        /// Replaces an existing model within the persistency layer with a new one.
        /// </summary>
        /// <param name="expression">expression defining what model to match</param>
        /// <param name="replacement">replacement for the model</param>
        /// <param name="upsert">true, if the replacement model should be saved if it didn't exist</param>
        void ReplaceOne<TModel>(Expression<Func<TModel, bool>> expression, TModel replacement, bool upsert = true) where TModel : Model;
        
        /// <summary>
        /// Searches for a model and returns the first match.
        /// </summary>
        /// <param name="expression">expression defining what model to match</param>
        /// <returns>The object found, or null if none was found.</returns>
        TModel FindOne<TModel>(Expression<Func<TModel, bool>> expression) where TModel : Model;
    }
}