using System;

namespace TPPCore.ChatProviders.DataModels
{
    public class ParrotRecord
    {
        /// <summary>
        /// The ID of the record.
        /// </summary>
        public int id;

        /// <summary>
        /// The contents of the record.
        /// </summary>
        public string contents;

        /// <summary>
        /// The timestamp of the record.
        /// </summary>
        public DateTime timestamp;
    }
}
