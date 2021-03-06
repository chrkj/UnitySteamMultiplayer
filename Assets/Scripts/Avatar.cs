using Steamworks;
using UnityEngine;
using UnityEngine.UI;

    public class Avatar : MonoBehaviour
    {
        public ulong SteamID;
        public Text TextName;
        public Image AvatarImage;

        void Start()
        {
            var user = new Friend(SteamID);
            TextName.text = user.Name;
            LoadAvatar(user);
        }

        private async void LoadAvatar(Friend user)
        {
            var avatarAsync = await user.GetLargeAvatarAsync();
            if (avatarAsync == null) return;
            
            var steamAvatar = avatarAsync.Value;
            Texture2D texture = new Texture2D((int)steamAvatar.Width, (int)steamAvatar.Height);
            for (int x = 0; x < steamAvatar.Width; x++)
            {
                for (int y = 0; y < steamAvatar.Height; y++)
                {
                    var color = steamAvatar.GetPixel(x, y);
                    texture.SetPixel(x, (int)(steamAvatar.Height - y), new Color(color.r / 255.0f, color.g / 255.0f, color.b / 255.0f, color.a / 255.0f));
                }
            }
            texture.Apply();
            AvatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, steamAvatar.Width, steamAvatar.Height), Vector2.zero);
        }
        
    }
