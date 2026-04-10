using DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models
{
    public enum MediaSortBy { Title, PublishDate, Like }

    public class Media : Record
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string YoutubeId { get; set; }
        public DateTime PublishDate { get; set; } = DateTime.Now;

        public int OwnerId { get; set; } = 1;
        public bool Shared { get; set; } = true;
        [JsonIgnore]
        public User Owner => DB.Users.Get(OwnerId).Copy();

        public override bool IsValid()
        {
            if (!HasRequiredLength(Title, 1)) return false;
            if (!HasRequiredLength(Category, 1)) return false;
            if (!HasRequiredLength(Description, 1)) return false;
            if (DB.Medias.ToList().Where(m => m.YoutubeId == YoutubeId && m.Id != Id).Any()) return false;
            return true;
        }


        public bool LikedByConnectedUser()
        {
            if(MediasLikes().Contains(User.ConnectedUser.Name))
                return true;

            return false;
        }

        public List<string> MediasLikes()
        {
            List<string> likes = new List<string>();

            foreach(Like like in DB.Likes.ToList())
            {
                if (like.MediaID == this.Id)
                    likes.Add(DB.Users.Get(like.UserID).Name);
            }
            return likes;
        }
    }
}