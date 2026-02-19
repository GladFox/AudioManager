using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AudioManagement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class UIButtonSound : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        [SerializeField] private SoundEvent clickEvent;
        [SerializeField] private string clickEventId;
        [SerializeField] private bool playOnSubmit = true;

        public void OnPointerClick(PointerEventData eventData)
        {
            Play();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (playOnSubmit)
            {
                Play();
            }
        }

        public void Play()
        {
            var manager = AudioManager.Instance;
            if (manager == null)
            {
                return;
            }

            if (clickEvent != null)
            {
                manager.PlayUI(clickEvent);
                return;
            }

            if (!string.IsNullOrWhiteSpace(clickEventId))
            {
                manager.PlayUI(clickEventId);
            }
        }
    }
}
