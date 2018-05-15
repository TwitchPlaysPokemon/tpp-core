using System.Collections.Generic;

namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// The response from the helix api for the users endpoint.
    /// </summary>
    public class UserApiResponse
    {
        /// <summary>
        /// The total amount of users returned.
        /// </summary>
        public int _total;

        /// <summary>
        /// The users queried.
        /// </summary>
        public List<Users> users;

        public class Users
        {
            /// <summary>
            /// The ID of the user.
            /// </summary>
            public int _id;

            /// <summary>
            /// The user's bio
            /// </summary>
            /// <remarks>
            /// This can be null.
            /// </remarks>
            public string bio;

            /// <summary>
            /// The time the user was created.
            /// </summary>
            public string created_at;

            /// <summary>
            /// The user's display name.
            /// </summary>
            public string display_name;

            /// <summary>
            /// Link to the user's logo.
            /// </summary>
            /// <remarks>
            /// This can be null.
            /// </remarks>
            public string logo;

            /// <summary>
            /// The user's username.
            /// </summary>
            public string name;

            /// <summary>
            /// THe user's type, e.g. staff.
            /// </summary>
            public string type;

            /// <summary>
            /// When this was last updated.
            /// </summary>
            public string updated_at;
        }
    }
}
