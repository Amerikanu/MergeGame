using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leedong.MergeGame
{
    public class Block : MonoBehaviour
    {
        public enum Type
        {
            Red, Orange, Yellow, Green, Blue, Pink
        }

        [SerializeField]
        private Transform _transform;

        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        private Type _type;

        public Type BlockType => _type;

        void OnEnable()
        {
            _transform.localScale = new Vector3(0.5f, 0.5f, 1);
        }

        public void SetBlockType(Type type, Sprite sprite)
        {
            _type = type;
            _spriteRenderer.sprite = sprite;
        }

        public void SetRendererOrder(int order)
        {
            _spriteRenderer.sortingOrder = order;
        }

        public void Pop()
        {
            StartCoroutine(IPop());
        }

        private IEnumerator IPop()
        {
            float scale = 1f;

            while (scale > 0)
            {
                _transform.localScale = Vector3.one * scale;
                scale -= 4f * Time.deltaTime;
                yield return null;
            }
        }

        void OnDisable()
        {            
            StopAllCoroutines();
        }

    }
}