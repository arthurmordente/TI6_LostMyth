using UnityEngine;
using Logic.Scripts.Services.CommandFactory;
using Zenject;
using Logic.Scripts.Turns;

namespace Logic.Scripts.GameDomain.Commands
{
    public struct SpawnOrbData
    {
        public ArenaPosReference Arena;
        public Vector3 Origin;
        public GameObject Prefab;
        public Logic.Scripts.GameDomain.MVC.Environment.Orb.OrbRegistry Registry;
        public float MoveStep;
        public float GrowStep;
        public float InitialRadius;
        public float MaxRadius;
        public int BaseDamage;
        public int InitialHp;
        /// <summary>Optional: CombatAttackVisualCatalog telegraph prefab for the orb AoE ring (OrbView). If null, disc is procedural.</summary>
        public GameObject OrbAreaVisualPrefab;
    }

    public class SpawnOrbCommand : BaseCommand, ICommandVoid
    {
        private SpawnOrbData _data;
        private DiContainer _container;
        private Logic.Scripts.GameDomain.MVC.Environment.Orb.OrbRegistry _registry;

        public SpawnOrbCommand SetData(SpawnOrbData data)
        {
            _data = data;
            return this;
        }

        public override void ResolveDependencies()
        {
            _container = _diContainer;
        }

        public void Execute()
        {
            if (_data.Prefab == null) { Debug.LogWarning("SpawnOrbCommand: Prefab null"); return; }
			Vector3 spawnPos = _data.Origin; spawnPos.y = 1f;
			Debug.Log($"[SpawnOrb] Instantiating orb prefab at {spawnPos}");
			GameObject orbGo = Object.Instantiate(_data.Prefab, spawnPos, Quaternion.identity);
            if (!orbGo.activeSelf) orbGo.SetActive(true);
            var controller = orbGo.GetComponent<Logic.Scripts.GameDomain.MVC.Environment.Orb.OrbController>();
            if (controller != null)
            {
                controller.Initialize(_data.Arena, _registry, _data.MoveStep, _data.GrowStep, _data.InitialRadius, _data.MaxRadius, _data.BaseDamage, _data.InitialHp, _data.OrbAreaVisualPrefab);
                // Registrar no registro único publicado via serviço estático.
                // Padrão oficial: qualquer ator criado em runtime deve ser adicionado via EnvironmentActorsRegistryService.Instance.
                var envReg = EnvironmentActorsRegistryService.Instance;
                if (envReg == null)
                {
                    Debug.LogWarning("[SpawnOrb] EnvironmentActorsRegistryService.Instance is null. Orb will NOT act on Environment turn.");
                    return;
                }
                envReg.Add(controller);
                Debug.Log("[SpawnOrb] Registered OrbController in IEnvironmentActorsRegistry");
            }
        }
    }
}


