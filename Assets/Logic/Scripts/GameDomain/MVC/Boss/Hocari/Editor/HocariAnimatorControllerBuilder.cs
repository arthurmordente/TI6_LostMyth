#if UNITY_EDITOR
using System.IO;
using Logic.Scripts.GameDomain.MVC.Boss.Hocari.Animation;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Hocari.Editor
{
    public static class HocariAnimatorControllerBuilder
    {
        public const string UnifiedControllerPath = AnimationControllerPaths.HocariBoss;
        private const string HokariBossPrefabPath = "Assets/GameDesign/Prefabs/Bosses/Hocari/HokariBoss.prefab";

        public static void BuildUnifiedOnly()
        {
            var controller = BuildUnifiedController();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HocariAnimatorControllerBuilder] Built HOC_Hocari_FINAL → {UnifiedControllerPath}");
        }

        public static void AssignToHokariPrefab()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(UnifiedControllerPath);
            if (controller == null)
            {
                Debug.LogError("[HocariAnimatorControllerBuilder] Run Export + Controller first.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HokariBossPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[HocariAnimatorControllerBuilder] Prefab not found: {HokariBossPrefabPath}");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(HokariBossPrefabPath);
            var animator = root.GetComponent<Animator>();
            if (animator == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("[HocariAnimatorControllerBuilder] HokariBoss root has no Animator (expected on HokariBoss GameObject).");
                return;
            }

            animator.runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(root, HokariBossPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[HocariAnimatorControllerBuilder] Assigned {UnifiedControllerPath} to HokariBoss root Animator.");
        }

        public static AnimatorController BuildUnifiedController()
        {
            var c = CreateOrReplaceController(UnifiedControllerPath);
            AddHocariParameters(c);
            var root = c.layers[0].stateMachine;

            var p1Idle = AddMotionState(root, "P1_CombatIdle",
                Load("Phase1", "Hocari_CombatIdle_2"), new Vector3(300, 0, 0), HocariAnimatorParams.TagIdle);
            var p2Idle = AddMotionState(root, "P2_CombatIdle",
                Load("Phase2", "Hocari_Phase2_CombatIdle"), new Vector3(300, 120, 0), HocariAnimatorParams.TagIdle);
            root.defaultState = p1Idle;

            var movementSm = AddMovementSubMachine(root, new Vector3(300, 260, 0));
            var phaseTxSm = AddPhaseTransitionSubMachine(root, new Vector3(300, 400, 0));
            var attacksSm = AddAttacksSubMachine(root, p1Idle, p2Idle, new Vector3(300, 540, 0));

            AddIdleTransitions(root, p1Idle, p2Idle, movementSm, attacksSm);
            AddPhaseSyncTransitions(p1Idle, p2Idle);

            AddHitAndDeathStates(root, p1Idle, p2Idle, attacksSm);

            return c;
        }

        private static void AddHocariParameters(AnimatorController c)
        {
            AddParam(c, HocariAnimatorParams.AttackId, AnimatorControllerParameterType.Int);
            AddParam(c, HocariAnimatorParams.AttackPrep, AnimatorControllerParameterType.Trigger);
            AddParam(c, HocariAnimatorParams.AttackLoop, AnimatorControllerParameterType.Bool);
            AddParam(c, HocariAnimatorParams.AttackFinish, AnimatorControllerParameterType.Trigger);
            AddParam(c, HocariAnimatorParams.Moving, AnimatorControllerParameterType.Bool);
            AddParam(c, HocariAnimatorParams.MovePrep, AnimatorControllerParameterType.Trigger);
            AddParam(c, HocariAnimatorParams.MoveFinish, AnimatorControllerParameterType.Trigger);
            AddParam(c, HocariAnimatorParams.Idle, AnimatorControllerParameterType.Trigger);
            AddParam(c, HocariAnimatorParams.BossPhase, AnimatorControllerParameterType.Int);
            AddParam(c, HocariAnimatorParams.PhaseTransition, AnimatorControllerParameterType.Trigger);
            AddParam(c, HocariAnimatorParams.Hit, AnimatorControllerParameterType.Trigger);
            AddParam(c, HocariAnimatorParams.Death, AnimatorControllerParameterType.Trigger);
        }

        private static void AddIdleTransitions(
            AnimatorStateMachine root,
            AnimatorState p1Idle,
            AnimatorState p2Idle,
            AnimatorStateMachine movementSm,
            AnimatorStateMachine attacksSm)
        {
            AddTransition(p1Idle, movementSm, AnimatorConditionMode.If, HocariAnimatorParams.MovePrep);
            AddTransition(p2Idle, movementSm, AnimatorConditionMode.If, HocariAnimatorParams.MovePrep);
            AddTransition(p1Idle, attacksSm, AnimatorConditionMode.If, HocariAnimatorParams.AttackPrep);
            AddTransition(p2Idle, attacksSm, AnimatorConditionMode.If, HocariAnimatorParams.AttackPrep);

            AddAnyStateTrigger(root, movementSm, HocariAnimatorParams.MovePrep);
            AddAnyStateTrigger(root, attacksSm, HocariAnimatorParams.AttackPrep);
        }

        private static void AddPhaseSyncTransitions(AnimatorState p1Idle, AnimatorState p2Idle)
        {
            var t = p1Idle.AddTransition(p2Idle);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Equals, HocariAnimatorParams.PhaseTwo, HocariAnimatorParams.BossPhase);

            var back = p2Idle.AddTransition(p1Idle);
            back.hasExitTime = false;
            back.duration = 0.15f;
            back.AddCondition(AnimatorConditionMode.Equals, HocariAnimatorParams.PhaseOne, HocariAnimatorParams.BossPhase);
        }

        private static AnimatorStateMachine AddMovementSubMachine(AnimatorStateMachine parent, Vector3 position)
        {
            var sm = parent.AddStateMachine("HOC_Movement", position);
            var prep = AddMotionState(sm, "Prep", Load("Shared", "Hocari_Movement_Prep"),
                new Vector3(0, 0, 0), HocariAnimatorParams.TagMovePrep);
            var loop = AddMotionState(sm, "Loop", Load("Shared", "Hocari_Movement_Loop"),
                new Vector3(250, 0, 0), HocariAnimatorParams.TagMoveLoop);
            var finish = AddMotionState(sm, "Finish", Load("Shared", "Hocari_Movement_Finish"),
                new Vector3(500, 0, 0), "");
            sm.defaultState = prep;

            AddTransition(prep, loop, AnimatorConditionMode.If, HocariAnimatorParams.Moving);
            AddTransition(loop, finish, AnimatorConditionMode.IfNot, HocariAnimatorParams.Moving);
            AddExitTransition(finish, null, 0.95f);
            return sm;
        }

        private static AnimatorStateMachine AddPhaseTransitionSubMachine(AnimatorStateMachine parent, Vector3 position)
        {
            var sm = parent.AddStateMachine("HOC_PhaseTransition", position);
            var prep = AddMotionState(sm, "Prep", Load("Shared", "Hocari_PhaseTransition_Prep"),
                new Vector3(0, 0, 0), "");
            var loop = AddMotionState(sm, "Loop", Load("Shared", "Hocari_PhaseTransition_Loop"),
                new Vector3(250, 0, 0), "");
            var finish = AddMotionState(sm, "Finish", Load("Shared", "Hocari_PhaseTransition_Finish"),
                new Vector3(500, 0, 0), "");
            var finish2 = AddMotionState(sm, "Finish_2", Load("Shared", "Hocari_PhaseTransition_Finish_2"),
                new Vector3(750, 0, 0), "");
            sm.defaultState = prep;

            AddExitTransition(prep, loop, 0.85f);
            AddExitTransition(loop, finish, 0.85f);
            AddExitTransition(finish, finish2, 0.85f);
            AddExitTransition(finish2, null, 0.95f);
            finish2.AddStateMachineBehaviour<HocariPhaseTransitionBehaviour>();

            AddAnyStateTrigger(parent, sm, HocariAnimatorParams.PhaseTransition);
            return sm;
        }

        private static AnimatorStateMachine AddAttacksSubMachine(
            AnimatorStateMachine parent,
            AnimatorState p1Idle,
            AnimatorState p2Idle,
            Vector3 position)
        {
            var sm = parent.AddStateMachine("HOC_Attacks", position);
            var chooser = sm.AddState("HOC_AttackChooser", new Vector3(0, 0, 0));
            sm.defaultState = chooser;

            AddDualPhaseAttack(sm, chooser, p1Idle, p2Idle, "Protean", HocariAnimatorParams.AttackProtean,
                "Hocari_Attack_Protean_2", "Hocari_Phase2_Attack_Protean", new Vector3(300, -200, 0));
            AddDualPhaseAttack(sm, chooser, p1Idle, p2Idle, "Circle", HocariAnimatorParams.AttackCircle,
                "Hocari_Attack_Circle", "Hocari_Phase2_Attack_Circle", new Vector3(300, -100, 0));
            AddDualPhaseAttack(sm, chooser, p1Idle, p2Idle, "Swords", HocariAnimatorParams.AttackSwordLines,
                "Hocari_Attack_SwordLines", "Hocari_Phase2_Attack_SwordLines", new Vector3(300, 0, 0));
            AddDualPhaseAttack(sm, chooser, p1Idle, p2Idle, "WingsLeft", HocariAnimatorParams.AttackWingLeft,
                "Hocari_Attack_WingSlash_Left", "Hocari_Phase2_Attack_WingSlash_Left", new Vector3(300, 100, 0));
            AddDualPhaseAttack(sm, chooser, p1Idle, p2Idle, "WingsRight", HocariAnimatorParams.AttackWingRight,
                "Hocari_Attack_WingSlash_Right", "Hocari_Phase2_Attack_WingSlash_Right", new Vector3(300, 200, 0));

            return sm;
        }

        private static void AddDualPhaseAttack(
            AnimatorStateMachine attacksSm,
            AnimatorState chooser,
            AnimatorState p1Idle,
            AnimatorState p2Idle,
            string label,
            int attackId,
            string p1Prefix,
            string p2Prefix,
            Vector3 position)
        {
            var p1Sm = AddAttackPipeline(attacksSm, $"P1_{label}", "Phase1", p1Prefix, position);
            var p2Sm = AddAttackPipeline(attacksSm, $"P2_{label}", "Phase2", p2Prefix, position + new Vector3(0, 80, 0));

            AddChooserTransition(chooser, p1Sm, attackId, HocariAnimatorParams.PhaseOne);
            AddChooserTransition(chooser, p2Sm, attackId, HocariAnimatorParams.PhaseTwo);
            WireAttackFinishToIdle(p1Sm, p1Idle);
            WireAttackFinishToIdle(p2Sm, p2Idle);
        }

        private static AnimatorStateMachine AddAttackPipeline(
            AnimatorStateMachine parent,
            string smName,
            string clipFolder,
            string clipPrefix,
            Vector3 position)
        {
            var sm = parent.AddStateMachine(smName, position);
            var prep = AddMotionState(sm, "Prep", Load(clipFolder, $"{clipPrefix}_Prep"),
                new Vector3(0, 0, 0), HocariAnimatorParams.TagAttackPrep);
            var loop = AddMotionState(sm, "Loop", Load(clipFolder, $"{clipPrefix}_Loop"),
                new Vector3(250, 0, 0), HocariAnimatorParams.TagAttackLoop);
            var finish = AddMotionState(sm, "Finish", Load(clipFolder, $"{clipPrefix}_Finish"),
                new Vector3(500, 0, 0), "");
            sm.defaultState = prep;

            AddTransition(prep, loop, AnimatorConditionMode.If, HocariAnimatorParams.AttackLoop);
            AddTransition(loop, finish, AnimatorConditionMode.If, HocariAnimatorParams.AttackFinish);
            return sm;
        }

        private static void AddChooserTransition(AnimatorState chooser, AnimatorStateMachine dst, int attackId, int phase)
        {
            var t = chooser.AddTransition(dst);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.AddCondition(AnimatorConditionMode.Equals, attackId, HocariAnimatorParams.AttackId);
            t.AddCondition(AnimatorConditionMode.Equals, phase, HocariAnimatorParams.BossPhase);
        }

        private static void WireAttackFinishToIdle(AnimatorStateMachine attackSm, AnimatorState idle)
        {
            foreach (var child in attackSm.states)
            {
                if (child.state.name != "Finish") continue;
                var t = child.state.AddTransition(idle);
                t.hasExitTime = true;
                t.exitTime = 0.95f;
                t.duration = 0.1f;
            }
        }

        private static void AddHitAndDeathStates(
            AnimatorStateMachine root,
            AnimatorState p1Idle,
            AnimatorState p2Idle,
            AnimatorStateMachine attacksSm)
        {
            var p1Hit = Load("Phase1", "Hocari_Hit");
            var p2Hit = Load("Phase2", "Hocari_Phase2_Hit");
            var death = Load("Phase2", "Hocari_Phase2_Death");

            if (p1Hit != null)
            {
                var hitState = AddMotionState(root, "P1_Hit", p1Hit, new Vector3(850, 0, 0), "");
                var t = root.AddAnyStateTransition(hitState);
                t.hasExitTime = false;
                t.duration = 0.1f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0f, HocariAnimatorParams.Hit);
                t.AddCondition(AnimatorConditionMode.Equals, HocariAnimatorParams.PhaseOne, HocariAnimatorParams.BossPhase);
                AddHitExitTransitions(hitState, p1Idle, attacksSm, HocariAnimatorParams.PhaseOne);
            }

            if (p2Hit != null)
            {
                var hitState = AddMotionState(root, "P2_Hit", p2Hit, new Vector3(850, 90, 0), "");
                var t = root.AddAnyStateTransition(hitState);
                t.hasExitTime = false;
                t.duration = 0.1f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0f, HocariAnimatorParams.Hit);
                t.AddCondition(AnimatorConditionMode.Equals, HocariAnimatorParams.PhaseTwo, HocariAnimatorParams.BossPhase);
                AddHitExitTransitions(hitState, p2Idle, attacksSm, HocariAnimatorParams.PhaseTwo);
            }

            if (death != null)
            {
                var deathState = AddMotionState(root, "Death", death, new Vector3(850, 180, 0), HocariAnimatorParams.TagDeath);
                AddAnyStateTrigger(root, deathState, HocariAnimatorParams.Death);
            }
        }

        private static AnimationClip Load(string folder, string clipName) =>
            HocariAnimationClipExporter.LoadClip(folder, clipName);

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
            if (string.IsNullOrEmpty(dir) || AssetDatabase.IsValidFolder(dir)) return;
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

        private static void AddParam(AnimatorController c, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in c.parameters)
                if (p.name == name) return;
            c.AddParameter(name, type);
        }

        private static AnimatorState AddMotionState(AnimatorStateMachine sm, string name, Motion motion, Vector3 pos, string tag)
        {
            var state = sm.AddState(name, pos);
            state.motion = motion;
            if (!string.IsNullOrEmpty(tag))
                state.tag = tag;
            return state;
        }

        private static void AddTransition(AnimatorState from, AnimatorStateMachine to, AnimatorConditionMode mode, string param)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(mode, 0f, param);
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string param, float threshold = 0f)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(mode, threshold, param);
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

        private static AnimatorStateTransition AddExitTransitionToSubMachine(
            AnimatorState from,
            AnimatorStateMachine destination,
            float exitTime,
            AnimatorConditionMode mode,
            float threshold,
            string param)
        {
            var t = from.AddTransition(destination);
            t.hasExitTime = true;
            t.exitTime = exitTime;
            t.duration = 0.15f;
            t.AddCondition(mode, threshold, param);
            return t;
        }

        private static void AddHitExitTransitions(
            AnimatorState hitState,
            AnimatorState idleState,
            AnimatorStateMachine attacksSm,
            int phase)
        {
            const float exitTime = 0.92f;

            var toAttacks = AddExitTransitionToSubMachine(
                hitState, attacksSm, exitTime,
                AnimatorConditionMode.If, 0f, HocariAnimatorParams.AttackLoop);
            toAttacks.AddCondition(AnimatorConditionMode.Equals, phase, HocariAnimatorParams.BossPhase);

            var toIdle = hitState.AddTransition(idleState);
            toIdle.hasExitTime = true;
            toIdle.exitTime = exitTime;
            toIdle.duration = 0.15f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, HocariAnimatorParams.AttackLoop);
            toIdle.AddCondition(AnimatorConditionMode.Equals, phase, HocariAnimatorParams.BossPhase);
        }

        private static void AddAnyStateTrigger(AnimatorStateMachine root, AnimatorStateMachine dstSm, string trigger)
        {
            var t = root.AddAnyStateTransition(dstSm);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void AddAnyStateTrigger(AnimatorStateMachine root, AnimatorState dst, string trigger)
        {
            var t = root.AddAnyStateTransition(dst);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }
    }
}
#endif
