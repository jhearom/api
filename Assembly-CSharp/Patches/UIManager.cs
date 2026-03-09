using UnityEngine;
using UnityEngine.UI;
using MonoMod;
using System.Collections;
using System;
using System.Reflection;
using Modding.Utils;

// ReSharper disable All
#pragma warning disable 1591, 0108, 0169, 0649, 0414, CS0626

namespace Modding.Patches
{
    [MonoModPatch("global::UIManager")]
    public class UIManager : global::UIManager
    {
        [MonoModIgnore]
        private static UIManager _instance;

        [MonoModIgnore]
        private global::InputHandler ih;

        [MonoModIgnore]
        private GraphicRaycaster graphicRaycaster;

        [MonoModIgnore]
        private global::GameManager gm;

        [MonoModIgnore]
        private global::GameSettings gs;

        [MonoModIgnore]
        private global::HeroController hero_ctrl;

        [MonoModIgnore]
        private global::PlayerData playerData;

        [MonoModIgnore]
        public Canvas UICanvas;

        [MonoModIgnore]
        public SpriteRenderer gameTitle;

        public MenuScreen currentDynamicMenu { get; set; }

        public static UIManager get_instance()
        {
            if (UIManager._instance == null)
            {
                UIManager._instance = UnityEngine.Object.FindObjectOfType<UIManager>();

                if (UIManager._instance == null)
                {
                    return null;
                }
            }

            return UIManager._instance;
        }

        public static event Action EditMenus
        {
            add
            {
                _editMenus += value;
                if (_instance != null && _instance.hasCalledEditMenus) value();
            }
            remove => _editMenus -= value;
        }

        private static Action _editMenus;

        private Sprite LoadImage() => Assembly.GetExecutingAssembly().LoadEmbeddedSprite("Modding.logo.png", pixelsPerUnit: 100f);

        private static string DescribeCamera(Camera camera) =>
            camera == null ? "<null>" : $"{camera.name}(enabled={camera.enabled},active={camera.gameObject.activeInHierarchy})";

        private static string DescribeCanvas(Canvas canvas) =>
            canvas == null ? "<null>" : $"renderMode={canvas.renderMode},worldCamera={DescribeCamera(canvas.worldCamera)},enabled={canvas.enabled}";

        private void LogUiSnapshot(string reason)
        {
            string modalAlpha = this.modalDimmer != null ? this.modalDimmer.alpha.ToString("0.00") : "<null>";
            Debug.Log(
                $"[MAPI UI] {reason} " +
                $"uiState={this.uiState} menuState={this.menuState} " +
                $"canvas={DescribeCanvas(UICanvas)} " +
                $"modalDimmerAlpha={modalAlpha} " +
                $"gm={(gm != null ? gm.gameObject.name : "<null>")} " +
                $"hero={(hero_ctrl != null ? hero_ctrl.gameObject.name : "<null>")}"
            );
        }

        private void SetupRefs()
        {
            gm = GameManager.instance;
            gs = gm.gameSettings;
            playerData = PlayerData.instance;
            ih = gm.inputHandler;

            if (gm.IsGameplayScene())
            {
                hero_ctrl = HeroController.instance;
            }

            if (gm.IsMenuScene() && gameTitle == null)
            {
                gameTitle = GameObject.Find("LogoTitle").GetComponent<SpriteRenderer>();
            }

            if (SuppressPreloadException.GameCameras.TryGetInstance(out var gameCameras))
            {
                if (gm.IsGameplayScene() && gameCameras.hudCamera != null)
                {
                    if (!gameCameras.hudCamera.gameObject.activeSelf)
                        gameCameras.hudCamera.gameObject.SetActive(true);

                    UICanvas.worldCamera = gameCameras.hudCamera;
                    UICanvas.renderMode = RenderMode.ScreenSpaceCamera;
                }
                else if (UICanvas.worldCamera == null)
                {
                    UICanvas.worldCamera = gameCameras.mainCamera;
                }
            }

            LogUiSnapshot("SetupRefs");
        }

        public extern void orig_Awake();

        public void Awake()
        {
            Transform root = transform.root;
            Debug.Log($"[MAPI DDOL] UIManager.Awake/orig target={gameObject.name} isRoot={ReferenceEquals(root, transform)} root={(root != null ? root.name : "<null>")}");
            orig_Awake();

            if (_instance != this) 
                return;
            
            _editMenus?.Invoke();
            this.hasCalledEditMenus = true;
        }

        private extern void orig_Start();

        private void Start()
        {
            orig_Start();

            if (_instance != this)
                return;
            
            var dlc = transform.Find("UICanvas/MainMenuScreen/TeamCherryLogo/Hidden_Dreams_Logo").gameObject;

            var clone = Instantiate(dlc, dlc.transform.parent);
            clone.SetActive(true);

            var pos = clone.transform.position;
            
            clone.transform.position = pos - new Vector3(0.4f, 0.1f, 0);
            dlc.transform.position = pos + new Vector3(0.6f, 0f, 0);
            clone.transform.localScale *= 0.1f;
            
            var sr = clone.GetComponent<SpriteRenderer>();
            sr.sprite = LoadImage();
        }
        
        private bool hasCalledEditMenus = false;

        public extern IEnumerator orig_HideCurrentMenu();

        public extern void orig_UIGoToPauseMenu();

        public void UIGoToPauseMenu()
        {
            LogUiSnapshot("Before UIGoToPauseMenu");
            orig_UIGoToPauseMenu();
            LogUiSnapshot("After UIGoToPauseMenu");
        }

        public extern void orig_UIClosePauseMenu();

        public void UIClosePauseMenu()
        {
            LogUiSnapshot("Before UIClosePauseMenu");
            orig_UIClosePauseMenu();
            LogUiSnapshot("After UIClosePauseMenu");
        }
        
        public static event Action BeforeHideDynamicMenu;
        public IEnumerator HideCurrentMenu()
        {
            if (((MainMenuState) this.menuState) == MainMenuState.DYNAMIC_MENU)
            {
                BeforeHideDynamicMenu?.Invoke();
                return this.HideMenu(this.currentDynamicMenu);
            }
            else
            {
                return this.orig_HideCurrentMenu();
            }
        }

        [MonoModIgnore]
        public extern void SetMenuState(MainMenuState state);

        [MonoModIgnore]
        public extern Coroutine StartMenuAnimationCoroutine(IEnumerator coro);

        public void UIGoToDynamicMenu(MenuScreen menu)
        {
            this.StartMenuAnimationCoroutine(this.GoToDynamicMenu(menu));
        }

        public IEnumerator GoToDynamicMenu(MenuScreen menu)
        {
            this.ih.StopUIInput();
            yield return this.HideCurrentMenu();
            yield return this.ShowMenu(menu);
            this.currentDynamicMenu = menu;
            this.SetMenuState(MainMenuState.DYNAMIC_MENU);
            this.ih.StartUIInput();
            yield break;
        }

        public void UILeaveDynamicMenu(MenuScreen to, MainMenuState state)
        {
            this.StartMenuAnimationCoroutine(this.LeaveDynamicMenu(to, state));
        }

        public IEnumerator LeaveDynamicMenu(MenuScreen to, MainMenuState state)
        {
            this.ih.StopUIInput();
            yield return this.HideCurrentMenu();
            yield return this.ShowMenu(to);
            this.SetMenuState(state);
            this.ih.StartUIInput();
            yield break;
        }

        [MonoModIgnore]
        private bool ignoreUnpause;

        // mimics the behaviour of the UIManager.SetState method
        public void UIPauseToDynamicMenu(MenuScreen to)
        {
            if (this.uiState != GlobalEnums.UIState.PAUSED)
            {
                this.StartMenuAnimationCoroutine(this.PauseToDynamicMenu(to));
                this.uiState = GlobalEnums.UIState.PAUSED;
            }
        }

        public IEnumerator PauseToDynamicMenu(MenuScreen to)
        {
            this.ih.StopUIInput();
            this.ignoreUnpause = true;
            if (this.uiState == GlobalEnums.UIState.PAUSED)
            {
                if (
                    this.menuState == GlobalEnums.MainMenuState.OPTIONS_MENU ||
                    this.menuState == GlobalEnums.MainMenuState.EXIT_PROMPT
                )
                {
                    yield return this.HideCurrentMenu();
                }
            }
            else
            {
                this.StartCoroutine(this.FadeInCanvasGroupAlpha(this.modalDimmer, 0.8f));
            }
            yield return this.ShowMenu(to);
            this.currentDynamicMenu = to;
            this.SetMenuState(MainMenuState.DYNAMIC_MENU);
            this.ih.StartUIInput();
            this.ignoreUnpause = false;
            yield break;
        }
    }

    [MonoModPatch("GlobalEnums.MainMenuState")]
    public enum MainMenuState
    {
        LOGO,
        MAIN_MENU,
        OPTIONS_MENU,
        GAMEPAD_MENU,
        KEYBOARD_MENU,
        SAVE_PROFILES,
        AUDIO_MENU,
        VIDEO_MENU,
        EXIT_PROMPT,
        OVERSCAN_MENU,
        GAME_OPTIONS_MENU,
        ACHIEVEMENTS_MENU,
        QUIT_GAME_PROMPT,
        RESOLUTION_PROMPT,
        BRIGHTNESS_MENU,
        PAUSE_MENU,
        PLAY_MODE_MENU,
        EXTRAS_MENU,
        REMAP_GAMEPAD_MENU,
        EXTRAS_CONTENT_MENU,
        ENGAGE_MENU,
        NO_SAVE_MENU,

        // Added for the dynamic menu API
        DYNAMIC_MENU
    }
}
