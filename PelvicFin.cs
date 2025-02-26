using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using ILLGames.Unity.Component;
using Character;
using CharacterCreation;

namespace PelvicFin
{
    public static class Util
    {
        internal static readonly Il2CppSystem.Threading.CancellationTokenSource Canceler = new();
        static Action AwaitDestroy<T>(Action onSetup, Action onDestroy) where T : SingletonInitializer<T> =>
            () => SingletonInitializer<T>.Instance.gameObject
                    .GetComponentInChildren<ObservableDestroyTrigger>()
                    .AddDisposableOnDestroy(Disposable.Create(onDestroy + AwaitSetup<T>(onSetup, onDestroy)));
        static Action AwaitSetup<T>(Action onSetup, Action onDestroy) where T : SingletonInitializer<T> =>
            () => UniTask.NextFrame().ContinueWith((Action)(() => Hook<T>(onSetup, onDestroy)));
        public static void Hook<T>(Action onSetup, Action onDestroy) where T : SingletonInitializer<T> =>
            SingletonInitializer<T>.WaitUntilSetup(Canceler.Token)
                .ContinueWith(onSetup + AwaitDestroy<T>(onSetup, onDestroy));
    }
    public delegate void Either(Action a, Action b);
    public static class FunctionalExtension
    {
        public static Either Either(bool value) => value ? (left, right) => right() : (left, right) => left();
        public static void Either(this bool value, Action left, Action right) => Either(value)(left, right);
        public static void Maybe(this bool value, Action maybe) => value.Either(() => { }, maybe);
        public static T With<T>(this T input, Action<T> sideEffect) => input.With(() => sideEffect(input));
        public static T With<T>(this T input, Action sideEffect)
        {
            sideEffect();
            return input;
        }
    }
    internal static class UIRef
    {
        internal static Transform Window = SV.Config.ConfigWindow
            .Instance.transform.Find("Canvas").Find("Background").Find("MainWindow");
        internal static GameObject Text => Window
                .Find("Settings").Find("Scroll View").Find("Viewport").Find("Content")
                .Find("CameraSetting").Find("Content").Find("SensitivityX").Find("Title").gameObject;
        internal static GameObject Slider => Window
                .Find("Settings").Find("Scroll View").Find("Viewport").Find("Content")
                .Find("CameraSetting").Find("Content").Find("SensitivityX").Find("Slider").gameObject;
        internal static GameObject Button => Window
                .Find("Settings").Find("Scroll View").Find("Viewport").Find("Content")
                .Find("CameraSetting").Find("Content").Find("SensitivityX").Find("btnReset").gameObject;
        internal static UI UI;
        internal static void Refresh() => (HumanCustom.Instance.Human != null && Plugin.Instance.Status.Value).Either(UI.Hide, UI.Show);
        internal static UniTask RefreshTask = UniTask.CompletedTask;
        internal static void ScheduleRefresh() =>
            RefreshTask.Status.IsCompleted().Maybe(() => RefreshTask = UniTask.NextFrame().ContinueWith((Action)Refresh));
        internal static Action InputCheck = () =>
            Plugin.Instance.Toggle.Value.IsDown()
                .Maybe(() => (Plugin.Instance.Status.Value = !Plugin.Instance.Status.Value).With(ScheduleRefresh));
        internal static void Setup() {
            (HumanCustom.Instance.Human != null).Either(
                () => UniTask.NextFrame().ContinueWith((Action)Setup),
                () => {
                    UI = new UI(new GameObject(Plugin.Name)
                        .With(HumanCustom.Instance.transform.Find("UI").Find("Root").Wrap), () => HumanCustom.Instance.Human, () => {})
                        .With(ScheduleRefresh);
                    Canvas.preWillRenderCanvases += InputCheck;
                });
        }
        internal static void Dispose() {
           Canvas.preWillRenderCanvases -= InputCheck;
            UnityEngine.Object.Destroy(UI.Window);
            UI = null;
        }
        internal static void Initialize() {
            Util.Hook<HumanCustom>(Setup, Dispose);
        }
    }
    internal static class UIRefHScene
    {
        static int CurrentIndex = 0;
        static Human CurrentTarget() =>
            SV.H.HScene.Instance.Actors[CurrentIndex].Human;
        static void ToggleTarget() => CurrentIndex = (CurrentIndex + 1) % SV.H.HScene.Instance.Actors.Count();
        internal static UI UI;
        internal static void Refresh() => Plugin.Instance.Status.Value.Either(UI.Hide, UI.Show);
        internal static UniTask RefreshTask = UniTask.CompletedTask;
        internal static void ScheduleRefresh() =>
            RefreshTask.Status.IsCompleted().Maybe(() => RefreshTask = UniTask.NextFrame().ContinueWith((Action)Refresh));
        internal static Action InputCheck = () =>
            Plugin.Instance.Toggle.Value.IsDown().Maybe(() => (Plugin.Instance.Status.Value = !Plugin.Instance.Status.Value).With(ScheduleRefresh));
        internal static Action<bool> TargetSelect;
        static void Setup() {
            UI = new UI(new GameObject(Plugin.Name)
                .With(SV.H.HScene.Instance.transform.Wrap), CurrentTarget, ToggleTarget)
                .With(ScheduleRefresh).With(ui => TargetSelect = value => value.Maybe(() => ui.Refresh()));
            Canvas.preWillRenderCanvases += InputCheck;
        }
        static void Dispose() {
            Canvas.preWillRenderCanvases -= InputCheck;
            UnityEngine.Object.Destroy(UI.Window);
            UI = null;
        }
        internal static void Initialize() => Util.Hook<SV.H.HScene>(Setup, Dispose);
    }
    internal enum ToggleProp {
        Son, Sack, Condom, EyesHighlight
    }
    internal enum CycledProp {
        EyebrowsPattern, EyesPattern, MouthPattern, Tears
    }
    internal enum RangedProp {
        EyesOpen, MouthOpen, NipStand, CheekRed, AssRed, Sweat, Wet
    }
    internal static class UIFactory
    {
        internal static void Wrap(this Transform tf, GameObject go) => go.transform.SetParent(tf);
        internal static IEnumerable<Renderer> ToRenderers(this Transform tf) =>
            Enumerable.Range(0, tf.childCount).Select(idx => tf.GetChild(idx).gameObject).SelectMany(ToRenderers);
        internal static IEnumerable<Renderer> ToRenderers(this GameObject go) =>
            go.GetComponents<Renderer>().Concat(ToRenderers(go.transform));
        internal static int Cycle(this CycledProp prop, Human human) =>
            prop switch {
                CycledProp.EyebrowsPattern => (human.data.Status.eyebrowPtn + 1) % human.face.eyebrowCtrl.GetMaxPtn(),
                CycledProp.EyesPattern => (human.data.Status.eyesPtn + 1) % human.face.eyesCtrl.GetMaxPtn(),
                CycledProp.MouthPattern => (human.data.Status.mouthPtn + 1) % human.face.mouthCtrl.GetMaxPtn(),
                _ => (human.data.Status.tearsLv + 1) % 4,
            };
        internal static Func<bool> ToGetter(this ToggleProp prop, Func<Human> func) =>
            prop switch {
                ToggleProp.Condom => () => func().data.Status.visibleGomu,
                ToggleProp.EyesHighlight => () => !func().data.Status.hideEyesHighlight,
                ToggleProp.Son => () => func().data.Status.visibleSonAlways,
                _ => () => func().body.objBody?.ToRenderers()
                    .Where(renderer => "o_dan_f".Equals(renderer.name))
                    .Select(renderer => renderer.enabled).FirstOrDefault() ?? false
            };
        internal static Action ToSetter(this ToggleProp prop, Func<Human> func) =>
            prop switch {
                ToggleProp.Condom => () => func().data.Status.visibleGomu = !func().data.Status.visibleGomu,
                ToggleProp.EyesHighlight => () => func().face.HideEyeHighlight(!func().data.Status.hideEyesHighlight),
                ToggleProp.Son => () => func().data.Status.visibleSonAlways = !func().data.Status.visibleSonAlways,
                _ => () => func().body.objBody.ToRenderers()
                    .Where(renderer => "o_dan_f".Equals(renderer.name))
                    .Do(renderer => renderer.enabled = !renderer.enabled)
            };
        internal static Func<int> ToGetter(this CycledProp prop, Func<Human> func) =>
            prop switch {
                CycledProp.EyebrowsPattern => () => func().data.Status.eyebrowPtn,
                CycledProp.EyesPattern => () => func().data.Status.eyesPtn,
                CycledProp.MouthPattern => () => func().data.Status.mouthPtn,
                _ => () => func()?.data?.Status?.tearsLv ?? 0,
            };
        internal static Action ToSetter(this CycledProp prop, Func<Human> func) =>
            prop switch {
                CycledProp.EyebrowsPattern => () => func().face.ChangeEyebrowPtn(prop.Cycle(func()), false),
                CycledProp.EyesPattern => () => func().face.ChangeEyesPtn(prop.Cycle(func()), false),
                CycledProp.MouthPattern => () => func().face.ChangeMouthPtn(prop.Cycle(func()), false),
                _ => () => func().data.Status.tearsLv = (byte)prop.Cycle(func())
            };
        internal static Func<float> ToGetter(this RangedProp prop, Func<Human> func) =>
            prop switch {
                RangedProp.EyesOpen => () => func().data.Status.eyesOpenMax,
                RangedProp.MouthOpen => () => func().data.Status.mouthOpenMax,
                RangedProp.NipStand => () => func().data.Status.nipStandRate,
                RangedProp.CheekRed => () => func().data.Status.hohoAkaRate,
                RangedProp.AssRed => () => func().data.Status.siriAkaRate,
                RangedProp.Sweat => () => func().data.Status.sweatRate,
                _ => () => func().data.Status.wetRate,
            };
        internal static Action<float> ToSetter(this RangedProp prop, Func<Human> func) =>
            prop switch {
                RangedProp.EyesOpen => (value) => func().face.ChangeEyesOpenMax(value),
                RangedProp.MouthOpen => (value) => func().face.ChangeMouthOpenMax(value),
                RangedProp.NipStand => (value) => func().body.ChangeNipRate(value),
                RangedProp.CheekRed => (value) => func().face.ChangeHohoAkaRate(new Il2CppSystem.Nullable<float>(value)),
                RangedProp.AssRed => (value) => func().body.ChangeSiriAkaRate(new Il2CppSystem.Nullable<float>(value)),
                RangedProp.Sweat => (value) => func().ChangeSweat(value),
                _ => (value) => func().ChangeWet(value)
            };
        internal static Action ToEdit(this GameObject go, Action setter, Func<bool> getter) {
            go.GetComponent<LayoutElement>().With(ui => {
                ui.preferredWidth = 230;
                ui.preferredHeight = 30;
            });
            UnityEngine.Object.Instantiate(UIRef.Text, go.transform).With(label =>
             {
                 label.AddComponent<LayoutElement>().preferredWidth = 150;
                 label.GetComponent<TextMeshProUGUI>().SetText(go.name);
             });
            UnityEngine.Object.Instantiate(UIRef.Button, go.transform).AddComponent<LayoutElement>().With(ui => {
                ui.preferredWidth = 30;
                ui.preferredWidth = 30;
            });
             UnityEngine.Object.Instantiate(UIRef.Text, go.transform).With(label =>
             {
                 label.AddComponent<LayoutElement>().preferredWidth = 50;
                 label.GetComponent<TextMeshProUGUI>().With(text => {
                    text.overflowMode = TextOverflowModes.Ellipsis;
                    text.SetText(getter() ? "ON" : "OFF");
                    setter += () => text.SetText(getter() ? "ON" : "OFF");
                });
             });
            go.GetComponentInChildren<Button>().onClick.AddListener(setter);
            return () => go.GetComponentsInChildren<TextMeshProUGUI>()[^1].SetText(getter() ? "ON" : "OFF");
        }
        internal static Action ToEdit(this GameObject go, Action setter, Func<int> getter) {
            go.GetComponent<LayoutElement>().With(ui => {
                ui.preferredWidth = 230;
                ui.preferredHeight = 30;
            });
            UnityEngine.Object.Instantiate(UIRef.Text, go.transform).With(label =>
             {
                 label.AddComponent<LayoutElement>().preferredWidth = 150;
                 label.GetComponent<TextMeshProUGUI>().SetText(go.name);
             });
            UnityEngine.Object.Instantiate(UIRef.Button, go.transform).AddComponent<LayoutElement>().With(ui => {
                ui.preferredWidth = 30;
                ui.preferredWidth = 30;
            });
            UnityEngine.Object.Instantiate(UIRef.Text, go.transform).With(label =>
             {
                 label.AddComponent<LayoutElement>().preferredWidth = 50;
                 label.GetComponent<TextMeshProUGUI>().With(text => {
                    text.overflowMode = TextOverflowModes.Ellipsis;
                    text.SetText(getter().ToString());
                    setter += () => text.SetText(getter().ToString());
                });
             });
            go.GetComponentInChildren<Button>().onClick.AddListener(setter);
            return () => go.GetComponentsInChildren<TextMeshProUGUI>()[^1].SetText(getter().ToString());
        }
        internal static Action ToEdit(this GameObject go, Action<float> setter, Func<float> getter) {
            go.GetComponent<LayoutElement>().With(ui => {
                ui.preferredWidth = 230;
                ui.preferredHeight = 30;
            });
            UnityEngine.Object.Instantiate(UIRef.Text, go.transform).With(label =>
             {
                 label.AddComponent<LayoutElement>().preferredWidth = 150;
                 label.GetComponent<TextMeshProUGUI>().SetText(go.name);
             });
            UnityEngine.Object.Instantiate(UIRef.Slider, go.transform)
                .AddComponent<LayoutElement>().preferredWidth= 80;
            go.GetComponentInChildren<Slider>().With(ui => {
                ui.value = getter();
                ui.minValue = 0.0f;
                ui.maxValue = 1.0f;
                ui.onValueChanged.AddListener(setter);
            });
            return () => go.GetComponentInChildren<Slider>().value = getter();
        }
        internal static void Window(this GameObject go) {
            go.GetComponent<RectTransform>().With(ui =>
            {
                ui.anchorMin = new(0.0f, 1.0f);
                ui.anchorMax = new(0.0f, 1.0f);
                ui.pivot = new(0.0f, 1.0f);
                ui.anchoredPosition = new(1400, -120);
                ui.sizeDelta = new(270, 524);
            });
            go.GetComponent<VerticalLayoutGroup>().With(ui => {
                ui.childControlWidth = true;
                ui.childControlHeight = true;
            });
            go.transform.Find("Title").gameObject.With(title => {
                title.GetComponent<LayoutElement>().With(ui => {
                    ui.preferredWidth = 230;
                    ui.preferredHeight = 44;
                });
                UnityEngine.Object.Destroy(title.transform.Find("Image").gameObject);
                UnityEngine.Object.Destroy(title.transform.Find("btnClose").gameObject);
            });
            go.transform.Find("Settings").gameObject.With(settings => {
                Enumerable.Range(0, settings.transform.childCount)
                    .Select(settings.transform.GetChild)
                    .Select(tf => tf.gameObject).Do(GameObject.Destroy);
                settings.GetComponent<LayoutElement>().preferredHeight = 480;
                settings.AddComponent<VerticalLayoutGroup>().With(ui => {
                    ui.childControlWidth = true;
                    ui.childControlHeight = true;
                    ui.padding = new (20, 20, 10, 10);
                });
            });
            go.AddComponent<UI_DragWindow>();
        }
        internal static void FitLayout<T>(this GameObject go) where T : HorizontalOrVerticalLayoutGroup
        {
            go.AddComponent<RectTransform>().localScale = new(1.0f, 1.0f);
            go.AddComponent<LayoutElement>();
            go.AddComponent<T>().With(ui =>
            {
                ui.childControlWidth = true;
                ui.childControlHeight = true;
            });
            go.AddComponent<ContentSizeFitter>().With(ui =>
            {
                ui.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                ui.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            });
        }
    }
    internal class UI {
        internal GameObject Window;
        internal Action Refresh;
        internal Action Show;
        internal Action Hide;
        internal UI(GameObject go, Func<Human> Target, Action Toggle) {
            go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>().With(ui =>
            {
                ui.referenceResolution = new(1920, 1080);
                ui.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                ui.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            });
            go.AddComponent<GraphicRaycaster>();
            Window = UnityEngine.Object.Instantiate(UIRef.Window.gameObject, go.transform).With(UIFactory.Window);
            Window.transform.Find("Settings").With(content => {
                Refresh = Enum.GetValues<ToggleProp>()
                    .Select(item => new GameObject(item.ToString())
                            .With(content.Wrap) 
                            .With(UIFactory.FitLayout<HorizontalLayoutGroup>)
                            .ToEdit(item.ToSetter(Target), item.ToGetter(Target)))
                    .Concat(Enum.GetValues<CycledProp>()
                        .Select(item => new GameObject(item.ToString())
                            .With(content.Wrap)
                            .With(UIFactory.FitLayout<HorizontalLayoutGroup>)
                            .ToEdit(item.ToSetter(Target), item.ToGetter(Target))))
                    .Concat(Enum.GetValues<RangedProp>()
                        .Select(item => new GameObject(item.ToString())
                            .With(content.Wrap)
                            .With(UIFactory.FitLayout<HorizontalLayoutGroup>)
                            .ToEdit(item.ToSetter(Target), item.ToGetter(Target))))
                    .Aggregate((act1, act2) => act1 + act2);
            });
            Window.transform.Find("Title").With(title => {
                Refresh += () => title.GetComponentInChildren<TextMeshProUGUI>().SetText(Target().fileParam.fullname);
                GameObject.Instantiate(UIRef.Button, title).With(toggle => {
                    toggle.transform.SetSiblingIndex(0);
                    toggle.GetComponent<RectTransform>().With(ui => {
                        ui.anchorMin = new (0.0f, 0.0f);
                        ui.anchorMax = new (0.0f, 0.0f);
                        ui.offsetMin = new (20.0f, 0.0f);
                        ui.offsetMax = new (50.0f, 30.0f);
                        ui.sizeDelta = new (30.0f, 30.0f);
                    });
                    toggle.GetComponent<Button>().onClick.AddListener(Toggle + Refresh);
                });
            });
            Show = Refresh + (() => Window.active = true);
            Hide = Refresh + (() => Window.active = false);
        }
    }
    [BepInProcess(Process)]
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BasePlugin
    {
        internal static Plugin Instance;
        internal ConfigEntry<bool> Status { get; set; }
        internal ConfigEntry<KeyboardShortcut> Toggle { get; set; }
        public const string Process = "SamabakeScramble";
        public const string Name = "PelvicFin";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "1.0.0";
        public override void Load() {
            Status = Config.Bind(new ConfigDefinition("General", "Visibility"), true);
            Toggle = Config.Bind("General", "Toggle PlevicFin GUI", new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl));
            Instance = this;
            UIRef.Initialize();
            UIRefHScene.Initialize();
        }
    }
}