using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.Services.AudioService {
    [Serializable]
    public struct SfxClipEntry {
        public string Id;
        public AudioClip Clip;
    }

    [CreateAssetMenu(fileName = "GameplaySfxClips", menuName = "Scriptable Objects/Audio/Sfx Clips")]
    public class SfxClipsScriptableObject : ScriptableObject {
        [SerializeField] private List<SfxClipEntry> _entries = new();

        [NonSerialized] private Dictionary<string, AudioClip> _cache;

        public bool TryGetClip(string id, out AudioClip clip) {
            if (_cache == null)
                BuildCache();
            if (string.IsNullOrEmpty(id)) {
                clip = null;
                return false;
            }
            return _cache.TryGetValue(id, out clip);
        }

        private void BuildCache() {
            _cache = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            if (_entries == null) return;
            for (int i = 0; i < _entries.Count; i++) {
                var e = _entries[i];
                if (e.Clip == null || string.IsNullOrWhiteSpace(e.Id)) continue;
                _cache[e.Id.Trim()] = e.Clip;
            }
        }

        private void OnValidate() {
            _cache = null;
        }
    }

    public static class SfxIds {
        public const string UI_Clique = "UI_Clique";
        public const string UI_Clique2 = "UI_Clique2";
        public const string UI_Tela_Vitoria = "UI_Tela_Vitoria";
        public const string UI_Tela_Derrota = "UI_Tela_Derrota";
        public const string Portal = "Portal";
        public const string Novo_Turno = "Novo_Turno";
        public const string NPC_Falando = "NPC_Falando";
        public const string Dados = "Dados";

        public const string Erza_Atingida = "Erza_Atingida";
        public const string Erza_Cast = "Erza_Cast";
        public const string Erza_Morte = "Erza_Morte";
        public const string Erza_Passos = "Erza_Passos";
        public const string Ezra_Clone = "Ezra_Clone";
        public const string Ezra_Trocar_personagem = "Ezra_Trocar_personagem";

        public const string Hocari_Ataque_Cortes = "Hocari_Ataque_Cortes";
        public const string Hocari_Ataque_Laminas = "Hocari_Ataque_Laminas";
        public const string Hocari_Ataque_Mecanico = "Hocari_Ataque_Mecanico";
        public const string Hocari_Atingida = "Hocari_Atingida";
        public const string Hocari_Crescer = "Hocari_Crescer";
        public const string Hocari_Explosao = "Hocari_Explosao";
        public const string Hocari_Lamina = "Hocari_Lamina";
        public const string Hocari_Morte = "Hocari_Morte";
        public const string Hocari_Movimento_Cortes = "Hocari_Movimento_Cortes";
        public const string Hocari_Movimento_Laminas = "Hocari_Movimento_Laminas";
        public const string Hocari_Movimento_Mecanico = "Hocari_Movimento_Mecanico";
        public const string Hocari_Orbe = "Hocari_Orbe";
        public const string Hocari_Spawnar = "Hocari_Spawnar";

        public const string Laki_Atingida = "Laki_Atingida";
        public const string Laki_Cantar_1 = "Laki_Cantar_1";
        public const string Laki_Cantar_2 = "Laki_Cantar_2";
        public const string Laki_Cantar_3 = "Laki_Cantar_3";
        public const string Laki_Ganhando = "Laki_Ganhando";
        public const string Laki_Morrer = "Laki_Morrer";
        public const string Laki_Perdendo = "Laki_Perdendo";
        public const string Laki_Reclamando = "Laki_Reclamando";
        public const string Laki_Risada_1 = "Laki_Risada_1";
        public const string Laki_Risada_2 = "Laki_Risada_2";
        public const string Laki_Turno = "Laki_Turno";

        public const string Livro_Atingido = "Livro_Atingido";
        public const string Livro_Cast = "Livro_Cast";
        public const string Livro_Movimento = "Livro_Movimento";
        public const string Livro_Pagina = "Livro_Pagina";
        public const string Livro_Paginas = "Livro_Paginas";
    }
}
