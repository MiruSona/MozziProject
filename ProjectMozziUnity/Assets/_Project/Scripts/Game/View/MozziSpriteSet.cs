using System;
using Mozzi.Data;
using Mozzi.Resource;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 찹쌀이 한 종이 쓰는 레이어 그림 묶음. 없는 그림은 null 이고 켜고 끄는 판단은 MozziVisual 이 한다.
    public sealed class MozziSpriteSet
    {
        public Sprite[] BodyHops { get; private set; }

        public Sprite Cheek { get; private set; }

        public Sprite[] Faces { get; private set; }

        public Sprite Accessory { get; private set; }

        // 몸 그림이 있나. 없으면 흰 네모에 종 색만 입힌 폴백으로 그린다.
        public bool HasBody
        {
            get
            {
                if (BodyHops == null || BodyHops.Length == 0)
                {
                    return false;
                }

                return BodyHops[0] != null;
            }
        }

        public static MozziSpriteSet Load(IResourceHub resource, MozziSpeciesRow species)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (species == null)
            {
                throw new ArgumentNullException(nameof(species));
            }

            MozziSpriteSet set = new MozziSpriteSet();

            set.BodyHops = new Sprite[MozziArtKeys.BodyHopCount];
            for (int i = 0; i < set.BodyHops.Length; i++)
            {
                set.BodyHops[i] = resource.GetSprite(MozziArtKeys.BodyHop(species.SpriteKey, i));
            }

            set.Cheek = resource.GetSprite(MozziArtKeys.Cheek);

            set.Faces = new Sprite[MozziArtKeys.FaceCount];
            for (int i = 0; i < set.Faces.Length; i++)
            {
                set.Faces[i] = resource.GetSprite(MozziArtKeys.Face(i + 1));
            }

            // 종마다 표의 accessoryId 를 쓴다 (반복 01 피드백 3 — 기본형은 왕관 없이). 비어 있으면 안 씌운다.
            string accessoryId = string.IsNullOrEmpty(species.AccessoryId) ? MozziArtKeys.NoneId : species.AccessoryId;
            set.Accessory = resource.GetSprite(MozziArtKeys.Accessory(accessoryId));

            return set;
        }

        // index 는 1 ~ MozziArtKeys.FaceCount. 범위를 벗어나면 null 이라 표정 렌더러가 꺼진다.
        public Sprite FaceOf(int index)
        {
            int i = index - 1;
            if (Faces == null || i < 0 || i >= Faces.Length)
            {
                return null;
            }

            return Faces[i];
        }
    }
}
