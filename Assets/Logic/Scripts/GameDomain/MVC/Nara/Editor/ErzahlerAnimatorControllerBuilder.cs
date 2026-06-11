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
        public const string ErzahlerBookControllerPath = AnimationControllerPaths.ErzahlerBook;
        public const string ErzahlerSoloControllerPath = AnimationControllerPaths.ErzahlerSolo;
        public const string BookCloneControllerPath = AnimationControllerPaths.BookClone;
        public const string LakiBossControllerPath = AnimationControllerPaths.LakiBoss;
        private const string ControllersSoPath = "Assets/Logic/Scripts/GameDomain/MVC/Nara/Animation/ErzahlerAnimatorControllers.asset";
        private const string ControllersResourcesPath = "Assets/Resources/ErzahlerAnimatorControllers.asset";

        public static void BuildAll()
        {
            BuildErzahlerStateMachines();
            BuildLakiBossOnly();
        }

        public static void BuildErzahlerStateMachines()
        {
            BuildErzahlerWithBookOnly();
            BuildErzahlerSoloOnly();
            BuildBookCloneOnly();
            Debug.Log("[ErzahlerAnimatorControllerBuilder] Built Erzahler controllers (core + optional states when exported clips exist).");
        }

        public static void BuildErzahlerWithBookOnly()
        {
            var controller = BuildErzahlerWithBookController();
            AssignControllerToSo(so => so.ErzahlerWithBook = controller);
            FinishBuild($"ERZ_ErzahlerBook_FINAL → {ErzahlerBookControllerPath}");
        }

        public static void BuildErzahlerSoloOnly()
        {
            var controller = BuildErzahlerSoloController();
            AssignControllerToSo(so => so.ErzahlerSolo = controller);
            FinishBuild($"ERZ_Erzahler_FINAL → {ErzahlerSoloControllerPath}");
        }

        public static void BuildBookCloneOnly()
        {
            var controller = BuildBookCloneController();
            AssignControllerToSo(so => so.BookClone = controller);
            FinishBuild($"ERZ_Book_FINAL → {BookCloneControllerPath}");
        }

        public static void BuildLakiBossOnly()
        {
            FixLakiAbilityClipLoop();
            var controller = BuildLakiController();
            AssignControllerToSo(so => so.LakiBoss = controller);
            FinishBuild($"LKI_Animator_FINAL → {LakiBossControllerPath}");
        }

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
            var clip = LakiAnimationClipExporter.LoadExported("Laki_Ability");
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
            var walk1 = ErzahlerErzaClipExporter.LoadExported("Erzahler+Book_Walk_1");
            var walk2 = ErzahlerErzaClipExporter.LoadExported("Erzahler+Book_Walk_2");
            var fast = ErzahlerErzaClipExporter.LoadExported("Erzahler+Book_FastConjuringWithTwoHands");
            var slowPrep = ErzahlerErzaClipExporter.LoadExported("Erzahler+Book_SlowConjuring_Prep");
            var slowLoop = ErzahlerErzaClipExporter.LoadExported("Erzahler+Book_SlowConjuring_Loop");
            var slowFinish = ErzahlerErzaClipExporter.LoadExported("Erzahler+Book_SlowConjuring_Finish");

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
            AddPlayerReactionStates(c, root, idle, includeDivideClips: true);

            return c;
        }

        private static AnimatorController BuildErzahlerSoloController()
        {
            var c = CreateOrReplaceController(ErzahlerSoloControllerPath);
            AddPlayerParameters(c);

            var root = c.layers[0].stateMachine;
            var idle1 = ErzahlerErzaClipExporter.LoadExported("Erzahler_Idle_1");
            var idle2 = ErzahlerErzaClipExporter.LoadExported("Erzahler_Idle_2");
            var walk = ErzahlerErzaClipExporter.LoadExported("Erzahler_Walk");
            var jog = ErzahlerErzaClipExporter.LoadExported("Erzahler_Jog");
            var fast = ErzahlerErzaClipExporter.LoadExported("Erzahler_FastConjuring");
            var slowPrep = ErzahlerErzaClipExporter.LoadExported("Erzahler_SlowConjuring_Prep");
            var slowLoop = ErzahlerErzaClipExporter.LoadExported("Erzahler_SlowConjuring_Loop");
            var slowFinish = ErzahlerErzaClipExporter.LoadExported("Erzahler_SlowConjuring_Finish");

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
            AddPlayerReactionStates(c, root, idle1State, includeDivideClips: false);

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
            var idle1 = ErzahlerErzaClipExporter.LoadExported("Book_Idle_1");
            var idle2 = ErzahlerErzaClipExporter.LoadExported("Book_Idle_2");
            var idle3 = ErzahlerErzaClipExporter.LoadExported("Book_Idle_3");
            var walk1 = ErzahlerErzaClipExporter.LoadExported("Book_Walk_1");
            var walk2 = ErzahlerErzaClipExporter.LoadExported("Book_Walk_2");
            var ability = ErzahlerErzaClipExporter.LoadExported("Book_Ability");

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
            var idle1 = LakiAnimationClipExporter.LoadExported("Laki_Idle_1");
            var p2Prep = LakiAnimationClipExporter.LoadExported("Laki_Idle_2_Prep");
            var p2Loop = LakiAnimationClipExporter.LoadExported("Laki_Idle_2_Loop");
            var p2Finish = LakiAnimationClipExporter.LoadExported("Laki_Idle_2_Finish");
            var p3Prep = LakiAnimationClipExporter.LoadExported("Laki_Idle_3_Prep");
            var p3Loop = LakiAnimationClipExporter.LoadExported("Laki_Idle_3_Loop");
            var p3Finish = LakiAnimationClipExporter.LoadExported("Laki_Idle_3_Finish");
            var ability = LakiAnimationClipExporter.LoadExported("Laki_Ability");

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

            AddLakiReactionStates(c, root, idle1State);
            AddLakiThrowDieSubMachine(c, root, new Vector3(550, 400, 0));

            return c;
        }

        private static void AddPlayerReactionStates(AnimatorController controller, AnimatorStateMachine root, AnimatorState idleFallback, bool includeDivideClips)
        {
            var death = ErzahlerErzaClipExporter.LoadExported("Erzahler_Death");
            var hit = ErzahlerErzaClipExporter.LoadExported("Erzahler_Hit");
            var betWon = ErzahlerErzaClipExporter.LoadExported("Erzahler_BetWon");
            var betLost = ErzahlerErzaClipExporter.LoadExported("Erzahler_BetLost");
            var conjuringFail = ErzahlerErzaClipExporter.LoadExported("Erzahler_Conjuring_Fail");

            if (death != null)
            {
                AddParam(controller, ErzahlerAnimatorParams.Dead, AnimatorControllerParameterType.Trigger);
                var deathState = AddMotionState(root, "Death", death, new Vector3(850, 0, 0), ErzahlerAnimatorParams.TagDeath);
                AddAnyStateTrigger(root, deathState, ErzahlerAnimatorParams.Dead);
            }

            if (hit != null)
            {
                AddParam(controller, ErzahlerAnimatorParams.Hit, AnimatorControllerParameterType.Trigger);
                var hitState = AddMotionState(root, "Hit", hit, new Vector3(850, 90, 0), "");
                AddAnyStateTrigger(root, hitState, ErzahlerAnimatorParams.Hit);
                AddExitTransition(hitState, idleFallback, 0.92f);
            }

            if (betWon != null)
            {
                AddParam(controller, ErzahlerAnimatorParams.BetWon, AnimatorControllerParameterType.Trigger);
                var wonState = AddMotionState(root, "BetWon", betWon, new Vector3(850, 180, 0), "");
                AddAnyStateTrigger(root, wonState, ErzahlerAnimatorParams.BetWon);
                AddExitTransition(wonState, idleFallback, 0.92f);
            }

            if (betLost != null)
            {
                AddParam(controller, ErzahlerAnimatorParams.BetLost, AnimatorControllerParameterType.Trigger);
                var lostState = AddMotionState(root, "BetLost", betLost, new Vector3(850, 270, 0), "");
                AddAnyStateTrigger(root, lostState, ErzahlerAnimatorParams.BetLost);
                AddExitTransition(lostState, idleFallback, 0.92f);
            }

            if (conjuringFail != null)
            {
                AddParam(controller, ErzahlerAnimatorParams.ConjuringFail, AnimatorControllerParameterType.Trigger);
                var failState = AddMotionState(root, "ConjuringFail", conjuringFail, new Vector3(850, 360, 0), "");
                AddAnyStateTrigger(root, failState, ErzahlerAnimatorParams.ConjuringFail);
                AddExitTransition(failState, idleFallback, 0.92f);
            }

            if (!includeDivideClips) return;

            var deploy = ErzahlerErzaClipExporter.LoadExported("Book_CreateClone");
            var recall = ErzahlerErzaClipExporter.LoadExported("Book_ReturnClone");
            if (deploy != null)
            {
                AddParam(controller, ErzahlerAnimatorParams.DivideDeploy, AnimatorControllerParameterType.Trigger);
                var deployState = AddMotionState(root, "DivideDeploy", deploy, new Vector3(850, 450, 0), "");
                AddAnyStateTrigger(root, deployState, ErzahlerAnimatorParams.DivideDeploy);
                AddExitTransition(deployState, idleFallback, 0.92f);
            }

            if (recall != null)
            {
                AddParam(controller, ErzahlerAnimatorParams.DivideRecall, AnimatorControllerParameterType.Trigger);
                var recallState = AddMotionState(root, "DivideRecall", recall, new Vector3(850, 540, 0), "");
                AddAnyStateTrigger(root, recallState, ErzahlerAnimatorParams.DivideRecall);
                AddExitTransition(recallState, idleFallback, 0.92f);
            }
        }

        private static void AddLakiReactionStates(AnimatorController controller, AnimatorStateMachine root, AnimatorState idleFallback)
        {
            var hit = LakiAnimationClipExporter.LoadExported("Laki_Hit_LoseBet");
            var betWon = LakiAnimationClipExporter.LoadExported("Laki_BetWon");
            var death = LakiAnimationClipExporter.LoadExported("Laki_Death");

            if (hit != null)
            {
                AddParam(controller, LakiAnimatorParams.HitReaction, AnimatorControllerParameterType.Trigger);
                var hitState = AddMotionState(root, "Hit_LoseBet", hit, new Vector3(850, 0, 0), "");
                AddAnyStateTrigger(root, hitState, LakiAnimatorParams.HitReaction);
                AddExitTransition(hitState, idleFallback, 0.92f);
            }

            if (betWon != null)
            {
                AddParam(controller, LakiAnimatorParams.BetWon, AnimatorControllerParameterType.Trigger);
                var wonState = AddMotionState(root, "BetWon", betWon, new Vector3(850, 90, 0), "");
                AddAnyStateTrigger(root, wonState, LakiAnimatorParams.BetWon);
                AddExitTransition(wonState, idleFallback, 0.92f);
            }

            if (hit != null)
            {
                AddParam(controller, LakiAnimatorParams.BetLost, AnimatorControllerParameterType.Trigger);
                var lostState = AddMotionState(root, "BetLost", hit, new Vector3(850, 135, 0), "");
                AddAnyStateTrigger(root, lostState, LakiAnimatorParams.BetLost);
                AddExitTransition(lostState, idleFallback, 0.92f);
            }

            if (death != null)
            {
                AddParam(controller, LakiAnimatorParams.Death, AnimatorControllerParameterType.Trigger);
                var deathState = AddMotionState(root, "Death", death, new Vector3(850, 180, 0), LakiAnimatorParams.TagDeath);
                AddAnyStateTrigger(root, deathState, LakiAnimatorParams.Death);
            }
        }

        private static void AddLakiThrowDieSubMachine(AnimatorController controller, AnimatorStateMachine root, Vector3 position)
        {
            var prep = LakiAnimationClipExporter.LoadExported("Laki_ThrowDie_Prep");
            var loop = LakiAnimationClipExporter.LoadExported("Laki_ThrowDie_Loop");
            var finish = LakiAnimationClipExporter.LoadExported("Laki_ThrowDie_Finish");
            if (prep == null || loop == null || finish == null) return;

            AddParam(controller, LakiAnimatorParams.ThrowDiePrep, AnimatorControllerParameterType.Trigger);
            AddParam(controller, LakiAnimatorParams.ThrowDieLoop, AnimatorControllerParameterType.Bool);
            AddParam(controller, LakiAnimatorParams.ThrowDieFinish, AnimatorControllerParameterType.Trigger);

            var sm = root.AddStateMachine("ThrowDie", position);
            var prepState = AddMotionState(sm, "Prep", prep, new Vector3(0, 0, 0), "");
            var loopState = AddMotionState(sm, "Loop", loop, new Vector3(250, 0, 0), LakiAnimatorParams.TagThrowDieLoop);
            var finishState = AddMotionState(sm, "Finish", finish, new Vector3(500, 0, 0), "");
            sm.defaultState = prepState;

            AddTransition(prepState, loopState, AnimatorConditionMode.If, LakiAnimatorParams.ThrowDieLoop);
            AddTransition(loopState, finishState, AnimatorConditionMode.If, LakiAnimatorParams.ThrowDieFinish);
            AddExitTransition(finishState, null, 0.95f);

            AddAnyStateTrigger(root, sm, LakiAnimatorParams.ThrowDiePrep);
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
