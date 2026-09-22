using Mozzi.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 손님 줄 칸 하나가 쥔 겹 그림 넷. 프리팹에 넷이 다 있어야 그림을 쓰고, 하나라도 없으면 글자판으로 돌아간다.
    public sealed class CustomerCellView
    {
        // 넷이 다 있나. 겹 자리를 아직 안 만든 프리팹에서도 화면이 뜨게 하려고 둔다.
        public readonly bool HasLayers;

        // 지금 손님 그림이 켜져 있나. 겹 자리는 있는데 png 를 아직 안 구운 동안은 false 다.
        public bool IsShowing { get; private set; }

        private readonly Image _body;
        private readonly Image _hair;
        private readonly Image _shirt;
        private readonly Image _face;

        public CustomerCellView(Transform cellRoot)
        {
            Image body;
            Image hair;
            Image shirt;
            Image face;
            bool hasBody = UiBindUtil.TryFind<Image>(cellRoot, UiNames.CustomerBody, out body);
            bool hasHair = UiBindUtil.TryFind<Image>(cellRoot, UiNames.CustomerHair, out hair);
            bool hasShirt = UiBindUtil.TryFind<Image>(cellRoot, UiNames.CustomerShirt, out shirt);
            bool hasFace = UiBindUtil.TryFind<Image>(cellRoot, UiNames.CustomerFace, out face);

            _body = body;
            _hair = hair;
            _shirt = shirt;
            _face = face;
            HasLayers = hasBody && hasHair && hasShirt && hasFace;
        }

        // 겹마다 스프라이트를 끼우고 켠다. 아직 안 구운 그림은 그 겹만 꺼진다. 몸이 없으면 글자판으로 돌아간다.
        public void Apply(IResourceHub resource, CustomerLook look, CustomerFace face)
        {
            if (HasLayers == false)
            {
                return;
            }

            Sprite body = resource.GetSprite(CustomerArtKeys.Body);
            SetLayer(_body, body);
            SetLayer(_hair, resource.GetSprite(CustomerArtKeys.Hair(look.HairShape, look.HairColor)));
            SetLayer(_shirt, resource.GetSprite(CustomerArtKeys.Shirt(look.ShirtColor)));
            SetLayer(_face, resource.GetSprite(CustomerArtKeys.Face(face)));
            IsShowing = body != null;
        }

        public void Hide()
        {
            IsShowing = false;
            if (HasLayers == false)
            {
                return;
            }

            UiBindUtil.SetActive(_body.gameObject, false);
            UiBindUtil.SetActive(_hair.gameObject, false);
            UiBindUtil.SetActive(_shirt.gameObject, false);
            UiBindUtil.SetActive(_face.gameObject, false);
        }

        private static void SetLayer(Image layer, Sprite sprite)
        {
            layer.sprite = sprite;
            UiBindUtil.SetActive(layer.gameObject, sprite != null);
        }
    }
}
