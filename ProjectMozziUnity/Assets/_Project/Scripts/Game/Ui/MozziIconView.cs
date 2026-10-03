using Mozzi.Data;
using Mozzi.Game.View;
using Mozzi.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // UI 에 그리는 찹쌀이 한 마리. 농장 것과 같은 겹(몸 · 볼 둘 · 표정 · 악세사리)을 Image 로 쌓는다.
    // 몸만 종 색을 입히고 나머지는 흰색 그대로다. 그림이 아직 없는 겹은 그 겹만 꺼진다.
    public sealed class MozziIconView
    {
        public readonly RectTransform Root;

        private readonly Image _body;
        private readonly Image _cheekL;
        private readonly Image _cheekR;
        private readonly Image _face;
        private readonly Image _acc;

        public MozziIconView(Transform root, string prefabName)
        {
            Root = root as RectTransform;
            _body = UiBindUtil.Find<Image>(root, UiNames.MozziIconBody, prefabName);
            _cheekL = UiBindUtil.Find<Image>(root, UiNames.MozziIconCheekL, prefabName);
            _cheekR = UiBindUtil.Find<Image>(root, UiNames.MozziIconCheekR, prefabName);
            _face = UiBindUtil.Find<Image>(root, UiNames.MozziIconFace, prefabName);
            _acc = UiBindUtil.Find<Image>(root, UiNames.MozziIconAcc, prefabName);
        }

        // 종 그림을 끼우고 켠다. 표정은 평소 표정이다.
        public void Show(IResourceHub resource, MozziSpeciesRow species)
        {
            SetLayer(_body, resource.GetSprite(MozziArtKeys.BodyHop(species.SpriteKey, 0)), MozziArtKeys.BodyColorOf(species));
            SetLayer(_cheekL, resource.GetSprite(MozziArtKeys.Cheek), Color.white);
            SetLayer(_cheekR, resource.GetSprite(MozziArtKeys.Cheek), Color.white);
            SetLayer(_face, resource.GetSprite(MozziArtKeys.Face(MozziArtKeys.BaseFaceIndex)), Color.white);

            string accessoryId = species.AccessoryId;
            if (string.IsNullOrEmpty(accessoryId))
            {
                accessoryId = MozziArtKeys.NoneId;
            }

            SetLayer(_acc, resource.GetSprite(MozziArtKeys.Accessory(accessoryId)), Color.white);
            UiBindUtil.SetActive(Root.gameObject, true);
        }

        // 몸 모양만 한 색으로. 「아직 모르는 찹쌀이」 그림자에 쓴다.
        public void ShowSilhouette(IResourceHub resource, MozziSpeciesRow species, Color color)
        {
            SetLayer(_body, resource.GetSprite(MozziArtKeys.BodyHop(species.SpriteKey, 0)), color);
            UiBindUtil.SetActive(_cheekL.gameObject, false);
            UiBindUtil.SetActive(_cheekR.gameObject, false);
            UiBindUtil.SetActive(_face.gameObject, false);
            UiBindUtil.SetActive(_acc.gameObject, false);
            UiBindUtil.SetActive(Root.gameObject, true);
        }

        public void Hide()
        {
            UiBindUtil.SetActive(Root.gameObject, false);
        }

        private static void SetLayer(Image layer, Sprite sprite, Color color)
        {
            layer.sprite = sprite;
            layer.color = color;
            UiBindUtil.SetActive(layer.gameObject, sprite != null);
        }
    }
}
