using System.Collections.Generic;

namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// The response from the helix api for the users endpoint.
    /// </summary>
    public class UserApiResponse
    {
        /// <summary>
        /// The users queried.
        /// </summary>
        public List<Data> data;

        public class Data
        {
            /// <summary>
            /// User’s ID.
            /// </summary>
            public string id;

            /// <summary>
            /// User’s login name.
            /// </summary>
            public string login;

            /// <summary>
            /// User’s display name.
            /// </summary>
            public string display_name;

            /// <summary>
            /// User’s type: "staff", "admin", "global_mod", or "".
            /// </summary>
            public string type;

            /// <summary>
            /// User’s broadcaster type: "partner", "affiliate", or "".
            /// </summary>
            public string broadcaster_type;

            /// <summary>
            /// User’s channel description.
            /// </summary>
            public string description;

            /// <summary>
            /// URL of the user’s profile image.
            /// </summary>
            public string profile_image_url;

            /// <summary>
            /// URL of the user’s offline image.
            /// </summary>
            public string offline_image_url;

            /// <summary>
            /// Total number of views of the user’s channel.
            /// </summary>
            public int view_count;
        }
    }
}
