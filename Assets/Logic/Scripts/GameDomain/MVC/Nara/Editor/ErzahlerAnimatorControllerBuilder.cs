#if UNITY_EDITOR
using System.IO;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara.Editor
{
    [InitializeOnLoad]
    internal static class ErzahlerAnimatorControllerAutoBuild
    {
        static ErzahlerAnimatorControllerAutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ErzahlerAnimatorControllerBuilder.ErzahlerBookControllerPath) != null)
                    return;
                ErzahlerAnimatorControllerBuilder.BuildAll();
            };
        }
    }

    public static class ErzahlerAnimatorControllerBuilder
    {
        public const string ErzahlerBookControllerPath = "Assets/Art/Animations/erz+book/ERZ_ErzahlerBook.controller";
        public const string ErzahlerSoloControllerPath = "Assets/Art/Animations/Erzahler/ERZ_Erzahler.controller";
        public const string BookCloneControllerPath = "Assets/Art/Animations/Book/ERZ_Book.controller";
        public const string LakiBossControllerPath = "Assets/Art/Animations/MadamLaki/LKI_Animator.controller";
        private const string ControllersSoPath = "Assets/Logic/Scripts/GameDomain/MVC/Nara/Animation/ErzahlerAnimatorControllers.asset";
        private const string ControllersResourcesPath = "Assets/Resources/ErzahlerAnimatorControllers.asset";

        [MenuItem("TI6/Animation/Build All (Erzahler + Laki)", priority = 0)]
        public static void BuildAll()
        {
            BuildErzahlerWithBookOnly();
            BuildErzahlerSoloOnly();
            BuildBookCloneOnly();
            BuildLakiBossOnly();
            Debug.Log("[ErzahlerAnimatorControllerBuilder] Built all animator controllers and updated ErzahlerAnimatorControllers.asset");
        }

        [MenuItem("TI6/Animation/Build/ERZ_ErzahlerBook (player + book)", priority = 10)]
        public static void BuildErzahlerWithBookOnly()
        {
            var controller = BuildErzahlerWithBookController();
            AssignControllerToSo(so => so.ErzahlerWithBook = controller);
            FinishBuild($"ERZ_ErzahlerBook → {ErzahlerBookControllerPath}");
        }

        [MenuItem("TI6/Animation/Build/ERZ_Erzahler (player solo)", priority = 11)]
        public static void BuildErzahlerSoloOnly()
        {
            var controller = BuildErzahlerSoloController();
            AssignControllerToSo(so => so.ErzahlerSolo = controller);
            FinishBuild($"ERZ_Erzahler → {ErzahlerSoloControllerPath}");
        }

        [MenuItem("TI6/Animation/Build/ERZ_Book (clone)", priority = 12)]
        public static void BuildBookCloneOnly()
        {
            var controller = BuildBookCloneController();
            AssignControllerToSo(so => so.BookClone = controller);
            FinishBuild($"ERZ_Book → {BookCloneControllerPath}");
        }

        [MenuItem("TI6/Animation/Build/LKI_Animator (Laki boss)", priority = 13)]
        public static void BuildLakiBossOnly()
        {
            FixLakiAbilityClipLoop();
            var controller = BuildLakiController();
            AssignControllerToSo(so => so.LakiBoss = controller);
            FinishBuild($"LKI_Animator → {LakiBossControllerPath}");
        }

        [MenuItem("TI6/Animation/Build Erzahler & Laki Animator Controllers", priority = 100)]
        public static void BuildAllLegacyMenu() => BuildAll();

        static void AssignControllerToSo(System.Action<ErzahlerAnimatorControllersSO> assign)
        {
            var so = AssetDatabase.LoadAssetAtPath<ErzahlerAnimatorControllersSO>(ControllersSoPath);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<ErzahlerAnimatorControllersSO>();
                AssetDatabase.CreateAsset(so, ControllersSoPath);
            }

            assign(so);
            EditorUtility.SetDirty(so);
            SyncResourcesCopy(so);
        }

        static void FinishBuild(string message)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ErzahlerAnimatorControllerBuilder] {message}");
        }

        private static void FixLakiAbilityClipLoop()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Animations/MadamLaki/Laki_Ability.anim");
            if (clip == null) return;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static AnimatorController BuildErzahlerWithBookController()
        {
            var c = CreateOrReplaceController(ErzahlerBookControllerPath);
            AddPlayerParameters(c);

            var root = c.layers[0].stateMachine;
            var walk1 = LoadClip("Assets/Art/Animations/erz+book/ErzahlerArmature_Erzahler+Book_Walk_1.anim");
            var walk2 = LoadClip("Assets/Art/Animations/erz+book/ErzahlerArmature_Erzahler+Book_Walk_2.anim");
            var fast = LoadClip("Assets/Art/Animations/erz+book/ErzahlerArmature_Erzahler+Book_FastConjuringWithTwoHands.anim");
            var slowPrep = LoadClip("Assets/Art/Animations/erz+book/ErzahlerArmature_Erzahler+Book_SlowConjuring_Prep.anim");
            var slowLoop = LoadClip("Assets/Art/Animations/erz+book/ErzahlerArmature_Erzahler+Book_SlowConjuring_Loop.anim");
            var slowFinish = LoadClip("Assets/Art/Animations/erz+book/ErzahlerArmature_Erzahler+Book_SlowConjuring_Finish.anim");

            var idle = AddMotionState(root, "Idle", walk1, new Vector3(300, 0, 0), ErzahlerAnimatorParams.TagIdle);
            idle.speed = 0f;

            var walk1State = AddMotionState(root, "Walk_1", walk1, new Vector3(300, 120, 0), ErzahlerAnimatorParams.TagLocomotion);
            var walk2State = AddMotionState(root, "Walk_2", walk2, new Vector3(300, 220, 0), ErzahlerAnimatorParams.TagLocomotion);
            var fastState = AddMotionState(root, "FastConjuring", fast, new Vector3(550, 0, 0), "");
            var slowSm = AddSlowConjuringSubMachine(root, slowPrep, slowLoop, slowFinish, new Vector3(550, 120, 0));

            root.defaultState = idle;

            AddTransition(idle, walk1State, AnimatorConditionMode.If, ErzahlerAnimatorParams.Moving);
            AddTransition(idle, walk2State, AnimatorConditionMode.If, ErzahlerAnimatorParams.Moving,
                (ErzahlerAnimatorParams.WalkVariant, 2, AnimatorConditionMode.Equals));
            AddTransition(walk1State, idle, AnimatorConditionMode.IfNot, ErzahlerAnimatorParams.Moving);
            AddTransition(walk2State, idle, AnimatorConditionMode.IfNot, ErzahlerAnimatorParams.Moving);
            AddTransition(walk1State, walk2State, AnimatorConditionMode.Equals, ErzahlerAnimatorParams.WalkVariant, 2);
            AddTransition(walk2State, walk1State, AnimatorConditionMode.Equals, ErzahlerAnimatorParams.WalkVariant, 1);

            AddAnyStateTrigger(root, fastState, ErzahlerAnimatorParams.ConjuringFast);
            AddExitTransition(fastState, idle, 0.95f);
            AddAnyStateTrigger(root, slowSm, ErzahlerAnimatorParams.ConjuringPrep);

            return c;
        }

        private static AnimatorController BuildErzahlerSoloController()
        {
            var c = CreateOrReplaceController(ErzahlerSoloControllerPath);
            AddPlayerParameters(c);

            var root = c.layers[0].stateMachine;
            var idle1 = LoadClip("Assets/Art/Animations/Erzahler/ErzahlerArmature_Erzahler_Idle_1.anim");
            var idle2 = LoadClip("Assets/Art/Animations/Erzahler/ErzahlerArmature_Erzahler_Idle_2.anim");
            var walk = LoadClip("Assets/Art/Animations/Erzahler/ErzahlerArmature_Erzahler_Walk.anim");
            var jog = LoadClip("Assets/Art/Animations/Erzahler/ErzahlerArmature_Erzahler_Jog.anim");
            var fast = LoadClip("Assets/Art/Animations/Erzahler/ErzahlerArmature_Erzahler_FastConjuring.anim");
            var slowPrep = LoadClip("Assets/Art/Animations/Erzahler/ErzahlerArmature_Erzahler_SlowConjuring_Prep.anim");
            var slowLoop = LoadClip("Assets/Art/Animations/Erzahler/ErzahlerArmature_Erzahler_SlowConjuring_Loop.anim");
            var slowFinish = LoadClip("Assets/Art/Animations/Erzahler/ErzahlerArmature_Erzahler_SlowConjuring_Finish.anim");

            var idle1State = AddMotionState(root, "Idle_1", idle1, new Vector3(300, 0, 0), ErzahlerAnimatorParams.TagIdle);
            var idle2State = AddMotionState(root, "Idle_2", idle2, new Vector3(300, 100, 0), ErzahlerAnimatorParams.TagIdle);
            var walkState = AddMotionState(root, "Walk", walk, new Vector3(300, 200, 0), ErzahlerAnimatorParams.TagLocomotion);
            var jogState = AddMotionState(root, "Jog", jog, new Vector3(300, 300, 0), ErzahlerAnimatorParams.TagLocomotion);
            var fastState = AddMotionState(root, "FastConjuring", fast, new Vector3(550, 0, 0), "");
            var slowSm = AddSlowConjuringSubMachine(root, slowPrep, slowLoop, slowFinish, new Vector3(550, 120, 0));

            root.defaultState = idle1State;

            AddTransition(idle1State, idle2State, AnimatorConditionMode.Equals, ErzahlerAnimatorParams.IdleVariant, 2);
            AddTransition(idle2State, idle1State, AnimatorConditionMode.Equals, ErzahlerAnimatorParams.IdleVariant, 1);
            AddTransition(idle1State, walkState, AnimatorConditionMode.If, ErzahlerAnimatorParams.Moving,
                (ErzahlerAnimatorParams.Running, 0, AnimatorConditionMode.IfNot));
            AddTransition(idle2State, walkState, AnimatorConditionMode.If, ErzahlerAnimatorParams.Moving,
                (ErzahlerAnimatorParams.Running, 0, AnimatorConditionMode.IfNot));
            AddTransition(idle1State, jogState, AnimatorConditionMode.If, ErzahlerAnimatorParams.Moving,
                (ErzahlerAnimatorParams.Running, 0, AnimatorConditionMode.If));
            AddTransition(idle2State, jogState, AnimatorConditionMode.If, ErzahlerAnimatorParams.Moving,
                (ErzahlerAnimatorParams.Running, 0, AnimatorConditionMode.If));
            AddTransition(walkState, idle1State, AnimatorConditionMode.IfNot, ErzahlerAnimatorParams.Moving);
            AddTransition(jogState, idle1State, AnimatorConditionMode.IfNot, ErzahlerAnimatorParams.Moving);
            AddTransition(walkState, jogState, AnimatorConditionMode.If, ErzahlerAnimatorParams.Running);
            AddTransition(jogState, walkState, AnimatorConditionMode.IfNot, ErzahlerAnimatorParams.Running);

            AddAnyStateTrigger(root, fastState, ErzahlerAnimatorParams.ConjuringFast);
            AddExitTransition(fastState, idle1State, 0.95f);
            AddAnyStateTrigger(root, slowSm, ErzahlerAnimatorParams.ConjuringPrep);

            return c;
        }

        private static AnimatorController BuildBookCloneController()
        {
            var c = CreateOrReplaceController(BookCloneControllerPath);
            AddParam(c, BookAnimatorParams.Moving, AnimatorControllerParameterType.Bool);
            AddParam(c, BookAnimatorParams.IdleVariant, AnimatorControllerParameterType.Int);
            AddParam(c, BookAnimatorParams.WalkVariant, AnimatorControllerParameterType.Int);
            AddParam(c, BookAnimatorParams.Ability, AnimatorControllerParameterType.Trigger);

            var root = c.layers[0].stateMachine;
            var idle1 = LoadClip("Assets/Art/Animations/Book/ErzahlerArmature_Book_Idle_1.anim");
            var idle2 = LoadClip("Assets/Art/Animations/Book/ErzahlerArmature_Book_Idle_2.anim");
            var idle3 = LoadClip("Assets/Art/Animations/Book/ErzahlerArmature_Book_Idle_3.anim");
            var walk1 = LoadClip("Assets/Art/Animations/Book/ErzahlerArmature_Book_Walk_1.anim");
            var walk2 = LoadClip("Assets/Art/Animations/Book/ErzahlerArmature_Book_Walk_2.anim");
            var ability = LoadClip("Assets/Art/Animations/Book/ErzahlerArmature_Book_Ability.anim");

            var idle1State = AddMotionState(root, "Idle_1", idle1, new Vector3(300, 0, 0), BookAnimatorParams.TagIdle);
            var idle2State = AddMotionState(root, "Idle_2", idle2, new Vector3(300, 90, 0), BookAnimatorParams.TagIdle);
            var idle3State = AddMotionState(root, "Idle_3", idle3, new Vector3(300, 180, 0), BookAnimatorParams.TagIdle);
            var walk1State = AddMotionState(root, "Walk_1", walk1, new Vector3(300, 280, 0), BookAnimatorParams.TagLocomotion);
            var walk2State = AddMotionState(root, "Walk_2", walk2, new Vector3(300, 380, 0), BookAnimatorParams.TagLocomotion);
            var abilityState = AddMotionState(root, "Ability", ability, new Vector3(550, 0, 0), BookAnimatorParams.TagAbility);

            root.defaultState = idle1State;

            AddTransition(idle1State, idle2State, AnimatorConditionMode.Equals, BookAnimatorParams.IdleVariant, 2);
            AddTransition(idle1State, idle3State, AnimatorConditionMode.Equals, BookAnimatorParams.IdleVariant, 3);
            AddTransition(idle2State, idle1State, AnimatorConditionMode.Equals, BookAnimatorParams.IdleVariant, 1);
            AddTransition(idle3State, idle1State, AnimatorConditionMode.Equals, BookAnimatorParams.IdleVariant, 1);

            AddTransition(idle1State, walk1State, AnimatorConditionMode.If, BookAnimatorParams.Moving);
            AddTransition(idle2State, walk1State, AnimatorConditionMode.If, BookAnimatorParams.Moving);
            AddTransition(idle3State, walk1State, AnimatorConditionMode.If, BookAnimatorParams.Moving);
            AddTransition(walk1State, idle1State, AnimatorConditionMode.IfNot, BookAnimatorParams.Moving);
            AddTransition(walk2State, idle1State, AnimatorConditionMode.IfNot, BookAnimatorParams.Moving);
            AddTransition(walk1State, walk2State, AnimatorConditionMode.Equals, BookAnimatorParams.WalkVariant, 2);
            AddTransition(walk2State, walk1State, AnimatorConditionMode.Equals, BookAnimatorParams.WalkVariant, 1);
            AddTransition(idle1State, walk2State, AnimatorConditionMode.If, BookAnimatorParams.Moving,
                (BookAnimatorParams.WalkVariant, 2, AnimatorConditionMode.Equals));

            AddAnyStateTrigger(root, abilityState, BookAnimatorParams.Ability);
            AddExitTransition(abilityState, idle1State, 0.95f);

            return c;
        }

        private static AnimatorController BuildLakiController()
        {
            var c = CreateOrReplaceController(LakiBossControllerPath);
            AddParam(c, LakiAnimatorParams.PerformanceId, AnimatorControllerParameterType.Int);
            AddParam(c, LakiAnimatorParams.PerformancePrep, AnimatorControllerParameterType.Trigger);
            AddParam(c, LakiAnimatorParams.PerformanceLoop, AnimatorControllerParameterType.Bool);
            AddParam(c, LakiAnimatorParams.PerformanceFinish, AnimatorControllerParameterType.Trigger);
            AddParam(c, LakiAnimatorParams.Ability, AnimatorControllerParameterType.Trigger);
            AddParam(c, LakiAnimatorParams.Spotlight, AnimatorControllerParameterType.Trigger);

            var root = c.layers[0].stateMachine;
            var idle1 = LoadClip("Assets/Art/Animations/MadamLaki/Laki_Idle_1.anim");
            var p2Prep = LoadClip("Assets/Art/Animations/MadamLaki/Laki_Idle_2_Prep.anim");
            var p2Loop = LoadClip("Assets/Art/Animations/MadamLaki/Laki_Idle_2_Loop.anim");
            var p2Finish = LoadClip("Assets/Art/Animations/MadamLaki/Laki_Idle_2_Finish.anim");
            var p3Prep = LoadClip("Assets/Art/Animations/MadamLaki/Laki_Idle_3_Prep.anim");
            var p3Loop = LoadClip("Assets/Art/Animations/MadamLaki/Laki_Idle_3_Loop.anim");
            var p3Finish = LoadClip("Assets/Art/Animations/MadamLaki/Laki_Idle_3_Finish.anim");
            var ability = LoadClip("Assets/Art/Animations/MadamLaki/Laki_Ability.anim");

            var idle1State = AddMotionState(root, "Idle_1", idle1, new Vector3(300, 0, 0), LakiAnimatorParams.TagIdle);
            root.defaultState = idle1State;

            var perf2 = AddPerformanceSubMachine(root, "PerfIdle_2", p2Prep, p2Loop, p2Finish, new Vector3(550, 0, 0));
            var perf3 = AddPerformanceSubMachine(root, "PerfIdle_3", p3Prep, p3Loop, p3Finish, new Vector3(550, 120, 0));
            var abilityState = AddMotionState(root, "Ability", ability, new Vector3(550, 260, 0), LakiAnimatorParams.TagAbility);

            AddAnyStateTrigger(root, perf2, LakiAnimatorParams.PerformancePrep,
                (LakiAnimatorParams.PerformanceId, 2, AnimatorConditionMode.Equals));
            AddAnyStateTrigger(root, perf3, LakiAnimatorParams.PerformancePrep,
                (LakiAnimatorParams.PerformanceId, 3, AnimatorConditionMode.Equals));
            AddAnyStateTrigger(root, abilityState, LakiAnimatorParams.Ability);

            // After Ability, return to base idle; runtime re-rolls performance via PerformancePrep.
            AddExitTransition(abilityState, idle1State, 0.92f);

            return c;
        }

        private static AnimatorStateMachine AddPerformanceSubMachine(
            AnimatorStateMachine parent,
            string name,
            AnimationClip prep,
            AnimationClip loop,
            AnimationClip finish,
            Vector3 position)
        {
            var sm = parent.AddStateMachine(name, position);
            var prepState = AddMotionState(sm, "Prep", prep, new Vector3(0, 0, 0), LakiAnimatorParams.TagPerformancePrep);
            var loopState = AddMotionState(sm, "Loop", loop, new Vector3(250, 0, 0), LakiAnimatorParams.TagPerformanceLoop);
            var finishState = AddMotionState(sm, "Finish", finish, new Vector3(500, 0, 0), "");
            sm.defaultState = prepState;

            AddTransition(prepState, loopState, AnimatorConditionMode.If, LakiAnimatorParams.PerformanceLoop);
            AddTransition(loopState, finishState, AnimatorConditionMode.If, LakiAnimatorParams.PerformanceFinish);
            AddExitTransition(finishState, null, 0.95f);

            return sm;
        }

        private static AnimatorStateMachine AddSlowConjuringSubMachine(
            AnimatorStateMachine parent,
            AnimationClip prep,
            AnimationClip loop,
            AnimationClip finish,
            Vector3 position)
        {
            var sm = parent.AddStateMachine("SlowConjuring", position);
            var prepState = AddMotionState(sm, "Prep", prep, new Vector3(0, 0, 0), "");
            var loopState = AddMotionState(sm, "Loop", loop, new Vector3(250, 0, 0), ErzahlerAnimatorParams.TagConjuringLoop);
            var finishState = AddMotionState(sm, "Finish", finish, new Vector3(500, 0, 0), "");
            sm.defaultState = prepState;

            AddTransition(prepState, loopState, AnimatorConditionMode.If, ErzahlerAnimatorParams.ConjuringLoop);
            AddTransition(prepState, finishState, AnimatorConditionMode.If, ErzahlerAnimatorParams.ConjuringFinish);
            AddTransition(loopState, finishState, AnimatorConditionMode.If, ErzahlerAnimatorParams.ConjuringFinish);
            AddExitByCondition(prepState, ErzahlerAnimatorParams.ConjuringCancel);
            AddExitByCondition(loopState, ErzahlerAnimatorParams.ConjuringCancel);
            AddExitTransition(finishState, null, 0.95f);

            return sm;
        }

        private static void AddPlayerParameters(AnimatorController c)
        {
            AddParam(c, ErzahlerAnimatorParams.Moving, AnimatorControllerParameterType.Bool);
            AddParam(c, ErzahlerAnimatorParams.Running, AnimatorControllerParameterType.Bool);
            AddParam(c, ErzahlerAnimatorParams.WalkVariant, AnimatorControllerParameterType.Int);
            AddParam(c, ErzahlerAnimatorParams.IdleVariant, AnimatorControllerParameterType.Int);
            AddParam(c, ErzahlerAnimatorParams.ConjuringFast, AnimatorControllerParameterType.Trigger);
            AddParam(c, ErzahlerAnimatorParams.ConjuringPrep, AnimatorControllerParameterType.Trigger);
            AddParam(c, ErzahlerAnimatorParams.ConjuringLoop, AnimatorControllerParameterType.Bool);
            AddParam(c, ErzahlerAnimatorParams.ConjuringFinish, AnimatorControllerParameterType.Trigger);
            AddParam(c, ErzahlerAnimatorParams.ConjuringCancel, AnimatorControllerParameterType.Trigger);
        }

        private static AnimatorController CreateOrReplaceController(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                AssetDatabase.DeleteAsset(path);

            EnsureDirectory(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        private static void EnsureDirectory(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                var parts = dir.Replace('\\', '/').Split('/');
                var current = parts[0];
                for (var i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }

        private static void SyncResourcesCopy(ErzahlerAnimatorControllersSO source)
        {
            EnsureDirectory(ControllersResourcesPath);
            var existing = AssetDatabase.LoadAssetAtPath<ErzahlerAnimatorControllersSO>(ControllersResourcesPath);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<ErzahlerAnimatorControllersSO>();
                AssetDatabase.CreateAsset(existing, ControllersResourcesPath);
            }

            existing.ErzahlerWithBook = source.ErzahlerWithBook;
            existing.ErzahlerSolo = source.ErzahlerSolo;
            existing.BookClone = source.BookClone;
            existing.LakiBoss = source.LakiBoss;
            EditorUtility.SetDirty(existing);
        }

        private static AnimationClip LoadClip(string path) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        private static void AddParam(AnimatorController c, string name, AnimatorControllerParameterType type)
        {
            if (HasParameter(c, name)) return;
            c.AddParameter(name, type);
        }

        private static bool HasParameter(AnimatorController c, string name)
        {
            foreach (var p in c.parameters)
                if (p.name == name) return true;
            return false;
        }

        private static AnimatorState AddMotionState(
            AnimatorStateMachine sm,
            string name,
            Motion motion,
            Vector3 position,
            string tag)
        {
            var state = sm.AddState(name, position);
            state.motion = motion;
            if (!string.IsNullOrEmpty(tag))
                state.tag = tag;
            return state;
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode mode,
            string param,
            float threshold = 0f)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(mode, threshold, param);
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode mode,
            string param,
            (string extraParam, float threshold, AnimatorConditionMode extraMode) extra)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(mode, 0f, param);
            t.AddCondition(extra.extraMode, extra.threshold, extra.extraParam);
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorStateMachine toSm,
            AnimatorConditionMode mode,
            string param,
            (string extraParam, float threshold, AnimatorConditionMode extraMode)? extra = null)
        {
            var t = from.AddTransition(toSm);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(mode, 0f, param);
            if (extra.HasValue)
                t.AddCondition(extra.Value.extraMode, extra.Value.threshold, extra.Value.extraParam);
        }

        private static void AddAnyStateTrigger(AnimatorStateMachine root, AnimatorState dst, string trigger)
        {
            var t = root.AddAnyStateTransition(dst);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void AddAnyStateTrigger(
            AnimatorStateMachine root,
            AnimatorStateMachine dstSm,
            string trigger)
        {
            var t = root.AddAnyStateTransition(dstSm);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void AddAnyStateTrigger(
            AnimatorStateMachine root,
            AnimatorStateMachine dstSm,
            string trigger,
            (string param, float threshold, AnimatorConditionMode mode) extra)
        {
            var t = root.AddAnyStateTransition(dstSm);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            t.AddCondition(extra.mode, extra.threshold, extra.param);
        }

        private static void AddExitTransition(AnimatorState from, AnimatorState fallback, float exitTime)
        {
            if (fallback != null)
            {
                var t = from.AddTransition(fallback);
                t.hasExitTime = true;
                t.exitTime = exitTime;
                t.duration = 0.15f;
            }
            else
            {
                var t = from.AddExitTransition();
                t.hasExitTime = true;
                t.exitTime = exitTime;
                t.duration = 0.15f;
            }
        }

        private static void AddExitByCondition(AnimatorState from, string triggerParam)
        {
            var t = from.AddExitTransition();
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.AddCondition(AnimatorConditionMode.If, 0f, triggerParam);
        }
    }
}
#endif
