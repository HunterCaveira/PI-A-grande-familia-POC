using UnityEngine;

    // Define a categoria do dano para aplicar efeitos visuais ou resistências
    public enum TipoDano
    {

        Fisico,
        Fogo,
        Veneno,
        Eletrico,
        Cura
       

    }

    // Uma struct agrupa vários dados em uma única variável temporária de forma rápida e leve
    public struct InformacaoDano
    {

        public float quantidade;
        public TipoDano tipo;
        public GameObject atacante; // Quem causou o dano (identificado pela Tag ou Componente)
        public string nomeHabilidade;




        public InformacaoDano(float quantidade, TipoDano tipo, GameObject atacante, string nomeHabilidade = "Ataque Basico")
        {
            this.quantidade = quantidade;
            this.tipo = tipo;
            this.atacante = atacante;
            this.nomeHabilidade = nomeHabilidade;

        }
    }



