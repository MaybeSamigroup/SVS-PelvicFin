using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using UnityEngine;
#if Aicomi
using HScene = H.HScene;
#else
using HScene = SV.H.HScene;
#endif
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using Character;
using CharacterCreation;
using CoastalSmell;

namespace PelvicFin
{
    enum ToggleProp
    {
        Son, Sack, Condom, Mosaic, Simple, EyesHighlight
    }
    enum RingProp
    {
        EyebrowsPattern, EyesPattern, MouthPattern, Tears
    }
    enum RangeProp
    {
#if SamabakeScramble
        EyesOpen, MouthOpen, NipStand, CheekRed, AssRed, Sweat, Wet, SonSize
#else
        EyesOpen, MouthOpen, NipStand, CheekRed, AssRed, Sweat, Wet
#endif
    }
    enum SceneType
    {
        Custom, HScene
    }
    static class UI
    {
        internal static IDisposable[] Initialize(Plugin plugin) =>
            Initialize(new WindowConfig(plugin, "PelvicFin", new(1000, -400), new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl)));

        static IDisposable[] Initialize(WindowConfig config) => [
            HumanCustomExtension.OnHumanInitialize.Subscribe(tuple => Initialize(config, tuple.Human)),
            SingletonInitializerExtension<HScene>.OnStartup.Subscribe(scene =>Initialize(config, ToHumans(scene)))
        ];

        static IEnumerable<Human> ToHumans(HScene scene) =>
#if Aicomi
            scene._hActorAll.Select(actor => actor.Human);
#else
            scene.Actors.Select(actor => actor.Human);
#endif
        internal static void Initialize(WindowConfig config, Human human) =>
            SceneType.Custom.Initialize(config.Create(260, 500, Plugin.Name), human);

        internal static void Initialize(WindowConfig config, IEnumerable<Human> humans) =>
            SceneType.HScene.Initialize(config.Create(260, 500, Plugin.Name), humans);

        static void Initialize(this SceneType scene, Window window, Human human) =>
            window.Subscriptions.Add(new CompositeDisposable([
                SingletonInitializerExtension<HumanCustom>.OnDestroy
                    .Subscribe(_ => UnityEngine.Object.Destroy(window.Background)),
                ..scene.Subscribe(window, scene.PrepareTemplate(window)
                    .With(UGUI.GameObject(active: true)).transform, human)]));

        static void Initialize(this SceneType scene, Window window, IEnumerable<Human> humans) =>
            window.Subscriptions.Add(new CompositeDisposable([
                SingletonInitializerExtension<HScene>.OnDestroy
                    .Subscribe(_ => UnityEngine.Object.Destroy(window.Background)),
                ..scene.Subscribe(window, scene.PrepareTemplate(window), Toggles(window), humans)
            ]));

        static IEnumerable<Toggle> Toggles(Window window) =>
            Toggles(new GameObject("selection").With(
                window.Content.AsParent() +
                UGUI.ColorPanel +
                UGUI.ToggleGroup() +
                UGUI.LayoutH(padding: UGUI.Offset(5, 0)) +
                UGUI.Interactable(false)
            ).AsParent() + UGUI.Component<Toggle, ToggleGroup>((toggle, group) => toggle.group = group));

        static IEnumerable<Toggle> Toggles(UIDesign design) =>
#if Aicomi
            new string[] { "1st", "2nd", "3rd", "4th", "5th" }
                .Select(name => new GameObject(name).With(UGUI.Toggle(50, 24, UGUI.Text(text: name)) + design))
#else
            new string[] { "1st", "2nd", "3rd" }
                .Select(name => new GameObject(name).With(UGUI.Toggle(80, 24, UGUI.Text(text: name)) + design))
#endif
                .Select(go => go.GetComponent<Toggle>()).ToList();

        static IEnumerable<IDisposable> Subscribe(this SceneType scene, Window window,
            GameObject template, IEnumerable<Toggle> toggles, IEnumerable<Human> humans) =>
            toggles.Zip(humans).SelectMany(((Toggle ui, Human human) tuple) =>
                scene.Subscribe(window, tuple.ui.With(ui => ui.interactable = true),
                    UnityEngine.Object.Instantiate(template, window.Content.transform), tuple.human));

        static IEnumerable<IDisposable> Subscribe(this SceneType scene,
            Window window, Toggle ui, GameObject edit, Human human) => [
                ..scene.Subscribe(window, edit.transform, human),
                ui.OnValueChangedAsObservable().Subscribe(edit.SetActive),
                edit.OnEnableAsObservable().Subscribe(_ => window.Title = human.fileParam.fullname)
            ];

        static GameObject PrepareTemplate(this SceneType scene, Window window) =>
            new GameObject("Template").With(
                window.Content.With(UGUI.LayoutV(spacing: 6)).AsParent(active: false) +
                UGUI.ColorPanel + UGUI.LayoutV(spacing: 5, padding: UGUI.Offset(10, 5)) +
                Enum.GetValues<ToggleProp>().Select(prop => prop.PrepareTemplate(scene)).AsDesign() +
                Enum.GetValues<RingProp>().Select(prop => prop.PrepareTemplate(scene)).AsDesign() +
                Enum.GetValues<RangeProp>().Select(prop => prop.PrepareTemplate(scene)).AsDesign());

        static IEnumerable<IDisposable> Subscribe(this SceneType scene, Window window, Transform edit, Human human) => [
            ..Enum.GetValues<ToggleProp>().SelectMany(prop => prop.Subscribe(scene, window, edit.Find(prop.ToString()), human)),
            ..Enum.GetValues<RingProp>().SelectMany(prop => prop.Subscribe(scene, window, edit.Find(prop.ToString()), human)),
            ..Enum.GetValues<RangeProp>().SelectMany(prop => prop.Subscribe(scene, window, edit.Find(prop.ToString()), human)),
            human.component.OnDestroyAsObservable().Subscribe(_ => UnityEngine.Object.Destroy(window.Background))
        ];

        internal static UIDesign PrepareSceneUI(this SceneType scene, string name) =>
            scene switch
            {
                SceneType.Custom =>
                    UGUI.LayoutH(spacing: 5, padding: UGUI.Offset(0, 2)) +
                    UGUI.Size(width: 240, height: 28) +
                    "Name".AsChild(UGUI.Label(140, 24) + UGUI.Text(text: name)),
                SceneType.HScene =>
                    UGUI.LayoutH(spacing: 2) +
                    UGUI.Size(width: 240, height: 28) +
                    "Check".AsChild(UGUI.Check(26, 26)) +
                    "Name".AsChild(UGUI.Label(106, 24) + UGUI.Text(text: name)),
                _ => throw new ArgumentOutOfRangeException(nameof(scene), scene, null)
            };
        internal static (IObservable<Unit> OnUpdateSet, IObservable<Unit> OnUpdateGet) Subscribe(this SceneType scene, Window window, Transform edit) =>
            scene switch
            {
                SceneType.Custom => (Observable.Never<Unit>(), window.OnUpdate),
                SceneType.HScene => Subscribe(window, edit.Find("Check").gameObject.GetComponent<Toggle>()),
                _ => throw new ArgumentOutOfRangeException(nameof(scene), scene, null)
            };

        static (IObservable<Unit> OnUpdateSet, IObservable<Unit> OnUpdateGet) Subscribe(Window window, Toggle toggle) =>
            (window.OnUpdate.Where(_ => toggle.isOn), window.OnUpdate.Where(_ => !toggle.isOn));
    }
    static class ToggleExtension
    {
        internal static UIDesign PrepareTemplate(this ToggleProp prop, SceneType scene) =>
            prop.ToString().AsChild(
                scene.PrepareSceneUI(prop.ToString()) +
                "Value".AsChild(UGUI.Check(26, 26)) +
                "Enable".AsChild(UGUI.Label(56, 24)));

        internal static IEnumerable<IDisposable> Subscribe(this ToggleProp prop, SceneType scene, Window window, Transform edit, Human human) =>
            Subscribe(edit.Find("Value").GetComponent<Toggle>(), scene.Subscribe(window, edit), prop.Transform(human));

        static IEnumerable<IDisposable> Subscribe(Toggle ui, (IObservable<Unit> OnUpdateSet, IObservable<Unit> OnUpdateGet) obs, (Func<bool> Get, Action<bool> Set) prop) => [
            ui.OnValueChangedAsObservable().Subscribe(prop.Set),
            obs.OnUpdateSet.Subscribe(_ => prop.Set(ui.isOn)),
            obs.OnUpdateGet.Subscribe(_ => ui.SetIsOnWithoutNotify(prop.Get()))
        ];

        static (Func<bool> Get, Action<bool> Set) Transform(this ToggleProp prop, Human human) =>
            prop switch
            {
                ToggleProp.Son => (human.IsSon, human.SetSon),
                ToggleProp.Sack => (human.IsSack, human.SetSack),
                ToggleProp.Condom => (human.IsCondom, human.SetCondom),
                ToggleProp.Mosaic => (human.IsMosaic, human.SetMosaic),
                ToggleProp.Simple => (human.IsSimple, human.SetSimple),
                ToggleProp.EyesHighlight => (human.IsEyeHighlight, human.SetEyeHighlight),
                _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, null)
            };
        static bool IsSon(this Human human) => human.data.Status.visibleSonAlways;
        static void SetSon(this Human human, bool value) => human.data.Status.visibleSonAlways = value;
        static bool IsSack(this Human human) => human.GetRefTransform(Table.RefObjKey.S_Son).Find("o_dan_f").GetComponent<Renderer>().enabled;
        static void SetSack(this Human human, bool value) => human.GetRefTransform(Table.RefObjKey.S_Son).Find("o_dan_f").GetComponent<Renderer>().enabled = value;
        static bool IsCondom(this Human human) => human.data.Status.visibleGomu;
        static void SetCondom(this Human human, bool value) => human.data.Status.visibleGomu = value;
        static bool IsMosaic(this Human human) => !human.hideMoz;
        static void SetMosaic(this Human human, bool value) => human.hideMoz = !value;
        static bool IsSimple(this Human human) => human.data.Status.visibleSimple;
        static void SetSimple(this Human human, bool value) => human.data.Status.visibleSimple = value;
        static bool IsEyeHighlight(this Human human) => !human.data.Status.hideEyesHighlight;
        static void SetEyeHighlight(this Human human, bool value) => human.data.Status.hideEyesHighlight = !value;
    }
    static class RingExtension
    {
        internal static UIDesign PrepareTemplate(this RingProp prop, SceneType scene) =>
            prop.ToString().AsChild(
                scene.PrepareSceneUI(prop.ToString()) +
                "Prev".AsChild(UGUI.Button(28, 24, UGUI.Text(text: "<)"))) +
                "Value".AsChild(UGUI.Label(28, 24)) +
                "Next".AsChild(UGUI.Button(28, 24, UGUI.Text(text: "()>"))));

        internal static IEnumerable<IDisposable> Subscribe(this RingProp prop, SceneType scene, Window window, Transform edit, Human human) =>
            Subscribe(Translate(edit.Find("Value").GetComponent<TextMeshProUGUI>()), edit.Find("Prev").GetComponent<Button>(), edit.Find("Next").GetComponent<Button>(), scene.Subscribe(window, edit), prop.Transform(human));

        static (Func<int> GetUI, Action<int> SetUI) Translate(TextMeshProUGUI ui) =>
            (() => int.Parse(ui.text), value => ui.SetText(value.ToString()));

        static IEnumerable<IDisposable> Subscribe((Func<int> Get, Action<int> Set) ui, Button prev, Button next,
            (IObservable<Unit> OnUpdateSet, IObservable<Unit> OnUpdateGet) obs, (Func<int, int> Ring, Func<int> Get, Action<int> Set) prop) => [
            prev.OnClickAsObservable().Select(_ => prop.Ring(prop.Get() - 1)).Subscribe(ui.Set + prop.Set),
            next.OnClickAsObservable().Select(_ => prop.Ring(prop.Get() + 1)).Subscribe(ui.Set + prop.Set),
            obs.OnUpdateSet.Subscribe(_ => prop.Set(ui.Get())),
            obs.OnUpdateGet.Subscribe(_ => ui.Set(prop.Get()))
        ];

        internal static (Func<int, int> Ring, Func<int> Get, Action<int> Set) Transform(this RingProp prop, Human human) =>
            prop switch
            {
                RingProp.EyebrowsPattern =>
                    (human.EyebrowPattern, human.GetEyebrowPattern, human.SetEyebrowPattern),
                RingProp.EyesPattern =>
                    (human.EyePattern, human.GetEyePattern, human.SetEyePattern),
                RingProp.MouthPattern =>
                    (human.MouthPattern, human.GetMouthPattern, human.SetMouthPattern),
                RingProp.Tears =>
                    (val => Ring(val, 4), human.GetTearsLevel, human.SetTearsLevel),
                _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, null)
            };
        static int Ring(int val, int max) => (val + max) % max;
        static int EyebrowPattern(this Human human, int value) => Ring(value, human.face.eyebrowCtrl.GetMaxPtn());
        static int GetEyebrowPattern(this Human human) => human.data.Status.eyebrowPtn;
        static void SetEyebrowPattern(this Human human, int value) => human.face.ChangeEyebrowPtn(value);
        static int EyePattern(this Human human, int value) => Ring(value, human.face.eyesCtrl.GetMaxPtn());
        static int GetEyePattern(this Human human) => human.data.Status.eyesPtn;
        static void SetEyePattern(this Human human, int value) => human.face.ChangeEyesPtn(value);
        static int MouthPattern(this Human human, int value) => Ring(value, human.face.mouthCtrl.GetMaxPtn());
        static int GetMouthPattern(this Human human) => human.data.Status.mouthPtn;
        static void SetMouthPattern(this Human human, int value) => human.face.ChangeMouthPtn(value);
        static int GetTearsLevel(this Human human) => human.data.Status.tearsLv;
        static void SetTearsLevel(this Human human, int value) => human.data.Status.tearsLv = (byte)value;
    }
    static class RangeExtension
    {
        internal static UIDesign PrepareTemplate(this RangeProp prop, SceneType scene) =>
            prop.ToString().AsChild(scene.PrepareSceneUI(prop.ToString()) + "Range".AsChild(UGUI.Slider(95, 24)));

        internal static IEnumerable<IDisposable> Subscribe(this RangeProp prop, SceneType scene, Window window, Transform edit, Human human) =>
            Subscribe(edit.Find("Range").GetComponent<Slider>(), scene.Subscribe(window, edit), prop.Transform(human));

        static IEnumerable<IDisposable> Subscribe(Slider ui, (IObservable<Unit> OnUpdateSet, IObservable<Unit> OnUpdateGet) obs, (Func<float> Get, Action<float> Set) prop) =>
            [ui.OnValueChangedAsObservable().Subscribe(prop.Set), obs.OnUpdateSet.Subscribe(_ => prop.Set(ui.value)), obs.OnUpdateGet.Subscribe(_ => ui.SetValueWithoutNotify(prop.Get()))];

        internal static (Func<float> Get, Action<float> Set) Transform(this RangeProp prop, Human human) =>
            prop switch
            {
                RangeProp.EyesOpen => (human.GetEyeOpen, human.SetEyeOpen),
                RangeProp.MouthOpen => (human.GetMouthOpen, human.SetMouthOpen),
                RangeProp.NipStand => (human.GetNipStand, human.SetNipStand),
                RangeProp.CheekRed => (human.GetCheekRed, human.SetCheekRed),
                RangeProp.AssRed => (human.GetAssRed, human.SetAssRed),
                RangeProp.Sweat => (human.GetSweat, human.SetSweat),
                RangeProp.Wet => (human.GetWet, human.SetWet),
#if SamabakeScramble
                RangeProp.SonSize => TransformScale(() => human.GetRefTransform(Table.RefObjKey.a_n_dan).parent),
#endif
                _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, null)
            };
        static float GetEyeOpen(this Human human) => human.data.Status.eyesOpenMax;
        static void SetEyeOpen(this Human human, float value) => human.face.ChangeEyesOpenMax(value);
        static float GetMouthOpen(this Human human) => human.data.Status.mouthOpenMax;
        static void SetMouthOpen(this Human human, float value) => human.face.ChangeMouthOpenMax(value);
        static float GetNipStand(this Human human) => human.data.Status.nipStandRate;
        static void SetNipStand(this Human human, float value) => human.body.ChangeNipRate(value);
        static float GetCheekRed(this Human human) => human.data.Status.hohoAkaRate;
        static void SetCheekRed(this Human human, float value) => human.face.ChangeHohoAkaRate(new Il2CppSystem.Nullable<float>(value));
        static float GetAssRed(this Human human) => human.data.Status.siriAkaRate;
        static void SetAssRed(this Human human, float value) => human.body.ChangeSiriAkaRate(new Il2CppSystem.Nullable<float>(value));
        static float GetSweat(this Human human) => human.data.Status.sweatRate;
        static void SetSweat(this Human human, float value) => human.ChangeSweat(value);
        static float GetWet(this Human human) => human.data.Status.wetRate;
        static void SetWet(this Human human, float value) => human.ChangeWet(value);
        static (Func<float> Get, Action<float> Set) TransformScale (this Func<Transform> tf) => (tf.GetScale, tf.SetScale);
        static float GetScale(this Func<Transform> tf) =>
            (ToScale(tf().localScale) - 0.5f) / 2.5f;
        static void SetScale(this Func<Transform> tf, float value) =>
            tf().localScale = ToVector3(0.5f + value * 2.5f);
        static float ToScale(Vector3 values) => (values.x + values.y + values.z) / 3;
        static Vector3 ToVector3(float value) => new(value, value, value);
    }

    [BepInProcess(Process)]
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(CoastalSmell.Plugin.Guid)]
    public partial class Plugin : BasePlugin
    {
        internal static Plugin Instance;
        public const string Name = "PelvicFin";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "1.2.0";
        CompositeDisposable Subscriptions;
        public override void Load() =>
            (Instance, Subscriptions) = (this, [.. UI.Initialize(this)]);
        public override bool Unload() =>
            true.With(Subscriptions.Dispose) && base.Unload();
    }
}