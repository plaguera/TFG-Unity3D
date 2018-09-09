using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

namespace Model
{
	public class Fader : MonoBehaviour
    {
        public static Fader Instance;
        public float Speed = .1f;
        const float WAIT = .001f;

        UnityEngine.UI.Image Image;
		bool FadingIn, FadingOut;
        
        void Awake()
        {
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}
            Instance = this;
            Image = GetComponent<UnityEngine.UI.Image>();
			SceneManager.activeSceneChanged += ChangedActiveScene;
        }

		void Start()
		{
			FadeIn();
		}

		void ChangedActiveScene(Scene previous, Scene current)
        {
			//FadeOut();
        }

        /// <summary>
        /// Hace aparecer la escena desvaneciendo la imagen negra.
        /// </summary>
        public void FadeIn()
        {
			if (FadingIn) return;
            StartCoroutine(FadeInCoroutine());
        }

        /// <summary>
        /// Hace desaparecer la escena haciendo aparecer la imagen negra.
        /// </summary>
        public void FadeOut()
        {
			if (FadingOut) return;
            StartCoroutine(FadeOutCoroutine());
        }

        IEnumerator FadeInCoroutine()
        {
			while (FadingOut) yield return null;

			FadingIn = true;
            Color colorFade;
            if (Image != null)
            {
                colorFade = Image.color;
                while (Image.color.a > 0)
                {
                    colorFade.a -= Speed;
                    Image.color = colorFade;
                    yield return new WaitForSeconds(WAIT);
                }
                Image.raycastTarget = false;
            }
            else
            {
                colorFade = GetComponent<Text>().color;
                while (GetComponent<Text>().color.a > 0)
                {
                    colorFade.a -= Speed;
                    GetComponent<Text>().color = colorFade;
                    yield return new WaitForSeconds(WAIT);
                }
                GetComponent<Text>().raycastTarget = false;
            }
			FadingIn = false;
        }

        public IEnumerator FadeOutCoroutine()
        {
			while (FadingIn) yield return null;

			FadingOut = true;
            Color colorFade;
            if (Image != null)
            {
                colorFade = Image.color;
                while (Image.color.a < 1)
                {
                    colorFade.a += Speed;
                    Image.color = colorFade;
                    yield return new WaitForSeconds(WAIT);
                }
            }
            else
            {
                colorFade = GetComponent<Text>().color;
                while (GetComponent<Text>().color.a < 1)
                {
                    colorFade.a += Speed;
                    GetComponent<Text>().color = colorFade;
                    yield return new WaitForSeconds(WAIT);
                }
            }
			FadingOut = false;
        }

        public void Reset(float limit)
        {
            try
            {
                Color colorFade = Image.color;
                colorFade.a = limit;
                Image.color = colorFade;
            }
            catch
            {
                Color colorFade = GetComponent<Text>().color;
                colorFade.a = limit;
                GetComponent<Text>().color = colorFade;
            }
        }
    }
}